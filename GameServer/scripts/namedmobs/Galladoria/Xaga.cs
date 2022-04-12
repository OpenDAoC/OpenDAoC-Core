using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Xaga : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Xaga()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 75; // dmg reduction for melee dmg
                case eDamageType.Crush: return 75; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 75; // dmg reduction for melee dmg
                default: return 90; // dmg reduction for rest resists
            }
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
            get { return 20000; }
        }
        public override int AttackRange
        {
            get { return 450; }
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
            return 850;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public void SpawnTineBeatha()
        {
            if (Tine.TineCount == 0)
            {
                Tine tine = new Tine();
                tine.X = 27575;
                tine.Y = 54730;
                tine.Z = 12951;
                tine.CurrentRegion = CurrentRegion;
                tine.Heading = 2157;
                tine.RespawnInterval = -1;
                tine.AddToWorld();
            }
            if (Beatha.BeathaCount == 0)
            {
                Beatha beatha = new Beatha();
                beatha.X = 27210;
                beatha.Y = 54721;
                beatha.Z = 12959;
                beatha.CurrentRegion = CurrentRegion;
                beatha.Heading = 2038;
                beatha.RespawnInterval = -1;
                beatha.AddToWorld();
            }
        }
        public static bool spawn_lights = false;
        public override void Die(GameObject killer)
        {
            foreach(GameNPC lights in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
            {
                if(lights != null)
                {
                    if(lights.IsAlive && (lights.Brain is TineBrain || lights.Brain is BeathaBrain))
                    {
                        lights.Die(lights);
                    }
                }
            }
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
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
            XagaBrain sBrain = new XagaBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            LoadedFromScript = false;
            spawn_lights = false;
            bool success = base.AddToWorld();
            if (success)
            {
                if (spawn_lights == false)
                {
                    SpawnTineBeatha();
                    spawn_lights = true;
                }
            }
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
                SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                SB.X = 27397;
                SB.Y = 54975;
                SB.Z = 12949;
                SB.MaxDistance = 2000;
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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public XagaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public static bool spawstaffs1 = true;
        public static bool spawstaffs2 = true;
        public static bool spawstaffs3 = true;
        public static bool spawstaffs4 = true;
        public static bool spawstaffs5 = true;
        public static bool spawstaffs6 = true;
        public static bool spawstaffs7 = true;
        public static bool spawstaffs8 = true;
        public static bool spawstaffs9 = true;

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                spawstaffs1 = true;
                spawstaffs2 = true;
                spawstaffs3 = true;
                spawstaffs4 = true;
                spawstaffs5 = true;
                spawstaffs6 = true;
                spawstaffs7 = true;
                spawstaffs8 = true;
                spawstaffs9 = true;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is XagaStaffBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            if (HasAggro && Body.InCombat)
            {
                if (Body.HealthPercent < 92 && Body.HealthPercent >= 90 && spawstaffs1 == true)
                {
                    Spawn();
                    PrepareMezz();
                    spawstaffs1 = false;
                }
                if (Body.HealthPercent < 82 && Body.HealthPercent >= 80 && spawstaffs2 == true)
                {
                    Spawn();
                    spawstaffs2 = false;
                }
                if (Body.HealthPercent < 72 && Body.HealthPercent >= 70 && spawstaffs3 == true)
                {
                    Spawn();
                    spawstaffs3 = false;
                }
                if (Body.HealthPercent < 62 && Body.HealthPercent >= 60 && spawstaffs4 == true)
                {
                    Spawn();
                    spawstaffs4 = false;
                }
                if (Body.HealthPercent < 52 && Body.HealthPercent >= 50 && spawstaffs5 == true)
                {
                    Spawn();
                    PrepareMezz();
                    spawstaffs5 = false;
                }
                if (Body.HealthPercent < 42 && Body.HealthPercent >= 40 && spawstaffs6 == true)
                {
                    Spawn();
                    spawstaffs6 = false;
                }
                if (Body.HealthPercent < 32 && Body.HealthPercent >= 30 && spawstaffs7 == true)
                {
                    Spawn();
                    spawstaffs7 = false;
                }
                if (Body.HealthPercent < 22 && Body.HealthPercent >= 20 && spawstaffs8 == true)
                {
                    Spawn();
                    spawstaffs8 = false;
                }
                if (Body.HealthPercent < 12 && Body.HealthPercent >= 10 && spawstaffs9 == true)
                {
                    Spawn();
                    PrepareMezz();
                    spawstaffs9 = false;
                }
            }
            base.Think();
        }
        
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (Body.IsAlive)
            {
                foreach (GameNPC mob_c in Body.GetNPCsInRadius(4000, false))
                {
                    if (mob_c?.Brain is BeathaBrain && mob_c.IsAlive && mob_c.IsAvailable)
                    {
                        AddAggroListTo(mob_c.Brain as BeathaBrain);
                    }
                    if (mob_c?.Brain is TineBrain && mob_c.IsAlive && mob_c.IsAvailable)
                    {
                        AddAggroListTo(mob_c.Brain as TineBrain);
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }

        public void Spawn() // We define here adds
        {
            foreach (GamePlayer ppl in Body.GetPlayersInRadius(1500))
            {
                if (ppl.IsAlive)
                {
                    XagaStaff Add = new XagaStaff();
                    Add.X = ppl.X;
                    Add.Y = ppl.Y;
                    Add.Z = ppl.Z;
                    Add.CurrentRegion = Body.CurrentRegion;
                    Add.Heading = ppl.Heading;
                    Add.AddToWorld();
                }
            }
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public void PrepareMezz()
        {
            if (Mezz.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
            {
                BroadcastMessage(
                    String.Format(Body.Name + " look at " + Body.TargetObject.Name + " angrly!", Body.Name));
                new RegionTimer(Body, new RegionTimerCallback(CastMezz), 5000);
            }
        }
        protected virtual int CastMezz(RegionTimer timer)
        {
            Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
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
                    spell.Radius = 800;
                    spell.SpellID = 11706;
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
}
#region Xaga Staff
namespace DOL.GS
{
    public class XagaStaff : GameNPC
    {
        public XagaStaff() : base()
        {
        }
        public static GameNPC m_XagaStaff = new GameNPC();
        public override int MaxHealth
        {
            get { return 650; }
        }
        public override bool AddToWorld()
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468, 0, 91);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            Model = 665;
            Name = "magic staff";
            MeleeDamageType = eDamageType.Crush;
            RespawnInterval = -1;
            MaxSpeedBase = 0;
            Intelligence = 250;
            Piety = 250;
            IsWorthReward = false; //worth no reward
            Size = (byte) Util.Random(50, 55);
            Level = (byte) Util.Random(55, 58);
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Realm = eRealm.None;
            XagaStaffBrain adds = new XagaStaffBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
        public override void DropLoot(GameObject killer) //no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null); //null to not gain experience
        }
    }
}
namespace DOL.AI.Brain
{
    public class XagaStaffBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public XagaStaffBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 450;
            ThinkInterval = 4000;
        }
        public override void Think()
        {
            Body.IsWorthReward = false;
            if (Body.IsAlive)
            {
                Body.SetGroundTarget(Body.X,Body.Y,Body.Z);
                Body.CastSpell(XagaStaffPBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        private Spell m_XagaStaffPBAOE;
        private Spell XagaStaffPBAOE
        {
            get
            {
                if (m_XagaStaffPBAOE == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 3;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 4468;
                    spell.Icon = 4468;
                    spell.Damage = 400;
                    spell.Name = "Xaga Staff Bomb";
                    spell.TooltipId = 4468;
                    spell.Range = 500;
                    spell.Radius = 450;
                    spell.SpellID = 11705;
                    spell.Target = "Area";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_XagaStaffPBAOE = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_XagaStaffPBAOE);
                }
                return m_XagaStaffPBAOE;
            }
        }
    }
}
#endregion
////////////////////////////////////////////////Beatha/////////////////////////////////////////////
#region Beatha
namespace DOL.GS
{
    public class Beatha : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Beatha()
            : base()
        {
        }
        public virtual int COifficulty
        {
            get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 50; // dmg reduction for melee dmg
                case eDamageType.Crush: return 50; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 50; // dmg reduction for melee dmg
                default: return 80; // dmg reduction for rest resists
            }
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 10000; }
        }

        public override int AttackRange
        {
            get { return 450; }
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
            return 700;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.45;
        }
        public static int BeathaCount = 0;
        public override void Die(GameObject killer)
        {
            --BeathaCount;
            base.Die(killer);
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (damageType == eDamageType.Cold)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to cold damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158330);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            ++BeathaCount;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            BeathaBrain sBrain = new BeathaBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class BeathaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BeathaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(10) && Body.TargetObject != null)
                {
                    new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
                }
            }
            base.Think();
        }
        public int CastAOEDD(RegionTimer timer)
        {
            Body.CastSpell(BeathaAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        private Spell m_BeathaAoe;
        private Spell BeathaAoe
        {
            get
            {
                if (m_BeathaAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 4568;
                    spell.Icon = 4568;
                    spell.Damage = 350;
                    spell.Name = "Xaga Staff Bomb";
                    spell.TooltipId = 4568;
                    spell.Radius = 650;
                    spell.SpellID = 11707;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Cold;
                    m_BeathaAoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BeathaAoe);
                }
                return m_BeathaAoe;
            }
        }
    }
}
#endregion
/////////////////////Tine///////////////
#region Tine
namespace DOL.GS
{
    public class Tine : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Tine()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 50; // dmg reduction for melee dmg
                case eDamageType.Crush: return 50; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 50; // dmg reduction for melee dmg
                default: return 80; // dmg reduction for rest resists
            }
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
            get { return 10000; }
        }

        public override int AttackRange
        {
            get { return 450; }
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
            return 700;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.45;
        }
        public static int TineCount = 0;
        public override void Die(GameObject killer)
        {
            --TineCount;
            base.Die(killer);
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (damageType == eDamageType.Heat)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to heat damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167084);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            ++TineCount;
            TineBrain sBrain = new TineBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class TineBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TineBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(10) && Body.TargetObject != null)
                {
                    new RegionTimer(Body, new RegionTimerCallback(CastAOEDD), 3000);
                }
            }
            base.Think();
        }
        public int CastAOEDD(RegionTimer timer)
        {
            Body.CastSpell(TineAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }
        private Spell m_TineAoe;
        private Spell TineAoe
        {
            get
            {
                if (m_TineAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 8;
                    spell.ClientEffect = 4227;
                    spell.Icon = 4227;
                    spell.Damage = 350;
                    spell.Name = "Xaga Staff Bomb";
                    spell.TooltipId = 4227;
                    spell.Radius = 650;
                    spell.SpellID = 11708;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Heat;
                    m_TineAoe = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TineAoe);
                }
                return m_TineAoe;
            }
        }
    }
}
#endregion