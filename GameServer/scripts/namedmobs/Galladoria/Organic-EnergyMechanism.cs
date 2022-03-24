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

        public virtual int OEMDifficulty
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

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.85;
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            return 1000;
        }

        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
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
            SetOwnBrain(sBrain);

            bool success = base.AddToWorld();
            if (success)
            {
                if (addeffect == true)
                {
                    StartTimer();
                    addeffect = false;
                }
            }

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
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OrganicEnergyMechanismBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            m_AOE_POison_Announce = "{0} looks sickly";
        }

        protected String m_AOE_POison_Announce;

        public override void AttackMostWanted() // mob doesnt attack
        {
            if (Body.IsAlive)
                return;
            base.AttackMostWanted();
        }

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        private int CastPoison(RegionTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            Body.TurnTo(RandomTarget);
            if (Body.TargetObject != null)
            {
                Body.CastSpell(OEMpoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spambroad = false; //to avoid spamming
            }

            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }

        public static bool spambroad = false;

        private void PrepareToPoison()
        {
            if (spambroad == false)
            {
                BroadcastMessage(String.Format(
                    m_AOE_POison_Announce + " and powerfull magic essense will errupt on " + RandomTarget.Name + "!",
                    Body.Name));
                new RegionTimer(Body, new RegionTimerCallback(CastPoison), 5000);
                spambroad = true;
            }
        }

        private GameLiving randomtarget;

        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }

        public void PickRandomTarget()
        {
            ArrayList inRangeLiving = new ArrayList();
            foreach (GameLiving living in Body.GetPlayersInRadius(2000))
            {
                if (living.IsAlive)
                {
                    if (living is GamePlayer || living is GamePet)
                    {
                        if (!inRangeLiving.Contains(living) || inRangeLiving.Contains(living) == false)
                        {
                            inRangeLiving.Add(living);
                        }
                    }
                }
            }

            if (inRangeLiving.Count > 0)
            {
                GameLiving ptarget = ((GameLiving) (inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                RandomTarget = ptarget;
                if (OEMpoison.TargetHasEffect(randomtarget) == false && randomtarget.IsVisibleTo(Body))
                {
                    PrepareToPoison();
                }
            }
        }

        public void CastDMGShield()
        {
            if (OEMDamageShield.TargetHasEffect(Body) == false)
            {
                Body.CastSpell(OEMDamageShield, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }

        public static bool spawnadds = true;

        public override void Think()
        {
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
                foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                {
                    if (npc.Brain is OEMAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }

            if (HasAggro && Body.InCombat)
            {
                if (Body.HealthPercent < 100)
                {
                    if (Util.Chance(15))
                    {
                        PickRandomTarget();
                    }

                    if (Util.Chance(15))
                    {
                        CastDMGShield();
                    }

                    if (Util.Chance(5))
                    {
                        Spawn(); // spawn images
                    }
                }
            }

            base.Think();
        }

        public void Spawn() // We define here adds
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
            {
                if (npc.Brain is OEMAddBrain)
                {
                    return;
                }
            }

            for (int i = 0; i < Util.Random(4, 6); i++) //Spawn 4 or 6 adds
            {
                OEMAdd Add = new OEMAdd();
                Add.X = Body.X + Util.Random(-50, 80);
                Add.Y = Body.Y + Util.Random(-50, 80);
                Add.Z = Body.Z;
                Add.CurrentRegion = Body.CurrentRegion;
                Add.Heading = Body.Heading;
                Add.AddToWorld();
            }
        }

        private Spell m_AOE_Poison;
        private Spell m_DamageShield;

        public Spell OEMpoison
        {
            get
            {
                if (m_AOE_Poison == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.Damage = 350;
                    spell.Name = "Essense of World Soul";
                    spell.Description =
                        "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Radius = 600;
                    spell.Duration = 45;
                    spell.Frequency = 40; //dot tick every 4s
                    spell.SpellID = 11700;
                    spell.Target = "Enemy";
                    spell.Type = "DamageOverTime";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int) eDamageType.Matter; //Spirit DMG Type
                    m_AOE_Poison = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AOE_Poison);
                }

                return m_AOE_Poison;
            }
        }

        public Spell OEMDamageShield
        {
            get
            {
                if (m_DamageShield == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 4317; //509
                    spell.Icon = 4317;
                    spell.Damage = 150;
                    spell.Name = "Shield of World Soul";
                    spell.Message2 = "{0}'s armor becomes sorrounded with powerfull magic.";
                    spell.Message4 = "{0}'s powerfull magic wears off.";
                    spell.TooltipId = 4317;
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
    }
}

////////////////////////////////////////adds//////////////////////
namespace DOL.GS
{
    public class OEMAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OEMAdd()
            : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int MaxHealth
        {
            get { return 1500; }
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

        public override bool AddToWorld()
        {
            Model = 905;
            Name = "bottom feeder";
            Size = 32;
            Level = (byte) Util.Random(50, 55);
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
            OEMAddBrain sBrain = new OEMAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
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
            AggroRange = 1000;
        }

        public override void Think()
        {
            Body.IsWorthReward = false; //worth no reward
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(15) && Body.TargetObject != null)
                {
                    if (FeederSCDebuff.TargetHasEffect(Body.TargetObject) == false &&
                        Body.TargetObject.IsVisibleTo(Body))
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastSCDebuff), 3000);
                    }
                }

                if (Util.Chance(15) && Body.TargetObject != null)
                {
                    if (FeederDQDebuff.TargetHasEffect(Body.TargetObject) == false &&
                        Body.TargetObject.IsVisibleTo(Body))
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastDQDebuff), 3000);
                    }
                }

                if (Util.Chance(15) && Body.TargetObject != null)
                {
                    if (FeederHasteDebuff.TargetHasEffect(Body.TargetObject) == false &&
                        Body.TargetObject.IsVisibleTo(Body))
                    {
                        new RegionTimer(Body, new RegionTimerCallback(CastHasteDebuff), 3000);
                    }
                }
            }

            base.Think();
        }

        public int CastSCDebuff(RegionTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(FeederSCDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            return 0;
        }

        public int CastDQDebuff(RegionTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(FeederDQDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }

            return 0;
        }

        public int CastHasteDebuff(RegionTimer timer)
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

        private Spell m_FeederDQDebuff;

        private Spell FeederDQDebuff
        {
            get
            {
                if (m_FeederDQDebuff == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 35;
                    spell.ClientEffect = 5418;
                    spell.Icon = 5418;
                    spell.Name = "D/Q Debuff";
                    spell.TooltipId = 5418;
                    spell.Range = 1200;
                    spell.Value = 85;
                    spell.Duration = 60;
                    spell.SpellID = 11714;
                    spell.Target = "Enemy";
                    spell.Type = "DexterityQuicknessDebuff";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int) eDamageType.Energy;
                    m_FeederDQDebuff = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FeederDQDebuff);
                }

                return m_FeederDQDebuff;
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
    }
}