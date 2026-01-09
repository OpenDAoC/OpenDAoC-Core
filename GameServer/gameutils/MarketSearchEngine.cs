using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;

namespace DOL.GS
{
    public readonly struct ItemQuery
    {
        public readonly eRealm? Realm { get; init; }
        public readonly int? Slot { get; init; }
        public readonly bool? IsCrafted { get; init; }
        public readonly bool? HasVisual { get; init; }
        public readonly string Owner { get; init; }
    }

    public class MarketSearchEngine : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly Dictionary<DbInventoryItem, IndexKeys> _itemKeyCache = new();
        private readonly Dictionary<eRealm, HashSet<DbInventoryItem>> _byRealm = new();
        private readonly Dictionary<int, HashSet<DbInventoryItem>> _bySlot = new();
        private readonly Dictionary<bool, HashSet<DbInventoryItem>> _byCrafted = new();
        private readonly Dictionary<bool, HashSet<DbInventoryItem>> _byVisual = new();
        private readonly Dictionary<string, HashSet<DbInventoryItem>> _byOwner = new();

        public int ItemCount
        {
            get
            {
                _lock.EnterReadLock();

                try
                {
                    return _itemKeyCache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public bool AddItem(DbInventoryItem item)
        {
            _lock.EnterWriteLock();

            try
            {
                IndexKeys keys = GetIndexKeys(item);

                if (!_itemKeyCache.TryAdd(item, keys))
                    return false;

                AddToIndex(_byRealm, keys.Realm, item);
                AddToIndex(_bySlot, keys.Slot, item);
                AddToIndex(_byCrafted, keys.IsCrafted, item);
                AddToIndex(_byVisual, keys.HasVisual, item);
                AddToIndex(_byOwner, keys.Owner, item);
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool RemoveItem(DbInventoryItem item)
        {
            _lock.EnterWriteLock();

            try
            {
                if (!_itemKeyCache.Remove(item, out IndexKeys keys))
                    return false;

                RemoveFromIndex(_byRealm, keys.Realm, item);
                RemoveFromIndex(_bySlot, keys.Slot, item);
                RemoveFromIndex(_byCrafted, keys.IsCrafted, item);
                RemoveFromIndex(_byVisual, keys.HasVisual, item);
                RemoveFromIndex(_byOwner, keys.Owner, item);
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerable<DbInventoryItem> Search(in ItemQuery query)
        {
            _lock.EnterReadLock();

            try
            {
                List<HashSet<DbInventoryItem>> resultSets = GetMatchingSets(query);

                // Return the full list if no filter was provided.
                if (resultSets == null)
                    return [.. _itemKeyCache.Keys];

                if (resultSets.Count == 0)
                    return [];

                // Find the smallest set to start with.
                HashSet<DbInventoryItem> smallestSet = resultSets[0];
                int minCount = smallestSet.Count;

                for (int i = 1; i < resultSets.Count; i++)
                {
                    HashSet<DbInventoryItem> set = resultSets[i];

                    if (set.Count < minCount)
                    {
                        minCount = set.Count;
                        smallestSet = set;
                    }
                }

                if (smallestSet.Count == 0)
                    return [];

                // Intersect all other sets with the smallest set.
                HashSet<DbInventoryItem> finalResult = [.. smallestSet];

                for (int i = 0; i < resultSets.Count; i++)
                {
                    if (resultSets[i] == smallestSet)
                        continue;

                    finalResult.IntersectWith(resultSets[i]);

                    if (finalResult.Count == 0)
                        break;
                }

                return finalResult;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public static eRealm GetRealmOfLot(ushort lot)
        {
            if (lot > 0)
            {
                if (lot <= 1382)
                    return eRealm.Albion;
                else if (lot <= 2573)
                    return eRealm.Midgard;
                else if (lot <= 4398)
                    return eRealm.Hibernia;
            }

            return eRealm.None;
        }

        private static IndexKeys GetIndexKeys(DbInventoryItem item)
        {
            return new(
                GetRealmOfLot(item.OwnerLot),
                GetClientSlot(item),
                item.IsCrafted,
                item.Effect > 0,
                item.OwnerID
            );
        }

        private static int GetClientSlot(DbInventoryItem item)
        {
            if ((eInventorySlot) item.Item_Type is eInventorySlot.TorsoArmor)
                return 5;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.HeadArmor)
                return 1;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.ArmsArmor)
                return 8;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.HandsArmor)
                return 2;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.LegsArmor)
                return 7;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.FeetArmor)
                return 3;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.Neck)
                return 9;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.Cloak)
                return 6;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.Jewelry)
                return 4;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.Waist)
                return 12;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.RightBracer || (eInventorySlot) item.Item_Type is eInventorySlot.LeftBracer)
                return 13;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.RightRing || (eInventorySlot) item.Item_Type is eInventorySlot.LeftRing)
                return 15;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.RightHandWeapon)
                return 100;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.LeftHandWeapon)
            {
                if ((eObjectType) item.Object_Type is eObjectType.Shield)
                    return 105;

                return 101;
            }

            if ((eInventorySlot) item.Item_Type is eInventorySlot.TwoHandWeapon)
                return 102;

            if ((eInventorySlot) item.Item_Type is eInventorySlot.DistanceWeapon)
            {
                if ((eObjectType) item.Object_Type is eObjectType.Instrument)
                    return 104;

                return 103;
            }

            if ((eObjectType) item.Object_Type is eObjectType.GenericItem)
                return 106;

            return 0;
        }

        private List<HashSet<DbInventoryItem>> GetMatchingSets(in ItemQuery query)
        {
            List<HashSet<DbInventoryItem>> sets = new();

            if (query.Realm.HasValue)
            {
                if (_byRealm.TryGetValue(query.Realm.Value, out var realmSet))
                    sets.Add(realmSet);
                else
                    return null;
            }

            if (query.Slot.HasValue)
            {
                if (_bySlot.TryGetValue(query.Slot.Value, out var slotSet))
                    sets.Add(slotSet);
                else
                    return null;
            }

            if (query.IsCrafted.HasValue)
            {
                if (_byCrafted.TryGetValue(query.IsCrafted.Value, out var craftedSet))
                    sets.Add(craftedSet);
                else
                    return null;
            }

            if (query.HasVisual.HasValue)
            {
                if (_byVisual.TryGetValue(query.HasVisual.Value, out var visualSet))
                    sets.Add(visualSet);
                else
                    return null;
            }

            if (!string.IsNullOrEmpty(query.Owner))
            {
                if (_byOwner.TryGetValue(query.Owner, out var ownerSet))
                    sets.Add(ownerSet);
                else
                    return null;
            }

            return sets;
        }

        private static void AddToIndex<TKey>(Dictionary<TKey, HashSet<DbInventoryItem>> index, TKey key, DbInventoryItem item)
        {
            if (!index.TryGetValue(key, out var set))
            {
                set = new();
                index[key] = set;
            }

            set.Add(item);
        }

        private static void RemoveFromIndex<TKey>(Dictionary<TKey, HashSet<DbInventoryItem>> index, TKey key, DbInventoryItem item)
        {
            if (index.TryGetValue(key, out var set))
            {
                set.Remove(item);

                if (set.Count == 0)
                    index.Remove(key);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _lock.Dispose();
        }

        private readonly struct IndexKeys
        {
            public readonly eRealm Realm;
            public readonly int Slot;
            public readonly bool IsCrafted;
            public readonly bool HasVisual;
            public readonly string Owner;

            public IndexKeys(eRealm realm, int slot, bool isCrafted, bool hasVisual, string owner)
            {
                Realm = realm;
                Slot = slot;
                IsCrafted = isCrafted;
                HasVisual = hasVisual;
                Owner = owner;
            }
        }
    }
}
