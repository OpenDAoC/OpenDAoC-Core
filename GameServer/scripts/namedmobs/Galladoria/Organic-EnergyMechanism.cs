using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using Timer = System.Timers.Timer;
using System.Timers;


namespace DOL.GS
{
    public class OrganicEnergyMechanism : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OrganicEnergyMechanism()
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
        public override void StartAttack(GameObject target)//dont attack
        {
        }
        public override int MaxHealth
        {
            get { return 200000; }
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public void StartTimer()
        {
            Timer myTimer = new Timer();
            myTimer.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
            myTimer.Interval = 4000; // 1000 ms is one second
            myTimer.Start();
        }
        public void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
            ShowEffect();
        }
        public void ShowEffect()
        {
            if (this.IsAlive)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(this, this, 509, 0, false, 0x01); //finished heal effect
                }
            }
        }
        public static bool addeffect = true;

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164704);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;
            OrganicEnergyMechanismBrain sBrain = new OrganicEnergyMechanismBrain();
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            SetOwnBrain(sBrain);
            addeffect = true;
            OrganicEnergyMechanismBrain.StartCastDOT = false;
            OrganicEnergyMechanismBrain.CanCast = false;
            OrganicEnergyMechanismBrain.RandomTarget = null;
            bool success = base.AddToWorld();
            if (success)
            {
                if (addeffect == true)
                {
                    StartTimer();
                    addeffect = false;
                }
            }
            SaveIntoDatabase();
            LoadedFromScript = false;
            return success;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Organic-Energy Mechanism", 191, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Organic-Energy Mechanism not found, creating it...");

                log.Warn("Initializing Organic-Energy Mechanism...");
                OrganicEnergyMechanism OEM = new OrganicEnergyMechanism();
                OEM.Name = "Organic-Energy Mechanism";
                OEM.Model = 665; //does have not any model just visual effect
                OEM.Realm = 0;
                OEM.Level = 79;
                OEM.Size = 200;
                OEM.CurrentRegionID = 191; //galladoria

                OEM.Strength = 500;
                OEM.Intelligence = 220;
                OEM.Piety = 220;
                OEM.Dexterity = 200;
                OEM.Constitution = 200;
                OEM.Quickness = 125;
                OEM.MeleeDamageType = eDamageType.Slash;
                OEM.Faction = FactionMgr.GetFactionByID(96);

                OEM.X = 49410;
                OEM.Y = 31267;
                OEM.Z = 14388;
                OEM.MaxDistance = 2000;
                OEM.MaxSpeedBase = 0; //mob doesnt move
                OEM.Heading = 193;

                OrganicEnergyMechanismBrain ubrain = new OrganicEnergyMechanismBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                OEM.SetOwnBrain(ubrain);
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164704);
                OEM.LoadTemplate(npcTemplate);
                OEM.AddToWorld();
                OEM.Brain.Start();
                OEM.SaveIntoDatabase();
            }
            else
                log.Warn(
                    "Organic-Energy Mechanism exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class OrganicEnergyMechanismBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OrganicEnergyMechanismBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        #region OEM Dot
        public static bool CanCast = false;
        public static bool StartCastDOT = false;
        public static GamePlayer randomtarget = null;
        public static GamePlayer RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        List<GamePlayer> Enemys_To_DOT = new List<GamePlayer>();
        public int PickRandomTarget(ECSGameTimer timer)
        {
            if (HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!Enemys_To_DOT.Contains(player))
                            {
                                Enemys_To_DOT.Add(player);
                            }
                        }
                    }
                }
                if (Enemys_To_DOT.Count > 0)
                {
                    if (CanCast == false)
                    {
                        GamePlayer Target = (GamePlayer)Enemys_To_DOT[Util.Random(0, Enemys_To_DOT.Count - 1)];//pick random target from list
                        RandomTarget = Target;//set random target to static RandomTarget
                        BroadcastMessage(String.Format(Body.Name + "looks sickly... powerfull magic essense will errupt on " + RandomTarget.Name + "!"));
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDOT), 5000);
                        CanCast = true;
                    }
                }
            }
            return 0;
        }
        public int CastDOT(ECSGameTimer timer)
        {
            if (HasAggro && RandomTarget != null)
            {
                GamePlayer oldTarget = (GamePlayer)Body.TargetObject;//old target
                if (RandomTarget != null && RandomTarget.IsAlive)
                {
                    Body.TargetObject = RandomTarget;
                    Body.TurnTo(RandomTarget);
                    Body.CastSpell(OEMpoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (oldTarget != null) Body.TargetObject = oldTarget;//return to old target
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDOT), 5000);
            }
            return 0;
        }
        public int ResetDOT(ECSGameTimer timer)
        {
            RandomTarget = null;
            CanCast = false;
            StartCastDOT = false;
            return 0;
        }
        #endregion
        public override void Think()
        {
            if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                if(AggroTable.Count>0)
                    ClearAggroList();
            }
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
                RandomTarget = null;
                CanCast = false;
                StartCastDOT = false;
                RandomTarget = null;
                SpawnFeeder = false;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.Brain is OEMAddBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
            if (HasAggro && Body.IsAlive)
            {
                //DOT is not classic like, can be anabled if we wish to
                /* if (StartCastDOT == false)
                 {
                     new RegionTimer(Body, new RegionTimerCallback(PickRandomTarget), Util.Random(20000, 25000));
                     StartCastDOT = true;
                 }*/
                if (Util.Chance(15))
                    Body.CastSpell(OEMDamageShield, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

                if (Util.Chance(25))
                    Body.CastSpell(OEMEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

                if (SpawnFeeder==false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnFeeders), 10000);
                    SpawnFeeder = true;
                }
            }
            base.Think();
        }
        public static bool SpawnFeeder = false;
        public int SpawnFeeders(ECSGameTimer timer) // We define here adds
        {
            if (Body.IsAlive && HasAggro)
            {
                for (int i = 0; i < Util.Random(3, 5); i++)
                {
                    OEMAdd Add = new OEMAdd();
                    Add.X = Body.X + Util.Random(-50, 80);
                    Add.Y = Body.Y + Util.Random(-50, 80);
                    Add.Z = Body.Z;
                    Add.CurrentRegion = Body.CurrentRegion;
                    Add.Heading = Body.Heading;
                    Add.AddToWorld();
                }
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpawnFeeders), Util.Random(15000,25000));
            }
            return 0;
        }
        public int ResetSpawnFeeders(ECSGameTimer timer)
        {
            SpawnFeeder = false;
            return 0;
        }
        #region Spells
        private Spell m_AOE_Poison;
        private Spell OEMpoison
        {
            get
            {
                if (m_AOE_Poison == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.Damage = 200;
                    spell.Name = "Essense of World Soul";
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony.";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Radius = 600;
                    spell.Duration = 50;
                    spell.Frequency = 50; //dot tick every 5s
                    spell.SpellID = 11700;
                    spell.Target = "Enemy";
                    spell.Type = "DamageOverTime";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int) eDamageType.Matter; //Spirit DMG Type
                    m_AOE_Poison = new Spell(spell, 50);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AOE_Poison);
                }
                return m_AOE_Poison;
            }
        }
        private Spell m_DamageShield;
        private Spell OEMDamageShield
        {
            get
            {
                if (m_DamageShield == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 11027; //509
                    spell.Icon = 11027;
                    spell.Damage = 150;
                    spell.Name = "Shield of World Soul";
                    spell.Message2 = "{0}'s armor becomes sorrounded with powerfull magic.";
                    spell.Message4 = "{0}'s powerfull magic wears off.";
                    spell.TooltipId = 11027;
                    spell.Range = 1800;
                    spell.Duration = 35;
                    spell.SpellID = 11701;
                    spell.Target = "Self";
                    spell.Type = "DamageShield";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int) eDamageType.Matter; //Spirit DMG Type
                    m_DamageShield = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DamageShield);
                }
                return m_DamageShield;
            }
        }
        private Spell m_OEMEffect;
        private Spell OEMEffect
        {
            get
            {
                if (m_OEMEffect == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.Duration = 5;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.Value = 1;
                    spell.Name = "Machanism Effect";
                    spell.TooltipId = 5126;
                    spell.SpellID = 11864;
                    spell.Target = "Self";
                    spell.Type = eSpellType.PowerRegenBuff.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_OEMEffect = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OEMEffect);
                }
                return m_OEMEffect;
            }
        }
        #endregion
    }
}

