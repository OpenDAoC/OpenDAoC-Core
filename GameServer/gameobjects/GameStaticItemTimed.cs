using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace DOL.GS
{
    /// <summary>
    /// Holds a static item in the world that will disappear after some interval
    /// </summary>
    public abstract class GameStaticItemTimed : GameStaticItem
    {
        protected Lock _pickUpLock = new();
        private uint _removeDelay = ServerProperties.Properties.WORLD_ITEM_DECAY_TIME;
        private RemoveItemAction _removeItemAction;

        // Make sure to use our custom comparer. This allows players logging back in to pick up items dropped during their previous session.
        public HashSet<IGameStaticItemOwner> Owners { get; private set; } = new(new IGameStaticItemOwner.OwnerEqualityComparer());

        public GameStaticItemTimed() : base() { }

        public GameStaticItemTimed(uint vanishTicks): this()
        {
            if (vanishTicks > 0)
                _removeDelay = vanishTicks;
        }

        public uint RemoveDelay
        {
            get => _removeDelay;
            set
            {
                if (value > 0)
                    _removeDelay = value;

                if (_removeItemAction.IsAlive)
                    _removeItemAction.Start((int) _removeDelay);
            }
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            _removeItemAction ??= new RemoveItemAction(this);
            _removeItemAction.Start((int) _removeDelay);
            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (RemoveFromWorld(RespawnInterval))
            {
                _removeItemAction?.Stop();
                return true;
            }

            return false;
        }

        public void AddOwner(IGameStaticItemOwner owner)
        {
            Owners.Add(owner);
        }

        public bool IsOwner(GamePlayer player)
        {
            if (Owners.Contains(player) || Owners.Contains(player.Group))
                return true;

            BattleGroup battleGroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
            return battleGroup != null && Owners.Contains(battleGroup);
        }

        public void AssertLockAcquisition()
        {
            if (!_pickUpLock.IsHeldByCurrentThread)
                throw new InvalidOperationException($"{Name} is not locked by the current thread.");
        }

        public abstract TryPickUpResult TryAutoPickUp(IGameStaticItemOwner itemOwner);
        public abstract TryPickUpResult TryPickUp(GamePlayer source, IGameStaticItemOwner itemOwner);

        protected class RemoveItemAction : ECSGameTimerWrapperBase
        {
            public RemoveItemAction(GameStaticItemTimed item) : base(item) { }

            protected override int OnTick(ECSGameTimer timer)
            {
                (timer.Owner as GameStaticItemTimed).Delete();
                return 0;
            }
        }
    }

    public interface IGameStaticItemOwner
    {
        // The pick up methods aren't thread safe in respect to the object being picked up, and are intended to be called from `GameStaticItemTimed` only.

        string Name { get; }
        object GameStaticItemOwnerComparand { get; } // Used by `GameStaticItemOwnerEqualityComparer`. Can be null, in which case it will defer to the object's `Equals` and `GetHashCode`.
        TryPickUpResult TryAutoPickUpMoney(GameMoney money);
        TryPickUpResult TryAutoPickUpItem(WorldInventoryItem item);
        TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money); // Expected to return false only if the object shouldn't try to pick up the item at all.
        TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item); // Expected to return false only if the object shouldn't try to pick up the item at all.

        public class ItemOwnerTotalDamagePair
        {
            public IGameStaticItemOwner Owner { get; set; }
            public double Damage { get; set; }

            public ItemOwnerTotalDamagePair() { }

            public ItemOwnerTotalDamagePair(IGameStaticItemOwner owner, double damage)
            {
                Owner = owner;
                Damage = damage;
            }
        }

        public class OwnerEqualityComparer : IEqualityComparer<IGameStaticItemOwner>
        {
            public bool Equals(IGameStaticItemOwner x, IGameStaticItemOwner y)
            {
                return x.GameStaticItemOwnerComparand != null && y.GameStaticItemOwnerComparand != null ?
                    x.GameStaticItemOwnerComparand == y.GameStaticItemOwnerComparand :
                    x.Equals(y);
            }

            public int GetHashCode([DisallowNull] IGameStaticItemOwner obj)
            {
                return obj.GameStaticItemOwnerComparand != null ?
                    obj.GameStaticItemOwnerComparand.GetHashCode() :
                    obj.GetHashCode();
            }
        }
    }

    public enum TryPickUpResult
    {
        Success,    // The item was picked up.
        Blocked,    // The item couldn't be picked up due to inventory being full, player not in range, etc.
        DoesNotWant // The item is rejected due to player, group, or battlegroup settings.
    }
}
