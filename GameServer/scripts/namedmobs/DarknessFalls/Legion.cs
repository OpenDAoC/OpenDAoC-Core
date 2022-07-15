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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static IArea legionArea = null;

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            const int radius = 650;
            Region region = WorldMgr.GetRegion(249);
            legionArea = region.AddArea(new Area.Circle("Legion's Lair", 45000, 51700, 15468, radius));
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
            get { return 300000; }
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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

            bool canReportNews = true;

            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
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
        private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
        {
            AreaEventArgs aargs = args as AreaEventArgs;
            GamePlayer player = aargs?.GameObject as GamePlayer;

            if (player == null)
                return;

            var mobsInArea = player.GetNPCsInRadius(2500);

            if (mobsInArea == null)
                return;

            foreach (GameNPC mob in mobsInArea)
            {
                if (mob is not Legion || !mob.InCombat) continue;

                if (Util.Chance(33))
                {
                    foreach (GamePlayer nearbyPlayer in mob.GetPlayersInRadius(2500))
                    {
                        nearbyPlayer.Out.SendMessage("Legion doesn't like enemies in his lair", eChatType.CT_Broadcast,
                            eChatLoc.CL_ChatWindow);
                        nearbyPlayer.Out.SendSpellEffectAnimation(mob, player, 5933, 0, false, 1);
                    }

                    player.Die(mob);
                }
                else
                {
                    foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
                    {
                        playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                        playerNearby.BroadcastUpdate();
                    }

                    player.MoveTo(249, 48200, 49566, 20833, 1028);
                }

                player.BroadcastUpdate();
            }
        }
        private static void PlayerKilledByLegion(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null)
                return;

            DyingEventArgs eArgs = args as DyingEventArgs;

            if (eArgs?.Killer?.Name != "Legion")
                return;

            foreach (GameNPC mob in player.GetNPCsInRadius(2000))
            {
                if (mob is not Legion) continue;
                mob.Health += player.MaxHealth;
                mob.UpdateHealthManaEndu();
            }

            foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
            {
                playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                playerNearby.BroadcastUpdate();
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            //possible AttackRange
            int distance = 400;
            
            if (source is GamePlayer || source is GamePet)
            {
                if (!source.IsWithinRadius(this, distance)) //take no damage from source that is not in radius 400
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is not attackable from this range and is immune to your damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                }
                else //take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
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
                player.Achieve(AchievementUtils.AchievementNames.Legion_Kills);
                player.RaiseRealmLoyaltyFloor(1);
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
        private bool IsCreatingSouls = false;
        public LegionBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
                        if (npc.Brain is LegionAddBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if(IsCreatingSouls==false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoSpawn), Util.Random(30000, 35000));//every 30-35s it will spawn tortured souls
                    IsCreatingSouls = true;
                }
            }

            base.Think();
        }
        
        public int DoSpawn(ECSGameTimer timer)
        {

            if (Body.InCombat && Body.IsAlive && HasAggro)
            {
                foreach (GamePlayer playerNearby in Body.GetPlayersInRadius(2000))
                {
                    int spawnAmount = 0;
                    if (Util.Chance(50))
                        spawnAmount++;

                    if (Util.Chance(5))
                        spawnAmount++;
                    
                    //var spawnAmount = Util.Random(10, 15);
                    SpawnAdds(playerNearby, spawnAmount);
                }
            }
            IsCreatingSouls = false;
            return 0;
        }
        
        private void SpawnAdds(GamePlayer target, int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                //var distanceDelta = Util.Random(0, 300);
                var level = Util.Random(52, 58);

                LegionAdd add = new LegionAdd();
                /*
                 add.X = target.X + distanceDelta;
                add.Y = target.Y + distanceDelta;
                add.Z = target.Z;
                add.CurrentRegionID = target.CurrentRegionID;
                */
                add.X = 45092;
                add.Y = 51689;
                add.Z = 15468;
                add.CurrentRegionID = 249;
                add.IsWorthReward = false;
                add.Level = (byte) level;
                add.AddToWorld();
                add.StartAttack(target);
            }
        }
    }
}

namespace DOL.GS
{
    public class LegionAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            get { return 1500; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 250;
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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
        }

        public override void Think()
        {
            if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                Body.RemoveFromWorld();
            }
        }
    }
}