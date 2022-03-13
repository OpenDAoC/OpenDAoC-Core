using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using DOL.GS.Scripts;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Scripts
{
    public class Legion : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Legion()
            : base()
        {
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13333);
            LoadTemplate(npcTemplate);
            
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            // demon
            BodyType = 2;

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));
            
            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
        
        

        public virtual int LegionDifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000 * LegionDifficulty;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500 * LegionDifficulty;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.65 * LegionDifficulty;
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc.Brain is LegionAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }
            
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            bool canReportNews = true;

            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
                {
                    if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                        canReportNews = false;
                }

            }
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,5000);
                }
            }
            DropLoot(killer);
            base.Die(killer);
            
            if (canReportNews)
            {
                ReportNews(killer);
            }
        }
        
        
        #region Custom Methods
        /// <summary>
        /// Post a message in the server news and award a legion kill point for
        /// every XP gainer in the raid.
        /// </summary>
        /// <param name="killer">The living that got the killing blow.</param>
        protected void ReportNews(GameObject killer)
        {
            int numPlayers = AwardLegionKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_LEGION_KILL > 0)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player.IsEligibleToGiveMeritPoints)
                    {
                        GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_LEGION_KILL);
                    }
                }
            }
        }

        /// <summary>
        /// Award legion kill point for each XP gainer.
        /// </summary>
        /// <returns>The number of people involved in the kill.</returns>
        protected int AwardLegionKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsLegion++;
                player.RaiseRealmLoyaltyFloor(2);
                count++;
            }
            return count;
        }

        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class LegionBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static IArea Legion_Area = null;
        private static GameLocation Legion_Lair = new GameLocation("Legion\'s Lair", 249, 45066, 51731, 15468, 2053);

        public LegionBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public static void BroadcastMessage(String message)
        {
            Legion legion = new Legion();
            foreach (GamePlayer player in legion.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is LegionAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            
            if (HasAggro && Body.InCombat)
            {
                if (Body.TargetObject != null)
                {
                    // 3% chance to spawn 15-20 zombies
                    if (Util.Chance(3))
                    {
                        SpawnAdds();
                    }
                }
            }
            else
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc.Brain is LegionAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            base.Think();
        }
        public void SpawnAdds()
        {
            for (int i = 0; i < Util.Random(15, 20); i++)
            {
                LegionAdd add = new LegionAdd();
                add.X = 45066;
                add.Y = 51731;
                add.Z = 15468;
                add.CurrentRegionID = 249;
                add.Heading = 2053;
                add.IsWorthReward = false;
                int level = Util.Random(52, 58);
                add.Level = (byte) level;
                add.AddToWorld();
            }
        }
        
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Legion initializing ...");
            
            #region defineAreas
            Legion_Area = WorldMgr.GetRegion(Legion_Lair.RegionID).AddArea(new Area.Circle("Legion Lair", Legion_Lair.X, Legion_Lair.Y, Legion_Lair.Z, 530));
            Legion_Area.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            #endregion
        }
        
        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            #region defineAreas
            Legion_Area.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            WorldMgr.GetRegion(Legion_Lair.RegionID).RemoveArea(Legion_Area);
            #endregion
        }
        
        protected static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
        {
            AreaEventArgs aargs = args as AreaEventArgs;
            GamePlayer player = aargs?.GameObject as GamePlayer;
            Legion legion = new Legion();

            if (player == null)
                return;
            
            if (e == AreaEvent.PlayerEnter)
            {
                BroadcastMessage("Legion doesn't like enemies in his lair");
                new RegionTimer(legion, new RegionTimerCallback(killAreaTimer), 3000);
            }
            if (e == GameLivingEvent.HealthChanged && sender is Legion)
            {
                foreach (GamePlayer portPlayer in player.GetPlayersInRadius(250))
                {
                    if (portPlayer.IsAlive)
                    {
                        portPlayer.MoveTo(249, 48117, 49573, 20833, 1006);
                        portPlayer.BroadcastUpdate();
                    }
                }
                player.MoveTo(249, 48117, 49573, 20833, 1006);
                player.BroadcastUpdate();
            }
        }

        private static int killAreaTimer(RegionTimer timer)
        {
            Legion legion = new Legion();
            foreach (GamePlayer player in legion.GetPlayersInRadius(800))
            {
                if (player == null)
                    return 0;
                
                List<GamePlayer> potKiller = new List<GamePlayer>();
                potKiller.Add(player);
                int ranId = Util.Random(0, potKiller.Count);
                if (ranId >= 0)
                {
                    potKiller[ranId].Die(legion);
                    //player.Die(potKiller[ranId]);
                }
                potKiller.Clear();
               
            }
            return 0;
        }
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            base.Notify(e, sender, args);
            if (e == AreaEvent.PlayerEnter)
            {
                if (Util.Chance(100))
                {
                    BroadcastMessage("Legion doesn't like enemies in his lair");
                    new RegionTimer(Body, new RegionTimerCallback(killAreaTimer), 3000);
                }
            }

            if (e == GameLivingEvent.Dying && sender is GamePlayer)
            {
                GamePlayer player = sender as GamePlayer;
                Body.Health += player.MaxHealth;
                Body.UpdateHealthManaEndu();
            }
        }
    }
}

namespace DOL.GS
{
    public class LegionAdd : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAdd()
            : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 1500;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 450;
            }
            set
            {
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 150;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.50;
        }
        public override bool AddToWorld()
        {
            Model = 660;
            Name = "graspering soul";
            Size = 50;
            Realm = 0;

            Strength = 60;
            Intelligence = 60;
            Piety = 60;
            Dexterity = 60;
            Constitution = 60;
            Quickness = 60;
            RespawnInterval = -1;

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;

            BodyType = 2;
            LegionAddBrain sBrain = new LegionAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 800;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public LegionAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}