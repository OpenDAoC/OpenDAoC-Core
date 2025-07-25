﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class CaptainAtwell : GameEpicBoss
    {
        public CaptainAtwell() : base()
        {
        }
        public static int PoleAnytimerID = 93;
        public static int PoleAnytimerClassID = 2;
        public static Style PoleAnytimer = SkillBase.GetStyleByID(PoleAnytimerID, PoleAnytimerClassID);

        public static int AfterParryID = 90;
        public static int AfterParryClassID = 2;
        public static Style AfterParry = SkillBase.GetStyleByID(AfterParryID, AfterParryClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if(Util.Chance(35))
            {
                if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
                {
                    CastSpell(Bleed, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            base.OnAttackEnemy(ad);
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 20; // dmg reduction for melee dmg
                case eDamageType.Crush: return 20; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
                default: return 30; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 30000; }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (IsOutOfTetherRange)
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
                        damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
                        damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", eChatType.CT_System,
                                eChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }

        public override int MeleeAttackRange => 350;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7718);
            LoadTemplate(npcTemplate);
            Faction = FactionMgr.GetFactionByID(187);
            RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            if (!Styles.Contains(PoleAnytimer))
                Styles.Add(PoleAnytimer);
            if (!Styles.Contains(AfterParry))
                Styles.Add(AfterParry);
            VisibleActiveWeaponSlots = 34;
            CaptainAtwellBrain sbrain = new CaptainAtwellBrain();
            SetOwnBrain(sbrain);
            LoadedFromScript = false; //load from database
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Captain Atwell", 277, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Captain Atwell not found, creating it...");

                log.Warn("Initializing Captain Atwell...");
                CaptainAtwell HOC = new CaptainAtwell();
                HOC.Name = "Captain Atwell";
                HOC.Model = 723;
                HOC.Realm = 0;
                HOC.Level = 65;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                HOC.Faction = FactionMgr.GetFactionByID(187);

                HOC.X = 34000;
                HOC.Y = 38791;
                HOC.Z = 14614;
                HOC.Heading = 1003;
                CaptainAtwellBrain ubrain = new CaptainAtwellBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Captain Atwell exist ingame, remove it and restart server if you want to add by script code.");
        }
        private Spell m_Bleed;

        private Spell Bleed
        {
            get
            {
                if (m_Bleed == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 2130;
                    spell.Icon = 3411;
                    spell.TooltipId = 3411;
                    spell.Damage = 65;
                    spell.Name = "Bleed";
                    spell.Description = "Does 65 damage to a target every 3 seconds for 30 seconds.";
                    spell.Message1 = "You are bleeding! ";
                    spell.Message2 = "{0} is bleeding! ";
                    spell.Duration = 30;
                    spell.Frequency = 30;
                    spell.Range = 350;
                    spell.SpellID = 11778;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.StyleBleeding.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Body;
                    m_Bleed = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bleed);
                }
                return m_Bleed;
            }
        }
    }
}

namespace DOL.AI.Brain
{
    public class CaptainAtwellBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CaptainAtwellBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 1500;
        }
        public static bool reset_darra = false;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro)
            {
                if(Body.TargetObject != null)
                {
                    Body.styleComponent.NextCombatBackupStyle = CaptainAtwell.PoleAnytimer;
                    Body.styleComponent.NextCombatStyle = CaptainAtwell.AfterParry;
                }
            }
            base.Think();
        }
    }
}
