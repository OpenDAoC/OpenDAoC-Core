using System;
using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class Parthanan : TimeDependentSpawnNpc
    {
        public Parthanan() : base(new ParthananBrain()) { }

        public override void ProcessDeath(GameObject killer)
        {
            ParthananFarmRegistry.GetByTrashPackageId(PackageID)?.OnTrashDeath(this);
            base.ProcessDeath(killer);
        }
    }

    public class AmalgamateParthanan : GameNPC
    {
        public override int MaxHealth => 3000;

        private ParthananFarmState FarmState => ParthananFarmRegistry.GetByBossPackageId(PackageID);

        public AmalgamateParthanan() : base(new AmalgamateParthananBrain())
        {
            const int TEMPLATE_ID = 60157792;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(TEMPLATE_ID);
            LoadTemplate(npcTemplate);
            LoadedFromScript = true;
            RespawnInterval = -1;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is not GameLiving)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                return;
            }

            bool immuneDamageType = damageType is
                eDamageType.Body or
                eDamageType.Cold or
                eDamageType.Energy or
                eDamageType.Heat or
                eDamageType.Matter or
                eDamageType.Spirit;

            if (!immuneDamageType)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                return;
            }

            if (source is not GamePlayer player)
                player = (source as GameSummonedPet)?.Owner as GamePlayer;

            player?.Out.SendMessage($"{Name} is immune to this form of attack!", eChatType.CT_SpellResisted, eChatLoc.CL_ChatWindow);
            base.TakeDamage(source, damageType, 0, 0);
            return;
        }


        public override void StartAttack(GameObject target)
        {
            ParthananFarmState state = FarmState;

            if (state != null && state.BossIsImmuneToDamage)
                return;

            base.StartAttack(target);
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            if (IsAlive && keyName == GS.Abilities.DamageImmunity)
            {
                ParthananFarmState state = FarmState;

                if (state != null && state.BossIsImmuneToDamage)
                    return true;
            }

            return base.HasAbility(keyName);
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            _ = new ECSGameTimer(this, RitualEffectLoop, 500);
            return true;
        }

        public override void ProcessDeath(GameObject killer)
        {
            ParthananFarmRegistry.GetByBossPackageId(PackageID)?.OnBossDeath();
            base.ProcessDeath(killer);
        }

        private int RitualEffectLoop(ECSGameTimer timer)
        {
            if (!IsAlive)
                return 0;

            ParthananFarmState state = FarmState;

            if (state != null && state.BossIsImmuneToDamage)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellCastAnimation(this, 2909, 1);

                return 1500;
            }
            else
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player?.Out.SendSpellEffectAnimation(this, this, 6159, 0, false, 0x01);
            }

            return 0;
        }
    }

    public class ParthananFarmController : GameNPC
    {
        public override bool IsVisibleToPlayers => true;

        public ParthananFarmController() : base(new ParthananFarmControllerBrain()) { }
    }
}

namespace DOL.AI.Brain
{
    public sealed class ParthananFarmConfig
    {
        public string FarmId { get; init; }
        public string TrashPackageId { get; init; }
        public string BossPackageId { get; init; }
        public int MinKillsToSacrifice { get; init; } = 60;
        public int MaxKillsToSacrifice { get; init; } = 120;
        public int MinionsRequiredForSacrifice { get; init; } = 5;
        public ushort GatherScanRadius { get; init; } = 3000;
        public ushort BossDuplicateCheckRadius { get; init; } = 8000;
    }

    public static class ParthananFarmDefinitions
    {
        public static readonly IReadOnlyList<ParthananFarmConfig> Configs =
        [
            new()
            {
                FarmId = "ParthananFarmConnacht",
                TrashPackageId = "ParthananConnacht",
                BossPackageId = "ParthananBossConnacht",
                MinKillsToSacrifice = 60,
                MaxKillsToSacrifice = 120,
            },
            new()
            {
                FarmId = "ParthananFarmLoughDerg",
                TrashPackageId = "ParthananLoughDerg",
                BossPackageId = "ParthananBossLoughDerg",
                MinKillsToSacrifice = 60,
                MaxKillsToSacrifice = 120,
            },
            new()
            {
                FarmId = "ParthananFarmLoughGur",
                TrashPackageId = "ParthananLoughGur",
                BossPackageId = "ParthananBossLoughGur",
                MinKillsToSacrifice = 60,
                MaxKillsToSacrifice = 120,
            }
        ];

