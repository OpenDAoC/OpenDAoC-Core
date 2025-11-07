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

        public bool HasAny =>
            Realm.HasValue ||
            Slot.HasValue ||
            IsCrafted.HasValue ||
            HasVisual.HasValue ||
            !string.IsNullOrEmpty(Owner);
    }

    public class MarketSearchEngine : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new();

        private readonly HashSet<DbInventoryItem> _allItems = new();
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
                    return _allItems.Count;
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
                if (!item.IsTradable || item.SellPrice <= 0)
                    return false;

                if (!_allItems.Add(item))
                    return false;

                AddToIndex(_byRealm, GetRealmOfLot(item.OwnerLot), item);
                AddToIndex(_bySlot, GetClientSlot(item), item);
                AddToIndex(_byCrafted, item.IsCrafted, item);
                AddToIndex(_byVisual, item.Effect > 0, item);
                AddToIndex(_byOwner, item.OwnerID, item);
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
                if (!_allItems.Remove(item))
                    return false;

                RemoveFromIndex(_byRealm, GetRealmOfLot(item.OwnerLot), item);
                RemoveFromIndex(_bySlot, GetClientSlot(item), item);
                RemoveFromIndex(_byCrafted, item.IsCrafted, item);
                RemoveFromIndex(_byVisual, item.Effect > 0, item);
                RemoveFromIndex(_byOwner, item.OwnerID, item);
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

                if (resultSets == null || resultSets.Count == 0)
                    return query.HasAny ? [] : [.. _allItems];  // Consider pooling this, better if the caller provides the collection.

                // Start with a copy of the smallest set for efficiency.
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

                // Copy the smallest set, then intersect with the others.
                HashSet<DbInventoryItem> finalResult = [.. smallestSet]; // Consider pooling this, better if the caller provides the collection.

                for (int i = 1; i < resultSets.Count; i++)
                {
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
            List<HashSet<DbInventoryItem>> sets = new(); // Consider pooling this.

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
            if (!index.TryGetValue(key, out var set))
                return;

            set.Remove(item);

            if (set.Count == 0)
                index.Remove(key);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _lock.Dispose();
        }
    }
}
