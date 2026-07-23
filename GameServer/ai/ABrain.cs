using System;
using System.Collections.Generic;
using System.Text;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.AI
{
    /// <summary>
    /// This class is the base of all artificial intelligence in game objects
    /// </summary>
    public abstract class ABrain : IServiceObject
    {
        public FSM FSM { get; set; }
        public ServiceObjectId ServiceObjectId { get; } = new(ServiceObjectType.Brain);
        public virtual GameNPC Body { get; set; }
        public virtual int ThinkInterval { get; set; } = 2500;
        public bool IsActive => Body != null && Body.IsAlive && Body.ObjectState is GameObject.eObjectState.Active && Body.IsVisibleToPlayers;
        public long NextThinkTick { get; set; }
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
            if (!ServiceObjectStore.Add(this))
                return false;

            // Offset the first think tick by a random amount so that not too many are grouped in one server tick.
            // We also delay the first think tick a bit because clients tend to send positive LoS checks when they shouldn't.
            NextThinkTick = GameLoop.GameLoopTime + ThinkOffsetOnStart;
            return true;
        }

        /// <summary>
        /// Stops the brain thinking
        /// </summary>
        /// <returns>true if stopped</returns>
        public virtual bool Stop()
        {
            if (!ServiceObjectStore.Remove(this))
                return false;

            // Without `IsActive` check, charming a NPC that's returning to spawn would teleport it.
            if (!Body.IsAtSpawn && !IsActive)
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, Body.SpawnHeading);

            Body.ClearObjectsInRadiusCache();
            FSM?.SetCurrentState(eFSMStateType.WAKING_UP);
            return true;
        }

        public GamePlayer GetLosChecker(GameObject target)
        {
            // Returns the GamePlayer that should perform LoS checks on behalf of this entity.

            if (target == null || target == Body)
                return null;

            GamePlayer losChecker = target as GamePlayer;

            if (CanReplyToLosCheckRequests(losChecker))
                return losChecker;

            if (this is IControlledBrain controlledBrain)
            {
                losChecker = controlledBrain.GetPlayerOwner();

                if (CanReplyToLosCheckRequests(losChecker))
                    return losChecker;
            }

            List<GamePlayer> playersInRadius = Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

            if (playersInRadius.Count == 0)
                return null;

            int start = Util.Random(playersInRadius.Count - 1);

            for (int i = 0; i < playersInRadius.Count; i++)
            {
                GamePlayer player = playersInRadius[(start + i) % playersInRadius.Count];

                if (CanReplyToLosCheckRequests(player))
                    return player;
            }

            return null;

            static bool CanReplyToLosCheckRequests(GamePlayer player)
            {
                // Currently allows players with a soft linkdeath timer running.
                return player != null &&
                    player.ObjectState is GameObject.eObjectState.Active &&
                    player.Client.ClientState is GameClient.eClientState.Playing;
            }
        }

        public virtual bool OnPathPointReached(PathPoint pathPoint)
        {
            // Returns true if the NPC should stop moving.
            // The path won't be resumed automatically.
            return false;
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
    }
}