        public static ParthananFarmConfig GetByFarmId(string farmId)
        {
            if (string.IsNullOrEmpty(farmId))
                return null;

            foreach (ParthananFarmConfig config in Configs)
            {
                if (string.Equals(config.FarmId, farmId, StringComparison.OrdinalIgnoreCase))
                    return config;
            }

            return null;
        }
    }

    public sealed class ParthananFarmState
    {
        private readonly Lock _lock = new();
        private readonly HashSet<Parthanan> _gatheringMinions = new();

        private int _killCount;
        private int _sacrificeThreshold;
        private bool _canMarchAndDie;
        private bool _bossSpawned;

        private volatile bool _trashVisible = true;
        private volatile bool _sacrificing;

        public ParthananFarmConfig Config { get; }
        public GameNPC ControllerBody { get; }
        public ParthananFarmPhase Phase { get; private set; }

        public bool TrashIsVisible => _trashVisible;
        public bool BossIsImmuneToDamage => _sacrificing;

        public ParthananFarmState(ParthananFarmConfig config, GameNPC controllerBody)
        {
            Config = config;
            ControllerBody = controllerBody;
            ResetCycleUnsafe();
        }

        public bool ShouldMarchAndDie(Parthanan npc)
        {
            lock (_lock)
            {
                return _canMarchAndDie && _gatheringMinions.Contains(npc);
            }
        }

        private void ResetCycleUnsafe()
        {
            _killCount = 0;
            _sacrificeThreshold = Util.Random(Config.MinKillsToSacrifice, Config.MaxKillsToSacrifice);
            Phase = ParthananFarmPhase.Farming;
            _canMarchAndDie = false;
            _bossSpawned = false;
            _gatheringMinions.Clear();
            _trashVisible = true;
            _sacrificing = false;
        }

        public void OnTrashDeath(Parthanan npc)
        {
            lock (_lock)
            {
                if (Phase is ParthananFarmPhase.Farming)
                {
                    _killCount++;

                    if (_killCount >= _sacrificeThreshold)
                    {
                        Phase = ParthananFarmPhase.Sacrificing;
                        _sacrificing = true;
                    }
                }
                else if (Phase is ParthananFarmPhase.Sacrificing)
                {
                    if (_gatheringMinions.Remove(npc) && _gatheringMinions.Count == 0 && _bossSpawned)
                    {
                        Phase = ParthananFarmPhase.BossActive;
                        _canMarchAndDie = false;
                        _sacrificing = false;
                        _trashVisible = false;
                    }
                }
            }
        }

        public void ScanForMinions()
        {
            bool needsBossSpawn = false;

            lock (_lock)
            {
                if (Phase is not ParthananFarmPhase.Sacrificing)
                    return;

                foreach (GameNPC npc in ControllerBody.GetNPCsInRadius(Config.GatherScanRadius))
                {
                    if (npc is Parthanan trash && trash.IsAlive && trash.PackageID == Config.TrashPackageId)
                        _gatheringMinions.Add(trash);
                }

                if (_gatheringMinions.Count >= Config.MinionsRequiredForSacrifice)
                {
                    _canMarchAndDie = true;

                    if (!_bossSpawned)
                    {
                        _bossSpawned = true;
                        needsBossSpawn = true;
                    }
                }
            }

            if (needsBossSpawn)
                SpawnBoss();
        }

        private void SpawnBoss()
        {
            foreach (GameNPC npc in ControllerBody.GetNPCsInRadius(Config.BossDuplicateCheckRadius))
            {
                if (npc is AmalgamateParthanan existing && existing.PackageID == Config.BossPackageId)
                    return;
            }

            new AmalgamateParthanan()
            {
                X = ControllerBody.X,
                Y = ControllerBody.Y,
                Z = ControllerBody.Z,
                Heading = ControllerBody.Heading,
                CurrentRegion = ControllerBody.CurrentRegion,
                PackageID = Config.BossPackageId
            }.AddToWorld();
        }

