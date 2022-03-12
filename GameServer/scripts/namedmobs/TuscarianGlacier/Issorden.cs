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
using Timer = System.Timers.Timer;
using System.Timers;

namespace DOL.GS
{
    public class Issorden : GameEpicBoss
    {
        public Issorden() : base() { }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get{return 350;}
            set{}
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
                    AtlasROGManager.GenerateOrbAmount(groupPlayer, 5000);//5k orbs for every player in group
                }
            }
            DropLoot(killer);
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162545);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy= npcTemplate.Empathy;
            IssordenBrain.BafMobs = false;
            Faction = FactionMgr.GetFactionByID(140);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

            IssordenBrain sbrain = new IssordenBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Issorden", 160, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Issorden not found, creating it...");

                log.Warn("Initializing Issorden...");
                Issorden TG = new Issorden();
                TG.Name = "Issorden";
                TG.Model = 920;
                TG.Realm = 0;
                TG.Level = 78;
                TG.Size = 180;
                TG.CurrentRegionID = 160;//tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                TG.Faction = FactionMgr.GetFactionByID(140);
                TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

                TG.AbilityBonus[(int)eProperty.Resist_Body] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Heat] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Cold] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Matter] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Energy] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Spirit] = 15;
                TG.AbilityBonus[(int)eProperty.Resist_Slash] = 25;
                TG.AbilityBonus[(int)eProperty.Resist_Crush] = 25;
                TG.AbilityBonus[(int)eProperty.Resist_Thrust] = 25;

                TG.X = 54583;
                TG.Y = 37745;
                TG.Z = 11435;
                IssordenBrain ubrain = new IssordenBrain();
                TG.SetOwnBrain(ubrain);
                TG.AddToWorld();
                TG.SaveIntoDatabase();
                TG.Brain.Start();
            }
            else
                log.Warn("Issorden exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}
namespace DOL.AI.Brain
{
    public class IssordenBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public IssordenBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool BafMobs = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                this.Body.Health = this.Body.MaxHealth;
                BafMobs = false;
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
                if(Body.TargetObject != null)
                {
                    if(Util.Chance(10))
                    {
                        Body.CastSpell(IssoRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    }
                }
                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && npc.PackageID == "IssordenBaf")
                            {
                                AddAggroListTo(npc.Brain as StandardMobBrain);// add to aggro mobs with IssordenBaf PackageID
                                BafMobs = true;
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        private Spell m_IssoRoot;
        private Spell IssoRoot
        {
            get
            {
                if (m_IssoRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 30;
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.Duration = 60;
                    spell.Radius = 1500;
                    spell.Value = 99;
                    spell.Name = "Issorden Root";
                    spell.TooltipId = 277;
                    spell.SpellID = 11741;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_IssoRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IssoRoot);
                }
                return m_IssoRoot;
            }
        }
    }
}