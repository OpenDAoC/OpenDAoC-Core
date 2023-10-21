using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Styles;

namespace Core.GS
{
    public class LieutenantMeade : GameNpc
    {
        public LieutenantMeade() : base()
        {
        }
        public static int TauntID = 357;
        public static int TauntClassID = 19;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int SideID = 362;
        public static int SideClassID = 19;
        public static Style Side = SkillBase.GetStyleByID(SideID, SideClassID);

        public static int SlamID = 228;
        public static int SlamClassID = 19;
        public static Style slam = SkillBase.GetStyleByID(SlamID, SlamClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            base.OnAttackEnemy(ad);
        }
        public override int GetResist(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Slash: return 35; // dmg reduction for melee dmg
                case EDamageType.Crush: return 35; // dmg reduction for melee dmg
                case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }

        public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (this.IsOutOfTetherRange)
                {
                    if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                        damageType == EDamageType.Energy || damageType == EDamageType.Heat
                        || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                        damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                        || damageType == EDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GameSummonedPet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System,
                                EChatLoc.CL_ChatWindow);
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
        public override double AttackDamage(DbInventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 150;
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (this.IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(EArmorSlot slot)
        {
            return 500;
        }
        public override double GetArmorAbsorb(EArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.35;
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7716);
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
            BodyType = (ushort)EBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 186, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 188, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 187, 0);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 189, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 190, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 865, 0, 0);
            template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1041, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.Standard);
            if (!this.Styles.Contains(Side))
            {
                Styles.Add(Side);
            }
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            if (!Styles.Contains(slam))
            {
                Styles.Add(slam);
            }
            LieutenantMeadeBrain.CanWalk = false;
            VisibleActiveWeaponSlots = 16;
            MeleeDamageType = EDamageType.Slash;
            LieutenantMeadeBrain sbrain = new LieutenantMeadeBrain();
            SetOwnBrain(sbrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNpc[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lieutenant Meade", 277, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lieutenant Meade not found, creating it...");

                log.Warn("Initializing Lieutenant Meade...");
                LieutenantMeade HOC = new LieutenantMeade();
                HOC.Name = "Lieutenant Meade";
                HOC.Model = 48;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.X = 31874;
                HOC.Y = 34994;
                HOC.Z = 15366;
                HOC.Heading = 9;
                LieutenantMeadeBrain ubrain = new LieutenantMeadeBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Lieutenant Meade exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}