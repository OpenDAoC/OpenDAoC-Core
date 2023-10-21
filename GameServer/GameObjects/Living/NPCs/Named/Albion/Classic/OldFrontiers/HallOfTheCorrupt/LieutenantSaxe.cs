using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Styles;

namespace Core.GS
{
    public class LieutenantSaxe : GameNpc
    {
        public LieutenantSaxe() : base()
        {
        }
        public static int TauntID = 240;
        public static int TauntClassID = 10;
        public static Style Taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);

        public static int AfterEvadeID = 238;
        public static int AfterEvadeClassID = 10;
        public static Style AfterEvade = SkillBase.GetStyleByID(AfterEvadeID, AfterEvadeClassID);

        public static int EvadeFollowUpID = 242;
        public static int EvadeFollowUpClassID = 10;
        public static Style EvadeFollowUp = SkillBase.GetStyleByID(EvadeFollowUpID, EvadeFollowUpClassID);
        public override void OnAttackedByEnemy(AttackData ad) // on Boss actions
        {
            if (ad != null && ad.AttackResult == EAttackResult.Evaded)
            {
                this.styleComponent.NextCombatBackupStyle = AfterEvade;
                this.styleComponent.NextCombatStyle = EvadeFollowUp;
            }
            base.OnAttackedByEnemy(ad);
        }
        public override void OnAttackEnemy(AttackData ad) //on enemy actions
        {
            if (ad != null && ad.AttackResult == EAttackResult.HitUnstyled)
            {
                this.styleComponent.NextCombatBackupStyle = Taunt;
                this.styleComponent.NextCombatStyle = EvadeFollowUp;
            }
            if (ad != null && ad.AttackResult == EAttackResult.HitStyle && ad.Style.ID == 238 && ad.Style.ClassID == 10)
            {
                this.styleComponent.NextCombatBackupStyle = Taunt;
                this.styleComponent.NextCombatStyle = EvadeFollowUp;
            }
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
            Faction = FactionMgr.GetFactionByID(187);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
            BodyType = (ushort)EBodyType.Humanoid;

            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
            template.AddNPCEquipment(EInventorySlot.TorsoArmor, 441, 0, 0, 0);//modelID,color,effect,extension
            template.AddNPCEquipment(EInventorySlot.ArmsArmor, 136, 0);
            template.AddNPCEquipment(EInventorySlot.LegsArmor, 135, 0);
            template.AddNPCEquipment(EInventorySlot.HandsArmor, 137, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.FeetArmor, 138, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
            template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 881, 0, 0);
            Inventory = template.CloseTemplate();
            SwitchWeapon(EActiveWeaponSlot.TwoHanded);
            if (!this.Styles.Contains(Taunt))
            {
                Styles.Add(Taunt);
            }
            if (!this.Styles.Contains(AfterEvade))
            {
                Styles.Add(AfterEvade);
            }
            if (!this.Styles.Contains(EvadeFollowUp))
            {
                Styles.Add(EvadeFollowUp);
            }
            Strength = 70;
            Constitution = 100;
            Dexterity = 200;
            Quickness = 80;
            EvadeChance = 50;
            MaxDistance = 2000;
            TetherRange = 1500;
            MaxSpeedBase = 225;
            Gender = EGender.Male;
            Flags = ENpcFlags.GHOST;
            VisibleActiveWeaponSlots = 34;
            MeleeDamageType = EDamageType.Crush;
            LieutenantSaxeBrain sbrain = new LieutenantSaxeBrain();
            SetOwnBrain(sbrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            GameNpc[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Lieutenant Saxe", 277, (ERealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Lieutenant Saxe not found, creating it...");

                log.Warn("Initializing Lieutenant Saxe...");
                LieutenantSaxe HOC = new LieutenantSaxe();
                HOC.Name = "Lieutenant Saxe";
                HOC.Model = 8;
                HOC.Realm = 0;
                HOC.Level = 50;
                HOC.Size = 50;
                HOC.CurrentRegionID = 277; //hall of the corrupt
                HOC.Faction = FactionMgr.GetFactionByID(187);
                HOC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));

                HOC.Strength = 70;
                HOC.Constitution = 100;
                HOC.Dexterity = 200;
                HOC.Quickness = 80;
                HOC.MaxSpeedBase = 225;
                HOC.X = 31368;
                HOC.Y = 34750;
                HOC.Z = 15365;
                HOC.Heading = 4088;
                LieutenantSaxeBrain ubrain = new LieutenantSaxeBrain();
                HOC.SetOwnBrain(ubrain);
                HOC.AddToWorld();
                HOC.SaveIntoDatabase();
                HOC.Brain.Start();
            }
            else
                log.Warn("Lieutenant Saxe exist ingame, remove it and restart server if you want to add by script code.");
        }
    }
}




