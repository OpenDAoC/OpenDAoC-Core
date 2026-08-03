using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Xaga : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Xaga()
            : base()
        {
        }
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            // Only players and their pets can damage Xaga.
            if (source is not GamePlayer and not GameSummonedPet)
                return;

            // Take no damage while too far away from the spawn point.
            if (IsOutOfTetherRange)
            {
                GamePlayer player = source as GamePlayer ?? (source as GameSummonedPet).Owner as GamePlayer;
                player?.Out.SendMessage($"{Name} is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }

            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        public override int MaxHealth
        {
            get { return 100000; }
        }
        public override int MeleeAttackRange => 450;
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
        public Tine Tine { get; private set; }
        public Beatha Beatha { get; private set; }

        private void SpawnTineBeatha()
        {
            if (Tine == null || !Tine.IsAlive)
            {
                Tine = new Tine
                {
                    X = 27211,
                    Y = 54902,
                    Z = 13213,
                    CurrentRegion = CurrentRegion,
                    Heading = 2157,
                    RespawnInterval = -1,
                    Xaga = this
                };
                Tine.AddToWorld();
            }
            if (Beatha == null || !Beatha.IsAlive)
            {
                Beatha = new Beatha
                {
                    X = 27614,
                    Y = 54866,
                    Z = 13213,
                    CurrentRegion = CurrentRegion,
                    Heading = 2038,
                    RespawnInterval = -1,
                    Xaga = this
                };
                Beatha.AddToWorld();
            }
        }
        public override void Die(GameObject killer)
        {
            if (Tine != null && Tine.IsAlive)
                Tine.Die(Tine);
            if (Beatha != null && Beatha.IsAlive)
                Beatha.Die(Beatha);
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
            LoadTemplate(npcTemplate);

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            XagaBrain sBrain = new XagaBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            LoadedFromScript = false;
            bool success = base.AddToWorld();
            if (success)
                SpawnTineBeatha();
            return success;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Xaga", 191, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Xaga not found, creating it...");

                log.Warn("Initializing Xaga...");
                Xaga SB = new Xaga();
                SB.Name = "Xaga";
                SB.Model = 917;
                SB.Realm = 0;
                SB.Level = 81;
                SB.Size = 250;
                SB.CurrentRegionID = 191; //galladoria

                SB.Strength = 260;
                SB.Intelligence = 220;
                SB.Piety = 220;
                SB.Dexterity = 200;
                SB.Constitution = 200;
                SB.Quickness = 125;
                SB.BodyType = 5;
                SB.MeleeDamageType = eDamageType.Slash;
                SB.Faction = FactionMgr.GetFactionByID(96);

                SB.X = 27397;
                SB.Y = 54975;
                SB.Z = 12949;
                SB.TetherRange = 2500;
                SB.MaxSpeedBase = 300;
                SB.Heading = 2013;

                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
                SB.LoadTemplate(npcTemplate);

                XagaBrain ubrain = new XagaBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                SB.SetOwnBrain(ubrain);

                SB.AddToWorld();
                SB.Brain.Start();
                SB.SaveIntoDatabase();
            }
            else
                log.Warn("Xaga exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class XagaBrain : StandardMobBrain
    {
        private bool _lightsAggroCleared;

        public XagaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            if (!HasAggro)
            {
                Body.Health = Body.MaxHealth;

                if (!_lightsAggroCleared && Body is Xaga xaga)
                {
                    _lightsAggroCleared = true;
                    ClearLightAggro(xaga.Tine);
                    ClearLightAggro(xaga.Beatha);
                }
            }
            else if (Body.TargetObject != null)
                _lightsAggroCleared = false;

            base.Think();
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive && Body is Xaga xaga)
            {
                PullLight(xaga.Tine);
                PullLight(xaga.Beatha);
            }

            base.OnAttackedByEnemy(ad);
        }

        private static void ClearLightAggro(GameNPC light)
        {
            if (light != null && light.IsAlive && light.Brain is StandardMobBrain brain && brain.HasAggro)
                brain.ClearAggroList();
        }

        private void PullLight(GameNPC light)
        {
            if (light != null && light.IsAlive && light.Brain is StandardMobBrain brain && !brain.HasAggro)
                AddAggroListTo(brain);
        }
    }
}
////////////////////////////////////////////////Beatha/////////////////////////////////////////////
#region Beatha
namespace DOL.GS
{
    public class Beatha : GameEpicBoss
    {
        public const short PATROL_SPEED = 250;

        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (27572, 54473, 13213),
            (27183, 54530, 13213),
            (27213, 55106, 13213),
            (27581, 55079, 13213)
        ];

        public Xaga Xaga { get; set; }

        public Beatha()
            : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override void DealDamage(AttackData ad)
        {
            // Beatha's damage heals Xaga.
            if (ad != null && Xaga != null && Xaga.IsAlive)
                Xaga.Health += ad.Damage * 2;

            base.DealDamage(ad);
        }
        public override int MaxHealth
        {
            get { return 50000; }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158330);
            LoadTemplate(npcTemplate);

            Flags = eFlags.FLYING;
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            AbilityBonus[eProperty.Resist_Body] = 60;
            AbilityBonus[eProperty.Resist_Heat] = -20;//weak to heat
            AbilityBonus[eProperty.Resist_Cold] = 99;//resi to cold
            AbilityBonus[eProperty.Resist_Matter] = 60;
            AbilityBonus[eProperty.Resist_Energy] = 60;
            AbilityBonus[eProperty.Resist_Spirit] = 60;
            AbilityBonus[eProperty.Resist_Slash] = 40;
            AbilityBonus[eProperty.Resist_Crush] = 40;
            AbilityBonus[eProperty.Resist_Thrust] = 40;

