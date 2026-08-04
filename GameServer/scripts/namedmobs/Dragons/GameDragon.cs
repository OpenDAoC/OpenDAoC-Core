using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public abstract class GameDragon : GameEpicBoss
    {
        public const ushort LAIR_RADIUS = 2000;

        protected abstract DragonConfig Config { get; }

        private bool _lairAreaAdded;

        public override bool IsVisibleToPlayers => true;
        public override int MeleeAttackRange => 350;
        public override int MaxHealth => (int) (Config.BaseMaxHealth * RaidEncounterHealthScalingFactor);
        public override double GetArmorAbsorb(eArmorSlot slot) => 0.25;
        public override double GetArmorAF(eArmorSlot slot)
        {
            double factor = (Brain as StandardMobBrain)?.RaidEncounter is { Active: true } raidEncounter
                ? raidEncounter.CalculateArmorFactorScalingFactor(DefaultArmorFactorScalingFactor, raidEncounter.GetActiveAttackerCount())
                : ArmorFactorScalingFactor;
            return 350 * factor / DefaultArmorFactorScalingFactor;
        }
        public override ushort SpawnHeading { get => Config.SpawnHeading; set { } }
        public override Point3D SpawnPoint { get => Config.SpawnPoint; set { } }

        public override int GetResist(eDamageType damageType)
        {
            return damageType switch
            {
                eDamageType.Slash or eDamageType.Crush or eDamageType.Thrust => 40,
                _ => 70
            };
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.DamageImmunity && !this.IsWithinRadius(SpawnPoint, LAIR_RADIUS))
                return true;

            return base.HasAbility(keyName);
        }

        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            if (_lairAreaAdded)
                return;

            _lairAreaAdded = true;
            Region region = WorldMgr.GetRegion(CurrentRegionID);
            string lairName = $"{Name.Split(' ')[0]}'s Lair";

            foreach (IArea area in region.GetAreasOfSpot(Config.SpawnPoint))
            {
                if (area is Area.Circle circle && circle.Description == lairName)
                    return;
            }

            region.AddArea(new Area.Circle(lairName, Config.SpawnPoint.X, Config.SpawnPoint.Y, 0, LAIR_RADIUS + 200));
        }

        public override bool AddToWorld()
        {
            NpcTemplate template = NpcTemplateMgr.GetTemplate(Config.NpcTemplateId);
            Faction = FactionMgr.GetFactionByID(Config.FactionId);

            if (template == null || Faction == null || Config.MessengerPaths.Length != Config.AddReturnPaths.Length)
            {
                log.Error($"{GetType().Name} has an invalid config: template {Config.NpcTemplateId} {(template == null ? "missing" : "found")}, " +
                    $"faction {Config.FactionId} {(Faction == null ? "missing" : "found")}, " +
                    $"{Config.MessengerPaths.Length} messenger path(s) vs {Config.AddReturnPaths.Length} add return path(s).");
                return false;
            }

            LoadTemplate(template);
            MeleeDamageType = Config.MeleeDamageType;
            TetherRange = LAIR_RADIUS;
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;
            DragonBrain brain = new(Config);

            brain.RaidEncounter = new(brain)
            {
                BountyPointsReward = Config.BountyPointsReward,
                CurrencyItemTemplateId = Config.CurrencyItemTemplateId,
                CurrencyItemCount = Config.CurrencyItemCount
            };

            SetOwnBrain(brain);

            LoadedFromScript = false;
            SaveIntoDatabase();
            return base.AddToWorld();
        }

        public override void EnemyKilled(GameLiving enemy)
        {
            if (enemy is GamePlayer player)
                Message.MessageToZone(CurrentZone, string.Format(Config.EnemyKilledTaunt, Name, player.CharacterClass.Name), eChatType.CT_Say, eChatLoc.CL_ChatWindow);

            base.EnemyKilled(enemy);
        }

        public override void ProcessDeath(GameObject killer)
        {
            RaidEncounter encounter = (Brain as StandardMobBrain)?.RaidEncounter;
            bool canReportNews = true;
            List<GamePlayer> participants = new();

            void Credit(GamePlayer player)
            {
                if (player.Client.Account.PrivLevel != (int) ePrivLevel.Player)
                    return;

                player.Notify(GameLivingEvent.EnemyKilled, player, new EnemyKilledEventArgs(this));
                player.KillsDragon++;
            }

            if (encounter is { Active: true })
            {
                foreach (GamePlayer player in encounter.GrantToParticipants(Credit))
                {
                    if (player.Client.Account.PrivLevel != (int) ePrivLevel.Player)
                        continue;

                    participants.Add(player);

                    if (!GameServer.ServerRules.CanGenerateNews(player))
                        canReportNews = false;
                }
            }
            else
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    Credit(player);

                    if (player.Client.Account.PrivLevel != (int) ePrivLevel.Player)
                        continue;

                    participants.Add(player);

                    if (!GameServer.ServerRules.CanGenerateNews(player))
                        canReportNews = false;
                }
            }

            base.ProcessDeath(killer);
            encounter?.Clear();

            foreach (string announce in Config.DeathAnnounces)
                Message.MessageToArea(this, string.Format(announce, Name), eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);

            if (canReportNews && killer != null)
                ReportNews(killer, participants);
        }

        private void ReportNews(GameObject killer, List<GamePlayer> participants)
        {
            NewsMgr.CreateNews($"{Name} has been slain by a force of {participants.Count} warriors!", killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_DRAGON_KILL <= 0)
                return;

            foreach (GamePlayer player in participants)
            {
                if (player.IsEligibleToGiveMeritPoints)
                    GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
            }
        }
    }
}
