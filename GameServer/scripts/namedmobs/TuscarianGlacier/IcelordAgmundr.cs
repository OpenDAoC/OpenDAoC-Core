using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.ServerRules;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using Timer = System.Timers.Timer;
using System.Timers;

namespace DOL.GS
{
    public class Agmundr : GameEpicBoss
    {
        public Agmundr() : base() { }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 85;// dmg reduction for melee dmg
                case eDamageType.Crush: return 85;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 85;// dmg reduction for melee dmg
                default: return 80;// dmg reduction for rest resists
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
        public override int MaxHealth
        {
            get { return 20000; }
        }
        public override void Die(GameObject killer)//on kill generate orbs
        {
            // debug
            log.Debug($"{Name} killed by {killer.Name}");

            GamePlayer playerKiller = killer as GamePlayer;

            if (playerKiller?.Group != null)
            {
                foreach (GamePlayer groupPlayer in playerKiller.Group.GetPlayersInTheGroup())
                {
                    AtlasROGManager.GenerateOrbAmount(groupPlayer,OrbsReward);
                }
            }
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162346);
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
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;

            AgmundrBrain sbrain = new AgmundrBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false;//load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Agmundr", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Icelord Agmundr not found, creating it...");

                log.Warn("Initializing Icelord Agmundr...");
                Agmundr TG = new Agmundr();
                TG.Name = "Icelord Agmundr";
                TG.Model = 918;
                TG.Realm = 0;
                TG.Level = 78;
                TG.Size = 70;
                TG.CurrentRegionID = 160;//tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

                GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
                TG.Inventory = template.CloseTemplate();
                TG.SwitchWeapon(eActiveWeaponSlot.TwoHanded);

                TG.VisibleActiveWeaponSlots = 34;
                TG.MeleeDamageType = eDamageType.Crush;

                TG.X = 24075;
                TG.Y = 35593;
                TG.Z = 12917;
                TG.Heading = 3094;
                AgmundrBrain ubrain = new AgmundrBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Icelord Agmundr exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class AgmundrBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public AgmundrBrain()
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
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "AgmundrBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with CryptLordBaf PackageID
                            IsPulled = true;
                        }
                    }
                }
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                IsPulled = false;
            }
            if (Body.IsOutOfTetherRange)
            {
                this.Body.Health = this.Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                this.Body.Health = this.Body.MaxHealth;
            }
            if (Body.InCombat || HasAggro || Body.AttackState == true)
            {
                if(Util.Chance(15))
                {
                    Body.CastSpell(AgmundrDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            SetMobstats();//setting mob distance+tether+speed
            base.Think();
        }
        public void SetMobstats()
        {
            if (Body.TargetObject != null && (Body.InCombat || HasAggro || Body.AttackState == true))//if in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "AgmundrBaf")
                        {
                            if (IsPulled == true && npc.TargetObject == Body.TargetObject)
                            {
                                npc.MaxDistance = 10000;//set mob distance to make it reach target
                                npc.TetherRange = 10000;//set tether to not return to home
                                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                                {
                                    npc.MaxSpeedBase = 300;//speed is is not near to reach target faster
                                }
                                else
                                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed;//return speed to normal
                            }
                        }
                    }
                }
            }
            else//if not in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "AgmundrBaf")
                        {
                            if (IsPulled == false)
                            {
                                npc.MaxDistance = npc.NPCTemplate.MaxDistance;//return distance to normal
                                npc.TetherRange = npc.NPCTemplate.TetherRange;//return tether to normal
                                npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed;//return speed to normal
                            }
                        }
                    }
                }
            }
        }
        public Spell m_AgmundrDD;
        public Spell AgmundrDD
        {
            get
            {
                if (m_AgmundrDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(25, 45);
                    spell.ClientEffect = 228;
                    spell.Icon = 208;
                    spell.TooltipId = 479;
                    spell.Damage = 650;
                    spell.Range = 1500;
                    spell.Radius = 800;
                    spell.SpellID = 11744;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamageNoVariance";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_AgmundrDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AgmundrDD);
                }
                return m_AgmundrDD;
            }
        }
    }
}
