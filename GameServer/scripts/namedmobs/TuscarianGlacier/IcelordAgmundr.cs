using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;

namespace DOL.GS
{
    public class Agmundr : GameEpicBoss
    {
        public Agmundr() : base()
        {
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 85; // dmg reduction for melee dmg
                case eDamageType.Crush: return 85; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 85; // dmg reduction for melee dmg
                default: return 80; // dmg reduction for rest resists
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
            RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 19, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);

            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;

            AgmundrBrain sbrain = new AgmundrBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Agmundr", 160, (eRealm) 0);
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
                TG.CurrentRegionID = 160; //tuscaran glacier
                TG.MeleeDamageType = eDamageType.Crush;
                TG.RespawnInterval =
                    ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                    60000; //1min is 60000 miliseconds
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
                log.Warn(
                    "Icelord Agmundr exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class AgmundrBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AgmundrBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }

        private static bool IsPulled;

        private static bool IsChanged;

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled == false)
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc == null) continue;
                    if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                    AddAggroListTo(
                        npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
                    IsPulled = true;
                }
                
                if (!IsChanged)
                    SetMobstats();
            }

            base.OnAttackedByEnemy(ad);
        }

        public override void AttackMostWanted()
        {
            if (Util.Chance(15))
            {
                Body.CastSpell(AgmundrDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.AttackMostWanted();
        }

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled = false;
            }
            
            if (FSM.GetCurrentState() == FSM.GetState(eFSMStateType.RETURN_TO_SPAWN))
            {
                if (IsChanged)
                    LoadBAFTemplate();
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            base.Think();
        }

        private void SetMobstats()
        {
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc == null) continue;
                if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                if (!IsPulled || npc.TargetObject != Body.TargetObject) continue;
                npc.MaxDistance = 10000; //set mob distance to make it reach target
                npc.TetherRange = 10000; //set tether to not return to home
                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                {
                    npc.MaxSpeedBase = 300; //speed is is not near to reach target faster
                }
                else
                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
            }
            IsChanged = true;
        }

        private void LoadBAFTemplate()
        {
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc == null) continue;
                    if (npc.NPCTemplate == null) continue;
                    INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(npc.NPCTemplate.TemplateId);
                    if (npcTemplate == null)
                        return;
                    if (!npc.IsAlive || npc.PackageID != "AgmundrBaf") continue;
                    if (IsPulled == false)
                    {
                        npc.LoadTemplate(npcTemplate);
                    }
                }
            }
            IsChanged = false;
        }

        private Spell m_AgmundrDD;

        private Spell AgmundrDD
        {
            get
            {
                if (m_AgmundrDD != null) return m_AgmundrDD;
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
                spell.DamageType = (int) eDamageType.Cold;
                m_AgmundrDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AgmundrDD);

                return m_AgmundrDD;
            }
        }
    }
}