////////////////////////////////////////adds//////////////////////
namespace DOL.GS
{
    public class OEMAdd : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OEMAdd()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 35; // dmg reduction for melee dmg
                case eDamageType.Crush: return 35; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 35; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.15;
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 200;
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override void DropLoot(GameObject killer) //no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null); //null to not gain experience
        }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public override bool AddToWorld()
        {
            Model = 905;
            Name = "Summoned Bottom Feeder";
            Size = 32;
            Level = (byte) Util.Random(51, 55);
            Realm = 0;
            CurrentRegionID = 191; //galladoria

            Strength = 150;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;
            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            IsWorthReward = false; //worth no reward

            BodyType = 1;
            MaxSpeedBase = 245;
            OEMAddBrain sBrain = new OEMAddBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class OEMAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OEMAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1800;
        }

        public override void Think()
        {
            Body.IsWorthReward = false; //worth no reward
            if (Body.InCombat && HasAggro)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (Util.Chance(15) && Body.TargetObject != null)
                {
                    if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.StrConDebuff))
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastSCDebuff), 3000);
                    }
                }
                if (Util.Chance(15) && Body.TargetObject != null)
                {
                    if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.MeleeHasteDebuff))
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHasteDebuff), 3000);
                    }
                }
                if (Util.Chance(15) && Body.TargetObject != null)
                {                    
                    if(!target.effectListComponent.ContainsEffectForEffectType(eEffect.MovementSpeedDebuff) && !target.effectListComponent.ContainsEffectForEffectType(eEffect.SnareImmunity))
                    {
                        Body.CastSpell(FeederRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
            }

            base.Think();
        }

        public int CastSCDebuff(ECSGameTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(FeederSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            return 0;
        }
        public int CastHasteDebuff(ECSGameTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(FeederHasteDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            return 0;
        }
        private Spell m_FeederSCDebuff;
        private Spell FeederSCDebuff
        {
            get
            {
                if (m_FeederSCDebuff == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 5408;
                    spell.Icon = 5408;
                    spell.Name = "S/C Debuff";
                    spell.TooltipId = 5408;
                    spell.Range = 1200;
                    spell.Value = 85;
                    spell.Duration = 60;
                    spell.SpellID = 11713;
                    spell.Target = "Enemy";
                    spell.Type = "StrengthConstitutionDebuff";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Energy;
                    m_FeederSCDebuff = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederSCDebuff);
                }
                return m_FeederSCDebuff;
            }
        }

        private Spell m_FeederHasteDebuff;
        private Spell FeederHasteDebuff
        {
            get
            {
                if (m_FeederHasteDebuff == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 5427;
                    spell.Icon = 5427;
                    spell.Name = "Haste Debuff";
                    spell.TooltipId = 5427;
                    spell.Range = 1200;
                    spell.Value = 24;
                    spell.Duration = 60;
                    spell.SpellID = 11715;
                    spell.Target = "Enemy";
                    spell.Type = "CombatSpeedDebuff";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Energy;
                    m_FeederHasteDebuff = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederHasteDebuff);
                }
                return m_FeederHasteDebuff;
            }
        }
        private Spell m_FeederRoot;
        private Spell FeederRoot
        {
            get
            {
                if (m_FeederRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 0;
                    spell.ClientEffect = 11027;
                    spell.Icon = 5440;
                    spell.Name = "Root";
                    spell.Description = "Target moves 40% slower for the spell's duration.";
                    spell.TooltipId = 5440;
                    spell.Range = 1200;
                    spell.Value = 60;
                    spell.Duration = 60;
                    spell.SpellID = 11865;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.SpeedDecrease.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Body;
                    m_FeederRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederRoot);
                }
                return m_FeederRoot;
            }
        }
    }
}