using System;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
namespace DOL.GS
{
    public class Avartack : GameEpicBoss
    {
        public Avartack() : base()
        {
        }
        public static int TauntID = 292;
        public static int TauntClassID = 45;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int BackStyleID = 304;
        public static int BackStyleClassID = 45;
        public static Style BackStyle = SkillBase.GetStyleByID(BackStyleID, BackStyleClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            base.OnAttackEnemy(ad);
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
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
            if (source is GamePlayer || source is GamePet)
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
                            truc = ((source as GamePet).Owner as GamePlayer);
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
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8820);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            BodyType = (ushort)NpcTemplateMgr.eBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 667, 0, 0, 6);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 410, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 409, 0);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 411, 0, 0, 4);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 412, 0, 0, 5);
            template.AddNPCEquipment(eInventorySlot.HeadArmor, 1200, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 678, 0, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 474, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            if (!Styles.Contains(Taunt))
                Styles.Add(Taunt);
            if (!Styles.Contains(BackStyle))
                Styles.Add(BackStyle);
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            AvartackBrain sbrain = new AvartackBrain();
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
            npcs = WorldMgr.GetNPCsByNameFromRegion("Avartack the Champion", 276, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Avartack the Champion not found, creating it...");

                log.Warn("Initializing Avartack the Champion...");
                Avartack HOC = new Avartack();
                HOC.Name = "Avartack the Champion";
                HOC.Model = 320;
                HOC.Realm = 0;
                HOC.Level = 65;
                HOC.Size = 50;
                HOC.CurrentRegionID = 276; //marfach caverns
                HOC.RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 28926;
                HOC.Y = 35755;
                HOC.Z = 15065;
                HOC.Heading = 2552;
                AvartackBrain ubrain = new AvartackBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Avartack the Champion exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}

namespace DOL.AI.Brain
{
    public class AvartackBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AvartackBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 1500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            if (Body.InCombat && HasAggro && Body.TargetObject != null)
            {
                if (Body.TargetObject != null)
                {
                    float angle = Body.TargetObject.GetAngle(Body);
                    if (angle >= 160 && angle <= 200)
                    {
                        Body.styleComponent.NextCombatBackupStyle = Avartack.Taunt;
                        Body.styleComponent.NextCombatStyle = Avartack.BackStyle;
                    }
                    else
                    {
                        Body.styleComponent.NextCombatStyle = Avartack.Taunt;
                    }
                }
                Body.CastSpell(AvartackDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        private Spell m_AvartackDD;
        private Spell AvartackDD
        {
            get
            {
                if (m_AvartackDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(15,25);
                    spell.ClientEffect = 5435;
                    spell.Icon = 5435;
                    spell.TooltipId = 5435;
                    spell.Damage = 300;
                    spell.Range = 1500;
                    spell.Name = "Avartack's Force";
                    spell.SpellID = 11786;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Body;
                    m_AvartackDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AvartackDD);
                }
                return m_AvartackDD;
            }
        }
    }
}


