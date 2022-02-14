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
    public class OrganicEnergyMechanism : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Organic-Energy Mechanism", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Organic-Energy Mechanism not found, creating it...");

                log.Warn("Initializing Organic-Energy Mechanism...");
                OrganicEnergyMechanism OEM = new OrganicEnergyMechanism();
                OEM.Name = "Organic-Energy Mechanism";
                OEM.Model = 908;
                OEM.Realm = 0;
                OEM.Level = 79;
                OEM.Size = 200;
                OEM.CurrentRegionID = 191;//galladoria

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
                OEM.MaxSpeedBase = 450;
                OEM.Heading = 193;

                OrganicEnergyMechanismBrain ubrain = new OrganicEnergyMechanismBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                OEM.SetOwnBrain(ubrain);
                OEM.AddToWorld();
                OEM.Brain.Start();
                OEM.SaveIntoDatabase();
            }
            else
                log.Warn("Organic-Energy Mechanism exist ingame, remove it and restart server if you want to add by script code.");
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
            m_AOE_POison_Announce = "{0} looks sickly";
        }
        protected String m_AOE_POison_Announce;

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
                spambroad = false;//to avoid spamming
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
                BroadcastMessage(String.Format(m_AOE_POison_Announce + " and powerfull magic essense will errupt on " + RandomTarget.Name + "!", Body.Name));
                new RegionTimer(Body, new RegionTimerCallback(CastPoison), 5000);
                spambroad = true;
            }
        }
        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value;}
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
                GameLiving ptarget = ((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                RandomTarget = ptarget;
                if (OEMpoison.TargetHasEffect(randomtarget) == false && randomtarget.IsVisibleTo(Body))
                {
                    PrepareToPoison();
                }
            }
        }

        public void CastDMGShield()
        {
            if(OEMDamageShield.TargetHasEffect(Body)==false)
            {
                Body.CastSpell(OEMDamageShield, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        public override void Think()
        {
            if (Body.IsOutOfTetherRange)
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.MoveTo(Body.CurrentRegionID, Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z, 1);
                this.Body.Health = this.Body.MaxHealth;
            }

            if (HasAggro && Body.InCombat)
            {
                if(Body.HealthPercent<100)
                {
                    if (Util.Chance(15))
                    {
                        PickRandomTarget();
                    }
                    if(Util.Chance(15))
                    {
                        CastDMGShield();
                    }
                }
            }
            base.Think();
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
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
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
                    spell.DamageType = (int)eDamageType.Matter; //Spirit DMG Type
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
                    spell.ClientEffect = 509;
                    spell.Icon = 509;
                    spell.Damage = 45;
                    spell.Name = "Shield of World Soul";
                    spell.Message2 = "{0}'s armor becomes sorrounded with powerfull magic.";
                    spell.Message4 = "{0}'s powerfull magic wears off.";
                    spell.TooltipId = 509;
                    spell.Range = 1800;
                    spell.Duration = 30;
                    spell.SpellID = 11701;
                    spell.Target = "Self";
                    spell.Type = "DamageShield";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int)eDamageType.Matter; //Spirit DMG Type
                    m_DamageShield = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_DamageShield);
                }
                return m_DamageShield;
            }
        }
    }
}