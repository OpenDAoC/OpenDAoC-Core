using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.GS
{
    public abstract class DragonMinion : GameNPC
    {
        protected DragonConfig Config { get; }

        public DragonBrain OwnerBrain { get; set; }
        public int PathIndex { get; set; }

        protected DragonMinion(DragonConfig config)
        {
            Config = config;
        }

        public override bool IsVisibleToPlayers => true;
        public override bool CanDropLoot => false;
        public override long ExperienceValue => 0;
        public override double GetArmorAF(eArmorSlot slot) => 200;

        public override bool AddToWorld()
        {
            Realm = eRealm.None;
            Faction = FactionMgr.GetFactionByID(Config.FactionId);
            RespawnInterval = -1;
            LoadedFromScript = true;

            if (!base.AddToWorld())
                return false;

            OwnerBrain?.RegisterAdd(this);
            return true;
        }
    }

    public abstract class DragonMessenger : DragonMinion
    {
        public const short MESSENGER_SPEED = 225;

        protected DragonMessenger(DragonConfig config) : base(config) { }

        public override int MaxHealth => 1500;
        public override double GetArmorAbsorb(eArmorSlot slot) => 0.10;
        public override void ReturnToSpawnPoint(short speed) { }
        public override void StartAttack(GameObject target) { }

        public override int GetResist(eDamageType damageType)
        {
            return damageType switch
            {
                eDamageType.Slash or eDamageType.Crush or eDamageType.Thrust => 10,
                _ => 20
            };
        }

        public override bool AddToWorld()
        {
            Model = Config.MessengerModel;
            Name = Config.MessengerName;
            Size = Config.MessengerSize;
            Level = (byte) Util.Random(50, 55);
            MaxSpeedBase = MESSENGER_SPEED;
            SetOwnBrain(new DragonMessengerBrain { RaidEncounter = OwnerBrain?.RaidEncounter });

            if (!base.AddToWorld())
                return false;

            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Once, MESSENGER_SPEED, Config.MessengerPaths[PathIndex]);
            return true;
        }

        public void SpawnAdds()
        {
            int count = Util.Random(4, 6);

            for (int i = 0; i < count; i++)
            {
                DragonAdd add = Config.CreateAdd(PathIndex);
                add.OwnerBrain = OwnerBrain;
                add.X = X + Util.Random(-200, 200);
                add.Y = Y + Util.Random(-200, 200);
                add.Z = Z;
                add.Heading = Heading;
                add.CurrentRegion = CurrentRegion;

                if (!add.AddToWorld())
                    continue;

                foreach (GamePlayer player in add.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(add, add, (ushort) Config.WaveEffect, 0, false, 1);
            }
        }
    }

    public abstract class DragonAdd : DragonMinion
    {
        public const short ADD_MAX_SPEED = 225;
        public const short ADD_RETURN_SPEED = 200;

        protected DragonAdd(DragonConfig config) : base(config) { }

        public override int MaxHealth => 5000;
        public override int GetResist(eDamageType damageType) => 20;
        public override double GetArmorAbsorb(eArmorSlot slot) => 0.15;

        public override bool AddToWorld()
        {
            DragonConfig.AddVariant variant = Config.AddVariants[Util.Random(Config.AddVariants.Length - 1)];
            Name = variant.Name;
            Model = variant.Model;
            Size = (byte) Util.Random(variant.MinSize, variant.MaxSize);
            Level = (byte) Util.Random(60, 64);
            Strength = 120;
            Quickness = 80;
            MaxSpeedBase = ADD_MAX_SPEED;
            SetOwnBrain(new StandardMobBrain { AggroLevel = 100, AggroRange = 1000, ThinkInterval = 1500, RaidEncounter = OwnerBrain?.RaidEncounter });

            if (!base.AddToWorld())
                return false;

            (int X, int Y, int Z)[] returnPath = Config.AddReturnPaths[PathIndex];
            (int X, int Y, int Z) lastPoint = returnPath[^1];
            SpawnPoint = new Point3D(lastPoint.X, lastPoint.Y, lastPoint.Z);
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Once, ADD_RETURN_SPEED, returnPath);
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class DragonMessengerBrain : StandardMobBrain
    {
        public DragonMessengerBrain()
        {
            AggroLevel = 0;
            AggroRange = 0;
        }

        public override void OnAttackedByEnemy(AttackData ad) { }

        public override bool OnPathPointReached(PathPoint pathPoint)
        {
            if (pathPoint.Next != null || Body is not DragonMessenger messenger)
                return false;

            messenger.SpawnAdds();
            _ = new ECSGameTimer(Body, DespawnMessenger, 1000);
            return true;
        }

        private int DespawnMessenger(ECSGameTimer timer)
        {
            if (Body.IsAlive)
                Body.RemoveFromWorld();

            return 0;
        }
    }
}
