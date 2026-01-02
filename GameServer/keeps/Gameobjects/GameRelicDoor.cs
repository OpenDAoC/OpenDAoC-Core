using System.Collections;
using DOL.GS.PacketHandler;

namespace DOL.GS.Keeps
{
    /// <summary>
    /// relic keep door in world
    /// </summary>
    public class GameRelicDoor : GameDoorBase
    {
        #region properties

        /// <summary>
        /// This flag is send in packet(keep door = 4, regular door = 0)
        /// </summary>
        public override uint Flag
        {
            get => 4;
            set { }
        }

        public override int Health
        {
            get => MaxHealth;
            set { }
        }

        public override string Name => "Relic Gate";

        #endregion

        #region function override

        /// <summary>
        /// This methode is override to remove XP system
        /// </summary>
        /// <param name="source">the damage source</param>
        /// <param name="damageType">the damage type</param>
        /// <param name="damageAmount">the amount of damage</param>
        /// <param name="criticalAmount">the amount of critical damage</param>
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            return;
        }

        public override int ChangeHealth(GameObject changeSource, eHealthChangeType healthChangeType, int changeAmount)
        {
            return 0;
        }

        /// <summary>
        /// This function is called from the ObjectInteractRequestHandler
        /// It teleport player in the keep if player and keep have the same realm
        /// </summary>
        /// <param name="player">GamePlayer that interacts with this object</param>
        /// <returns>false if interaction is prevented</returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.IsMezzed)
            {
                player.Out.SendMessage("You are mesmerized!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (player.IsStunned)
            {
                player.Out.SendMessage("You are stunned!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (GameServer.ServerRules.IsSameRealm(player, this, true) || player.Client.Account.PrivLevel != 1)
            {
                Point2D point;
                //calculate x y
                if (IsObjectInFront(player, 180) )
                    point = this.GetPointFromHeading(this.Heading, -500);
                else
                    point = this.GetPointFromHeading(this.Heading, 500);

                //move player
                player.MoveTo(CurrentRegionID, point.X, point.Y, player.Z, player.Heading);
            }
            return base.Interact(player);
        }

        public override IList GetExamineMessages(GamePlayer player)
        {
            /*
             * You select the Keep Gate. It belongs to your realm.
             * You target [the Keep Gate]
             * 
             * You select the Keep Gate. It belongs to an enemy realm and can be attacked!
             * You target [the Keep Gate]
             * 
             * You select the Postern Door. It belongs to an enemy realm!
             * You target [the Postern Door]
             */

            IList list = base.GetExamineMessages(player);
            string text = "You select the " + Name + ".";
            if (this.Realm == player.Realm)
                text = text + " It belongs to your realm.";
            else
            {
                text = text + " It belongs to an enemy realm!";
            }
            list.Add(text);
            return list;
        }

        public override string GetName(int article, bool firstLetterUppercase)
        {
            return "the " + base.GetName(article, firstLetterUppercase);
        }

        public override void StartHealthRegeneration()
        {
            return;
        }

        #endregion

        #region Save/load DB

        /// <summary>
        /// save the keep door object in DB
        /// </summary>
        public override void SaveIntoDatabase() { }

        #endregion

        public virtual void OpenDoor()
        {
            State = eDoorState.Open;
            BroadcastDoorStatus();
        }

        /// <summary>
        /// This method is called when door is repair or keep is reset
        /// </summary>
        public virtual void CloseDoor()
        {
            State = eDoorState.Closed;
            BroadcastDoorStatus();
        }

        /// <summary>
        /// boradcast the door statut to all player near the door
        /// </summary>
        public virtual void BroadcastDoorStatus()
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                ClientService.UpdateObjectForPlayer(player, this);
                player.Out.SendDoorState(CurrentRegion, this);
            }
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str))
                return false;

            if (source is GamePlayer == false)
                return false;

            str = str.ToLower();

            if (str.Contains("enter") || str.Contains("exit"))
                Interact(source as GamePlayer);
            return true;
        }

        public override bool SayReceive(GameLiving source, string str)
        {
            if (!base.SayReceive(source, str))
                return false;

            if (source is GamePlayer == false)
                return false;

            str = str.ToLower();

            if (str.Contains("enter") || str.Contains("exit"))
                Interact(source as GamePlayer);
            return true;
        }
    }
}