            Faction = FactionMgr.GetFactionByID(96);
            BeathaBrain sBrain = new BeathaBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }
    }
}
namespace DOL.AI.Brain
{
    public class BeathaBrain : StandardMobBrain
    {
        public BeathaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive && Body is Beatha beatha && beatha.Xaga != null)
            {
                PullFriend(beatha.Xaga);
                PullFriend(beatha.Xaga.Tine);
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            // Beatha never attacks directly and endlessly circles the room, even while in combat.
            // The aggro state stops path movement, so restart it whenever it's interrupted.
            if (Body.IsAlive && !Body.IsMovingOnPath)
                Body.MoveOnPath(Beatha.PATROL_SPEED);

            if (!HasAggro)
                Body.Health = Body.MaxHealth;
            else if (Body.IsAlive && Body.TargetObject is GameLiving target)
            {
                Body.SetGroundTarget(target.X, target.Y, target.Z);
                Body.CastSpell(BeathaAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }

            base.Think();
        }

        private void PullFriend(GameNPC friend)
        {
            if (friend != null && friend.IsAlive && friend.IsAvailableToJoinFight && friend.Brain is StandardMobBrain brain)
                AddAggroListTo(brain);
        }

        private static Spell BeathaAoe => ScriptSpells.GetOrCreate("beatha-aoe", 70, static db =>
        {
            db.CastTime = 0;
            db.RecastDelay = Util.Random(4, 8);
            db.ClientEffect = 4568;
            db.Icon = 4568;
            db.Damage = 450;
            db.Name = "Beatha's Void";
            db.TooltipId = 4568;
            db.Range = 3000;
            db.Radius = 450;
            db.SpellID = 11707;
            db.Target = eSpellTarget.AREA.ToString();
            db.Type = eSpellType.DirectDamageNoVariance.ToString();
            db.Uninterruptible = true;
            db.MoveCast = true;
            db.DamageType = (int) eDamageType.Cold;
        });
    }
}
#endregion
/////////////////////Tine///////////////
#region Tine
namespace DOL.GS
{
    public class Tine : GameEpicBoss
    {
        public const short PATROL_SPEED = 250;

        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (27168, 54598, 13213),
            (27597, 54579, 13213),
            (27606, 55086, 13213),
            (27208, 55133, 13213)
        ];

        public Xaga Xaga { get; set; }

        public Tine()
            : base()
        {
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override int MaxHealth
        {
            get { return 50000; }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167084);
            LoadTemplate(npcTemplate);

            Faction = FactionMgr.GetFactionByID(96);
            Flags = eFlags.FLYING;
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            AbilityBonus[eProperty.Resist_Body] = 60;
            AbilityBonus[eProperty.Resist_Heat] = 99;//resi to heat
            AbilityBonus[eProperty.Resist_Cold] = -20;//weak to cold
            AbilityBonus[eProperty.Resist_Matter] = 60;
            AbilityBonus[eProperty.Resist_Energy] = 60;
            AbilityBonus[eProperty.Resist_Spirit] = 60;
            AbilityBonus[eProperty.Resist_Slash] = 40;
            AbilityBonus[eProperty.Resist_Crush] = 40;
            AbilityBonus[eProperty.Resist_Thrust] = 40;

            TineBrain sBrain = new TineBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }
    }
}
namespace DOL.AI.Brain
{
    public class TineBrain : StandardMobBrain
    {
        public TineBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive && Body is Tine tine && tine.Xaga != null)
            {
                PullFriend(tine.Xaga);
                PullFriend(tine.Xaga.Beatha);
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            // Tine never attacks directly and endlessly circles the room, even while in combat.
            // The aggro state stops path movement, so restart it whenever it's interrupted.
            if (Body.IsAlive && !Body.IsMovingOnPath)
                Body.MoveOnPath(Tine.PATROL_SPEED);

            if (!HasAggro)
                Body.Health = Body.MaxHealth;
            else if (Body.IsAlive && Body.TargetObject is GameLiving target)
            {
                Body.SetGroundTarget(target.X, target.Y, target.Z);
                Body.CastSpell(TineAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }

            base.Think();
        }

        private void PullFriend(GameNPC friend)
        {
            if (friend != null && friend.IsAlive && friend.IsAvailableToJoinFight && friend.Brain is StandardMobBrain brain)
                AddAggroListTo(brain);
        }

        private static Spell TineAoe => ScriptSpells.GetOrCreate("tine-aoe", 70, static db =>
        {
            db.CastTime = 0;
            db.RecastDelay = Util.Random(4, 8);
            db.ClientEffect = 4227;
            db.Icon = 4227;
            db.Damage = 450;
            db.Name = "Tine's Fire";
            db.TooltipId = 4227;
            db.Range = 3000;
            db.Radius = 450;
            db.SpellID = 11708;
            db.Target = eSpellTarget.AREA.ToString();
            db.Type = eSpellType.DirectDamageNoVariance.ToString();
            db.Uninterruptible = true;
            db.MoveCast = true;
            db.DamageType = (int) eDamageType.Heat;
        });
    }
}
#endregion