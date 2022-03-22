using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Scripts
{
    public class Legion : GameEpicBoss
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static IArea legionArea = null;
        
        [ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
            const int radius = 650;
            Region region = WorldMgr.GetRegion(249);
            legionArea = region.AddArea(new Area.Circle("Legion's Lair", 45000,51700,15468, radius));
            log.Debug("Legion's Lair created with radius " + radius + " at 45000 51700 15468");
            legionArea.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));
            
			if (log.IsInfoEnabled)
				log.Info("Legion initialized..");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
            legionArea.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
			WorldMgr.GetRegion(249).RemoveArea(legionArea);
            
            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));
        }
        
        public Legion()
            : base()
        {
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13333);
            LoadTemplate(npcTemplate);

            Size = 120;
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            // demon
            BodyType = 2;
            Race = 2001;

            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));
            
            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            
            base.AddToWorld();
            return true;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get
            {
                return 20000;
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
            return 1000;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
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

                if (!canReportNews || GameServer.ServerRules.CanGenerateNews(player) != false) continue;
                if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                    canReportNews = false;

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
        
        private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
        {
            AreaEventArgs aargs = args as AreaEventArgs;
            GamePlayer player = aargs?.GameObject as GamePlayer;
            
            if (player == null)
                return;

            Console.Write(player?.Name + " entered Legion's Lair");

            var mobsInArea = player.GetNPCsInRadius(2500);
            
            if (mobsInArea == null)
                return;
            
            foreach (GameNPC mob in mobsInArea)
            {
                if (mob is not Legion || !mob.InCombat) continue;
                Console.WriteLine("Legion is alive and in combat");

                if (Util.Chance(33))
                {
                    Console.WriteLine("Whops, we got a hit!");
                    foreach (GamePlayer nearbyPlayer in mob.GetPlayersInRadius(2500))
                    {
                        nearbyPlayer.Out.SendMessage("Legion doesn't like enemies in his lair", eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
                        nearbyPlayer.Out.SendSpellEffectAnimation(mob, player, 5933, 0, false, 1);
                    }
                    player.Die(mob);
                }
                else
                {
                    foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
                    {
                        playerNearby.MoveTo(249, 48117, 49573, 20833, 1006);
                        playerNearby.BroadcastUpdate();
                    }
                    player.MoveTo(249, 48117, 49573, 20833, 1006);
                }
                player.BroadcastUpdate();
            }
        }
        private static void PlayerKilledByLegion(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            
            if (player == null)
                return;
            
            foreach (GameNPC mob in player.GetNPCsInRadius(2500))
            {
                if (mob is not Legion) continue;
                mob.Health += player.MaxHealth;
                mob.UpdateHealthManaEndu();
            }
            
            foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
            {
                playerNearby.MoveTo(249, 48117, 49573, 20833, 1006);
                playerNearby.BroadcastUpdate();
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                if (Util.Chance(3))
                {
                    var spawnAmount = Util.Random(15, 20);
                    SpawnAdds((GamePlayer) source, spawnAmount);
                }
            }
            
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        private void SpawnAdds(GamePlayer target, int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                var distanceDelta = Util.Random(0, 300);
                var level = Util.Random(52, 58);
                
                LegionAdd add = new LegionAdd();
                add.X = target.X + distanceDelta;
                add.Y = target.Y + distanceDelta;
                add.Z = target.Z;
                add.CurrentRegionID = target.CurrentRegionID;
                add.IsWorthReward = false;
                add.Level = (byte) level;
                add.AddToWorld();
                add.StartAttack(target);
            }
        }

        private void ReportNews(GameObject killer)
        {
            int numPlayers = AwardLegionKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_LEGION_KILL <= 0) return;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player.IsEligibleToGiveMeritPoints)
                {
                    GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_LEGION_KILL);
                }
            }
        }

        private int AwardLegionKillPoint()
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
        
    }
}

namespace DOL.AI.Brain
{
    public class LegionBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public LegionBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
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
            
            base.Think();
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
    }
}