        public void OnBossDeath()
        {
            lock (_lock)
            {
                ResetCycleUnsafe();
            }
        }
    }

    public static class ParthananFarmRegistry
    {
        private static readonly Dictionary<string, ParthananFarmState> ByTrashPackageId = new();
        private static readonly Dictionary<string, ParthananFarmState> ByBossPackageId = new();
        private static readonly ReaderWriterLockSlim _lock = new();

        public static void Register(ParthananFarmState state)
        {
            _lock.EnterWriteLock();

            try
            {
                ByTrashPackageId[state.Config.TrashPackageId] = state;
                ByBossPackageId[state.Config.BossPackageId] = state;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public static ParthananFarmState GetByTrashPackageId(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
                return null;

            _lock.EnterReadLock();

            try
            {
                return ByTrashPackageId.TryGetValue(packageId, out ParthananFarmState state) ? state : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public static ParthananFarmState GetByBossPackageId(string packageId)
        {
            if (string.IsNullOrEmpty(packageId))
                return null;

            _lock.EnterReadLock();

            try
            {
                return ByBossPackageId.TryGetValue(packageId, out ParthananFarmState state) ? state : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public class ParthananBrain : TimeDependentSpawnBrain
    {
        private ParthananFarmState FarmState => ParthananFarmRegistry.GetByTrashPackageId(Body.PackageID);

        public override void Think()
        {
            if (Body.IsAlive && !Body.IsControlledNPC(Body))
            {
                ParthananFarmState state = FarmState;

                if (state != null && Body is Parthanan self && state.ShouldMarchAndDie(self))
                {
                    GameNPC controller = state.ControllerBody;

                    if (!Body.IsWithinRadius(controller, 50))
                        Body.PathTo(controller, Body.MaxSpeedBase);
                    else
                        Body.Die(controller);
                }
            }

            base.Think();
        }

        protected override bool ShouldBeVisible()
        {
            ParthananFarmState state = FarmState;
            return state == null || state.TrashIsVisible;
        }

        public override void AttackMostWanted()
        {
            ParthananFarmState state = FarmState;

            if (state != null && Body is Parthanan self && state.ShouldMarchAndDie(self))
                return;

            base.AttackMostWanted();
        }
    }

    public class AmalgamateParthananBrain : StandardMobBrain
    {
        public AmalgamateParthananBrain()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 1500;
        }
    }

    public class ParthananFarmControllerBrain : APlayerVicinityBrain
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ParthananFarmState _farmState;
        private bool _initFailed;

        public override void Think()
        {
            if (_farmState == null && !_initFailed)
                InitializeFarmState();

            if (_farmState != null && _farmState.Phase is ParthananFarmPhase.Sacrificing)
                _farmState.ScanForMinions();
        }

        private void InitializeFarmState()
        {
            ParthananFarmConfig config = ParthananFarmDefinitions.GetByFarmId(Body.PackageID);

            if (config == null)
            {
                string knownIds = string.Empty;

                foreach (ParthananFarmConfig known in ParthananFarmDefinitions.Configs)
                    knownIds = $"{knownIds}{(knownIds.Length > 0 ? ", " : string.Empty)}{known.FarmId}";

                if (log.IsErrorEnabled)
                    log.Error($"ParthananFarmController '{Body.Name}' has an unrecognized PackageID ('{Body.PackageID}'). Set its PackageID to one of: {knownIds}. This farm will not function until that's fixed.");

                _initFailed = true;
                return;
            }

            _farmState = new(config, Body);
            ParthananFarmRegistry.Register(_farmState);
        }
    }

    public enum ParthananFarmPhase
    {
        // Trash is being farmed by players; kills count toward the sacrifice threshold.
        Farming,
        // Threshold reached: minions are gathering at the controller and will self-sacrifice;
        // the boss is spawned but immune while this is in progress.
        Sacrificing,
        // All gathered minions are dead: the boss is vulnerable and fighting, trash is hidden.
        BossActive
    }
}
