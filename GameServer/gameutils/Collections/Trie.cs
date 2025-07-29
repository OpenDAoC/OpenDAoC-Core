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
                RemoveInternal(_root, key, 0, value, out bool wasFound);
                return wasFound;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _lock?.Dispose();
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        private static bool RemoveInternal(TrieNode node, string key, int depth, T value, out bool wasFound)
        {
            if (depth == key.Length)
            {
                if (node.HasValue && EqualityComparer<T>.Default.Equals(node.Value, value))
                {
                    node.HasValue = false;
                    node.Value = default;
                    wasFound = true;
                    return node.Children.Count == 0;
                }

                wasFound = false;
                return false;
            }

            char c = key[depth];

            if (node.Children.TryGetValue(c, out TrieNode child))
            {
                if (RemoveInternal(child, key, depth + 1, value, out wasFound))
                    node.Children.Remove(c);

                return !node.HasValue && node.Children.Count == 0;
            }

            wasFound = false;
            return false;
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
            public Dictionary<char, TrieNode> Children { get; } = new(new CaseInsensitiveCharComparer());
            public T Value { get; set; }
            public bool HasValue { get; set; }

            private class CaseInsensitiveCharComparer : IEqualityComparer<char>
            {
                private static readonly char[] _unicodeToLowerTable = new char[65536];

                static CaseInsensitiveCharComparer()
                {
                    for (int i = 0; i < 65536; i++)
                        _unicodeToLowerTable[i] = char.ToLowerInvariant((char) i);
                }

                public bool Equals(char x, char y)
                {
                    return _unicodeToLowerTable[x] == _unicodeToLowerTable[y];
                }

                public int GetHashCode(char obj)
                {
                    return _unicodeToLowerTable[obj].GetHashCode();
                }
            }
        }
    }
}
