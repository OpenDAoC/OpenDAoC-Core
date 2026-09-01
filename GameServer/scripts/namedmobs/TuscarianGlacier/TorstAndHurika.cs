using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;

#region Torst
namespace DOL.GS
{
    public class Torst : GameEpicBoss
    {
        public Torst() : base()
        {
        }

        private const short PATROL_SPEED = 200;

        // Flying patrol route, starting at the spawn point.
        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (50897, 36006, 16659),
            (51166, 37442, 17331),
            (53201, 39956, 16314),
            (55178, 38616, 17901),
            (54852, 36185, 17859),
            (53701, 35635, 17859),
            (52118, 36114, 17265)
        ];

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }

        public override int MeleeAttackRange => 350;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 350; }
        #endregion
        public override bool AddToWorld()
        {
            Name = "Torst";
            Level = 80;
            Size = 90;
            Model = 696;
            Faction = FactionMgr.GetFactionByID(140);
            MaxSpeedBase = 250;
            Flags = eFlags.FLYING;
            RespawnInterval =ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            TorstBrain sbrain = new TorstBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        public override void ProcessDeath(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                    npc.RemoveFromWorld();
            }
            base.ProcessDeath(killer);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (Util.Chance(20))
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                    CastSpell(TorstDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackEnemy(ad);
        }
        public Spell m_TorstDD;
        public Spell TorstDD
        {
            get
            {
                if (m_TorstDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(25, 45);
                    spell.ClientEffect = 228;
                    spell.Icon = 208;
                    spell.TooltipId = 479;
                    spell.Damage = 550;
                    spell.Range = 500;
                    spell.Radius = 400;
                    spell.SpellID = 11743;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_TorstDD = new Spell(spell, 70);
                }
                return m_TorstDD;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class TorstBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TorstBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }

        public List<GamePlayer> PlayersToAttack = new List<GamePlayer>();

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        private bool SpawnEddies = false;
        private bool RemoveAdds = false;
        public override void Think()
        {
            if (CheckProximityAggro() && Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0; //dont fly
            }

            if (!CheckProximityAggro())
            {
                Body.Health = Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING; //fly
                SpawnEddies = false;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
                        if (npc != null && npc.IsAlive && npc.Brain is TorstEddiesBrain)
                            npc.RemoveFromWorld();
                    }
                    RemoveAdds = true;
                }
            }

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                if (!SpawnEddies)
                {
                    CreateEddies();
                    SpawnEddies = true;
                }
                foreach (GamePlayer gamePlayer in Body.GetPlayersInRadius(1500))
                {
                    if (gamePlayer != null && gamePlayer.IsAlive && gamePlayer.Client.Account.PrivLevel == 1)
                    {
                        if (!PlayersToAttack.Contains(gamePlayer))
                            PlayersToAttack.Add(gamePlayer);
                    }
                }

                PickNotRottedTarget();

                if (Util.Chance(10))
                    Body.CastSpell(TorstRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        private void CreateEddies()
        {
            BroadcastMessage(String.Format("{0}'s powerful wings stir swirling eddies of air that threaten to freeze anyone caught in their wake!",Body.Name));
            for (int i = 0; i < 5; i++)
            {
                TorstEddies add = new TorstEddies();
                add.X = Body.X + Util.Random(-200, 200);
                add.Y = Body.Y + Util.Random(-200, 200);
                add.Z = Body.Z;
                add.Heading = Body.Heading;
                add.CurrentRegion = Body.CurrentRegion;
                add.AddToWorld();
            }
        }

        public GameLiving randomtarget = null;
        public GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void PickNotRottedTarget()
        {
            if (PlayersToAttack.Count == 0)
            {
                //do nothing
            }
            else
            {
                if (Body.TargetObject != null)
                {
                    GameLiving target = Body.TargetObject as GameLiving; //mob target
                    RandomTarget = PlayersToAttack[Util.Random(0, PlayersToAttack.Count - 1)]; //mob next random target
                    if (target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff)) //if target got root
                    {
                        Body.StopAttack();
                        ClearAggroList();
                        if (RandomTarget != null && RandomTarget.IsAlive)
                        {
                            AddToAggroList(RandomTarget);
                            Body.StartAttack(RandomTarget);
                        }
                    }
                }
            }
        }
        #region Spell root
        private Spell m_TorstRoot;
        private Spell TorstRoot
        {
            get
            {
                if (m_TorstRoot == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.Duration = 60;
                    spell.Value = 99;
                    spell.Name = "Torst Root";
                    spell.TooltipId = 277;
                    spell.SpellID = 11742;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Cold;
                    m_TorstRoot = new Spell(spell, 70);
                }
                return m_TorstRoot;
            }
        }
        #endregion
    }
}
#endregion

#region Hurika
namespace DOL.GS
{
    public class Hurika : GameEpicBoss
    {
        public Hurika() : base()
        {
        }

        private const short PATROL_SPEED = 200;

