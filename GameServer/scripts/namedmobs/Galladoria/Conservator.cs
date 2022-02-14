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
    public class Conservator : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Conservator()
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


        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;

            npcs = WorldMgr.GetNPCsByNameFromRegion("Conservator", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Conservator not found, creating it...");

                log.Warn("Initializing Conservator...");
                Conservator CO = new Conservator();
                CO.Name = "Conservator";
                CO.Model = 817;
                CO.Realm = 0;
                CO.Level = 77;
                CO.Size = 250;
                CO.CurrentRegionID = 191;//galladoria

                CO.Strength = 500;
                CO.Intelligence = 220;
                CO.Piety = 220;
                CO.Dexterity = 200;
                CO.Constitution = 200;
                CO.Quickness = 125;
                CO.BodyType = 5;
                CO.MeleeDamageType = eDamageType.Slash;
                CO.Faction = FactionMgr.GetFactionByID(96);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                CO.X = 31297;
                CO.Y = 41040;
                CO.Z = 13473;
                CO.MaxDistance = 2000;
                CO.TetherRange = 2500;
                CO.MaxSpeedBase = 300;
                CO.Heading = 409;

                ConservatorBrain ubrain = new ConservatorBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 500;
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.Brain.Start();
                CO.SaveIntoDatabase();
            }
            else
                log.Warn("Conservator exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class ConservatorBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ConservatorBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        protected virtual int PoisonTimer(RegionTimer timer)
        {
            Body.CastSpell(COPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            spampoison = false;
            return 0;
        }
        protected virtual int DiseaseTimer(RegionTimer timer)
        {
            Body.CastSpell(CODisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            spamdisease = false;
            return 0;
        }
        public static bool spampoison = false;
        public static bool spamdisease = false;
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
                if(Util.Chance(5))//cast aoe dot
                {
                    int rand = Util.Random(1, 2);//pick randomly only spell from 2
                    switch(rand)
                    {
                        case 1:
                            {
                                if (Body.TargetObject != null && spampoison == false)
                                {
                                    if (COPoison.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
                                    {
                                        Body.TurnTo(Body.TargetObject);
                                        new RegionTimer(Body, new RegionTimerCallback(PoisonTimer), 5000);
                                        spampoison = true;
                                    }
                                }
                            }
                            break;
                        case 2:
                            {
                                if (Body.TargetObject != null && spamdisease == false)
                                {
                                    if (CODisease.TargetHasEffect(Body.TargetObject) == false && Body.TargetObject.IsVisibleTo(Body))
                                    {
                                        Body.TurnTo(Body.TargetObject);
                                        new RegionTimer(Body, new RegionTimerCallback(DiseaseTimer), 5000);
                                        spamdisease = true;
                                    }
                                }
                            }
                            break;
                    } 
                }
            }
            base.Think();
        }

        public Spell m_co_poison;

        public Spell COPoison
        {
            get
            {
                if (m_co_poison == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 40;
                    spell.ClientEffect = 4445;
                    spell.Icon = 4445;
                    spell.Damage = 250;
                    spell.Name = "Essense of World Soul";
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Radius = 1000;//big range
                    spell.Duration = 45;
                    spell.Frequency = 40; //dot tick every 4s
                    spell.SpellID = 11703;
                    spell.Target = "Enemy";
                    spell.Type = "DamageOverTime";
                    spell.Uninterruptible = true;
                    spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
                    m_co_poison = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_poison);
                }
                return m_co_poison;
            }
        }

        public Spell m_co_disease;

        public Spell CODisease
        {
            get
            {
                if (m_co_disease == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 4375;
                    spell.Icon = 4375;
                    spell.Name = "Disease of World Soul";
                    spell.Description = "Inflicts a wasting disease on the target that slows it, weakens it, and inhibits heal spells.";
                    spell.TooltipId = 4375;
                    spell.Range = 1800;
                    spell.Radius = 1000;
                    spell.Duration = 120;//2min
                    spell.SpellID = 11704;
                    spell.Target = "Enemy";
                    spell.Type = "Disease";
                    spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
                    m_co_disease = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_disease);
                }
                return m_co_disease;
            }
        }
    }
}