using System;
using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class Trie<T> : IDisposable
    {
        private readonly TrieNode _root = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private bool _disposed = false;

        public void Insert(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _lock.EnterWriteLock();

            try
            {
                TrieNode node = _root;

                foreach (char c in key)
                {
                    if (!node.Children.TryGetValue(c, out TrieNode child))
                    {
                        child = new TrieNode();
                        node.Children[c] = child;
                    }

                    node = child;
                }

                node.Value = value;
                node.HasValue = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Remove(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            _lock.EnterWriteLock();

            try
            {
                return RemoveInternal(_root, key, 0, value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public List<T> FindByPrefix(string prefix)
        {
            List<T> results = new();

            if (string.IsNullOrEmpty(prefix))
                return results;

            _lock.EnterReadLock();

            try
            {
                TrieNode node = _root;

                foreach (char c in prefix)
                {
                    if (!node.Children.TryGetValue(c, out node))
                        return results;
                }

                CollectInternal(node, results);
            }
            finally
            {
                _lock.ExitReadLock();
            }

            return results;
        }

        public T FindExact(string key)
        {
            if (string.IsNullOrEmpty(key))
                return default;

            _lock.EnterReadLock();

            try
            {
                TrieNode node = _root;

                foreach (char c in key)
                {
                    if (!node.Children.TryGetValue(c, out node))
                        return default;
                }

                return node.HasValue ? node.Value : default;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private static bool RemoveInternal(TrieNode node, string key, int depth, T value)
        {
            if (depth == key.Length)
            {
                if (node.HasValue && EqualityComparer<T>.Default.Equals(node.Value, value))
                {
                    node.HasValue = false;
                    node.Value = default;
                    return node.Children.Count == 0;
                }

                return false;
            }

            char c = key[depth];

            if (node.Children.TryGetValue(c, out TrieNode child))
            {
                if (RemoveInternal(child, key, depth + 1, value))
                    node.Children.Remove(c);

                return !node.HasValue && node.Children.Count == 0;
            }

            return false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _lock?.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        private static void CollectInternal(TrieNode node, List<T> results)
        {
            if (node.HasValue)
                results.Add(node.Value);

            foreach (TrieNode child in node.Children.Values)
                CollectInternal(child, results);
        }

        private class TrieNode
        {
            public Dictionary<char, TrieNode> Children { get; } = new(CaseInsensitiveCharComparer.Instance);
            public T Value { get; set; }
            public bool HasValue { get; set; }

            private class CaseInsensitiveCharComparer : IEqualityComparer<char>
            {
                public static readonly CaseInsensitiveCharComparer Instance = new();

                public bool Equals(char x, char y)
                {
                    return char.ToLowerInvariant(x) == char.ToLowerInvariant(y);
                }

                public int GetHashCode(char obj)
                {
                    return char.ToLowerInvariant(obj).GetHashCode();
                }
            }
        }
    }
}