        // Flying patrol route in Tuscaran Glacier.
        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (54652, 36348, 18279),
            (55113, 38549, 16679),
            (53370, 40527, 16268),
            (51711, 38978, 17130),
            (51519, 37213, 17046)
        ];

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }

        public override int MeleeAttackRange => 350;

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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

        public override void Die(GameObject killer) //on kill generate orbs
        {
            base.Die(killer);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162284);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(140);
            Flags = eFlags.FLYING;
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            if (CurrentRegionID == 160) //tuscaran glacier, mob will not roam elsewhere
                CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            HurikaBrain sbrain = new HurikaBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class HurikaBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HurikaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2000;
        }

        public List<GamePlayer> Port_Enemys = new List<GamePlayer>();
        public bool IsTargetPicked = false;
        public GamePlayer randomtarget = null;
        public GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void Think()
        {
            if (CheckProximityAggro() && Body.IsWithinRadius(Body.TargetObject, Body.attackComponent.AttackRange) && Body.InCombat)
            {
                Body.Flags = 0; //dont fly
            }

            if (!CheckProximityAggro())
            {
                Body.Health = Body.MaxHealth;
                Body.Flags = GameNPC.eFlags.FLYING; //fly
                IsTargetPicked = false;
                RandomTarget = null;
                if (Port_Enemys.Count > 0)
                    Port_Enemys.Clear();
            }

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if(HasAggro && Body.TargetObject != null)
            {
                foreach(GamePlayer player in Body.GetPlayersInRadius(1000))
                {
                    if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && !Port_Enemys.Contains(player))
                        Port_Enemys.Add(player);
                }
                if(Port_Enemys.Count > 0)
                {
                    GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                    RandomTarget = Target;
                    if (RandomTarget.IsAlive && RandomTarget != null && !IsTargetPicked)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TeleportPlayer), Util.Random(15000, 20000));
                        IsTargetPicked = true;
                    }
                }
            }

            base.Think();
        }
        private int TeleportPlayer(ECSGameTimer timer)
        {
            if (RandomTarget != null && RandomTarget.IsAlive && HasAggro && Body.IsAlive)
            {
                RandomTarget.MoveTo(Body.CurrentRegionID, Body.X, Body.Y, Body.Z + Util.Random(500, 700), Body.Heading);
                BroadcastMessage(String.Format("A powerful gust of wind generated by Hurika's wings sends {0} flying into the air!", RandomTarget.Name));
            }
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetPort), 3500);
            return 0;
        }
        private int ResetPort(ECSGameTimer timer)
        {
            RandomTarget = null;//reset random target to null
            IsTargetPicked = false;
            return 0;
        }
    }
}
#endregion

#region Torst eddies
namespace DOL.GS
{
    public class TorstEddies : GameNPC
    {
        public TorstEddies() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 15;// dmg reduction for melee dmg
                case eDamageType.Crush: return 15;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
                default: return 15;// dmg reduction for rest resists
            }
        }

        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.10;
        }
        public override void StopFollowing()
        {
            if (IsAlive)
                return;
            base.StopFollowing();
        }
        public override void Follow(GameObject target, long minDistance, long maxDistance)
        {
            if (IsAlive)
                return;
            base.Follow(target, minDistance, maxDistance);
        }
        public override void ReturnToSpawnPoint(short speed)
        {
            if (IsAlive)
                return;
            base.ReturnToSpawnPoint(speed);
        }
        public override void StartAttack(GameObject target)
        {
        }
        #region Stats
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        #endregion
        public override bool AddToWorld()
        {
            Model = 665;
            Name = "eddie";
            Level = (byte)Util.Random(55, 58);
            Size = 50;
            RespawnInterval = -1;
            Flags = (GameNPC.eFlags)44;//noname notarget flying
            Faction = FactionMgr.GetFactionByID(140);
            MaxSpeedBase = 300;

            LoadedFromScript = true;
            TorstEddiesBrain sbrain = new TorstEddiesBrain();
            SetOwnBrain(sbrain);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }

        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player?.Out.SendSpellEffectAnimation(this, this, 4168, 0, false, 0x01);

                return 1600;
            }

            return 0;
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class TorstEddiesBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TorstEddiesBrain() : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
            ThinkInterval = 1500;
        }
        private protected bool Point1check = false;
        private protected bool Point2check = false;
        bool SetNpcTarget = false;

        private protected GameNPC trostnpc = null;
        private protected GameNPC TrostNpc
        {
            get { return trostnpc; }
            set { trostnpc = value; }
        }
        public override void Think()
        {
            if (!SetNpcTarget)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is TorstBrain)
                    {
                        trostnpc = npc;
                        SetNpcTarget = true;
                    }
                }
            }

            if (TrostNpc != null && TrostNpc.IsAlive)
            {
                Point3D oldPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
                Point3D newPoint = new Point3D(TrostNpc.X + Util.Random(-200, 200), TrostNpc.Y + Util.Random(-200, 200), TrostNpc.Z + Util.Random(0, 100));
                if (!Body.IsWithinRadius(oldPoint, 20) && !Point1check)
                {
                    Body.PathTo(oldPoint, 300);
                }
                else
                {
                    Point1check = true;
                    Point2check = false;
                    if (!Body.IsWithinRadius(newPoint, 20) && Point1check && !Point2check)
                    {
                        Body.PathTo(newPoint, 300);
                    }
                    else
                    {
                        Point2check = true;
                        Point1check = false;
                    }
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                Body.CastSpell(ColdGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
            base.Think();
        }
        private Spell m_ColdGroundDD;
        private Spell ColdGroundDD
        {
            get
            {
                if (m_ColdGroundDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(5,12);
                    spell.ClientEffect = 161;
                    spell.Icon = 161;
                    spell.TooltipId = 368;
                    spell.Name = "Cold Snap";
                    spell.Damage = 110;
                    spell.Range = 200;
                    spell.Radius = 300;
                    spell.SpellID = 11926;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_ColdGroundDD = new Spell(spell, 60);
                }
                return m_ColdGroundDD;
            }
        }
    }
}
#endregion