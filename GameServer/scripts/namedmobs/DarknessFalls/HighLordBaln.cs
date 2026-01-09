using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class HighLordBaln : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("High Lord Baln initialized..");
        }

        public HighLordBaln()
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
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
            LoadTemplate(npcTemplate);


            // Custom Respawn +/- 20% 1h
            int baseRespawnMS = 3600000; 
            int maxOffsetMS = 720000; 
            Random rnd = new Random();
            int randomOffset = rnd.Next(maxOffsetMS * 2) - maxOffsetMS;
            RespawnInterval = baseRespawnMS + randomOffset;


            // demon
            BodyType = 2;
            Faction = FactionMgr.GetFactionByID(191);
            BalnBrain sBrain = new BalnBrain();
            SetOwnBrain(sBrain);
            LoadedFromScript = false;
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
        public override void OnAttackedByEnemy(AttackData ad)
        {          
            if (!InCombat)
            {
                var mobs = GetNPCsInRadius(3000);
                foreach (GameNPC mob in mobs)
                {
                    if (!mob.InCombat)
                    {
                        mob.StartAttack(ad.Attacker);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void Die(GameObject killer)
        {
            bool canReportNews = true;
            DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>("daemon_blood_seal");
            int itemCount = 100;
            string message_currency = "High Lord Baln drops " + itemCount + " " + template.Name + ".";
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
    public class BalnBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        public BalnBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        private bool RemoveAdds = false;
        private bool CanPopMinions = false;
        private int SpawnMinion(ECSGameTimer timer)
        {
            for (int i = 0; i < Util.Random(8, 12); i++)
            {
                BalnMinion sMinion = new BalnMinion();
                sMinion.X = Body.X + Util.Random(-100, 100);
                sMinion.Y = Body.Y + Util.Random(-100, 100);
                sMinion.Z = Body.Z;
                sMinion.CurrentRegion = Body.CurrentRegion;
                sMinion.Heading = Body.Heading;
                sMinion.AddToWorld();
            }
            CanPopMinions = false;
            return 0;
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.RespawnInterval == -1 && npc.Brain is BalnMinionBrain)
                                npc.Die(npc);
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (!CanPopMinions)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnMinion), Util.Random(20000, 35000));
                    CanPopMinions = true;
                }
            }
            base.Think();
        }
    }
}

namespace DOL.GS
{
    public class BalnMinion : GameNPC
    {
        public override int MaxHealth
        {
            get { return 800; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
            LoadTemplate(npcTemplate);
            Level = 58;
            Strength = 300;
            Size = 50;
            Name += "'s Minion";
            RoamingRange = 350;
            RespawnInterval = -1;
            TetherRange = 2000;
            Realm = eRealm.None;
            BalnMinionBrain adds = new BalnMinionBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);

            base.AddToWorld();
            return true;
        }
        public override bool CanDropLoot => false;
        public override long ExperienceValue => 0;
        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }
    }
}

namespace DOL.AI.Brain
{
    public class BalnMinionBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BalnMinionBrain()
        {
            AggroLevel = 100;
            AggroRange = 450;
        }
    }
}
