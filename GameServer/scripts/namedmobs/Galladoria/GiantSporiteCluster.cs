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
    public class GiantSporiteCluster : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool Master = true;
        public GameNPC Master_NPC;
        public List<GameNPC> CopyNPC;

        public GiantSporiteCluster()
            : base()
        {
        }

        public GiantSporiteCluster(bool master)
        {
            Master = master;
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 35; // dmg reduction for melee dmg
                case eDamageType.Crush: return 35; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }
        public virtual int GSCifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 20000; }
        }

        public override int AttackRange
        {
            get { return 250; }
            set { }
        }

        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }

        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (!Master && Master_NPC != null)
                Master_NPC.TakeDamage(source, damageType, damageAmount, criticalAmount);
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                int damageDealt = damageAmount + criticalAmount;

                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc == null) break;
                            npc.Health = Health; //they share same healthpool
                        }
                    }
                }
            }
        }

        public override void Die(GameObject killer)
        {
            if (!(killer is GiantSporiteCluster) && !Master && Master_NPC != null)
                Master_NPC.Die(killer);
            else
            {
                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc.IsAlive)
                                npc.Die(this); //if one die all others aswell
                        }
                    }
                }

                CopyNPC = new List<GameNPC>();
                GiantSporiteClusterBrain.spawn3 = true;

                base.Die(killer);
            }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161336);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;

            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            GiantSporiteClusterBrain sBrain = new GiantSporiteClusterBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Giant Sporite Cluster", 191, (eRealm) 0);
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
                SB.CurrentRegionID = 191; //galladoria

                SB.Strength = 160;
                SB.Intelligence = 150;
                SB.Piety = 130;
                SB.Dexterity = 100;
                SB.Constitution = 100;
                SB.Quickness = 75;
                SB.BodyType = 5;
                SB.MeleeDamageType = eDamageType.Slash;
                SB.Faction = FactionMgr.GetFactionByID(96);
                SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                SB.X = 42024;
                SB.Y = 49976;
                SB.Z = 10846;
                SB.MaxDistance = 2000;
                SB.TetherRange = 2500;
                SB.MaxSpeedBase = 200; //is slow to ppls may try kite it
                SB.Heading = 2764;

                GiantSporiteClusterBrain ubrain = new GiantSporiteClusterBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                SB.SetOwnBrain(ubrain);
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161336);
                SB.LoadTemplate(npcTemplate);
                SB.AddToWorld();
                SB.Brain.Start();
                SB.SaveIntoDatabase();
            }
            else
                log.Warn(
                    "Giant Sporite Cluster exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

public class GiantSporiteClusterBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (Body.IsAlive)
        {
            if (spawn3 == true)
            {
                Spawn();
                spawn3 = false;
            }
        }

        base.OnAttackedByEnemy(ad);
    }

    public override void Think()
    {
        if (!(Body is GiantSporiteCluster))
        {
            base.Think();
            return;
        }

        GiantSporiteCluster sg = Body as GiantSporiteCluster;

        if (sg.CopyNPC == null || sg.CopyNPC.Count == 0)
            sg.CopyNPC = new List<GameNPC>();


        if (!HasAggressionTable())
        {
            //set state to RETURN TO SPAWN
            FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            this.Body.Health = this.Body.MaxHealth;
            foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is GiantSporiteClusterBrain)
                {
                    if (npc.PackageID == "GSCCopy")
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
        }

        if (Body.IsOutOfTetherRange)
        {
            if (Body.PackageID != "GSCCopy")
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                spawn1 = true;
                spawn3 = true;
                spawn5 = true;
                spawn7 = true;
                spawn9 = true;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is GiantSporiteClusterBrain)
                    {
                        if (npc.PackageID == "GSCCopy")
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
        }
        else if (Body.InCombatInLast(40 * 1000) == false && this.Body.InCombatInLast(45 * 1000))
        {
            if (Body.PackageID != "GSCCopy")
            {
                this.Body.Health = this.Body.MaxHealth;
                spawn1 = true;
                spawn3 = true;
                spawn5 = true;
                spawn7 = true;
                spawn9 = true;
            }
        }

        if (Body.InCombat && HasAggro)
        {
            if (Util.Chance(5) && Body.TargetObject != null)
            {
                new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
            }

            if (Body.PackageID != "GSCCopy")
            {
                if (Body.HealthPercent < 91 && Body.HealthPercent >= 90 && spawn1 == true)
                {
                    PrepareMezz();
                    spawn1 = false;
                }

                if (Body.HealthPercent < 51 && Body.HealthPercent >= 50 && spawn5 == true)
                {
                    PrepareMezz();
                    spawn5 = false;
                }

                if (Body.HealthPercent < 11 && Body.HealthPercent >= 10 && spawn9 == true)
                {
                    PrepareMezz();
                    spawn9 = false;
                }
            }
        }

        base.Think();
    }

    public void Spawn() // We define here adds
    {
        for (int i = 0; i < Util.Random(2, 3); i++) //Spawn 2 or 3 adds
        {
            GameLiving ptarget = CalculateNextAttackTarget();
            GiantSporiteCluster Add = new GiantSporiteCluster();
            Add.X = Body.X + Util.Random(-50, 80);
            Add.Y = Body.Y + Util.Random(-50, 80);
            Add.Z = Body.Z;
            Add.Model = Body.Model;
            Add.Name = Body.Name;
            Add.Size = 120;
            Add.Level = Body.Level;
            Add.Faction = FactionMgr.GetFactionByID(96);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Add.PackageID = "GSCCopy";
            Add.Strength = 60;
            Add.Intelligence = 110;
            Add.Piety = 110;
            Add.Dexterity = 100;
            Add.Constitution = 100;
            Add.Quickness = 55;
            Add.RespawnInterval = -1;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.MaxSpeedBase = 185; //slow so players can kite
            Add.Heading = Body.Heading;
            GiantSporiteClusterBrain smb = new GiantSporiteClusterBrain();
            smb.AggroLevel = 100;
            smb.AggroRange = 1000;
            Add.AddBrain(smb);
            Add.AddToWorld();
            GiantSporiteClusterBrain brain = (GiantSporiteClusterBrain) Add.Brain;
            brain.AddToAggroList(ptarget, 1);
            Add.StartAttack(ptarget);

            Add.Master_NPC = Body;
            Add.Master = false;
            if (Body is GiantSporiteCluster)
            {
                GiantSporiteCluster sg = Body as GiantSporiteCluster;
                sg.CopyNPC.Add(Add);
            }
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
                spell.RecastDelay = 25;
                spell.ClientEffect = 4568;
                spell.Icon = 4568;
                spell.Damage = 200;
                spell.Name = "Xaga Staff Bomb";
                spell.TooltipId = 4568;
                spell.Radius = 200;
                spell.Range = 600;
                spell.SpellID = 11709;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) eDamageType.Cold;
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
                spell.DamageType = (int) eDamageType.Spirit; //Spirit DMG Type
                m_mezSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
            }

            return m_mezSpell;
        }
    }
}