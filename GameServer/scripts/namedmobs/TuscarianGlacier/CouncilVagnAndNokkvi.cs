using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class Vagn : GameEpicBoss
    {
        public Vagn() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 20;// dmg reduction for melee dmg
                case eDamageType.Crush: return 20;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
                default: return 90;// dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
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
            get { return 200000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159453);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            VagnBrain sbrain = new VagnBrain();
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
    public class VagnBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public VagnBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool IsPulled = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                foreach (GameNPC Nokkvi in Body.GetNPCsInRadius(1800))
                {
                    if (Nokkvi != null && Nokkvi.IsAlive && Nokkvi.Brain is NokkviBrain)
                        AddAggroListTo(Nokkvi.Brain as NokkviBrain);
                }
                IsPulled = true;
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (HasAggro && Body.TargetObject != null)
            {
                Body.CastSpell(VagnDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
            }
            base.Think();
        }
        private Spell m_VagnDD;
        public Spell VagnDD
        {
            get
            {
                if (m_VagnDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.Power = 0;
                    spell.RecastDelay = Util.Random(10,15);
                    spell.ClientEffect = 4075;
                    spell.Icon = 4075;
                    spell.Damage = 600;
                    spell.DamageType = (int)eDamageType.Cold;
                    spell.Name = "Frost Shock";
                    spell.Range = 1500;
                    spell.SpellID = 11927;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_VagnDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagnDD);
                }
                return m_VagnDD;
            }
        }
    }
}

////////////////////////////////////////////////////////////Council Nokkvi///////////////////////////////////////////////////////////
namespace DOL.GS
{
    public class Nokkvi : GameEpicBoss
    {
        public Nokkvi() : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 90;// dmg reduction for melee dmg
                case eDamageType.Crush: return 90;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 90;// dmg reduction for melee dmg
                default: return 20;// dmg reduction for rest resists
            }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
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
            get { return 200000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159450);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            NokkviBrain sbrain = new NokkviBrain();
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
    public class NokkviBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public NokkviBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool IsPulled2 = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled2 == false)
            {
                foreach (GameNPC Vagn in Body.GetNPCsInRadius(1800))
                {
                    if (Vagn != null && Vagn.IsAlive && Vagn.Brain is VagnBrain)
                        AddAggroListTo(Vagn.Brain as VagnBrain);
                }
                IsPulled2 = true;
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled2 = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}