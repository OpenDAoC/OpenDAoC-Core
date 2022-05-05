using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class BlueLady : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (log.IsInfoEnabled)
                log.Info("Blue Lady initialized..");
        }
        public BlueLady()
            : base()
        {
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
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8818);
            LoadTemplate(npcTemplate);

            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(eInventorySlot.TorsoArmor, 58, 54, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(eInventorySlot.ArmsArmor, 380, 54, 0);
            template.AddNPCEquipment(eInventorySlot.LegsArmor, 379, 54);
            template.AddNPCEquipment(eInventorySlot.HandsArmor, 381, 54, 0, 0);
            template.AddNPCEquipment(eInventorySlot.FeetArmor, 382, 54, 0, 0);
            template.AddNPCEquipment(eInventorySlot.Cloak, 443, 54, 0, 0);
            template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468, 43, 91);
            Inventory = template.CloseTemplate();
            SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            // humanoid
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = eDamageType.Crush;
            IsCloakHoodUp = true;
            BlueLadyBrain sBrain = new BlueLadyBrain();
            SetOwnBrain(sBrain);
            base.AddToWorld();
            return true;
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(this.CurrentRegionID))
            {
                if (npc.Brain is BlueLadyAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }            
            base.Die(killer);
        }
    }
}

namespace DOL.AI.Brain
{
    public class BlueLadyBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BlueLadyBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc.Brain is BlueLadyAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            if (Body.InCombat && HasAggro && Body.TargetObject != null)
            {
                Body.CastSpell(BlueLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                if(BlueLadySwordAdd.SwordCount <15 || BlueLadyAxeAdd.AxeCount <15)
                {
                    SpawnAdds();
                }
            }         
            base.Think();
        }
        public void SpawnAdds()
        {
            for (int i = 0; i < 15; i++)
            {
                if (BlueLadySwordAdd.SwordCount < 15)
                {
                    BlueLadySwordAdd add = new BlueLadySwordAdd();
                    add.X = Body.X + Util.Random(-100, 100);
                    add.Y = Body.Y + Util.Random(-100, 100);
                    add.Z = Body.Z;
                    add.CurrentRegion = Body.CurrentRegion;
                    add.Heading = Body.Heading;
                    add.AddToWorld();
                }
            }
            for (int i = 0; i < 15; i++)
            {
                if (BlueLadyAxeAdd.AxeCount < 15)
                {
                    BlueLadyAxeAdd add2 = new BlueLadyAxeAdd();
                    add2.X = Body.X + Util.Random(-100, 100);
                    add2.Y = Body.Y + Util.Random(-100, 100);
                    add2.Z = Body.Z;
                    add2.CurrentRegion = Body.CurrentRegion;
                    add2.Heading = Body.Heading;
                    add2.AddToWorld();
                }
            }         
        }
        public Spell m_BlueLady_DD;
        public Spell BlueLady_DD
        {
            get
            {
                if (m_BlueLady_DD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 5;
                    spell.RecastDelay = Util.Random(25, 35);
                    spell.ClientEffect = 4369;
                    spell.Icon = 4369;
                    spell.TooltipId = 4369;
                    spell.Damage = 800;
                    spell.Name = "Mana Bomb";
                    spell.Radius = 550;
                    spell.Range = 0;
                    spell.SpellID = 11788;
                    spell.Target = eSpellTarget.Enemy.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_BlueLady_DD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlueLady_DD);
                }

                return m_BlueLady_DD;
            }
        }
    }
}
namespace DOL.GS
{
    public class BlueLadySwordAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BlueLadySwordAdd()
            : base()
        {
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 200;
        }
        public override int MaxHealth
        {
            get { return 700; }
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 400;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.25;
        }
        public static int SwordCount = 0;
        public override void Die(GameObject killer)
        {
            --SwordCount;
            base.Die(killer);
        }
        public override long ExperienceValue => 0;
        public override void DropLoot(GameObject killer) //no loot
        {
        }
        public override bool AddToWorld()
        {
            BlueLadySwordAdd blueLady = new BlueLadySwordAdd();
            Model = 665;
            Name = "summoned sword";
            Size = 60;
            Level = (byte)Util.Random(50,55);
            Realm = 0;

            ++SwordCount;
            Strength = 50;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;

            GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
            templateHib.AddNPCEquipment(eInventorySlot.RightHandWeapon, 5);
            Inventory = templateHib.CloseTemplate();
            VisibleActiveWeaponSlots = (byte) eActiveWeaponSlot.Standard;

            BodyType = 6;
            BlueLadyAddBrain sBrain = new BlueLadyAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            base.AddToWorld();
            return true;
        }
    }
    public class BlueLadyAxeAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BlueLadyAxeAdd()
            : base()
        {
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 200;
        }
        public override int MaxHealth
        {
            get { return 700; }
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public static int AxeCount = 0;
        public override void Die(GameObject killer)
        {
            --AxeCount;
            base.Die(killer);
        }
        public override long ExperienceValue => 0;
        public override void DropLoot(GameObject killer) //no loot
        {
        }
        public override bool AddToWorld()
        {
            BlueLadyAxeAdd blueLady = new BlueLadyAxeAdd();
            Model = 665;
            Name = "summoned axe";
            Size = 60;
            Level = (byte)Util.Random(50, 55);
            Realm = 0;

            GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
            templateHib.AddNPCEquipment(eInventorySlot.RightHandWeapon, 316);
            Inventory = templateHib.CloseTemplate();
            VisibleActiveWeaponSlots = (byte) eActiveWeaponSlot.Standard;

            ++AxeCount;
            Strength = 50;
            Intelligence = 150;
            Piety = 150;
            Dexterity = 200;
            Constitution = 200;
            Quickness = 125;
            RespawnInterval = -1;
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;

            BodyType = 6;
            BlueLadyAddBrain sBrain = new BlueLadyAddBrain();
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
    public class BlueLadyAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BlueLadyAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
    }
}