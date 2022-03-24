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
        
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Charisma = npcTemplate.Charisma;
            Empathy = npcTemplate.Empathy;

            ConservatorBrain sBrain = new ConservatorBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
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
            // debug
            log.Debug($"{Name} killed by {killer.Name}");
            
            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,ServerProperties.Properties.EPIC_ORBS);
                }
            }
            
            base.Die(killer);
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
                ubrain.AggroRange = 1500;//so players cant just pass him without aggroing
                CO.SetOwnBrain(ubrain);
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
                CO.LoadTemplate(npcTemplate);
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
            AggroRange = 1500;//so players cant just pass him without aggroing
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        protected virtual int PoisonTimer(RegionTimer timer)
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(COPoison, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spampoison = false;
            }
            return 0;
        }
        protected virtual int AoeTimer(RegionTimer timer)//1st timer to spam broadcast before real spell
        {
            if (Body.TargetObject != null)
            {
                BroadcastMessage(String.Format(Body.Name + " gathers energy from the water..."));
                if (spamaoe == true)
                {
                    new RegionTimer(Body, new RegionTimerCallback(RealAoe), 5000);//5s
                }
            }
            return 0;
        }
        protected virtual int RealAoe(RegionTimer timer)//real timer to cast spell and reset check
        {
            if (Body.TargetObject != null)
            {
                Body.CastSpell(COaoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                spamaoe = false;
            }
            return 0;
        }
        public static bool spampoison = false;
        public static bool spamaoe = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
            }
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
                if (Util.Chance(10))//cast dot
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
                if(Util.Chance(10))
                {
                    if (Body.TargetObject != null && spamaoe == false)
                    {
                        Body.TurnTo(Body.TargetObject);
                            new RegionTimer(Body, new RegionTimerCallback(AoeTimer), 15000);//15s to avoid being it too often called
                        spamaoe = true;
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
                    spell.Damage = 45;
                    spell.Name = "Essense of World Soul";
                    spell.Description = "Inflicts powerfull magic damage to the target, then target dies in painfull agony";
                    spell.Message1 = "You are wracked with pain!";
                    spell.Message2 = "{0} is wracked with pain!";
                    spell.Message3 = "You look healthy again.";
                    spell.Message4 = "{0} looks healthy again.";
                    spell.TooltipId = 4445;
                    spell.Range = 1800;
                    spell.Duration = 40;
                    spell.Frequency = 10; 
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

        public Spell m_co_aoe;

        public Spell COaoe
        {
            get
            {
                if (m_co_aoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 3510;
                    spell.Icon = 3510;
                    spell.TooltipId = 3510;
                    spell.Damage = 350;
                    spell.Range = 1800;
                    spell.Radius = 1200;
                    spell.SpellID = 11704;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
                    m_co_aoe = new Spell(spell, 70);                   
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_co_aoe);
                }
                return m_co_aoe;
            }
        }
    }
}