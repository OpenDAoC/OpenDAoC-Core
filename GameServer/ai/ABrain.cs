using System;
using System.Text;
using DOL.Events;
using DOL.GS;

namespace DOL.AI
{
    /// <summary>
    /// This class is the base of all artificial intelligence in game objects
    /// </summary>
    public abstract class ABrain : IShardedServiceObject
    {
        public FSM FSM { get; set; }
        public ShardedServiceObjectId ServiceObjectId { get; } = new(ServiceObjectType.Brain);
        ServiceObjectId IServiceObject.ServiceObjectId => ServiceObjectId;
        SchedulableServiceObjectId ISchedulableServiceObject.ServiceObjectId => ServiceObjectId;
        ShardedServiceObjectId IShardedServiceObject.ServiceObjectId => ServiceObjectId;
        public virtual GameNPC Body { get; set; }
        public virtual int ThinkInterval { get; set; } = 2500;
        public bool IsActive => Body != null && Body.IsAlive && Body.ObjectState is GameObject.eObjectState.Active && Body.IsVisibleToPlayers;
        public long NextThinkTick { get; private set; }
        protected virtual int ThinkOffsetOnStart => Util.Random(750, 3000);

        /// <summary>
        /// Returns the string representation of the ABrain
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new StringBuilder()
                .Append("body name='").Append(Body==null?"(null)":Body.Name)
                .Append("' (id=").Append(Body==null?"(null)":Body.ObjectID.ToString())
                .Append("), active=").Append(IsActive)
                .Append(", ThinkInterval=").Append(ThinkInterval)
                .ToString();
        }

        /// <summary>
        /// Starts the brain thinking
        /// </summary>
        /// <returns>true if started</returns>
        public virtual bool Start()
        {
            DOL.GS.ServiceObjectId.PendingAction pendingAction = ServiceObjectId.PeekAction();

            if (ServiceObjectId.IsRegistered && pendingAction is not DOL.GS.ServiceObjectId.PendingAction.Remove)
                return false;

            if (!ServiceObjectId.IsRegistered && pendingAction is DOL.GS.ServiceObjectId.PendingAction.Add or DOL.GS.ServiceObjectId.PendingAction.Schedule)
                return false;

            // Offset the first think tick by a random amount so that not too many are grouped in one server tick.
            // We also delay the first think tick a bit because clients tend to send positive LoS checks when they shouldn't.
            NextThinkTick = GameLoop.GameLoopTime + ThinkOffsetOnStart;

            return ServiceObjectStore.Schedule(this, NextThinkTick, allowPendingRemove: true);
        }

        public bool ScheduleNextThink(long nextThinkTick)
        {
            NextThinkTick = nextThinkTick;
            return ServiceObjectStore.Schedule(this, nextThinkTick);
        }

        public bool WakeNow()
        {
            NextThinkTick = GameLoop.GameLoopTime;
            return ServiceObjectStore.Wake(this);
        }

        /// <summary>
        /// Stops the brain thinking
        /// </summary>
        /// <returns>true if stopped</returns>
        public virtual bool Stop()
        {
            if (ServiceObjectId.PeekAction() is DOL.GS.ServiceObjectId.PendingAction.Remove)
                return false; // Prevents overrides from doing any redundant work. Maybe counter intuitive.

            // Without `IsActive` check, charming a NPC that's returning to spawn would teleport it.
            if (!Body.IsNearSpawn && !IsActive)
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, Body.SpawnHeading);

            Body.ClearObjectsInRadiusCache();
            FSM?.SetCurrentState(eFSMStateType.WAKING_UP);
            return ServiceObjectStore.Remove(this);
        }

        /// <summary>
        /// Receives all messages of the body
        /// </summary>
        /// <param name="e">The event received</param>
        /// <param name="sender">The event sender</param>
        /// <param name="args">The event arguments</param>
        public virtual void Notify(DOLEvent e, object sender, EventArgs args) { }

        /// <summary>
        /// This method is called whenever the brain does some thinking
        /// </summary>
        public abstract void Think();

        public abstract void KillFSM();
    }
}
