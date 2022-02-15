using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;


namespace DOL.GS
{
    public class GiantSporiteCluster : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GiantSporiteCluster()
            : base()
        {
        }

        public virtual int COifficulty
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
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
            if(killer != null)
            {
                foreach (GameNPC npc in this.GetNPCsInRadius(8000))
                {
                    if (npc.IsAlive && npc.Brain is GSCAddBrain)
                    {
                        npc.Die(killer);
                    }
                }
            }
           base.Die(killer);
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Giant Sporite Cluster", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Giant Sporite Cluster not found, creating it...");

                log.Warn("Initializing Giant Sporite Cluster...");
                GiantSporiteCluster SB = new GiantSporiteCluster();
                SB.Name = "Giant Sporite Cluster";
                SB.Model = 906;
                SB.Realm = 0;
                SB.Level = 79;
                SB.Size = 200;
                SB.CurrentRegionID = 191;//galladoria

                SB.Strength = 550;
                SB.Intelligence = 150;
                SB.Piety = 150;
                SB.Dexterity = 200;
                SB.Constitution = 200;
                SB.Quickness = 125;
                SB.BodyType = 5;
                SB.MeleeDamageType = eDamageType.Slash;
                SB.Faction = FactionMgr.GetFactionByID(96);
                SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                SB.X = 42024;
                SB.Y = 49976;
                SB.Z = 10846;
                SB.MaxDistance = 2000;
                SB.TetherRange = 2500;
                SB.MaxSpeedBase = 300;
                SB.Heading = 2764;

                GiantSporiteClusterBrain ubrain = new GiantSporiteClusterBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                SB.SetOwnBrain(ubrain);
                SB.AddToWorld();
                SB.Brain.Start();
                SB.SaveIntoDatabase();
            }
            else
                log.Warn("Giant Sporite Cluster exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

public class GiantSporiteClusterBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GiantSporiteClusterBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public static bool spawn1 = true;
    public static bool spawn3 = true;
    public static bool spawn5 = true;
    public static bool spawn7 = true;
    public static bool spawn9 = true;

    public override void Think()
    {
        if (!HasAggressionTable())
        {
            //set state to RETURN TO SPAWN
            FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is GSCAddBrain)
                {
                    npc.RemoveFromWorld();
                    spawn1 = true;
                    spawn3 = true;
                    spawn5 = true;
                    spawn7 = true;
                    spawn9 = true;
                }
            }
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
            this.Body.Health = this.Body.MaxHealth;
            spawn1 = true;
            spawn3 = true;
            spawn5 = true;
            spawn7 = true;
            spawn9 = true;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            this.Body.Health = this.Body.MaxHealth;
        }

        if (Body.InCombat && HasAggro)
        {
            if (Util.Chance(5) && Body.TargetObject != null)
            {
                new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
            }
            if(Body.HealthPercent <91 && Body.HealthPercent>=90 && spawn1==true)
            {
                PrepareMezz();
                Spawn();
                spawn1 = false;
            }
            if (Body.HealthPercent < 71 && Body.HealthPercent >= 70 && spawn3 == true)
            {
                Spawn();
                spawn3 = false;
            }
            if (Body.HealthPercent < 51 && Body.HealthPercent >= 50 && spawn5 == true)
            {
                PrepareMezz();
                Spawn();
                spawn5 = false;
            }
            if (Body.HealthPercent < 31 && Body.HealthPercent >= 30 && spawn7 == true)
            {
                Spawn();
                spawn7 = false;
            }
            if (Body.HealthPercent < 11 && Body.HealthPercent >= 10 && spawn9 == true)
            {
                PrepareMezz();
                Spawn();
                spawn9 = false;
            }
        }
        base.Think();
    }
    public void Spawn() // We define here adds
    {
        for (int i = 0; i < Util.Random(2,4); i++)//Spawn 2 or 4 adds
        {
            GSCAdd Add = new GSCAdd();
            Add.X = Body.X + Util.Random(-50, 80);
            Add.Y = Body.Y + Util.Random(-50, 80);
            Add.Z = Body.Z;
            Add.Strength = 450;
            Add.Intelligence = 150;
            Add.Piety = 150;
            Add.Dexterity = 200;
            Add.Constitution = 200;
            Add.Quickness = 125;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.IsWorthReward = false;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }
    }

    public void PrepareMezz()
    {
        if (Mezz.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
        {
            new RegionTimer(Body, new RegionTimerCallback(CastMezz), 5000);
        }
    }
    protected virtual int CastMezz(RegionTimer timer)
    {
        Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        return 0;
    }

    public int CastAOEDD(RegionTimer timer)
    {
        Body.CastSpell(GSCAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        return 0;
    }
    private Spell m_GSCAoe;
    private Spell GSCAoe
    {
        get
        {
            if (m_GSCAoe == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 8;
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.Damage = 550;
                spell.Name = "Xaga Staff Bomb";
                spell.TooltipId = 4568;
                spell.Radius = 650;
                spell.SpellID = 11709;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)eDamageType.Cold;
                m_GSCAoe = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GSCAoe);
            }
            return m_GSCAoe;
        }
    }

    protected Spell m_mezSpell;

    /// <summary>
    /// The Mezz spell.
    /// </summary>
    protected Spell Mezz
    {
        get
        {
            if (m_mezSpell == null)
            {
                DBSpell spell = new DBSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 30;
                spell.ClientEffect = 1681;
                spell.Icon = 1685;
                spell.Damage = 0;
                spell.Name = "Mesmerized";
                spell.Range = 1500;
                spell.Radius = 600;
                spell.SpellID = 11710;
                spell.Duration = 30;
                spell.Target = "Enemy";
                spell.Type = "Mesmerize";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                m_mezSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
            }
            return m_mezSpell;
        }
    }
}
/////////////////////////////////////////////adds//////////////////////////////

namespace DOL.GS
{
    public class GSCAdd : GameNPC
    {

        public GSCAdd() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        public override int MaxHealth
        {
            get
            {
                return 8000;
            }
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
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null);//null to not gain experience
        }
        public override bool AddToWorld()
        {
            Model = 906;
            Name = "Giant Sporite Cluster";
            IsWorthReward = false;
            MeleeDamageType = eDamageType.Slash;
            RespawnInterval = -1;
            Strength = 550;
            Intelligence =150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            BodyType = 5;
            Size = 150;
            Level = 79;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Realm = eRealm.None;
            GSCAddBrain adds = new GSCAddBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class GSCAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public GSCAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 800;
        }

        public override void Think()
        {
            Body.IsWorthReward = false;
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(5) && Body.TargetObject != null)
                {
                    new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
                }
            }
          base.Think();
        }

        public int CastAOEDD(RegionTimer timer)
        {
            Body.CastSpell(GSCAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        private Spell m_GSCAoe;
        private Spell GSCAoe
        {
            get
            {
                if (m_GSCAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 4568;
                    spell.Icon = 4568;
                    spell.Damage = 550;
                    spell.Name = "Xaga Staff Bomb";
                    spell.TooltipId = 4568;
                    spell.Radius = 650;
                    spell.SpellID = 11711;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_GSCAoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GSCAoe);
                }
                return m_GSCAoe;
            }
        }
    }
}