using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;


namespace DOL.GS
{
    public class PrinceAsmoien : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Prince Asmoien initialized..");
        }
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
        }
        public PrinceAsmoien()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165030);
            LoadTemplate(npcTemplate);
            // demon
            BodyType = 2;


            // Custom Respawn +/- 20% 4h
            int baseRespawnMS = 14400000; 
            int maxOffsetMS = 2880000; 
            Random rnd = new Random();
            int randomOffset = rnd.Next(maxOffsetMS * 2) - maxOffsetMS;
            RespawnInterval = baseRespawnMS + randomOffset;


            Faction = FactionMgr.GetFactionByID(191);
            AsmoienBrain sBrain = new AsmoienBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override int MeleeAttackRange => 450;

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public override void Die(GameObject killer)
        {
            bool canReportNews = true;
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("daemon_blood_seal");
            int itemCount = 50;
            string message_currency = "Prince Asmoien drops " + itemCount + " " + template.Name + ".";
            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                DbInventoryItem item = GameInventoryItem.Create(template);
                item.Count = itemCount;
                if (player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item))
                {
                    player.Out.SendMessage(message_currency, eChatType.CT_Loot, eChatLoc.CL_ChatWindow);
                    InventoryLogging.LogInventoryAction(player, player, eInventoryActionType.Other, template, itemCount);
                }
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (!canReportNews || GameServer.ServerRules.CanGenerateNews(player) != false) continue;
                if (player.Client.Account.PrivLevel == (int) ePrivLevel.Player)
                    canReportNews = false;
            }

            base.Die(killer);

            if (canReportNews)
            {
                ReportNews(killer);
            }
        }
        private void ReportNews(GameObject killer)
        {
            //int numPlayers = AwardHLKillPoint();
            String message = String.Format("{0} has been slain!", Name/*, numPlayers*/);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);
        }
        /*private int AwardHLKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                count++;
            }
            return count;
        }*/
    }
}

namespace DOL.AI.Brain
{
    public class AsmoienBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public AsmoienBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }

        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            else
            {
                foreach (GameNPC pet in Body.GetNPCsInRadius(2000))
                {
                    if (pet.Brain is not IControlledBrain) continue;
                    Body.Health += pet.MaxHealth;
                    pet.Emote(eEmote.SpellGoBoom);
                    pet.Die(Body);
                }
            }

            base.Think();
        }
    }
}