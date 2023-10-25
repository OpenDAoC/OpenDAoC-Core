using System;
using System.Collections;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Server;
using Core.GS.Spells;

namespace Core.GS.GameUtils 
{
    /// <summary>
    /// GeneratedUniqueItem is a subclass of UniqueItem used to create RoG object
    /// Using it as a class is much more extendable to other usage than just loot and inventory
    /// </summary>
    public class GeneratedUniqueItem : DbItemUnique 
    {
        //The following properties are weights for each roll
        //It is *not* a direct chance to receive the item. It is instead
        //a chance for that item type to be randomly selected as a valid generation type
        private static int ToaItemChance = ServerProperty.ROG_TOA_ITEM_CHANCE;
        private static int ArmorWeight = ServerProperty.ROG_ARMOR_WEIGHT;
        private static int JewelryWeight = ServerProperty.ROG_MAGICAL_WEIGHT;
        private static int WeaponWeight = ServerProperty.ROG_WEAPON_WEIGHT;
        //The following 5 weights are for EACH roll on an item
        //I do not recommend putting any of them above 45
        private static int ToaStatWeight = ServerProperty.ROG_TOA_STAT_WEIGHT;
        private static int ItemStatWeight = ServerProperty.ROG_ITEM_STAT_WEIGHT;
        private static int ItemResistWeight = ServerProperty.ROG_ITEM_RESIST_WEIGHT;
        private static int ItemSkillWeight = ServerProperty.ROG_ITEM_SKILL_WEIGHT;
        private static int ItemAllSkillWeight = ServerProperty.ROG_STAT_ALLSKILL_WEIGHT;

        //base item quality for all rogs
        private static int RogStartingQual = ServerProperty.ROG_STARTING_QUAL;
        //max possible quality for any rog
        private static int RogCapQuality = ServerProperty.ROG_CAP_QUAL;
        //base Chance to get a magical RoG item, PlayerLevel*2 is added to get final value
        private static int MagicalItemOffset = ServerProperty.ROG_MAGICAL_ITEM_OFFSET;

        private EPlayerClass charClass = EPlayerClass.Unknown;

        private static Dictionary<int,Spell> ProcSpells = new Dictionary<int,Spell>();

        protected static Dictionary<EProperty, string> hPropertyToMagicPrefix = new Dictionary<EProperty, string>();

        [ScriptLoadedEvent]
        public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
        { 
            InitializeHashtables();
        }

        public GeneratedUniqueItem()
            : this((ERealm)Util.Random(1, 3), (EPlayerClass)Util.Random(1, 32), (byte)Util.Random(1, 50))
        {

        }

        #region Constructor Randomized

        public GeneratedUniqueItem(ERealm realm, EPlayerClass charClass, byte level, int minUtility = 15)
            : this(realm, charClass, level, GenerateObjectType(realm, charClass, level), minUtility)
        {

        }

        public GeneratedUniqueItem(ERealm realm, EPlayerClass charClass, byte level, EObjectType type, int minUtility = 15)
            : this(realm, charClass, level, type, GenerateItemType(type), minUtility)
        {

        }

        public GeneratedUniqueItem(ERealm realm, EPlayerClass charClass, byte level, EObjectType type, EInventorySlot slot, int minUtility = 15)
            : this(realm, charClass, level, type, slot, GenerateDamageType(type, charClass), minUtility)
        {

        }

        public GeneratedUniqueItem(ERealm realm, EPlayerClass charClass, byte level, EObjectType type, EInventorySlot slot, EDamageType dmg, int minUtility = 15)
            : this(false, realm, charClass, level, type, slot, dmg, minUtility)
        {

        }

        public GeneratedUniqueItem(bool toa)
            : this(toa, (ERealm)Util.Random(1, 3), (EPlayerClass)Util.Random(1, 32), (byte)Util.Random(1, 50))
        {

        }

        public GeneratedUniqueItem(bool toa, ERealm realm, EPlayerClass charClass, byte level)
            : this(toa, realm, charClass, level, GenerateObjectType(realm, charClass, level))
        {

        }

        public GeneratedUniqueItem(bool toa, ERealm realm, EPlayerClass charClass, byte level, EObjectType type)
            : this(toa, realm, charClass, level, type, GenerateItemType(type))
        {

        }

        public GeneratedUniqueItem(bool toa, ERealm realm, EPlayerClass charClass, byte level, EObjectType type, EInventorySlot slot)
            : this(toa, realm, charClass, level, type, slot, GenerateDamageType(type, charClass))
        {

        }

        public GeneratedUniqueItem(bool toa, ERealm realm, EPlayerClass charClass, byte level, EObjectType type, EInventorySlot slot, EDamageType dmg, int utilityMinimum = 15)
            : base()
        {
            this.Realm = (int)realm;
            this.Level = level;
            this.Object_Type = (int)type;
            this.Item_Type = (int)slot;
            this.Type_Damage = (int)dmg;
            this.charClass = charClass;

            // shouldn't need more Randomized public set values

            //need stats before naming
            this.GenerateItemStats();

            //name item
            this.GenerateItemNameModel();

            //set item quality (this can be called again by any script with real mob values)
            this.GenerateItemQuality((double)Util.Random(0, 6) - 3);

            //item magical bonuses
            //if staff and magic..... focus
            this.GenerateMagicalBonuses(toa);

            this.Color = GetRandomColorForRealm(realm);

            this.IsDropable = true;
            this.IsPickable = true;
            this.IsTradable = true;
            this.CapUtility(this.Level, utilityMinimum);

            if (this.Level > 51)
            {
                this.Level = 51;
            }

            this.GenerateProc();

            //item bonus
            int temp = this.Level - 15;
            temp -= temp % 5;
            this.Bonus = temp;
            if (this.Bonus < 5)
                this.Bonus = 5;

            //constants
            int condition = this.Level * 2000;
            this.Condition = condition;
            this.MaxCondition = condition;
            this.Durability = condition;
            this.MaxDurability = condition;

            this.GenerateItemWeight();

            this.Description = "Atlas ROG | " + charClass.ToString();

            //don't add to database implicitly, must be done explicitly
            this.AllowAdd = false;
        }

        #endregion

        #region generate item properties
        public void GenerateItemQuality(double conlevel)
        {
            // set base quality
            int minQuality = RogStartingQual + Math.Max(0, this.Level - 59);
            int maxQuality = (int)(1.310 * conlevel + 94.29 + 3);

            if (this.Level > 51 && minQuality < 97)
                minQuality = 97;

            // CAPS
            maxQuality = Math.Min(maxQuality, RogCapQuality);  // unique objects capped at 99 quality
            minQuality = Math.Min(minQuality, RogCapQuality);  // unique objects capped at 99 quality

            maxQuality = Math.Max(maxQuality, minQuality);

            this.Quality = Util.Random(minQuality, maxQuality);

            this.Price = MoneyMgr.SetAutoPrice(this.Level, this.Quality);
            this.Price /= 8;
            if (this.Price <= 0)
                this.Price = 2; // 2c as sell price is 50%
        }

        private void GenerateItemStats()
        {
            int templevel = 0;
            if (Level > 51)
            {
                templevel = this.Level;
                this.Level = 51;
            }

            EObjectType type = (EObjectType)this.Object_Type;

            //special property for instrument
            if (type == EObjectType.Instrument)
                this.DPS_AF = Util.Random(0, 3);

            //set hand
            switch (type)
            {
                //two handed weapons
                case EObjectType.CelticSpear:
                case EObjectType.CompositeBow:
                case EObjectType.Crossbow:
                case EObjectType.Fired:
                case EObjectType.Instrument:
                case EObjectType.LargeWeapons:
                case EObjectType.Longbow:
                case EObjectType.PolearmWeapon:
                case EObjectType.RecurvedBow:
                case EObjectType.Scythe:
                case EObjectType.Spear:
                case EObjectType.Staff:
                case EObjectType.TwoHandedWeapon:
                case EObjectType.MaulerStaff: //Maulers
                    {
                        this.Hand = 1;
                        break;
                    }
                //right or left handed weapons
                case EObjectType.Blades:
                case EObjectType.Blunt:
                case EObjectType.CrushingWeapon:
                case EObjectType.HandToHand:
                case EObjectType.Piercing:
                case EObjectType.SlashingWeapon:
                case EObjectType.ThrustWeapon:
                case EObjectType.FistWraps: //Maulers
                    {
                        if ((EInventorySlot)this.Item_Type == EInventorySlot.LeftHandWeapon)
                            this.Hand = 2;
                        break;
                    }
                //left handed weapons
                case EObjectType.LeftAxe:
                case EObjectType.Shield:
                    {
                        this.Hand = 2;
                        break;
                    }
                //right or two handed weapons
                case EObjectType.Sword:
                case EObjectType.Hammer:
                case EObjectType.Axe:
                    {
                        if ((EInventorySlot)this.Item_Type == EInventorySlot.TwoHandWeapon)
                            this.Hand = 1;
                        break;
                    }
            }

            //set dps_af and spd_abs
            if ((int)type >= (int)EObjectType._FirstArmor && (int)type <= (int)EObjectType._LastArmor)
            {
                if (type == EObjectType.Cloth)
                    this.DPS_AF = this.Level;
                else this.DPS_AF = this.Level * 2;
                this.SPD_ABS = GetAbsorb(type);
            }

            switch (type)
            {
                case EObjectType.Axe:
                case EObjectType.Blades:
                case EObjectType.Blunt:
                case EObjectType.CelticSpear:
                case EObjectType.CompositeBow:
                case EObjectType.Crossbow:
                case EObjectType.CrushingWeapon:
                case EObjectType.Fired:
                case EObjectType.Flexible:
                case EObjectType.Hammer:
                case EObjectType.HandToHand:
                case EObjectType.LargeWeapons:
                case EObjectType.LeftAxe:
                case EObjectType.Longbow:
                case EObjectType.Piercing:
                case EObjectType.PolearmWeapon:
                case EObjectType.RecurvedBow:
                case EObjectType.Scythe:
                case EObjectType.Shield:
                case EObjectType.SlashingWeapon:
                case EObjectType.Spear:
                case EObjectType.Staff:
                case EObjectType.Sword:
                case EObjectType.ThrustWeapon:
                case EObjectType.TwoHandedWeapon:
                case EObjectType.MaulerStaff: //Maulers
                case EObjectType.FistWraps: //Maulers
                    {
                        this.DPS_AF = (int)(((this.Level * 0.3) + 1.2) * 10);
                        SetWeaponSpeed();
                        break;
                    }
            }

            if (templevel != 0)
                this.Level = templevel;
        }

        private void GenerateProc()
        {
            if (!Util.Chance(1)) return;
            if (this.Object_Type == (int)EObjectType.Magical)
                return;

            this.ProcChance = 10;

            if(((this.Object_Type >= (int)EObjectType._FirstWeapon && this.Object_Type <= (int)EObjectType._LastWeapon) || this.Object_Type == (int)EObjectType.Shield))
            {
                if (Util.Chance(50))
                {
                    //LT procs
                    if (Level < 10)
                    {
                        this.ProcSpellID = 8010;
                        this.LevelRequirement = 1;
                    }
                    else if (Level < 15)
                    {
                        this.ProcSpellID = 8011;
                        this.LevelRequirement = 10;
                    }
                    else if (Level < 20)
                    {
                        this.ProcSpellID = 8012;
                        this.LevelRequirement = 15;
                    }
                    else if (Level < 25)
                    {
                        this.ProcSpellID = 8013;
                        this.LevelRequirement = 20;
                    }
                    else if (Level < 30)
                    {
                        this.ProcSpellID = 8014;
                        this.LevelRequirement = 25;
                    }
                    else if (Level < 35)
                    {
                        this.ProcSpellID = 8015;
                        this.LevelRequirement = 30;
                    }
                    else if (Level < 40)
                    {
                        this.ProcSpellID = 8016;
                        this.LevelRequirement = 35;
                    }
                    else if (Level < 43)
                    {
                        this.ProcSpellID = 8017;
                        this.LevelRequirement = 40;
                    }
                }
                else
                {
                    //DD procs
                    if (Level < 10)
                    {
                        this.ProcSpellID = 8020;
                        this.LevelRequirement = 1;
                    }
                    else if (Level < 15)
                    {
                        this.ProcSpellID = 8021;
                        this.LevelRequirement = 10;
                    }
                    else if (Level < 20)
                    {
                        this.ProcSpellID = 8022;
                        this.LevelRequirement = 15;
                    }
                    else if (Level < 25)
                    {
                        this.ProcSpellID = 8023;
                        this.LevelRequirement = 20;
                    }
                    else if (Level < 30)
                    {
                        this.ProcSpellID = 8024;
                        this.LevelRequirement = 25;
                    }
                    else if (Level < 35)
                    {
                        this.ProcSpellID = 8025;
                        this.LevelRequirement = 30;
                    }
                    else if (Level < 40)
                    {
                        this.ProcSpellID = 8026;
                        this.LevelRequirement = 35;
                    }
                    else if (Level < 43)
                    {
                        this.ProcSpellID = 8027;
                        this.LevelRequirement = 40;
                    }
                }
            }
            else if(this.Object_Type >= (int)EObjectType._FirstArmor && this.Object_Type <= (int)EObjectType._LastArmor && this.Item_Type == Slot.TORSO)
            {
                if (Util.Chance(50))
                {
                    //Heal procs
                    if (Level < 10)
                    {
                        this.ProcSpellID = 8030;
                        this.LevelRequirement = 1;
                    }
                    else if (Level < 15)
                    {
                        this.ProcSpellID = 8031;
                        this.LevelRequirement = 10;
                    }
                    else if (Level < 20)
                    {
                        this.ProcSpellID = 8032;
                        this.LevelRequirement = 15;
                    }
                    else if (Level < 25)
                    {
                        this.ProcSpellID = 8033;
                        this.LevelRequirement = 20;
                    }
                    else if (Level < 30)
                    {
                        this.ProcSpellID = 8034;
                        this.LevelRequirement = 25;
                    }
                    else if (Level < 35)
                    {
                        this.ProcSpellID = 8035;
                        this.LevelRequirement = 30;
                    }
                    else if (Level < 40)
                    {
                        this.ProcSpellID = 8036;
                        this.LevelRequirement = 35;
                    }
                    else if (Level < 43)
                    {
                        this.ProcSpellID = 8037;
                        this.LevelRequirement = 40;
                    }
                }
                else
                {
                    //ABS procs
                    if (Level < 10)
                    {
                        this.ProcSpellID = 8040;
                        this.LevelRequirement = 1;
                    }
                    else if (Level < 15)
                    {
                        this.ProcSpellID = 8041;
                        this.LevelRequirement = 10;
                    }
                    else if (Level < 20)
                    {
                        this.ProcSpellID = 8042;
                        this.LevelRequirement = 15;
                    }
                    else if (Level < 25)
                    {
                        this.ProcSpellID = 8043;
                        this.LevelRequirement = 20;
                    }
                    else if (Level < 30)
                    {
                        this.ProcSpellID = 8044;
                        this.LevelRequirement = 25;
                    }
                    else if (Level < 35)
                    {
                        this.ProcSpellID = 8045;
                        this.LevelRequirement = 30;
                    }
                    else if (Level < 40)
                    {
                        this.ProcSpellID = 8046;
                        this.LevelRequirement = 35;
                    }
                    else if (Level < 43)
                    {
                        this.ProcSpellID = 8047;
                        this.LevelRequirement = 40;
                    }
                }
                
            }
        }

        private int GetRandomColorForRealm(ERealm realm)
        {
            List<int> validColors = new List<int>();
            validColors.Add(0); //white

            if (Level > 10)
            {
                validColors.Add(6); //grey
                validColors.Add(4); //old yellow
            }
            
            if (Level > 20)
            {
                validColors.Add(17); //iron
                validColors.Add(16); //bronze
            }
            
            if (Level > 30)
            {
                validColors.Add(18); //steel
                validColors.Add(19); //alloy
                validColors.Add(72); //grey1
            }
            
            if (Level > 40)
            {
                validColors.Add(22); //asterite
                validColors.Add(20); //fine alloy
                validColors.Add(73); //gray2
            }
            
            if (Level > 50)
            {
                validColors.Add(21); //mithril
                validColors.Add(25); //vaanum
                validColors.Add(26); //adamantium
                validColors.Add(43); //black cloth
                validColors.Add(74); //grey3
                validColors.Add(118); //charcoal
            }
            
            switch(realm){
                case ERealm.Hibernia:
                    if (Level > 10)
                    {
                        validColors.Add(2); //old green
                    }
            
                    if (Level > 20)
                    {
                        validColors.Add(10); //leather green
                        
                    }
            
                    if (Level > 30)
                    {
                        validColors.Add(31); //yellow green
                        validColors.Add(32); //green
                    }
            
                    if (Level > 40)
                    {
                        validColors.Add(33); //blue green
                        validColors.Add(68); //green1
                    }
            
                    if (Level > 50)
                    {
                        validColors.Add(70); //green3
                        validColors.Add(71); //green4
                        validColors.Add(142); //forest green
                    }
                    break;
                case ERealm.Albion:
                    if (Level > 10)
                    {
                        validColors.Add(1); //old red
                    }
            
                    if (Level > 20)
                    {
                        validColors.Add(9); //leather red
                        
                    }
            
                    if (Level > 30)
                    {
                        validColors.Add(24); //yellow red
                        validColors.Add(27); //red
                    }
            
                    if (Level > 40)
                    {
                        validColors.Add(64); //red1
                        validColors.Add(65); //red2
                    }
            
                    if (Level > 50)
                    {
                        validColors.Add(66); //red3
                        validColors.Add(67); //red4
                        validColors.Add(143); //burgundy
                    }
                    break;
                case ERealm.Midgard:
                    if (Level > 10)
                    {
                        validColors.Add(3); //old red
                    }
            
                    if (Level > 20)
                    {
                        validColors.Add(14); //leather red
                        
                    }
            
                    if (Level > 30)
                    {
                        validColors.Add(34); //turqoise cloth
                        validColors.Add(35); //light blue
                    }
            
                    if (Level > 40)
                    {
                        validColors.Add(36); //blue
                        validColors.Add(51); //blue1
                    }
            
                    if (Level > 50)
                    {
                        validColors.Add(52); //blue2
                        validColors.Add(54); //blue4
                        validColors.Add(86); //blue4 again?
                        validColors.Add(141); //navy blue
                    }
                    break;
            }

            return validColors[Util.Random(validColors.Count - 1)];
        }

        private void GenerateMagicalBonuses(bool toa)
        {
            // unique objects have more bonuses as level rises

            int number = 0;

            // WHRIA
            //if (this.Level>60) number++;
            if (this.Level > 60 && Util.Chance(10)) number++;
            if (this.Level > 70 && Util.Chance(25)) number++;
            if (this.Level > 70 && Util.Chance(25)) number++;
            if (this.Level > 80 && Util.Chance(80)) number++;
            // END

            if (Util.Chance(MagicalItemOffset + this.Level * 2) || (EObjectType)Object_Type == EObjectType.Magical) // 100% magical starting at level 40
            {
                //1
                number++;

                if (Util.Chance(this.Level * 8 - 40)) // level 6 - 17 (100%)
                {
                    //2
                    number++;

                    if (Util.Chance(this.Level * 6 - 60)) // level 11 - 27 (100%)
                    {
                        //3
                        number++;

                        if (Util.Chance(this.Level * 4 - 80)) // level 21 - 45 (100%)
                        {
                            //4
                            number++;

                            if (this.Level > 75)
                                number++; // 5
                        }
                    }
                }

            }


            // Magical items have at least 1 bonus
            if (this.Object_Type == (int)EObjectType.Magical && number < 1)
                number = 1;


            bool fMagicScaled = false;
            bool fAddedBonus = false;

            double quality = (double)this.Quality * .01;

            double multiplier = (quality * quality * quality) + 0.20;

            if (toa)
            {
                multiplier += 0.15;
            }

            for (int i = 0; i < number; i++)
            {
                eBonusType type = this.GetPropertyType(toa);
                EProperty property = this.GetProperty(type);
                double tmpMulti = multiplier;
                if (type == eBonusType.Stat)
                    tmpMulti = 1;
                if (!this.BonusExists(property))
                {
                    int amount = (int)Math.Ceiling((double)GetBonusAmount(type, property));
                    this.WriteBonus(property, amount);
                    fAddedBonus = true;
                    if (!fMagicScaled)
                    {
                        fMagicScaled = true;
                        multiplier *= 0.75;
                    }
                }
            }

            // non magical items get lowercase names
            if (number == 0 || !fAddedBonus)
                this.Name = this.Name.ToLower();
        }

        private EProperty GetPropertyFromBonusLine(int BonusLine)
        {
            int property = 0;

            switch (BonusLine)
            {
                case 1:
                    property = Bonus1Type;
                    break;
                case 2:
                    property = Bonus2Type;
                    break;
                case 3:
                    property = Bonus3Type;
                    break;
                case 4:
                    property = Bonus4Type;
                    break;
                case 5:
                    property = Bonus5Type;
                    break;
                case 6:
                    property = Bonus6Type;
                    break;
                case 7:
                    property = Bonus7Type;
                    break;
                case 8:
                    property = Bonus8Type;
                    break;
                case 9:
                    property = Bonus9Type;
                    break;
                case 10:
                    property = Bonus10Type;
                    break;
                case 11:
                    property = ExtraBonusType;
                    break;
            }

            return (EProperty)property;
        }

        private eBonusType GetPropertyType(bool toa)
        {
            //allfocus
            if (CanAddFocus())
                return eBonusType.Focus;
            
			// ToA allows stat cap bonuses
			if (toa && Util.Chance(ToaItemChance))
			{
				return eBonusType.AdvancedStat;
			}

            if (Level < 10)
            {
                if (Util.Chance(65))
                    return eBonusType.Stat;
                else
                    return eBonusType.Skill;
            }

            //weighted rolls
            if (ServerProperty.ROG_USE_WEIGHTED_GENERATION)
            {
                List<eBonusType> bonTypes = new List<eBonusType>();
                if (Util.Chance(ItemStatWeight)) { bonTypes.Add(eBonusType.Stat); }
                if (Util.Chance(ItemResistWeight)) { bonTypes.Add(eBonusType.Resist); }
                if (Util.Chance(ItemSkillWeight) && !HasSkill) { bonTypes.Add(eBonusType.Skill); }
            
                //if none of the object types were added, randomly pick between stat/resist
                if (bonTypes.Count < 1)
                {
                    int bonType = Util.Random(3);
                    if (bonType == 1) bonType--; //no toa stats
                    bonTypes.Add((eBonusType)bonType);
                }

                return bonTypes[Util.Random(bonTypes.Count - 1)];
            }
            
            //simple generation
            int rand = Util.Random(100);
            if (rand < 15)
                return eBonusType.Skill;
            if (rand < 45)
                return eBonusType.Resist;
            return eBonusType.Stat;
        }

        private bool HasSkill
        {
            get { return (this.Bonus1Type == (int) eBonusType.Skill
                || this.Bonus2Type == (int) eBonusType.Skill
                || this.Bonus3Type == (int) eBonusType.Skill
                || this.Bonus4Type == (int) eBonusType.Skill
                || this.Bonus5Type == (int) eBonusType.Skill
                || this.Bonus6Type == (int) eBonusType.Skill
                || this.Bonus7Type == (int) eBonusType.Skill
                || this.Bonus8Type == (int) eBonusType.Skill
                || this.Bonus9Type == (int) eBonusType.Skill
                || this.Bonus10Type == (int) eBonusType.Skill
                ); }
        }

        private bool CanAddFocus()
        {
            if (this.Object_Type == (int)EObjectType.Staff)
            {
                if (this.Bonus1Type != 0)
                    return false;

                if (this.Realm == (int)ERealm.Albion && this.Description == "friar")
                    return false;

                return true;
            }

            return false;
        }
        #endregion

        #region check valid stat
        private EProperty GetProperty(eBonusType type)
        {
            switch (type)
            {
                case eBonusType.Focus:
                    {
                        return EProperty.AllFocusLevels;
                    }
                case eBonusType.Resist:
                    {
                        return (EProperty)Util.Random((int)EProperty.Resist_First, (int)EProperty.Resist_Last);
                    }
                case eBonusType.Skill:
                    {
                        // fill valid skills
                        ArrayList validSkills = new ArrayList();

                        bool fIndividualSkill = false;

                        // All Skills is never combined with any other skill
                        if (!BonusExists(EProperty.AllSkills))
                        {
                            // All type skills never combined with individual skills
                            if (!BonusExists(EProperty.AllMagicSkills) &&
                                !BonusExists(EProperty.AllMeleeWeaponSkills) &&
                                !BonusExists(EProperty.AllDualWieldingSkills) &&
                                !BonusExists(EProperty.AllArcherySkills))
                            {
                                // individual realm specific skills
                                if ((ERealm)this.Realm == ERealm.Albion)
                                {
                                    foreach (EProperty property in AlbSkillBonus)
                                    {
                                        if (!BonusExists(property) && SkillIsValidForClass(property) && !IsCompetingSkillLine(property))
                                        {
                                            if (SkillIsValidForObjectType(property))
                                                validSkills.Add(property);
                                        }
                                        else
                                            fIndividualSkill = true;
                                    }
                                }
                                else if ((ERealm)this.Realm == ERealm.Hibernia)
                                {
                                    foreach (EProperty property in HibSkillBonus)
                                    {
                                        if (!BonusExists(property) && SkillIsValidForClass(property) && !IsCompetingSkillLine(property))
                                        {
                                            if (SkillIsValidForObjectType(property))
                                                validSkills.Add(property);
                                        }
                                        else
                                            fIndividualSkill = true;
                                    }
                                }
                                else if ((ERealm)this.Realm == ERealm.Midgard)
                                {
                                    foreach (EProperty property in MidSkillBonus)
                                    {
                                        if (!BonusExists(property) && SkillIsValidForClass(property) && !IsCompetingSkillLine(property))
                                        {
                                            if (SkillIsValidForObjectType(property))
                                                validSkills.Add(property);
                                        }
                                        else
                                            fIndividualSkill = true;
                                    }
                                }

                                if (!fIndividualSkill)
                                {
                                    // ok to add AllSkills, but reduce the chance
                                    if (SkillIsValidForObjectType(EProperty.AllSkills) && Util.Chance(ItemAllSkillWeight))
                                        validSkills.Add(EProperty.AllSkills);
                                }
                            }

                            // All type skills never combined with individual skills
                            if (!fIndividualSkill)
                            {
                                if (!BonusExists(EProperty.AllMagicSkills) && SkillIsValidForObjectType(EProperty.AllMagicSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(EProperty.AllMagicSkills);

                                if (!BonusExists(EProperty.AllMeleeWeaponSkills) && SkillIsValidForObjectType(EProperty.AllMeleeWeaponSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(EProperty.AllMeleeWeaponSkills);

                                if (!BonusExists(EProperty.AllDualWieldingSkills) && SkillIsValidForObjectType(EProperty.AllDualWieldingSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(EProperty.AllDualWieldingSkills);

                                if (!BonusExists(EProperty.AllArcherySkills) && SkillIsValidForObjectType(EProperty.AllArcherySkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(EProperty.AllArcherySkills);
                            }

                        }

                        int index = 0;
                        index = validSkills.Count - 1;
                        if (index < 1)
                        {
                            // return a safe random stat

                            type = eBonusType.Stat;

                            switch (Util.Random(0, 4))
                            {
                                case 0:
                                    return EProperty.MaxHealth;
                                case 1:
                                    return EProperty.Strength;
                                case 2:
                                    return EProperty.Dexterity;
                                case 3:
                                    return EProperty.Quickness;
                                case 4:
                                    return EProperty.Constitution;
                            }
                        }

                        return (EProperty)validSkills[Util.Random(0, index)];
                    }
                case eBonusType.Stat:
                    {
                        /*
                        // ToDo: this does not check for duplicates like INT and Acuity
                        ArrayList validStats = new ArrayList();
                        foreach (eProperty property in StatBonus)
                        {
                            if (!BonusExists(property) && StatIsValidForObjectType(property) && StatIsValidForRealm(property) && StatIsValidForClass(property))
                            {
                                validStats.Add(property);
                            }
                        }
                        return (eProperty)validStats[Util.Random(0, validStats.Count - 1)];
                        */
                        return GetWeightedStatForClass(this.charClass);
                    }
                case eBonusType.AdvancedStat:
                    {
                        // ToDo: this does not check for duplicates like INT and Acuity
                        ArrayList validStats = new ArrayList();
                        foreach (EProperty property in AdvancedStats)
                        {
                            if (!BonusExists(property) && StatIsValidForObjectType(property) && StatIsValidForRealm(property))
                                validStats.Add(property);
                        }
                        return (EProperty)validStats[Util.Random(0, validStats.Count - 1)];
                    }
            }
            return EProperty.MaxHealth;
        }

        private bool IsCompetingSkillLine(EProperty prop)
        {
            List<EProperty> skillsToCheck = new List<EProperty>();
            if(prop == EProperty.Skill_Slashing || prop == EProperty.Skill_Thrusting || prop == EProperty.Skill_Crushing)
            {
                skillsToCheck.Add(EProperty.Skill_Slashing);
                skillsToCheck.Add(EProperty.Skill_Thrusting);
                skillsToCheck.Add(EProperty.Skill_Crushing);
            }
            if (prop == EProperty.Skill_Blades || prop == EProperty.Skill_Piercing || prop == EProperty.Skill_Blunt)
            {
                skillsToCheck.Add(EProperty.Skill_Blades);
                skillsToCheck.Add(EProperty.Skill_Piercing);
                skillsToCheck.Add(EProperty.Skill_Blunt);
            }
            if (prop == EProperty.Skill_Axe || prop == EProperty.Skill_Sword || prop == EProperty.Skill_Hammer)
            {
                skillsToCheck.Add(EProperty.Skill_Axe);
                skillsToCheck.Add(EProperty.Skill_Sword);
                skillsToCheck.Add(EProperty.Skill_Hammer);
            }

            if (prop == EProperty.Skill_Matter || prop == EProperty.Skill_Body || prop == EProperty.Skill_Spirit || prop == EProperty.Skill_Mind)
            {
                skillsToCheck.Add(EProperty.Skill_Matter);
                skillsToCheck.Add(EProperty.Skill_Body);
                skillsToCheck.Add(EProperty.Skill_Spirit);
                skillsToCheck.Add(EProperty.Skill_Mind);
            }
            if (prop == EProperty.Skill_Earth || prop == EProperty.Skill_Cold || prop == EProperty.Skill_Fire || prop == EProperty.Skill_Wind)
            {
                skillsToCheck.Add(EProperty.Skill_Earth);
                skillsToCheck.Add(EProperty.Skill_Cold);
                skillsToCheck.Add(EProperty.Skill_Fire);
                skillsToCheck.Add(EProperty.Skill_Wind);
            }
            if (prop == EProperty.Skill_DeathSight || prop == EProperty.Skill_Death_Servant || prop == EProperty.Skill_Pain_working)
            {
                skillsToCheck.Add(EProperty.Skill_DeathSight);
                skillsToCheck.Add(EProperty.Skill_Death_Servant);
                skillsToCheck.Add(EProperty.Skill_Pain_working);
            }
            if (prop == EProperty.Skill_Light || prop == EProperty.Skill_Mana || prop == EProperty.Skill_Void || prop == EProperty.Skill_Enchantments || prop == EProperty.Skill_Mentalism)
            {
                skillsToCheck.Add(EProperty.Skill_Light);
                skillsToCheck.Add(EProperty.Skill_Mana);
                skillsToCheck.Add(EProperty.Skill_Void);
                skillsToCheck.Add(EProperty.Skill_Enchantments);
                skillsToCheck.Add(EProperty.Skill_Mentalism);
            }
            if (prop == EProperty.Skill_Arboreal || prop == EProperty.Skill_Creeping || prop == EProperty.Skill_Verdant)
            {
                skillsToCheck.Add(EProperty.Skill_Arboreal);
                skillsToCheck.Add(EProperty.Skill_Creeping);
                skillsToCheck.Add(EProperty.Skill_Verdant);
            }
            if (prop == EProperty.Skill_Darkness || prop == EProperty.Skill_Suppression || prop == EProperty.Skill_Runecarving || prop == EProperty.Skill_Summoning || prop == EProperty.Skill_BoneArmy)
            {
                skillsToCheck.Add(EProperty.Skill_Darkness);
                skillsToCheck.Add(EProperty.Skill_Suppression);
                skillsToCheck.Add(EProperty.Skill_Runecarving);
                skillsToCheck.Add(EProperty.Skill_Summoning);
                skillsToCheck.Add(EProperty.Skill_BoneArmy);
            }


            foreach (var propCheck in skillsToCheck)
            {
                if (Bonus1Type == (int)propCheck)
                    return true;
                if (Bonus2Type == (int)propCheck)
                    return true;
                if (Bonus3Type == (int)propCheck)
                    return true;
                if (Bonus4Type == (int)propCheck)
                    return true;
                if (Bonus5Type == (int)propCheck)
                    return true;
                if (Bonus6Type == (int)propCheck)
                    return true;
                if (Bonus7Type == (int)propCheck)
                    return true;
                if (Bonus8Type == (int)propCheck)
                    return true;
                if (Bonus9Type == (int)propCheck)
                    return true;
                if (Bonus10Type == (int)propCheck)
                    return true;
                if (ExtraBonusType == (int)propCheck)
                    return true;
            }

            return false;
        }

        private EProperty GetWeightedStatForClass(EPlayerClass charClass)
        {
            if (Util.Chance(10))
                return EProperty.MaxHealth;

            int rand = Util.Random(100);
            switch (charClass)
            {
                case EPlayerClass.Armsman:
                case EPlayerClass.Mercenary:
                case EPlayerClass.Infiltrator:
                case EPlayerClass.Scout:
                case EPlayerClass.Blademaster:
                case EPlayerClass.Hero:
                case EPlayerClass.Berserker:
                case EPlayerClass.Warrior:
                case EPlayerClass.Savage:
                case EPlayerClass.Hunter:
                case EPlayerClass.Shadowblade:
                case EPlayerClass.Nightshade:
                case EPlayerClass.Ranger:
                    //25% chance of getting any useful stat
                    //for classes who do not need mana/acuity/casting stats
                    if (rand <= 25)
                        return EProperty.Strength;
                    else if (rand <= 50)
                        return EProperty.Dexterity;
                    else if (rand <= 75)
                        return EProperty.Constitution;
                    else return EProperty.Quickness;

                case EPlayerClass.Cabalist:
                case EPlayerClass.Sorcerer:
                case EPlayerClass.Theurgist:
                case EPlayerClass.Wizard:
                case EPlayerClass.Necromancer:
                case EPlayerClass.Eldritch:
                case EPlayerClass.Enchanter:
                case EPlayerClass.Mentalist:
                case EPlayerClass.Animist:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    
                    //weight stats for casters towards dex, acu, con
                    //keep some 10% chance of str or quick since useful for carrying/occasional melee
                    if (rand <= 30)
                        return EProperty.Dexterity;
                    else if (rand <= 40)
                        return EProperty.Strength;
                    else if (rand <= 70)
                        return EProperty.Intelligence;
                    else if (rand <= 80)
                        return EProperty.Quickness;
                    else return EProperty.Constitution;

                case EPlayerClass.Runemaster:
                case EPlayerClass.Spiritmaster:
                case EPlayerClass.Bonedancer:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    //weight stats for casters towards dex, acu, con
                    //keep some 10% chance of str or quick since useful for carrying/occasional melee
                    if (rand <= 30)
                        return EProperty.Dexterity;
                    else if (rand <= 40)
                        return EProperty.Strength;
                    else if (rand <= 70)
                        return EProperty.Piety;
                    else if (rand <= 80)
                        return EProperty.Quickness;
                    else return EProperty.Constitution;

                case EPlayerClass.Paladin:
                    if (rand <= 25)
                        return EProperty.Strength;
                    else if (rand <= 40)
                        return EProperty.Dexterity;
                    else if (rand <= 60)
                        return EProperty.Quickness;
                    else if (rand <= 75)
                        return EProperty.Piety;
                    else return EProperty.Constitution;
                
                case EPlayerClass.Cleric:
                case EPlayerClass.Shaman:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    if (rand <= 10)
                        return EProperty.Strength;
                    else if (rand <= 40)
                        return EProperty.Dexterity;
                    else if (rand <= 50)
                        return EProperty.Quickness;
                    else if (rand <= 80)
                        return EProperty.Piety;
                    else return EProperty.Constitution;
                
                case EPlayerClass.Thane:
                case EPlayerClass.Reaver:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    if (rand <= 20)
                        return EProperty.Strength;
                    else if (rand <= 40)
                        return EProperty.Dexterity;
                    else if (rand <= 65)
                        return EProperty.Quickness;
                    else if (rand <= 80)
                        return EProperty.Piety;
                    else return EProperty.Constitution;

                case EPlayerClass.Friar:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    if (rand <= 25)
                        return EProperty.Piety;
                    else if (rand <= 50)
                        return EProperty.Dexterity;
                    else if (rand <= 75)
                        return EProperty.Constitution;
                    else return EProperty.Quickness;

                
                case EPlayerClass.Druid:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    if (rand <= 10)
                        return EProperty.Strength;
                    else if (rand <= 40)
                        return EProperty.Dexterity;
                    else if (rand <= 50)
                        return EProperty.Quickness;
                    else if (rand <= 80)
                        return EProperty.Empathy;
                    else return EProperty.Constitution;

                case EPlayerClass.Warden:
                    if (Util.Chance(10))
                        return EProperty.MaxMana;
                    if (rand <= 20)
                        return EProperty.Strength;
                    else if (rand <= 40)
                        return EProperty.Dexterity;
                    else if (rand <= 60)
                        return EProperty.Quickness;
                    else if (rand <= 80)
                        return EProperty.Empathy;
                    else return EProperty.Constitution;

                case EPlayerClass.Champion:
                case EPlayerClass.Valewalker:
                    if (Util.Chance(10))
                        return EProperty.MaxMana;
                    if (rand <= 22)
                        return EProperty.Strength;
                    else if (rand <= 44)
                        return EProperty.Dexterity;
                    else if (rand <= 66)
                        return EProperty.Quickness;
                    else if (rand <= 88)
                        return EProperty.Constitution;
                    else return EProperty.Intelligence;

                case EPlayerClass.Bard:
                case EPlayerClass.Skald:
                case EPlayerClass.Minstrel:
                    if (Util.Chance(20))
                        return EProperty.MaxMana;
                    if (rand <= 22)
                        return EProperty.Strength;
                    else if (rand <= 44)
                        return EProperty.Dexterity;
                    else if (rand <= 66)
                        return EProperty.Quickness;
                    else if (rand <= 88)
                        return EProperty.Constitution;
                    else return EProperty.Charisma;

                case EPlayerClass.Healer:
                    if (Util.Chance(15))
                        return EProperty.MaxMana;
                    if (rand <= 30)
                        return EProperty.Dexterity;
                    else if (rand <= 60)
                        return EProperty.Piety;
                    else if (rand <= 80)
                        return EProperty.Constitution;
                    else return EProperty.Strength;
            }
            return EProperty.Constitution;

        }

        private bool SkillIsValidForClass(EProperty property)
        {
            switch (charClass)
            {
                case EPlayerClass.Paladin:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Crushing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Two_Handed ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Armsman:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Crushing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Two_Handed ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Polearms ||
                        property == EProperty.Skill_Cross_Bows ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Reaver:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Crushing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Flexible_Weapon ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_SoulRending ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Mercenary:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Crushing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Dual_Wield ||
                        property == EProperty.AllDualWieldingSkills ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Cleric:
                    if (property == EProperty.Skill_Rejuvenation ||
                        property == EProperty.Skill_Enhancement ||
                        property == EProperty.Skill_Smiting ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Friar:
                    if (property == EProperty.Skill_Rejuvenation ||
                        property == EProperty.Skill_Enhancement ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Staff ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Infiltrator:
                    if (property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Envenom ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Critical_Strike ||
                        property == EProperty.Skill_Dual_Wield ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllDualWieldingSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Minstrel:
                    if (property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Instruments ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Scout:
                    if (property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Slashing ||
                        property == EProperty.Skill_Thrusting ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Long_bows ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllArcherySkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Cabalist:
                    if (property == EProperty.Skill_Matter ||
                        property == EProperty.Skill_Body ||
                        property == EProperty.Skill_Spirit ||
                        property == EProperty.Focus_Matter ||
                        property == EProperty.Focus_Body ||
                        property == EProperty.Focus_Spirit ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Sorcerer:
                    if (property == EProperty.Skill_Matter ||
                        property == EProperty.Skill_Body ||
                        property == EProperty.Skill_Mind ||
                        property == EProperty.Focus_Matter ||
                        property == EProperty.Focus_Body ||
                        property == EProperty.Focus_Mind ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Theurgist:
                    if (property == EProperty.Skill_Earth ||
                        property == EProperty.Skill_Cold ||
                        property == EProperty.Skill_Wind ||
                        property == EProperty.Focus_Earth ||
                        property == EProperty.Focus_Cold ||
                        property == EProperty.Focus_Air ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Wizard:
                    if (property == EProperty.Skill_Earth ||
                        property == EProperty.Skill_Cold ||
                        property == EProperty.Skill_Fire ||
                        property == EProperty.Focus_Earth ||
                        property == EProperty.Focus_Cold ||
                        property == EProperty.Focus_Fire ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Necromancer:
                    if (property == EProperty.Skill_DeathSight ||
                        property == EProperty.Skill_Death_Servant ||
                        property == EProperty.Skill_Pain_working ||
                        property == EProperty.Focus_Earth ||
                        property == EProperty.Focus_Cold ||
                        property == EProperty.Focus_Air ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Bard:
                    if (property == EProperty.Skill_Regrowth ||
                        property == EProperty.Skill_Nurture ||
                        property == EProperty.Skill_Music ||
                        property == EProperty.Skill_Blunt ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Druid:
                    if (property == EProperty.Skill_Regrowth ||
                        property == EProperty.Skill_Nurture ||
                        property == EProperty.Skill_Nature ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Warden:
                    if (property == EProperty.Skill_Regrowth ||
                        property == EProperty.Skill_Nurture ||
                        property == EProperty.Skill_Blunt ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Blademaster:
                    if (property == EProperty.Skill_Blunt ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Piercing ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Celtic_Dual ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllDualWieldingSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Hero:
                    if (property == EProperty.Skill_Blunt ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Piercing ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Celtic_Spear ||
                        property == EProperty.Skill_Large_Weapon ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Champion:
                    if (property == EProperty.Skill_Blunt ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Piercing ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.Skill_Valor ||
                        property == EProperty.Skill_Large_Weapon ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Eldritch:
                    if (property == EProperty.Skill_Light ||
                        property == EProperty.Skill_Mana ||
                        property == EProperty.Skill_Void ||
                        property == EProperty.Focus_Light ||
                        property == EProperty.Focus_Mana ||
                        property == EProperty.Focus_Void ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Enchanter:
                    if (property == EProperty.Skill_Light ||
                        property == EProperty.Skill_Mana ||
                        property == EProperty.Skill_Enchantments ||
                        property == EProperty.Focus_Light ||
                        property == EProperty.Focus_Mana ||
                        property == EProperty.Focus_Enchantments ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Mentalist:
                    if (property == EProperty.Skill_Light ||
                        property == EProperty.Skill_Mana ||
                        property == EProperty.Skill_Mentalism ||
                        property == EProperty.Focus_Light ||
                        property == EProperty.Focus_Mana ||
                        property == EProperty.Focus_Mentalism ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Nightshade:
                    if (property == EProperty.Skill_Envenom ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Piercing ||
                        property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Critical_Strike ||
                        property == EProperty.Skill_Celtic_Dual ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllDualWieldingSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Ranger:
                    if (property == EProperty.Skill_RecurvedBow ||
                        property == EProperty.Skill_Blades ||
                        property == EProperty.Skill_Piercing ||
                        property == EProperty.Skill_Celtic_Dual ||
                        property == EProperty.Skill_Stealth ||
                        property == EProperty.AllArcherySkills ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllDualWieldingSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Animist:
                    if (property == EProperty.Skill_Arboreal ||
                        property == EProperty.Skill_Creeping ||
                        property == EProperty.Skill_Verdant ||
                        property == EProperty.Focus_Arboreal ||
                        property == EProperty.Focus_CreepingPath ||
                        property == EProperty.Focus_Verdant ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Valewalker:
                    if (property == EProperty.Skill_Arboreal ||
                        property == EProperty.Skill_Scythe ||
                        property == EProperty.Skill_Parry ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Berserker:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Hammer ||
                        property == EProperty.Skill_Left_Axe ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Warrior:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Hammer ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Skald:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Hammer ||
                        property == EProperty.Skill_Battlesongs ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Thane:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Hammer ||
                        property == EProperty.Skill_Stormcalling ||
                        property == EProperty.Skill_Shields ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Savage:
                    if (property == EProperty.Skill_Parry ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Hammer ||
                        property == EProperty.Skill_Savagery ||
                        property == EProperty.Skill_HandToHand ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Healer:
                    if (property == EProperty.Skill_Mending ||
                        property == EProperty.Skill_Augmentation ||
                        property == EProperty.Skill_Pacification ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Shaman:
                    if (property == EProperty.Skill_Mending ||
                        property == EProperty.Skill_Augmentation ||
                        property == EProperty.Skill_Subterranean ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Hunter:
                    if (property == EProperty.Skill_BeastCraft ||
                        property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Composite ||
                        property == EProperty.Skill_Spear ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Shadowblade:
                    if (property == EProperty.Skill_Envenom ||
                        property == EProperty.Skill_Stealth ||
                        property == EProperty.Skill_Sword ||
                        property == EProperty.Skill_Axe ||
                        property == EProperty.Skill_Left_Axe ||
                        property == EProperty.Skill_Critical_Strike ||
                        property == EProperty.AllMeleeWeaponSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Runemaster:
                    if (property == EProperty.Skill_Darkness ||
                        property == EProperty.Skill_Suppression ||
                        property == EProperty.Skill_Runecarving ||
                        property == EProperty.Focus_Darkness ||
                        property == EProperty.Focus_Suppression ||
                        property == EProperty.Focus_Runecarving ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Spiritmaster:
                    if (property == EProperty.Skill_Darkness ||
                        property == EProperty.Skill_Suppression ||
                        property == EProperty.Skill_Summoning ||
                        property == EProperty.Focus_Darkness ||
                        property == EProperty.Focus_Suppression ||
                        property == EProperty.Focus_Summoning ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
                case EPlayerClass.Bonedancer:
                    if (property == EProperty.Skill_Darkness ||
                        property == EProperty.Skill_Suppression ||
                        property == EProperty.Skill_BoneArmy ||
                        property == EProperty.Focus_Darkness ||
                        property == EProperty.Focus_Suppression ||
                        property == EProperty.Focus_BoneArmy ||
                        property == EProperty.AllFocusLevels ||
                        property == EProperty.AllMagicSkills ||
                        property == EProperty.AllSkills
                        )
                        return true;
                    return false;
            }

            return false;
        }

        private bool StatIsValidForObjectType(EProperty property)
        {
            switch ((EObjectType)this.Object_Type)
            {
                case EObjectType.Magical:
                    return StatIsValidForRealm(property) && StatIsValidForClass(property);
                case EObjectType.Cloth:
                case EObjectType.Leather:
                case EObjectType.Studded:
                case EObjectType.Reinforced:
                case EObjectType.Chain:
                case EObjectType.Scale:
                case EObjectType.Plate:
                    return StatIsValidForArmor(property) && StatIsValidForClass(property);
                case EObjectType.Axe:
                case EObjectType.Blades:
                case EObjectType.Blunt:
                case EObjectType.CelticSpear:
                case EObjectType.CompositeBow:
                case EObjectType.Crossbow:
                case EObjectType.CrushingWeapon:
                case EObjectType.Fired:
                case EObjectType.Flexible:
                case EObjectType.Hammer:
                case EObjectType.HandToHand:
                case EObjectType.Instrument:
                case EObjectType.LargeWeapons:
                case EObjectType.LeftAxe:
                case EObjectType.Longbow:
                case EObjectType.Piercing:
                case EObjectType.PolearmWeapon:
                case EObjectType.RecurvedBow:
                case EObjectType.Scythe:
                case EObjectType.Shield:
                case EObjectType.SlashingWeapon:
                case EObjectType.Spear:
                case EObjectType.Staff:
                case EObjectType.Sword:
                case EObjectType.ThrustWeapon:
                case EObjectType.FistWraps: //Maulers
                case EObjectType.MaulerStaff: //Maulers
                case EObjectType.TwoHandedWeapon:
                    return StatIsValidForWeapon(property) && StatIsValidForClass(property);
            }
            return true;
        }

        private bool StatIsValidForClass(EProperty property)
        {
            switch (property)
            {
                case EProperty.MaxMana: //mana isn't a thing!! >:(
                case EProperty.PowerPool:
                    if (charClass == EPlayerClass.Armsman ||
                        charClass == EPlayerClass.Mercenary ||
                        charClass == EPlayerClass.Infiltrator ||
                        charClass == EPlayerClass.Scout ||
                        charClass == EPlayerClass.Paladin ||
                        charClass == EPlayerClass.Blademaster ||
                        charClass == EPlayerClass.Hero ||
                        charClass == EPlayerClass.Nightshade ||
                        charClass == EPlayerClass.Ranger ||
                        charClass == EPlayerClass.Berserker ||
                        charClass == EPlayerClass.Warrior ||
                        charClass == EPlayerClass.Savage ||
                        charClass == EPlayerClass.Shadowblade)
                    {
                        return false;
                    }
                    return true;

                case EProperty.Acuity:
                    if (charClass == EPlayerClass.Armsman ||
                        charClass == EPlayerClass.Mercenary ||
                        charClass == EPlayerClass.Paladin ||
                        charClass == EPlayerClass.Reaver ||
                        charClass == EPlayerClass.Infiltrator ||
                        charClass == EPlayerClass.Scout ||
                        charClass == EPlayerClass.Warden ||
                        charClass == EPlayerClass.Champion ||
                        charClass == EPlayerClass.Nightshade ||
                        charClass == EPlayerClass.Ranger ||
                        charClass == EPlayerClass.Blademaster ||
                        charClass == EPlayerClass.Hero ||
                        charClass == EPlayerClass.Hunter ||
                        charClass == EPlayerClass.Berserker ||
                        charClass == EPlayerClass.Warrior ||
                        charClass == EPlayerClass.Savage ||
                        charClass == EPlayerClass.Shadowblade)
                    {
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private bool SkillIsValidForObjectType(EProperty property)
        {
            switch ((EObjectType)this.Object_Type)
            {
                case EObjectType.Magical:
                    return SkillIsValidForMagical(property);
                case EObjectType.Cloth:
                case EObjectType.Leather:
                case EObjectType.Studded:
                case EObjectType.Reinforced:
                case EObjectType.Chain:
                case EObjectType.Scale:
                case EObjectType.Plate:
                    return SkillIsValidForArmor(property);
                case EObjectType.Axe:
                case EObjectType.Blades:
                case EObjectType.Blunt:
                case EObjectType.CelticSpear:
                case EObjectType.CompositeBow:
                case EObjectType.Crossbow:
                case EObjectType.CrushingWeapon:
                case EObjectType.Fired:
                case EObjectType.Flexible:
                case EObjectType.Hammer:
                case EObjectType.HandToHand:
                case EObjectType.Instrument:
                case EObjectType.LargeWeapons:
                case EObjectType.LeftAxe:
                case EObjectType.Longbow:
                case EObjectType.Piercing:
                case EObjectType.PolearmWeapon:
                case EObjectType.RecurvedBow:
                case EObjectType.Scythe:
                case EObjectType.Shield:
                case EObjectType.SlashingWeapon:
                case EObjectType.Spear:
                case EObjectType.Staff:
                case EObjectType.Sword:
                case EObjectType.ThrustWeapon:
                case EObjectType.MaulerStaff:
                case EObjectType.FistWraps:
                case EObjectType.TwoHandedWeapon:
                    return SkillIsValidForWeapon(property);
            }
            return true;
        }

        private bool SkillIsValidForMagical(EProperty property)
        {
            int level = this.Level;
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;
            EPlayerClass charClass = this.charClass;

            switch (property)
            {
                case EProperty.Skill_Augmentation:
                    {
                        if (charClass != EPlayerClass.Healer &&
                            charClass != EPlayerClass.Shaman)
                        {
                            return false;
                        }
                        else { return true; }

                    }
                case EProperty.Skill_Axe:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Battlesongs:
                    {
                        if (charClass != EPlayerClass.Skald)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Pathfinding:
                case EProperty.Skill_BeastCraft:
                    {
                        if (charClass != EPlayerClass.Hunter)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Blades:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Blunt:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }
                        return true;
                    }
                //Cloth skills
                //witchcraft is unused except as a goto target for cloth checks
                case EProperty.Skill_Arboreal:
                    if (charClass != EPlayerClass.Valewalker &&
                        charClass != EPlayerClass.Animist)
                    {
                        return false;
                    }
                    return true;
                case EProperty.Skill_Matter:
                case EProperty.Skill_Body:
                    {
                        if (charClass != EPlayerClass.Cabalist &&
                            charClass != EPlayerClass.Sorcerer)
                        {
                            return false;
                        }
                        return true;
                    }

                case EProperty.Skill_Earth:
                case EProperty.Skill_Cold:
                    {
                        if (charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard)
                        {
                            return false;
                        }
                        return true;
                    }

                case EProperty.Skill_Suppression:
                case EProperty.Skill_Darkness:
                    {
                        if (charClass != EPlayerClass.Spiritmaster &&
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Bonedancer)
                        {
                            return false;
                        }
                        return true;
                    }

                case EProperty.Skill_Light:
                case EProperty.Skill_Mana:
                    {
                        if (charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Mentalist)
                        {
                            return false;
                        }
                        return true;
                    }


                case EProperty.Skill_Mind:
                    if (charClass != EPlayerClass.Sorcerer) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Spirit:
                    if (charClass != EPlayerClass.Cabalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Wind:
                    if (charClass != EPlayerClass.Theurgist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Fire:
                    if (charClass != EPlayerClass.Wizard) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Death_Servant:
                case EProperty.Skill_DeathSight:
                case EProperty.Skill_Pain_working:
                    if (charClass != EPlayerClass.Necromancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Summoning:
                    if (charClass != EPlayerClass.Spiritmaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Runecarving:
                    if (charClass != EPlayerClass.Runemaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_BoneArmy:
                    if (charClass != EPlayerClass.Bonedancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Void:
                    if (charClass != EPlayerClass.Eldritch) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Enchantments:
                    if (charClass != EPlayerClass.Enchanter) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Mentalism:
                    if (charClass != EPlayerClass.Mentalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Creeping:
                case EProperty.Skill_Verdant:
                    if (charClass != EPlayerClass.Animist) { return false; }
                    goto case EProperty.Skill_Witchcraft;



                case EProperty.Skill_Hexing:
                case EProperty.Skill_Cursing:
                case EProperty.Skill_EtherealShriek:
                case EProperty.Skill_PhantasmalWail:
                case EProperty.Skill_SpectralForce:
                    return false;

                case EProperty.Skill_Witchcraft:
                    {
                        return true;
                    }
                case EProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != EPlayerClass.Hero)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Chants:
                    {
                        if (charClass != EPlayerClass.Paladin)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Composite:
                case EProperty.Skill_RecurvedBow:
                case EProperty.Skill_Long_bows:
                case EProperty.Skill_Archery:
                    {
                        if (charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout &&
                            charClass != EPlayerClass.Hunter)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Critical_Strike:
                case EProperty.Skill_Envenom:
                case EProperty.Skill_Dementia:
                case EProperty.Skill_Nightshade:
                case EProperty.Skill_ShadowMastery:
                case EProperty.Skill_VampiiricEmbrace:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Cross_Bows:
                    {
                        if (charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Crushing:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Dual_Wield:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Enhancement:
                    {
                        if (charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Cleric)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        return true;
                    }
                case EProperty.Skill_Hammer:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_HandToHand:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        return true;
                    }
                case EProperty.Skill_Instruments:
                    {
                        if (charClass != EPlayerClass.Minstrel) { return false; }
                        return true;
                    }
                case EProperty.Skill_Large_Weapon:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Left_Axe:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Music:
                    {
                        if (charClass != EPlayerClass.Bard) { return false; }
                        return true;
                    }
                case EProperty.Skill_Nature:
                    {
                        if (charClass != EPlayerClass.Druid) { return false; }
                        return true;
                    }
                case EProperty.Skill_Nurture:
                case EProperty.Skill_Regrowth:
                    {
                        if (charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Druid)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_OdinsWill:
                    {
                        return false;
                    }
                case EProperty.Skill_Pacification:
                    {
                        if (charClass != EPlayerClass.Healer) { return false; }
                        return true;
                    }
                case EProperty.Skill_Parry:
                    {
                        if (charClass != EPlayerClass.Berserker && //midgard
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Champion && //hibernia
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Piercing:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Polearms:
                    {
                        if (charClass != EPlayerClass.Armsman) { return false; }
                        return true;
                    }
                case EProperty.Skill_Rejuvenation:
                    {
                        if (charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Cleric)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Savagery:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        return true;
                    }
                case EProperty.Skill_Scythe:
                    {
                        if (charClass != EPlayerClass.Valewalker) { return false; }
                        return true;
                    }
                case EProperty.Skill_Shields:
                    {
                        if (charClass != EPlayerClass.Thane &&  //midgard
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Champion && //hibernia
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case EProperty.Skill_Smiting:
                    {
                        if (charClass != EPlayerClass.Cleric) { return false; }
                        return true;
                    }
                case EProperty.Skill_SoulRending:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        return true;
                    }
                case EProperty.Skill_Spear:
                    {
                        if (charClass != EPlayerClass.Hunter) { return false; }
                        return true;
                    }
                case EProperty.Skill_Staff:
                    {
                        if (charClass != EPlayerClass.Friar) { return false; }
                        return true;
                    }
                case EProperty.Skill_Stealth:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Stormcalling:
                    {
                        if (charClass != EPlayerClass.Thane) { return false; }
                        return true;
                    }
                case EProperty.Skill_Subterranean:
                    {
                        if (charClass != EPlayerClass.Shaman) { return false; }
                        return true;
                    }
                case EProperty.Skill_Sword:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Slashing:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Thrusting:
                    {

                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.Skill_Two_Handed:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Paladin)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.Skill_Valor:
                    {
                        if (charClass != EPlayerClass.Champion) { return false; }
                        return true;
                    }
                case EProperty.AllArcherySkills:
                    {
                        if (charClass != EPlayerClass.Scout &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.AllDualWieldingSkills:
                    {
                        if (charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary)
                        {
                            return false;
                        }
                        return true;
                    }
                case EProperty.AllMagicSkills:
                    {
                        if (charClass != EPlayerClass.Cabalist && //albion
                            charClass != EPlayerClass.Cleric &&
                            charClass != EPlayerClass.Necromancer &&
                            charClass != EPlayerClass.Sorcerer &&
                            charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard &&
                            charClass != EPlayerClass.Animist && //hibernia
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Mentalist &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Bonedancer && //midgard
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Spiritmaster)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != EPlayerClass.Berserker &&  //midgard
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Blademaster && //hibernia
                            charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case EProperty.AllSkills:
                    {
                        return true;
                    }
                case EProperty.Skill_Power_Strikes:
                case EProperty.Skill_Magnetism:
                case EProperty.Skill_MaulerStaff:
                case EProperty.Skill_Aura_Manipulation:
                case EProperty.Skill_FistWraps:
                    {
                        return false;
                    }

            }

            return false;
        }


        private bool SkillIsValidForArmor(EProperty property)
        {
            int level = this.Level;
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;
            EPlayerClass charClass = this.charClass;

            switch (property)
            {
                case EProperty.Skill_Mending:
                case EProperty.Skill_Augmentation:
                    {
                        if (charClass != EPlayerClass.Healer &&
                            charClass != EPlayerClass.Shaman)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Axe:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather || type == EObjectType.Studded)
                            return true;
                        else if (type == EObjectType.Chain && level >= 10)
                            return true;

                        return false;
                    }
                case EProperty.Skill_Battlesongs:
                    {
                        if (charClass != EPlayerClass.Skald)
                        {
                            return false;
                        }
                        if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Pathfinding:
                case EProperty.Skill_BeastCraft:
                    {
                        if (charClass != EPlayerClass.Hunter)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Blades:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }

                        if (type == EObjectType.Leather || type == EObjectType.Reinforced || type == EObjectType.Scale)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Blunt:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }

                        if (type == EObjectType.Leather && level < 10)
                            return true;
                        else if (type == EObjectType.Reinforced || type == EObjectType.Scale)
                            return true;
                        return false;
                    }
                //Cloth skills
                //witchcraft is unused except as a goto target for cloth checks
                case EProperty.Skill_Arboreal:
                    if (charClass != EPlayerClass.Valewalker &&
                        charClass != EPlayerClass.Animist)
                    {
                        return false;
                    }
                    goto case EProperty.Skill_Witchcraft;


                case EProperty.Skill_Matter:
                case EProperty.Skill_Body:
                    {
                        if (charClass != EPlayerClass.Cabalist &&
                            charClass != EPlayerClass.Sorcerer)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Earth:
                case EProperty.Skill_Cold:
                    {
                        if (charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Suppression:
                case EProperty.Skill_Darkness:
                    {
                        if (charClass != EPlayerClass.Spiritmaster &&
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Bonedancer)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Light:
                case EProperty.Skill_Mana:
                    {
                        if (charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Mentalist)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }


                case EProperty.Skill_Mind:
                    if (charClass != EPlayerClass.Sorcerer) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Spirit:
                    if (charClass != EPlayerClass.Cabalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Wind:
                    if (charClass != EPlayerClass.Theurgist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Fire:
                    if (charClass != EPlayerClass.Wizard) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Death_Servant:
                case EProperty.Skill_DeathSight:
                case EProperty.Skill_Pain_working:
                    if (charClass != EPlayerClass.Necromancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Summoning:
                    if (charClass != EPlayerClass.Spiritmaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Runecarving:
                    if (charClass != EPlayerClass.Runemaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_BoneArmy:
                    if (charClass != EPlayerClass.Bonedancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Void:
                    if (charClass != EPlayerClass.Eldritch) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Enchantments:
                    if (charClass != EPlayerClass.Enchanter) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Mentalism:
                    if (charClass != EPlayerClass.Mentalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Creeping:
                case EProperty.Skill_Verdant:
                    if (charClass != EPlayerClass.Animist) { return false; }
                    goto case EProperty.Skill_Witchcraft;



                case EProperty.Skill_Hexing:
                case EProperty.Skill_Cursing:
                case EProperty.Skill_EtherealShriek:
                case EProperty.Skill_PhantasmalWail:
                case EProperty.Skill_SpectralForce:
                    return false;

                case EProperty.Skill_Witchcraft:
                    {
                        if (property == EProperty.Skill_Witchcraft)
                        {
                            return false; //we don't want actual Witchcraft skills
                        }
                        if (type == EObjectType.Cloth)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade)
                        {
                            return false;
                        }

                        if (type == EObjectType.Leather ||
                            type == EObjectType.Reinforced)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != EPlayerClass.Hero)
                        {
                            return false;
                        }
                        if (level < 15)
                        {
                            if (type == EObjectType.Reinforced)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Chants:
                    {
                        if (charClass != EPlayerClass.Paladin)
                        {
                            return false;
                        }
                        return false;
                    }
                case EProperty.Skill_Composite:
                case EProperty.Skill_RecurvedBow:
                case EProperty.Skill_Long_bows:
                case EProperty.Skill_Archery:
                    {
                        if (charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout &&
                            charClass != EPlayerClass.Hunter)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Studded || type == EObjectType.Reinforced)
                                return true;

                            return false;
                        }
                    }
                case EProperty.Skill_Critical_Strike:
                case EProperty.Skill_Envenom:
                case EProperty.Skill_Dementia:
                case EProperty.Skill_Nightshade:
                case EProperty.Skill_ShadowMastery:
                case EProperty.Skill_VampiiricEmbrace:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Cross_Bows:
                    {
                        return false; // disabled for armor

                        //						if (level < 15)
                        //						{
                        //							if (type == eObjectType.Chain)
                        //								return true;
                        //							return false;
                        //						}
                        //						else
                        //						{
                        //							if (type == eObjectType.Plate)
                        //								return true;
                        //							return false;
                        //						}
                    }
                case EProperty.Skill_Crushing:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver)
                        {
                            return false;
                        }
                        if (realm == ERealm.Albion && type == EObjectType.Cloth) // heretic
                            return true;

                        if (level < 15)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain || type == EObjectType.Plate)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Dual_Wield:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary)
                        {
                            return false;
                        }

                        if (level < 20)
                        {
                            if (type == EObjectType.Leather || type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Leather || type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Enhancement:
                    {
                        if (charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Cleric)
                        {
                            return false;
                        }
                        // friar
                        if (type == EObjectType.Leather)
                            return true;

                        if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        if (type == EObjectType.Cloth) // Heretic
                            return true;

                        if (level < 10)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Hammer:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_HandToHand:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        if (type == EObjectType.Studded)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Instruments:
                    {
                        if (charClass != EPlayerClass.Minstrel) { return false; }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Large_Weapon:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero)
                        {
                            return false;
                        }
                        if (level < 15)
                        {
                            if (type == EObjectType.Reinforced)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Scale)
                                return true;

                            return false;
                        }
                    }
                case EProperty.Skill_Left_Axe:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather || type == EObjectType.Studded)
                            return true;
                        break;
                    }
                case EProperty.Skill_Music:
                    {
                        if (charClass != EPlayerClass.Bard) { return false; }
                        if (level < 15)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Reinforced)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Nature:
                    {
                        if (charClass != EPlayerClass.Druid) { return false; }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == EObjectType.Reinforced)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Nurture:
                case EProperty.Skill_Regrowth:
                    {
                        if (charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Druid)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Reinforced || type == EObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_OdinsWill:
                    {
                        return false;
                        if (level < 10)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Pacification:
                    {
                        if (charClass != EPlayerClass.Healer) { return false; }
                        if (level < 10)
                        {
                            if (type == EObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == EObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case EProperty.Skill_Parry:
                    {
                        if (charClass != EPlayerClass.Berserker && //midgard
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Champion && //hibernia
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver)
                        {
                            return false;
                        }

                        if (type == EObjectType.Cloth && realm == ERealm.Hibernia && level >= 5)
                            return true;
                        else if (realm == ERealm.Hibernia && level < 2)
                            return false;
                        else if (realm == ERealm.Albion && level < 5)
                            return false;
                        else if (realm == ERealm.Albion && level < 10 && type == EObjectType.Studded)
                            return true;
                        else if (realm == ERealm.Albion && level >= 10 && (type == EObjectType.Leather || type == EObjectType.Chain || type == EObjectType.Plate))
                            return true;
                        else if (realm == ERealm.Hibernia && level < 20 && type == EObjectType.Reinforced)
                            return true;
                        else if (realm == ERealm.Hibernia && level >= 15 && type == EObjectType.Scale)
                            return true;
                        else if (realm == ERealm.Midgard && (type == EObjectType.Studded || type == EObjectType.Chain))
                            return true;

                        break;
                    }
                case EProperty.Skill_Piercing:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather || type == EObjectType.Reinforced || type == EObjectType.Scale)
                            return true;
                        return false;
                    }
                case EProperty.Skill_Polearms:
                    {
                        if (charClass != EPlayerClass.Armsman) { return false; }
                        if (level < 5 && type == EObjectType.Studded)
                        {
                            return true;
                        }
                        else if (level < 15)
                        {
                            if (type == EObjectType.Chain)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == EObjectType.Plate)
                                return true;

                            return false;
                        }
                    }
                case EProperty.Skill_Rejuvenation:
                    {
                        if (charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Cleric)
                        {
                            return false;
                        }
                        if (type == EObjectType.Cloth)
                            return true;
                        else if (type == EObjectType.Leather)
                            return true;
                        else if (type == EObjectType.Studded && level >= 10 && level < 20)
                            return true;
                        else if (type == EObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case EProperty.Skill_Savagery:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        if (type == EObjectType.Studded)
                            return true;
                        break;
                    }
                case EProperty.Skill_Scythe:
                    {
                        if (charClass != EPlayerClass.Valewalker) { return false; }
                        if (type == EObjectType.Cloth)
                            return true;
                        break;
                    }
                case EProperty.Skill_Shields:
                    {
                        if (charClass != EPlayerClass.Thane &&  //midgard
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Champion && //hibernia
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Cloth && realm == ERealm.Albion)
                            return true;
                        else if (type == EObjectType.Studded || type == EObjectType.Chain || type == EObjectType.Reinforced || type == EObjectType.Scale || type == EObjectType.Plate)
                            return true;
                        break;
                    }
                case EProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case EProperty.Skill_Smiting:
                    {
                        if (charClass != EPlayerClass.Cleric) { return false; }
                        if (type == EObjectType.Leather && level < 10)
                            return true;
                        else if (type == EObjectType.Studded && level < 20)
                            return true;
                        else if (type == EObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case EProperty.Skill_SoulRending:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        if (type == EObjectType.Studded && level < 10)
                            return true;
                        else if (type == EObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case EProperty.Skill_Spear:
                    {
                        if (charClass != EPlayerClass.Hunter) { return false; }
                        if (type == EObjectType.Leather && level < 10)
                            return true;
                        else if (type == EObjectType.Studded)
                            return true;
                        else if (type == EObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case EProperty.Skill_Staff:
                    {
                        if (charClass != EPlayerClass.Friar) { return false; }
                        if (type == EObjectType.Leather && realm == ERealm.Albion)
                            return true;
                        break;
                    }
                case EProperty.Skill_Stealth:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather || type == EObjectType.Studded || type == EObjectType.Reinforced)
                            return true;
                        else if (realm == ERealm.Albion && level >= 20 && type == EObjectType.Chain)
                            return true;
                        break;
                    }
                case EProperty.Skill_Stormcalling:
                    {
                        if (charClass != EPlayerClass.Thane) { return false; }
                        if (type == EObjectType.Studded && level < 10)
                            return true;
                        else if (type == EObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case EProperty.Skill_Subterranean:
                    {
                        if (charClass != EPlayerClass.Shaman) { return false; }
                        if (type == EObjectType.Leather && level < 10)
                            return true;
                        else if (type == EObjectType.Studded && level < 20)
                            return true;
                        else if (type == EObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case EProperty.Skill_Sword:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        if (type == EObjectType.Studded || type == EObjectType.Chain)
                            return true;
                        break;
                    }
                case EProperty.Skill_Slashing:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        if (type == EObjectType.Leather || type == EObjectType.Studded || type == EObjectType.Chain || type == EObjectType.Plate)
                            return true;
                        break;
                    }
                case EProperty.Skill_Thrusting:
                    {

                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        if (type == EObjectType.Leather || type == EObjectType.Studded || type == EObjectType.Chain || type == EObjectType.Plate)
                            return true;
                        break;
                    }
                case EProperty.Skill_Two_Handed:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Paladin)
                        {
                            return false;
                        }
                        if (type == EObjectType.Studded && level < 10)
                            return true;
                        else if (type == EObjectType.Chain && level < 20)
                            return true;
                        else if (type == EObjectType.Plate)
                            return true;
                        break;
                    }
                case EProperty.Skill_Valor:
                    {
                        if (charClass != EPlayerClass.Champion) { return false; }
                        if (type == EObjectType.Reinforced && level < 20)
                            return true;
                        else if (type == EObjectType.Scale)
                            return true;
                        break;
                    }
                case EProperty.AllArcherySkills:
                    {
                        if (charClass != EPlayerClass.Scout &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        if (type == EObjectType.Leather && level < 10)
                            return true;
                        else if (level >= 10 && (type == EObjectType.Reinforced || type == EObjectType.Studded))
                            return true;

                        break;
                    }
                case EProperty.AllDualWieldingSkills:
                    {
                        if (charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary)
                        {
                            return false;
                        }
                        //Dualwielders are always above level 4 and can wear better than cloth from the start.
                        if (type == EObjectType.Cloth)
                            return false;
                        //mercs are the only dualwielder who can wear chain
                        else if (realm == ERealm.Albion && type == EObjectType.Studded && level < 10)
                            return true;
                        else if (realm == ERealm.Albion && type == EObjectType.Chain)
                            return true;
                        //all assassins wear leather, blademasters and zerks wear studded.
                        else if (type == EObjectType.Leather || type == EObjectType.Reinforced || (type == EObjectType.Studded && realm == ERealm.Midgard))
                            return true;
                        break;
                    }
                case EProperty.AllMagicSkills:
                    {
                        if (charClass != EPlayerClass.Cabalist && //albion
                            charClass != EPlayerClass.Cleric &&
                            charClass != EPlayerClass.Necromancer &&
                            charClass != EPlayerClass.Sorcerer &&
                            charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard &&
                            charClass != EPlayerClass.Animist && //hibernia
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Mentalist &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Bonedancer && //midgard
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Spiritmaster)
                        {
                            return false;
                        }

                        // not for scouts
                        if (realm == ERealm.Albion && type == EObjectType.Studded && level >= 20)
                            return false;
                        // Paladins can't use + magic skills
                        if (realm == ERealm.Albion && type == EObjectType.Plate)
                            return false;

                        return true;
                    }
                case EProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != EPlayerClass.Berserker &&  //midgard
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Blademaster && //hibernia
                            charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        if (realm == ERealm.Midgard && type == EObjectType.Cloth)
                            return false;
                        else if (level >= 5)
                            return true;

                        break;
                    }
                case EProperty.AllSkills:
                    {
                        return true;
                    }
                case EProperty.Skill_Power_Strikes:
                case EProperty.Skill_Magnetism:
                case EProperty.Skill_MaulerStaff:
                case EProperty.Skill_Aura_Manipulation:
                case EProperty.Skill_FistWraps:
                    {
                        return false;
                        //Maulers
                        if (type == EObjectType.Leather) //Maulers can only wear leather.
                            return true;

                        break;
                    }

            }

            return false;
        }

        private bool SkillIsValidForWeapon(EProperty property)
        {
            int level = this.Level;
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;

            switch (property)
            {
                case EProperty.Skill_SpectralForce:
                case EProperty.Skill_EtherealShriek:
                case EProperty.Skill_PhantasmalWail:
                case EProperty.Skill_Hexing:
                case EProperty.Skill_Cursing:
                    return false;

                case EProperty.Skill_Arboreal:
                    if (charClass != EPlayerClass.Valewalker &&
                        charClass != EPlayerClass.Animist)
                    {
                        return false;
                    }
                    goto case EProperty.Skill_Witchcraft;


                case EProperty.Skill_Matter:
                case EProperty.Skill_Body:
                    {
                        if (charClass != EPlayerClass.Cabalist &&
                            charClass != EPlayerClass.Sorcerer)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Earth:
                case EProperty.Skill_Cold:
                    {
                        if (charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Suppression:
                case EProperty.Skill_Darkness:
                    {
                        if (charClass != EPlayerClass.Spiritmaster &&
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Bonedancer)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }

                case EProperty.Skill_Light:
                case EProperty.Skill_Mana:
                    {
                        if (charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Mentalist)
                        {
                            return false;
                        }
                        goto case EProperty.Skill_Witchcraft;
                    }


                case EProperty.Skill_Mind:
                    if (charClass != EPlayerClass.Sorcerer) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Spirit:
                    if (charClass != EPlayerClass.Cabalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Wind:
                    if (charClass != EPlayerClass.Theurgist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Fire:
                    if (charClass != EPlayerClass.Wizard) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Death_Servant:
                case EProperty.Skill_DeathSight:
                case EProperty.Skill_Pain_working:
                    if (charClass != EPlayerClass.Necromancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Summoning:
                    if (charClass != EPlayerClass.Spiritmaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Runecarving:
                    if (charClass != EPlayerClass.Runemaster) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_BoneArmy:
                    if (charClass != EPlayerClass.Bonedancer) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Void:
                    if (charClass != EPlayerClass.Eldritch) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Enchantments:
                    if (charClass != EPlayerClass.Enchanter) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Mentalism:
                    if (charClass != EPlayerClass.Mentalist) { return false; }
                    goto case EProperty.Skill_Witchcraft;
                case EProperty.Skill_Creeping:
                case EProperty.Skill_Verdant:
                    if (charClass != EPlayerClass.Animist) { return false; }
                    goto case EProperty.Skill_Witchcraft;

                case EProperty.Skill_Witchcraft:
                    {
                        if (property == EProperty.Skill_Witchcraft)
                        {
                            return false; //we don't want actual Witchcraft skills
                        }
                        if (type == EObjectType.Staff && this.Description != "friar")
                            return true;
                        break;
                    }
                //healer things
                case EProperty.Skill_Smiting:
                    {
                        if (((type == EObjectType.Shield && this.Type_Damage < 3) || type == EObjectType.CrushingWeapon)
                            && charClass == EPlayerClass.Cleric)
                            return true;
                        break;
                    }
                case EProperty.Skill_Enhancement:
                case EProperty.Skill_Rejuvenation:
                    {
                        if (realm != ERealm.Albion || (charClass != EPlayerClass.Cleric && charClass != EPlayerClass.Friar)) { return false; }
                        if ((type == EObjectType.Staff && this.Description == "friar") || (type == EObjectType.Shield && this.Type_Damage < 3) || type == EObjectType.CrushingWeapon)
                            return true;
                        break;
                    }
                case EProperty.Skill_Augmentation:
                case EProperty.Skill_Mending:
                    {
                        if (realm != ERealm.Midgard || (charClass != EPlayerClass.Healer && charClass != EPlayerClass.Shaman)) { return false; }
                        if ((type == EObjectType.Shield && this.Type_Damage < 2) || type == EObjectType.Hammer)
                        {
                            return true;
                        }
                        break;
                    }
                case EProperty.Skill_Subterranean:
                    {
                        if (realm != ERealm.Midgard || charClass != EPlayerClass.Shaman) { return false; }
                        if ((type == EObjectType.Shield && this.Type_Damage < 2) || type == EObjectType.Hammer)
                        {
                            return true;
                        }
                        break;
                    }
                case EProperty.Skill_Nurture:
                case EProperty.Skill_Nature:
                case EProperty.Skill_Regrowth:
                    {
                        if (realm != ERealm.Hibernia) { return false; }
                        if (type == EObjectType.Blunt || type == EObjectType.Blades || (type == EObjectType.Shield && this.Type_Damage < 2))
                            return true;
                        break;
                    }
                //archery things
                case EProperty.Skill_Archery:
                    if (type == EObjectType.CompositeBow || type == EObjectType.RecurvedBow || type == EObjectType.Longbow)
                        return true;
                    break;
                case EProperty.Skill_Composite:
                    {
                        if (type == EObjectType.CompositeBow)
                            return true;
                        break;
                    }
                case EProperty.Skill_RecurvedBow:
                    {
                        if (type == EObjectType.RecurvedBow)
                            return true;
                        break;
                    }
                case EProperty.Skill_Long_bows:
                    {
                        if (type == EObjectType.Longbow)
                            return true;
                        break;
                    }
                //other specifics
                case EProperty.Skill_Staff:
                    {
                        if (type == EObjectType.Staff && this.Description == "friar")
                            return true;
                        break;
                    }
                case EProperty.Skill_Axe:
                    {
                        if (realm != ERealm.Midgard) { return false; }
                        if (type == EObjectType.Axe || type == EObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case EProperty.Skill_Battlesongs:
                    {
                        if (charClass != EPlayerClass.Skald) { return false; }
                        if (type == EObjectType.Sword || type == EObjectType.Axe || type == EObjectType.Hammer || (type == EObjectType.Shield && this.Type_Damage < 3))
                            return true;
                        break;
                    }
                case EProperty.Skill_BeastCraft:
                    {
                        if (charClass != EPlayerClass.Hunter) { return false; }
                        if (type == EObjectType.Spear)
                            return true;
                        break;
                    }
                case EProperty.Skill_Blades:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }

                        if (type == EObjectType.Blades)
                            return true;
                        break;
                    }
                case EProperty.Skill_Blunt:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Bard &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Warden)
                        {
                            return false;
                        }

                        if (type == EObjectType.Blunt)
                            return true;
                        break;
                    }
                case EProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster)
                        {
                            return false;
                        }
                        if (type == EObjectType.Piercing || type == EObjectType.Blades || type == EObjectType.Blunt)
                            return true;
                        break;
                    }
                case EProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != EPlayerClass.Hero) { return false; }
                        if (type == EObjectType.CelticSpear)
                            return true;
                        break;
                    }
                case EProperty.Skill_Chants:
                    {
                        return false;
                    }
                case EProperty.Skill_Critical_Strike:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == EObjectType.Piercing || type == EObjectType.SlashingWeapon || type == EObjectType.ThrustWeapon || type == EObjectType.Blades || type == EObjectType.Sword || type == EObjectType.Axe || type == EObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case EProperty.Skill_Cross_Bows:
                    {
                        if (type == EObjectType.Crossbow)
                            return true;
                        break;
                    }
                case EProperty.Skill_Crushing:
                    {
                        if (realm != ERealm.Albion || type == EObjectType.Flexible) { return false; }
                        if (type == EObjectType.CrushingWeapon ||
                            ((type == EObjectType.TwoHandedWeapon || type == EObjectType.PolearmWeapon) && this.Type_Damage == (int)EDamageType.Crush))
                            return true;
                        break;
                    }
                case EProperty.Skill_Dual_Wield:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Berserker)
                        {
                            return false;
                        }

                        if (type == EObjectType.SlashingWeapon || type == EObjectType.ThrustWeapon || type == EObjectType.CrushingWeapon)
                            return true;
                        break;
                    }
                case EProperty.Skill_Envenom:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == EObjectType.SlashingWeapon || type == EObjectType.ThrustWeapon)
                            return true;
                        break;
                    }
                case EProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        if (type == EObjectType.Flexible || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_Hammer:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        if (type == EObjectType.Hammer)
                            return true;
                        break;
                    }
                case EProperty.Skill_HandToHand:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        if (type == EObjectType.HandToHand)
                            return true;
                        break;
                    }
                case EProperty.Skill_Instruments:
                    {
                        if (charClass != EPlayerClass.Minstrel) { return false; }
                        if (type == EObjectType.Instrument)
                            return true;
                        break;
                    }
                case EProperty.Skill_Large_Weapon:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero)
                        {
                            return false;
                        }
                        if (type == EObjectType.LargeWeapons)
                            return true;
                        break;
                    }
                case EProperty.Skill_Left_Axe:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Shadowblade)
                        {
                            return false;
                        }
                        if (this.Item_Type == Slot.TWOHAND) return false;
                        if (type == EObjectType.Axe || type == EObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case EProperty.Skill_Music:
                    {
                        if (charClass != EPlayerClass.Bard)
                        {
                            return false;
                        }
                        if (type == EObjectType.Blades || type == EObjectType.Blunt || (type == EObjectType.Shield && this.Type_Damage == 1) || type == EObjectType.Instrument)
                            return true;
                        break;
                    }
                case EProperty.Skill_Nightshade:
                    {
                        if (charClass != EPlayerClass.Nightshade)
                        {
                            return false;
                        }
                        if (type == EObjectType.Blades || type == EObjectType.Piercing || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_OdinsWill:
                    {
                        return false;
                        if (type == EObjectType.Sword || type == EObjectType.Spear || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_Parry:
                    if (charClass != EPlayerClass.Berserker &&  //midgard
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Blademaster && //hibernia
                            charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver)
                    {
                        return false;
                    }
                    return true;
                case EProperty.Skill_Pathfinding:
                    {
                        if (charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        if (type == EObjectType.RecurvedBow || type == EObjectType.Piercing || type == EObjectType.Blades)
                            return true;
                        break;
                    }
                case EProperty.Skill_Piercing:
                    {
                        if (charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        if (type == EObjectType.Piercing)
                            return true;
                        break;
                    }
                case EProperty.Skill_Polearms:
                    {
                        if (charClass != EPlayerClass.Armsman) { return false; }
                        if (type == EObjectType.PolearmWeapon)
                            return true;
                        break;
                    }
                case EProperty.Skill_Savagery:
                    {
                        if (charClass != EPlayerClass.Savage) { return false; }
                        if (type == EObjectType.Sword || type == EObjectType.Axe || type == EObjectType.Hammer || type == EObjectType.HandToHand)
                            return true;
                        break;
                    }
                case EProperty.Skill_Scythe:
                    {
                        if (charClass != EPlayerClass.Valewalker) { return false; }
                        if (type == EObjectType.Scythe)
                            return true;
                        break;
                    }

                case EProperty.Skill_VampiiricEmbrace:
                case EProperty.Skill_ShadowMastery:
                    {
                        return false;
                        if (type == EObjectType.Piercing)
                            return true;
                        break;
                    }
                case EProperty.Skill_Shields:
                    {
                        if (charClass != EPlayerClass.Thane &&  //midgard
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Champion && //hibernia
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case EProperty.Skill_Slashing:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }

                        if (type == EObjectType.Flexible)
                            return false;
                        if (type == EObjectType.SlashingWeapon ||
                            ((type == EObjectType.TwoHandedWeapon || type == EObjectType.PolearmWeapon) && this.Type_Damage == (int)EDamageType.Slash))
                            return true;
                        break;
                    }
                case EProperty.Skill_SoulRending:
                    {
                        if (charClass != EPlayerClass.Reaver) { return false; }
                        if (type == EObjectType.SlashingWeapon || type == EObjectType.CrushingWeapon || type == EObjectType.ThrustWeapon || type == EObjectType.Flexible || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_Spear:
                    {
                        if (charClass != EPlayerClass.Hunter) { return false; }
                        if (type == EObjectType.Spear)
                            return true;
                        break;
                    }
                case EProperty.Skill_Stealth:
                    {
                        if (charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Longbow || type == EObjectType.RecurvedBow || type == EObjectType.CompositeBow || (realm == ERealm.Albion && type == EObjectType.Shield && this.Type_Damage == 1) || type == EObjectType.Spear || type == EObjectType.Sword || type == EObjectType.Axe || type == EObjectType.LeftAxe || type == EObjectType.SlashingWeapon || type == EObjectType.ThrustWeapon || type == EObjectType.Piercing || type == EObjectType.Blades || (realm == ERealm.Albion && type == EObjectType.Instrument))
                            return true;
                        break;
                    }
                case EProperty.Skill_Stormcalling:
                    {
                        if (charClass != EPlayerClass.Thane) { return false; }
                        if (type == EObjectType.Sword || type == EObjectType.Axe || type == EObjectType.Hammer || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_Sword:
                    {
                        if (charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior)
                        {
                            return false;
                        }
                        if (type == EObjectType.Sword)
                            return true;
                        break;
                    }
                case EProperty.Skill_Thrusting:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Flexible)
                            return false;
                        if (type == EObjectType.ThrustWeapon ||
                            ((type == EObjectType.TwoHandedWeapon || type == EObjectType.PolearmWeapon) && this.Type_Damage == (int)EDamageType.Thrust))
                            return true;
                        break;
                    }
                case EProperty.Skill_Two_Handed:
                    {
                        if (charClass != EPlayerClass.Armsman &&
                            charClass != EPlayerClass.Paladin)
                        {
                            return false;
                        }
                        if (type == EObjectType.TwoHandedWeapon)
                            return true;
                        break;
                    }
                case EProperty.Skill_Valor:
                    {
                        if (charClass != EPlayerClass.Champion) { return false; }
                        if (type == EObjectType.Blades || type == EObjectType.Piercing || type == EObjectType.Blunt || type == EObjectType.LargeWeapons || type == EObjectType.Shield)
                            return true;
                        break;
                    }
                case EProperty.Skill_Thrown_Weapons:
                    {
                        return false;
                    }
                case EProperty.Skill_Pacification:
                    {
                        if (charClass != EPlayerClass.Healer) { return false; }
                        if (type == EObjectType.Hammer)
                            return true;
                        break;
                    }
                case EProperty.Skill_Dementia:
                    {
                        return false;
                        if (type == EObjectType.Piercing)
                            return true;
                        break;
                    }
                case EProperty.AllArcherySkills:
                    {
                        if (charClass != EPlayerClass.Scout &&
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Ranger)
                        {
                            return false;
                        }
                        if (type == EObjectType.CompositeBow || type == EObjectType.Longbow || type == EObjectType.RecurvedBow)
                            return true;
                        break;
                    }
                case EProperty.AllDualWieldingSkills:
                    {
                        if (charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Berserker &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Blademaster &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary)
                        {
                            return false;
                        }
                        if (type == EObjectType.Axe || type == EObjectType.Sword || type == EObjectType.Hammer || type == EObjectType.LeftAxe || type == EObjectType.SlashingWeapon || type == EObjectType.CrushingWeapon || type == EObjectType.ThrustWeapon || type == EObjectType.Piercing || type == EObjectType.Blades || type == EObjectType.Blunt)
                            return true;
                        break;
                    }
                case EProperty.AllMagicSkills:
                    {
                        if (charClass != EPlayerClass.Cabalist && //albion
                            charClass != EPlayerClass.Cleric &&
                            charClass != EPlayerClass.Necromancer &&
                            charClass != EPlayerClass.Sorcerer &&
                            charClass != EPlayerClass.Theurgist &&
                            charClass != EPlayerClass.Wizard &&
                            charClass != EPlayerClass.Animist && //hibernia
                            charClass != EPlayerClass.Eldritch &&
                            charClass != EPlayerClass.Enchanter &&
                            charClass != EPlayerClass.Mentalist &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Bonedancer && //midgard
                            charClass != EPlayerClass.Runemaster &&
                            charClass != EPlayerClass.Spiritmaster)
                        {
                            return false;
                        }
                        //scouts, armsmen, paladins, mercs, blademasters, heroes, zerks, warriors do not need this.
                        if (type == EObjectType.Longbow || type == EObjectType.CelticSpear || type == EObjectType.PolearmWeapon || type == EObjectType.TwoHandedWeapon || type == EObjectType.Crossbow || (type == EObjectType.Shield && this.Type_Damage > 2))
                            return false;
                        else
                            return true;
                    }
                case EProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != EPlayerClass.Berserker &&  //midgard
                            charClass != EPlayerClass.Hunter &&
                            charClass != EPlayerClass.Savage &&
                            charClass != EPlayerClass.Shadowblade &&
                            charClass != EPlayerClass.Skald &&
                            charClass != EPlayerClass.Thane &&
                            charClass != EPlayerClass.Warrior &&
                            charClass != EPlayerClass.Blademaster && //hibernia
                            charClass != EPlayerClass.Champion &&
                            charClass != EPlayerClass.Hero &&
                            charClass != EPlayerClass.Nightshade &&
                            charClass != EPlayerClass.Ranger &&
                            charClass != EPlayerClass.Valewalker &&
                            charClass != EPlayerClass.Warden &&
                            charClass != EPlayerClass.Armsman && //albion
                            charClass != EPlayerClass.Friar &&
                            charClass != EPlayerClass.Infiltrator &&
                            charClass != EPlayerClass.Mercenary &&
                            charClass != EPlayerClass.Minstrel &&
                            charClass != EPlayerClass.Paladin &&
                            charClass != EPlayerClass.Reaver &&
                            charClass != EPlayerClass.Scout)
                        {
                            return false;
                        }
                        if (type == EObjectType.Staff && realm != ERealm.Albion)
                            return false;
                        else if (type == EObjectType.Staff && this.Description != "friar") // do not add if caster staff
                            return false;
                        else if (type == EObjectType.Longbow || type == EObjectType.CompositeBow || type == EObjectType.RecurvedBow || type == EObjectType.Crossbow || type == EObjectType.Fired || type == EObjectType.Instrument)
                            return false;
                        else
                            return true;
                    }
                case EProperty.Skill_Aura_Manipulation: //Maulers
                    {
                        return false;
                        if (type == EObjectType.MaulerStaff || type == EObjectType.FistWraps)
                            return true;
                        break;
                    }
                case EProperty.Skill_Magnetism: //Maulers
                    {
                        return false;
                        if (type == EObjectType.FistWraps || type == EObjectType.MaulerStaff)
                            return true;
                        break;
                    }
                case EProperty.Skill_MaulerStaff: //Maulers
                    {
                        return false;
                        if (type == EObjectType.MaulerStaff)
                            return true;
                        break;
                    }
                case EProperty.Skill_Power_Strikes: //Maulers
                    {
                        return false;
                        if (type == EObjectType.MaulerStaff || type == EObjectType.FistWraps)
                            return true;
                        break;
                    }
                case EProperty.Skill_FistWraps: //Maulers
                    {
                        return false;
                        if (type == EObjectType.FistWraps)
                            return true;
                        break;
                    }
            }
            return false;
        }

        private bool StatIsValidForRealm(EProperty property)
        {
            switch (property)
            {
                case EProperty.Piety:
                case EProperty.PieCapBonus:
                    {
                        if (this.Realm == (int)ERealm.Hibernia)
                            return false;
                        break;
                    }
                case EProperty.Empathy:
                case EProperty.EmpCapBonus:
                    {
                        if (this.Realm == (int)ERealm.Midgard || this.Realm == (int)ERealm.Albion)
                            return false;
                        break;
                    }
                case EProperty.Intelligence:
                case EProperty.IntCapBonus:
                    {
                        if (this.Realm == (int)ERealm.Midgard)
                            return false;
                        break;
                    }
            }
            return true;
        }

        private bool StatIsValidForArmor(EProperty property)
        {
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;

            switch (property)
            {
                case EProperty.Intelligence:
                case EProperty.IntCapBonus:
                    {
                        if (realm == ERealm.Midgard)
                            return false;

                        if (realm == ERealm.Hibernia && this.Level < 20 && type != EObjectType.Reinforced && type != EObjectType.Cloth)
                            return false;

                        if (realm == ERealm.Hibernia && this.Level >= 20 && type != EObjectType.Scale && type != EObjectType.Cloth)
                            return false;

                        if (type != EObjectType.Cloth)
                            return false;

                        break;
                    }
                case EProperty.Acuity:
                case EProperty.AcuCapBonus:
                case EProperty.PowerPool:
                case EProperty.PowerPoolCapBonus:
                    {
                        if (realm == ERealm.Albion && this.Level >= 20 && type == EObjectType.Studded)
                            return false;

                        if (realm == ERealm.Midgard && this.Level >= 10 && type == EObjectType.Leather)
                            return false;

                        if (realm == ERealm.Midgard && this.Level >= 20 && type == EObjectType.Studded)
                            return false;

                        break;
                    }
                case EProperty.Piety:
                case EProperty.PieCapBonus:
                    {
                        if (realm == ERealm.Albion)
                        {
                            if (type == EObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == EObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == EObjectType.Chain && this.Level < 10)
                                return false;
                        }
                        else if (realm == ERealm.Midgard)
                        {
                            if (type == EObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == EObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == EObjectType.Chain && this.Level < 10)
                                return false;
                        }
                        else if (realm == ERealm.Hibernia)
                        {
                            return false;
                        }
                        break;
                    }
                case EProperty.Charisma:
                case EProperty.ChaCapBonus:
                    {
                        if (realm == ERealm.Albion)
                        {
                            if (type == EObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == EObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == EObjectType.Chain && this.Level < 20)
                                return false;
                        }
                        if (realm == ERealm.Midgard)
                        {
                            if (type == EObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == EObjectType.Chain && this.Level < 20)
                                return false;
                        }
                        else if (realm == ERealm.Hibernia)
                        {
                            if (type == EObjectType.Leather && this.Level >= 15)
                                return false;

                            if (type == EObjectType.Reinforced && this.Level < 15)
                                return false;
                        }
                        break;
                    }
                case EProperty.Empathy:
                case EProperty.EmpCapBonus:
                    {
                        if (realm != ERealm.Hibernia)
                            return false;

                        if (type == EObjectType.Leather && this.Level >= 10)
                            return false;

                        if (type == EObjectType.Reinforced && this.Level >= 20)
                            return false;

                        if (type == EObjectType.Scale && this.Level < 20)
                            return false;

                        break;
                    }
            }
            return true;
        }

        private bool StatIsValidForWeapon(EProperty property)
        {
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;

            switch (type)
            {
                case EObjectType.Staff:
                    {
                        if ((property == EProperty.Piety || property == EProperty.PieCapBonus) && realm == ERealm.Hibernia)
                            return false;
                        else if ((property == EProperty.Piety || property == EProperty.PieCapBonus) && realm == ERealm.Albion && this.Description != "friar")
                            return false; // caster staff
                        else if (property == EProperty.Charisma || property == EProperty.Empathy || property == EProperty.ChaCapBonus || property == EProperty.EmpCapBonus)
                            return false;
                        else if ((property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.AcuCapBonus) && this.Description == "friar")
                            return false;
                        break;
                    }

                case EObjectType.Shield:
                    {
                        if ((realm == ERealm.Albion || realm == ERealm.Midgard) && (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus))
                            return false;
                        else if (realm == ERealm.Hibernia && (property == EProperty.Piety || property == EProperty.PieCapBonus))
                            return false;
                        else if ((realm == ERealm.Albion || realm == ERealm.Hibernia) && this.Type_Damage > 1 && (property == EProperty.Charisma || property == EProperty.ChaCapBonus))
                            return false;
                        else if (realm == ERealm.Midgard && this.Type_Damage > 2 && (property == EProperty.Charisma || property == EProperty.ChaCapBonus))
                            return false;
                        else if (this.Type_Damage > 2 && property == EProperty.MaxMana)
                            return false;

                        break;
                    }
                case EObjectType.Blades:
                case EObjectType.Blunt:
                    {
                        if (property == EProperty.Piety || property == EProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case EObjectType.LargeWeapons:
                case EObjectType.Piercing:
                case EObjectType.Scythe:
                    {
                        if (property == EProperty.Piety || property == EProperty.Empathy || property == EProperty.Charisma)
                            return false;
                        break;
                    }
                case EObjectType.CrushingWeapon:
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.Charisma || property == EProperty.ChaCapBonus)
                            return false;
                        break;
                    }
                case EObjectType.SlashingWeapon:
                case EObjectType.ThrustWeapon:
                case EObjectType.Hammer:
                case EObjectType.Sword:
                case EObjectType.Axe:
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.AcuCapBonus || property == EProperty.Acuity)
                            return false;
                        break;
                    }
                case EObjectType.TwoHandedWeapon:
                case EObjectType.Flexible:
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.Charisma || property == EProperty.ChaCapBonus)
                            return false;
                        break;
                    }
                case EObjectType.RecurvedBow:
                case EObjectType.CompositeBow:
                case EObjectType.Longbow:
                case EObjectType.Crossbow:
                case EObjectType.Fired:
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.Charisma || property == EProperty.ChaCapBonus ||
                            property == EProperty.MaxMana || property == EProperty.PowerPool || property == EProperty.PowerPoolCapBonus || property == EProperty.AcuCapBonus || property == EProperty.Acuity || property == EProperty.Piety || property == EProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case EObjectType.Spear:
                case EObjectType.CelticSpear:
                case EObjectType.LeftAxe:
                case EObjectType.PolearmWeapon:
                case EObjectType.HandToHand:
                case EObjectType.FistWraps: //Maulers
                case EObjectType.MaulerStaff: //Maulers
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.Charisma || property == EProperty.ChaCapBonus ||
                            property == EProperty.MaxMana || property == EProperty.PowerPool || property == EProperty.PowerPoolCapBonus || property == EProperty.AcuCapBonus || property == EProperty.Acuity || property == EProperty.Piety || property == EProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case EObjectType.Instrument:
                    {
                        if (property == EProperty.Intelligence || property == EProperty.IntCapBonus || property == EProperty.Empathy || property == EProperty.EmpCapBonus || property == EProperty.Piety || property == EProperty.PieCapBonus)
                            return false;
                        break;
                    }
            }
            return true;
        }

        private void WriteBonus(EProperty property, int amount)
        {
            if (property == EProperty.AllFocusLevels)
            {
                amount = Math.Min(50, amount);
            }

            if (this.Bonus1 == 0)
            {
                this.Bonus1 = amount;
                this.Bonus1Type = (int)property;

                if (property == EProperty.AllFocusLevels)
                    this.Name = "Focus " + this.Name;
            }
            else if (this.Bonus2 == 0)
            {
                this.Bonus2 = amount;
                this.Bonus2Type = (int)property;
            }
            else if (this.Bonus3 == 0)
            {
                this.Bonus3 = amount;
                this.Bonus3Type = (int)property;
            }
            else if (this.Bonus4 == 0)
            {
                this.Bonus4 = amount;
                this.Bonus4Type = (int)property;
            }
            else if (this.Bonus5 == 0)
            {
                this.Bonus5 = amount;
                this.Bonus5Type = (int)property;
            }
        }

        private bool BonusExists(EProperty property)
        {
            if (this.Bonus1Type == (int)property ||
                this.Bonus2Type == (int)property ||
                this.Bonus3Type == (int)property ||
                this.Bonus4Type == (int)property ||
                this.Bonus5Type == (int)property)
                return true;

            return false;
        }

        private int GetBonusAmount(eBonusType type, EProperty property)
        {
            switch (type)
            {
                case eBonusType.Focus:
                    {
                        return this.Level;
                    }
                case eBonusType.Resist:
                    {
                        int max = (int)Math.Ceiling((((this.Level / 2.0) + 1) / 4));
                        return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                    }
                case eBonusType.Skill:
                    {
                        int max = (int)Util.Random(1, 4);
                        if (property == EProperty.AllSkills ||
                            property == EProperty.AllMagicSkills ||
                            property == EProperty.AllDualWieldingSkills ||
                            property == EProperty.AllMeleeWeaponSkills ||
                            property == EProperty.AllArcherySkills)
                            max = (int)Math.Ceiling((double)max / 2.0);
                        return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                    }
                case eBonusType.Stat:
                    {
                        if (property == EProperty.MaxHealth)
                        {
                            int max = (int)Math.Ceiling(((double)this.Level * 4.0) / 4);
                            return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                        }
                        else if (property == EProperty.MaxMana)
                        {
                            int max = (int)Math.Ceiling(((double)this.Level / 2.0 + 1) / 4);
                            return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                        }
                        else
                        {
                            int max = (int)Math.Ceiling(((double)this.Level * 1) / 3);
                            return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                        }
                    }
                case eBonusType.AdvancedStat:
                    {
                        if (property == EProperty.MaxHealthCapBonus)
                            return Util.Random(5, 25); // cap is 400
                        else if (property == EProperty.PowerPoolCapBonus)
                            return Util.Random(1, 10); // cap is 50
                        else
                            return Util.Random(1, 6); // cap is 26
                    }
            }
            return 1;
        }
        #endregion

        private void CapUtility(int mobLevel, int utilityMinimum)
        {
            int cap = 0;
            if (utilityMinimum < 1) utilityMinimum = 1;

            cap = mobLevel - 5;
            
            if (mobLevel > 70)
                cap = mobLevel + (Util.Random(1, 5));
            
            if (mobLevel < 65)
                cap -= (Util.Random(1, 5));
            
            if (mobLevel > 70 && cap < 60)
                cap = mobLevel-10;
            
            if (cap > 80) cap = 80;

            //randomize cap to be 80-105% of normal value
            double random = (80 + Util.Random(25)) / 100.0;
            cap = (int)Math.Floor(cap * random);

            if (cap < 15)
                cap = 15; //all items can gen with up to 15 uti

            if (this.ProcSpellID != 0 || this.ProcSpellID1 != 0)
                cap = (int)Math.Floor(cap * .7); //proc items generate with lower utility
            
            //Console.WriteLine($"Cap: {cap} floor {utilityMinimum} startUti: {startUti}");
            //bring uti up to floor first
            if (GetTotalUtility() < utilityMinimum)
            {
                int worstline = 1;
                int numAttempts = 0;
                int utiScaleAttempts = 25;
                while (GetTotalUtility() < utilityMinimum && numAttempts < utiScaleAttempts)
                {
                    //find highest utility line on the item
                    worstline = GetLowestUtilitySingleLine();
                    //Console.WriteLine($"TotalUti: {GetTotalUtility()} worstline {worstline} ");
                    numAttempts++;

                    //lower the value of it by
                    //1-5% for resist
                    //1-15 for stat
                    //1-3 for skill
                    switch (worstline)
                    {
                        case 1:
                            Bonus1 = IncreaseSingleLineUtility(Bonus1Type, Bonus1);
                            break;
                        case 2:
                            Bonus2 = IncreaseSingleLineUtility(Bonus2Type, Bonus2);
                            break;
                        case 3:
                            Bonus3 = IncreaseSingleLineUtility(Bonus3Type, Bonus3);
                            break;
                        case 4:
                            Bonus4 = IncreaseSingleLineUtility(Bonus4Type, Bonus4);
                            break;
                        case 5:
                            Bonus5 = IncreaseSingleLineUtility(Bonus5Type, Bonus5);
                            break;
                        case 6:
                            Bonus6 = IncreaseSingleLineUtility(Bonus6Type, Bonus6);
                            break;
                        case 7:
                            Bonus7 = IncreaseSingleLineUtility(Bonus7Type, Bonus7);
                            break;
                        case 8:
                            Bonus8 = IncreaseSingleLineUtility(Bonus8Type, Bonus8);
                            break;
                        case 9:
                            Bonus9 = IncreaseSingleLineUtility(Bonus9Type, Bonus9);
                            break;
                        case 10:
                            Bonus10 = IncreaseSingleLineUtility(Bonus10Type, Bonus10);
                            break;
                        case 11:
                            ExtraBonus = IncreaseSingleLineUtility(ExtraBonusType, ExtraBonus);
                            break;
                    }
                    //then recalculate
                }
            }
            
            //then cap it down to cieling
            if (GetTotalUtility() > cap)
            {
                int bestline = 1;
                while (GetTotalUtility() > cap)
                {
                    //find highest utility line on the item
                    bestline = GetHighestUtilitySingleLine();
                    //Console.WriteLine($"TotalUti: {GetTotalUtility()} bestline {bestline} ");

                    //lower the value of it by
                    //1-5% for resist
                    //1-15 for stat
                    //1-3 for skill
                    switch (bestline)
                    {
                        case 1:
                            Bonus1 = ReduceSingleLineUtility(Bonus1Type, Bonus1);
                            break;
                        case 2:
                            Bonus2 = ReduceSingleLineUtility(Bonus2Type, Bonus2);
                            break;
                        case 3:
                            Bonus3 = ReduceSingleLineUtility(Bonus3Type, Bonus3);
                            break;
                        case 4:
                            Bonus4 = ReduceSingleLineUtility(Bonus4Type, Bonus4);
                            break;
                        case 5:
                            Bonus5 = ReduceSingleLineUtility(Bonus5Type, Bonus5);
                            break;
                        case 6:
                            Bonus6 = ReduceSingleLineUtility(Bonus6Type, Bonus6);
                            break;
                        case 7:
                            Bonus7 = ReduceSingleLineUtility(Bonus7Type, Bonus7);
                            break;
                        case 8:
                            Bonus8 = ReduceSingleLineUtility(Bonus8Type, Bonus8);
                            break;
                        case 9:
                            Bonus9 = ReduceSingleLineUtility(Bonus9Type, Bonus9);
                            break;
                        case 10:
                            Bonus10 = ReduceSingleLineUtility(Bonus10Type, Bonus10);
                            break;
                        case 11:
                            ExtraBonus = ReduceSingleLineUtility(ExtraBonusType, ExtraBonus);
                            break;
                    }
                    //then recalculate
                }
            }

            //Console.WriteLine($"Capped Uti: {GetTotalUtility()}");
            //write name of item based off of capped lines
            int utiLine = GetHighestUtilitySingleLine();
            EProperty bonus = GetPropertyFromBonusLine(utiLine);
            //Console.WriteLine($"HighUti: {utiLine} bonus: {bonus}");
            WriteMagicalName(bonus);
            //Console.WriteLine($"Item name: {Name}");
        }

        public int GetHighestUtilitySingleLine()
        {
            double highestUti = GetSingleUtility(Bonus1Type, Bonus1);
            int highestLine = highestUti > 0 ? 1 : 0; //if line1 had a bonus, set it as highest line, otherwise default to 0

            if (GetSingleUtility(Bonus2Type, Bonus2) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus2Type, Bonus2);
                highestLine = 2;
            }

            if (GetSingleUtility(Bonus3Type, Bonus3) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus3Type, Bonus3);
                highestLine = 3;
            }

            if (GetSingleUtility(Bonus4Type, Bonus4) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus4Type, Bonus4);
                highestLine = 4;
            }

            if (GetSingleUtility(Bonus5Type, Bonus5) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus5Type, Bonus5);
                highestLine = 5;
            }

            if (GetSingleUtility(Bonus6Type, Bonus6) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus6Type, Bonus6);
                highestLine = 2;
            }

            if (GetSingleUtility(Bonus7Type, Bonus7) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus7Type, Bonus7);
                highestLine = 7;
            }

            if (GetSingleUtility(Bonus8Type, Bonus8) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus8Type, Bonus8);
                highestLine = 8;
            }

            if (GetSingleUtility(Bonus9Type, Bonus9) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus9Type, Bonus9);
                highestLine = 9;
            }

            if (GetSingleUtility(Bonus10Type, Bonus10) > highestUti)
            {
                highestUti = GetSingleUtility(Bonus10Type, Bonus10);
                highestLine = 10;
            }

            if (GetSingleUtility(ExtraBonusType, ExtraBonus) > highestUti)
            {
                highestLine = 11;
            }

            return highestLine;
        }
        
        public int GetLowestUtilitySingleLine()
        {
            double lowestUti = GetSingleUtility(Bonus1Type, Bonus1);
            int lowestLine = lowestUti > 0 ? 1 : 0; //if line1 had a bonus, set it as highest line, otherwise default to 0

            if (GetSingleUtility(Bonus2Type, Bonus2) < lowestUti && IsValidUpscaleType(Bonus2Type))
            {
                lowestUti = GetSingleUtility(Bonus2Type, Bonus2);
                lowestLine = 2;
            }

            if (GetSingleUtility(Bonus3Type, Bonus3) < lowestUti && IsValidUpscaleType(Bonus3Type))
            {
                lowestUti = GetSingleUtility(Bonus3Type, Bonus3);
                lowestLine = 3;
            }

            if (GetSingleUtility(Bonus4Type, Bonus4) < lowestUti && IsValidUpscaleType(Bonus4Type))
            {
                lowestUti = GetSingleUtility(Bonus4Type, Bonus4);
                lowestLine = 4;
            }

            if (GetSingleUtility(Bonus5Type, Bonus5) < lowestUti && IsValidUpscaleType(Bonus5Type))
            {
                lowestUti = GetSingleUtility(Bonus5Type, Bonus5);
                lowestLine = 5;
            }

            if (GetSingleUtility(Bonus6Type, Bonus6) < lowestUti && IsValidUpscaleType(Bonus6Type))
            {
                lowestUti = GetSingleUtility(Bonus6Type, Bonus6);
                lowestLine = 2;
            }

            if (GetSingleUtility(Bonus7Type, Bonus7) < lowestUti && IsValidUpscaleType(Bonus7Type))
            {
                lowestUti = GetSingleUtility(Bonus7Type, Bonus7);
                lowestLine = 7;
            }

            if (GetSingleUtility(Bonus8Type, Bonus8) < lowestUti && IsValidUpscaleType(Bonus8Type))
            {
                lowestUti = GetSingleUtility(Bonus8Type, Bonus8);
                lowestLine = 8;
            }

            if (GetSingleUtility(Bonus9Type, Bonus9) < lowestUti && IsValidUpscaleType(Bonus9Type))
            {
                lowestUti = GetSingleUtility(Bonus9Type, Bonus9);
                lowestLine = 9;
            }

            if (GetSingleUtility(Bonus10Type, Bonus10) < lowestUti && IsValidUpscaleType(Bonus10Type))
            {
                lowestUti = GetSingleUtility(Bonus10Type, Bonus10);
                lowestLine = 10;
            }

            if (GetSingleUtility(ExtraBonusType, ExtraBonus) < lowestUti && IsValidUpscaleType(ExtraBonusType))
            {
                lowestLine = 11;
            }

            return lowestLine;
        }

        private bool IsValidUpscaleType(int BonusType)
        {
            return BonusType != 0 
                   && BonusType != 163 
                   && BonusType != 164 
                   && BonusType != 167 
                   && BonusType != 168 
                   && BonusType != 213;
        }

        private int ReduceSingleLineUtility(int BonusType, int Bonus)
        {
            //Console.WriteLine($"Reducing utility for {this.Name}. Total bonus before: {Bonus}");
            //based off of eProperty
            //1-8 == stats = *.6667
            //9 == power cap = *2
            //10 == maxHP =  *.25
            //11-19 == resists = *2
            //20-115 == skill = *5
            //163 == all magic = *10
            //164 == all melee = *10
            //167 == all dual weild = *10
            //168 == all archery = *10
            if (BonusType != 0 &&
                Bonus != 0)
            {
                if (BonusType < 9 || BonusType == 156)
                {
                    //reduce by 1-4, but not more than exists
                    Bonus = Bonus - Util.Random(1, Math.Min(Bonus, 10)); //up to ~7 uti reduction
                }
                else if (BonusType == 9)
                {
                    Bonus = Bonus - Util.Random(1, Math.Min(Bonus, 2)); //up to 4 uti reduction
                }
                else if (BonusType == 10)
                {
                    Bonus = Bonus - Util.Random(1, Math.Min(Bonus, 20)); //up to 5 uti reduction
                }
                else if (BonusType < 20)
                {
                    Bonus = Bonus - Util.Random(1, Math.Min(Bonus, 3)); //up to 6 uti reduction
                }
                else if (BonusType < 115)
                {
                    Bonus = Bonus - Util.Random(1, Math.Min(Bonus, 1)); //up to 5 uti reduction
                }
                else if (BonusType == 163
                  || BonusType == 164
                  || BonusType == 167
                  || BonusType == 168
                  || BonusType == 213)
                {
                    Bonus = 0; //no +all skills on rogs
                }
            }
            //Console.WriteLine($"Total bonus after: {Bonus}");
            return Bonus;
        }
        
        private int IncreaseSingleLineUtility(int BonusType, int Bonus)
        {
            //Console.WriteLine($"Increasing utility for {this.Name}. Total bonus before: {Bonus} bonustype {BonusType}");
            //based off of eProperty
            //1-8 == stats = *.6667
            //9 == power cap = *2
            //10 == maxHP =  *.25
            //11-19 == resists = *2
            //20-115 == skill = *5
            //163 == all magic = *10
            //164 == all melee = *10
            //167 == all dual weild = *10
            //168 == all archery = *10
            if (BonusType != 0 &&
                Bonus != 0)
            {
                if (BonusType < 9 || BonusType == 156)
                {
                    //reduce by 1-4, but not more than exists
                    Bonus = Bonus + Util.Random(1, Math.Min(Bonus, 10)); //up to ~7 uti reduction
                }
                else if (BonusType == 9)
                {
                    Bonus = Bonus + Util.Random(1, Math.Min(Bonus, 2)); //up to 4 uti reduction
                }
                else if (BonusType == 10)
                {
                    Bonus = Bonus + Util.Random(1, Math.Min(Bonus, 20)); //up to 5 uti reduction
                }
                else if (BonusType < 20)
                {
                    Bonus = Bonus + Util.Random(1, Math.Min(Bonus, 3)); //up to 6 uti reduction
                }
                else if (BonusType < 115)
                {
                    Bonus = Bonus + Util.Random(1, Math.Min(Bonus, 1)); //up to 5 uti reduction
                }
                else if (BonusType == 163
                         || BonusType == 164
                         || BonusType == 167
                         || BonusType == 168
                         || BonusType == 213)
                {
                    Bonus = 0; //no +all skills on rogs
                }
            }
            //Console.WriteLine($"Total bonus after: {Bonus}");
            return Bonus;
        }

        private double GetTotalUtility()
        {
            double totalUti = 0;

            //based off of eProperty
            //1-8 == stats = *.6667
            //9 == power cap = *1
            //10 == maxHP =  *.25
            //11-19 == resists = *2
            //20-115 == skill = *5
            //163 == all magic = *10
            //164 == all melee = *10
            //167 == all dual weild = *10
            //168 == all archery = *10
            if (Bonus1Type != 0 &&
                Bonus1 != 0)
            {
                if (Bonus1Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus1 * .6667;
                }
                else if (Bonus1Type == 9)
                {
                    totalUti += Bonus1 ;
                }
                else if (Bonus1Type == 10)
                {
                    totalUti += Bonus1 * .25;
                }
                else if (Bonus1Type < 20)
                {
                    totalUti += Bonus1 * 2;
                }
                else if (Bonus1Type < 115)
                {
                    totalUti += Bonus1 * 5;
                }
                else if (Bonus1Type == 163
                  || Bonus1Type == 164
                  || Bonus1Type == 167
                  || Bonus1Type == 168
                  || Bonus1Type == 213)
                {
                    totalUti += Bonus1 * 10;
                }
            }

            if (Bonus2Type != 0 &&
                Bonus2 != 0)
            {
                if (Bonus2Type < 9 || Bonus2Type == 156)
                {
                    totalUti += Bonus2 * .6667;
                }
                else if (Bonus2Type == 9)
                {
                    totalUti += Bonus2;
                }
                else if (Bonus2Type == 10)
                {
                    totalUti += Bonus2 * .25;
                }
                else if (Bonus2Type < 20)
                {
                    totalUti += Bonus2 * 2;
                }
                else if (Bonus2Type < 115)
                {
                    totalUti += Bonus2 * 5;
                }
                else if (Bonus2Type == 163
                  || Bonus2Type == 164
                  || Bonus2Type == 167
                  || Bonus2Type == 168
                  || Bonus2Type == 213)
                {
                    totalUti += Bonus2 * 10;
                }
            }

            if (Bonus3Type != 0 &&
                Bonus3 != 0)
            {
                if (Bonus3Type < 9 || Bonus3Type == 156)
                {
                    totalUti += Bonus3 * .6667;
                }
                else if (Bonus3Type == 9)
                {
                    totalUti += Bonus3 ;
                }
                else if (Bonus3Type == 10)
                {
                    totalUti += Bonus3 * .25;
                }
                else if (Bonus3Type < 20)
                {
                    totalUti += Bonus3 * 2;
                }
                else if (Bonus3Type < 115)
                {
                    totalUti += Bonus3 * 5;
                }
                else if (Bonus3Type == 163
                  || Bonus3Type == 164
                  || Bonus3Type == 167
                  || Bonus3Type == 168
                  || Bonus3Type == 213)
                {
                    totalUti += Bonus3 * 10;
                }
            }

            if (Bonus4Type != 0 &&
                Bonus4 != 0)
            {
                if (Bonus4Type < 9 || Bonus4Type == 156)
                {
                    totalUti += Bonus4 * .6667;
                }
                else if (Bonus4Type == 9)
                {
                    totalUti += Bonus4;
                }
                else if (Bonus4Type == 10)
                {
                    totalUti += Bonus4 * .25;
                }
                else if (Bonus4Type < 20)
                {
                    totalUti += Bonus4 * 2;
                }
                else if (Bonus4Type < 115)
                {
                    totalUti += Bonus4 * 5;
                }
                else if (Bonus4Type == 163
                  || Bonus4Type == 164
                  || Bonus4Type == 167
                  || Bonus4Type == 168
                  || Bonus4Type == 213)
                {
                    totalUti += Bonus4 * 10;
                }
            }

            if (Bonus5Type != 0 &&
                Bonus5 != 0)
            {
                if (Bonus5Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus5 * .6667;
                }
                else if (Bonus5Type == 9)
                {
                    totalUti += Bonus5;
                }
                else if (Bonus5Type == 10)
                {
                    totalUti += Bonus5 * .25;
                }
                else if (Bonus5Type < 20)
                {
                    totalUti += Bonus5 * 2;
                }
                else if (Bonus5Type < 115)
                {
                    totalUti += Bonus5 * 5;
                }
                else if (Bonus5Type == 163
                  || Bonus5Type == 164
                  || Bonus5Type == 167
                  || Bonus5Type == 168
                  || Bonus5Type == 213)
                {
                    totalUti += Bonus5 * 10;
                }
            }

            if (Bonus6Type != 0 &&
                Bonus6 != 0)
            {
                if (Bonus6Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus6 * .6667;
                }
                else if (Bonus6Type == 9)
                {
                    totalUti += Bonus6 ;
                }
                else if (Bonus6Type == 10)
                {
                    totalUti += Bonus6 * .25;
                }
                else if (Bonus6Type < 20)
                {
                    totalUti += Bonus6 * 2;
                }
                else if (Bonus6Type < 115)
                {
                    totalUti += Bonus6 * 5;
                }
                else if (Bonus6Type == 163
                  || Bonus6Type == 164
                  || Bonus6Type == 167
                  || Bonus6Type == 168
                  || Bonus6Type == 213)
                {
                    totalUti += Bonus6 * 10;
                }
            }

            if (Bonus7Type != 0 &&
                Bonus7 != 0)
            {
                if (Bonus7Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus7 * .6667;
                }
                else if (Bonus7Type == 9)
                {
                    totalUti += Bonus7;
                }
                else if (Bonus7Type == 10)
                {
                    totalUti += Bonus7 * .25;
                }
                else if (Bonus7Type < 20)
                {
                    totalUti += Bonus7 * 2;
                }
                else if (Bonus7Type < 115)
                {
                    totalUti += Bonus7 * 5;
                }
                else if (Bonus7Type == 163
                  || Bonus7Type == 164
                  || Bonus7Type == 167
                  || Bonus7Type == 168
                  || Bonus7Type == 213)
                {
                    totalUti += Bonus7 * 10;
                }
            }
            if (Bonus8Type != 0 &&
                Bonus8 != 0)
            {
                if (Bonus8Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus8 * .6667;
                }
                else if (Bonus8Type == 9)
                {
                    totalUti += Bonus8;
                }
                else if (Bonus8Type == 10)
                {
                    totalUti += Bonus8 * .25;
                }
                else if (Bonus8Type < 20)
                {
                    totalUti += Bonus8 * 2;
                }
                else if (Bonus8Type < 115)
                {
                    totalUti += Bonus8 * 5;
                }
                else if (Bonus8Type == 163
                  || Bonus8Type == 164
                  || Bonus8Type == 167
                  || Bonus8Type == 168
                  || Bonus8Type == 213)
                {
                    totalUti += Bonus8 * 10;
                }
            }
            if (Bonus9Type != 0 &&
                Bonus9 != 0)
            {
                if (Bonus9Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus9 * .6667;
                }
                else if (Bonus9Type == 9)
                {
                    totalUti += Bonus9;
                }
                else if (Bonus9Type == 10)
                {
                    totalUti += Bonus9 * .25;
                }
                else if (Bonus9Type < 20)
                {
                    totalUti += Bonus9 * 2;
                }
                else if (Bonus9Type < 115)
                {
                    totalUti += Bonus9 * 5;
                }
                else if (Bonus9Type == 163
                  || Bonus9Type == 164
                  || Bonus9Type == 167
                  || Bonus9Type == 168
                  || Bonus9Type == 213)
                {
                    totalUti += Bonus9 * 10;
                }
            }
            if (Bonus10Type != 0 &&
                Bonus10 != 0)
            {
                if (Bonus10Type < 9 || Bonus1Type == 156)
                {
                    totalUti += Bonus10 * .6667;
                }
                else if (Bonus10Type == 9)
                {
                    totalUti += Bonus10;
                }
                else if (Bonus10Type == 10)
                {
                    totalUti += Bonus10 * .25;
                }
                else if (Bonus10Type < 20)
                {
                    totalUti += Bonus10 * 2;
                }
                else if (Bonus10Type < 115)
                {
                    totalUti += Bonus10 * 5;
                }
                else if (Bonus10Type == 163
                  || Bonus10Type == 164
                  || Bonus10Type == 167
                  || Bonus10Type == 168
                  || Bonus10Type == 213)
                {
                    totalUti += Bonus10 * 10;
                }
            }
            if (ExtraBonusType != 0 &&
                ExtraBonus != 0)
            {
                if (ExtraBonusType < 9 || Bonus1Type == 156)
                {
                    totalUti += ExtraBonus * .6667;
                }
                else if (ExtraBonusType == 9)
                {
                    totalUti += ExtraBonus;
                }
                else if (ExtraBonusType == 10)
                {
                    totalUti += ExtraBonus * .25;
                }
                else if (ExtraBonusType < 20)
                {
                    totalUti += ExtraBonus * 2;
                }
                else if (ExtraBonusType < 115)
                {
                    totalUti += ExtraBonus * 5;
                }
                else if (ExtraBonusType == 163
                  || ExtraBonusType == 164
                  || ExtraBonusType == 167
                  || ExtraBonusType == 168
                  || ExtraBonusType == 213)
                {
                    totalUti += ExtraBonus * 10;
                }
            }

            return totalUti;
        }

        private double GetSingleUtility(int BonusType, int Bonus)
        {
            double totalUti = 0;

            //based off of eProperty
            //1-8 == stats = *.6667
            //9 == power cap = *1
            //10 == maxHP =  *.25
            //11-19 == resists = *2
            //20-115 == skill = *5
            //163 == all magic = *10
            //164 == all melee = *10
            //167 == all dual weild = *10
            //168 == all archery = *10
            if (BonusType != 0 &&
                Bonus != 0)
            {
                if (BonusType < 9 || BonusType == 156)
                {
                    totalUti += Bonus * .6667;
                }
                else if (BonusType == 9)
                {
                    totalUti += Bonus;
                }
                else if (BonusType == 10)
                {
                    totalUti += Bonus * .25;
                }
                else if (BonusType < 20)
                {
                    totalUti += Bonus * 2;
                }
                else if (BonusType < 115)
                {
                    totalUti += Bonus * 5;
                }
                else if (BonusType == 163
                  || BonusType == 164
                  || BonusType == 167
                  || BonusType == 168
                  || BonusType == 213)
                {
                    totalUti += Bonus * 10;
                }
            }


            return totalUti;
        }

        #region generate item type


        private static EObjectType GenerateObjectType(ERealm realm, EPlayerClass charClass, byte level)
        {
            eGenerateType type = GetObjectTypeByWeight(level);

            switch ((ERealm)realm)
            {
                case ERealm.Albion:
                    {
                        int maxArmor = AlbionArmor.Length - 1;
                        int maxWeapon = AlbionWeapons.Length - 1;

                        if (level < 15)
                            maxArmor--; // remove plate

                        if (level < 5)
                        {
                            maxArmor--; // remove chain
                            maxWeapon = 4; // remove all but base weapons and shield
                        }

                        switch (type)
                        {
                            case eGenerateType.Armor: return GetAlbionArmorType(charClass, level);//AlbionArmor[Util.Random(0, maxArmor)];
                            case eGenerateType.Weapon: return GetAlbionWeapon(charClass);//AlbionWeapons[Util.Random(0, maxWeapon)];
                            case eGenerateType.Magical: return EObjectType.Magical;
                        }
                        break;
                    }
                case ERealm.Midgard:
                    {
                        int maxArmor = MidgardArmor.Length - 1;
                        int maxWeapon = MidgardWeapons.Length - 1;

                        if (level < 10)
                            maxArmor--; // remove chain

                        if (level < 5)
                        {
                            maxWeapon = 4; // remove all but base weapons and shield
                        }

                        switch (type)
                        {
                            case eGenerateType.Armor: return GetMidgardArmorType(charClass, level); //MidgardArmor[Util.Random(0, maxArmor)];
                            case eGenerateType.Weapon: return GetMidgardWeapon(charClass); //MidgardWeapons[Util.Random(0, maxWeapon)];
                            case eGenerateType.Magical: return EObjectType.Magical;
                        }
                        break;
                    }
                case ERealm.Hibernia:
                    {
                        int maxArmor = HiberniaArmor.Length - 1;
                        int maxWeapon = HiberniaWeapons.Length - 1;

                        if (level < 15)
                            maxArmor--; // remove scale

                        if (level < 5)
                        {
                            maxWeapon = 4; // remove all but base weapons and shield
                        }

                        switch (type)
                        {
                            case eGenerateType.Armor: return GetHiberniaArmorType(charClass, level);//HiberniaArmor[Util.Random(0, maxArmor)];
                            case eGenerateType.Weapon: return GetHiberniaWeapon(charClass);//HiberniaWeapons[Util.Random(0, maxWeapon)];
                            case eGenerateType.Magical: return EObjectType.Magical;
                        }
                        break;
                    }
            }
            return EObjectType.GenericItem;
        }

        private static eGenerateType GetObjectTypeByWeight(byte level)
        {
            List<eGenerateType> genTypes = new List<eGenerateType>();

            //weighted so that early levels get many more weapons/armor
            if (level < 5)
            {
                if (Util.Chance(45))
                    return eGenerateType.Weapon;
                //else if (Util.Chance(15))
                  //  return eGenerateType.Magical;
                else return eGenerateType.Armor;
            }
            else if (level < 10)
            {
                if (Util.Chance(ArmorWeight)) { genTypes.Add(eGenerateType.Armor); }
                //if (Util.Chance(ROG_MAGICAL_CHANCE)) { genTypes.Add(eGenerateType.Magical); }
                if (Util.Chance(WeaponWeight)) { genTypes.Add(eGenerateType.Weapon); }
            }
            else
            {
                if (Util.Chance(ArmorWeight + Util.Random(ArmorWeight))) { genTypes.Add(eGenerateType.Armor); }
                if (Util.Chance(JewelryWeight)) { genTypes.Add(eGenerateType.Magical); }
                if (Util.Chance(WeaponWeight + Util.Random(WeaponWeight)/2) ) { genTypes.Add(eGenerateType.Weapon); }
            }

            //if none of the object types were added, default to armor
            if (genTypes.Count < 1)
            {
                if(Util.Chance(50))
                    genTypes.Add(eGenerateType.Armor);
                else
                    genTypes.Add(eGenerateType.Weapon);
            }

            return genTypes[Util.Random(genTypes.Count - 1)];
        }

        public static EObjectType GetAlbionWeapon(EPlayerClass charClass)
        {
            List<EObjectType> weaponTypes = new List<EObjectType>();
            /*
			 * Albion Weapons
			eObjectType.ThrustWeapon,
			eObjectType.CrushingWeapon,
			eObjectType.SlashingWeapon, 
			eObjectType.Shield,
			eObjectType.Staff,//
			eObjectType.TwoHandedWeapon,
			eObjectType.Longbow,//
			eObjectType.Flexible,//
			eObjectType.PolearmWeapon,
			eObjectType.FistWraps, //Maulers//
			eObjectType.MaulerStaff,//Maulers//
			eObjectType.Instrument,//
			eObjectType.Crossbow,
			*/
            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Cabalist:
                case EPlayerClass.Necromancer:
                case EPlayerClass.Sorcerer:
                case EPlayerClass.Theurgist:
                case EPlayerClass.Wizard:
                    weaponTypes.Add(EObjectType.Staff);
                    break;
                case EPlayerClass.Friar:
                    weaponTypes.Add(EObjectType.Staff);
                    weaponTypes.Add(EObjectType.Staff);
                    weaponTypes.Add(EObjectType.Staff);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Armsman:
                    weaponTypes.Add(EObjectType.PolearmWeapon);
                    weaponTypes.Add(EObjectType.PolearmWeapon);
                    weaponTypes.Add(EObjectType.PolearmWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.TwoHandedWeapon);
                    weaponTypes.Add(EObjectType.TwoHandedWeapon);
                    weaponTypes.Add(EObjectType.Crossbow);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Paladin:
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.TwoHandedWeapon);
                    weaponTypes.Add(EObjectType.TwoHandedWeapon);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Reaver:
                    weaponTypes.Add(EObjectType.Flexible);
                    weaponTypes.Add(EObjectType.Flexible);
                    weaponTypes.Add(EObjectType.Flexible);
                    weaponTypes.Add(EObjectType.Flexible);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Minstrel:
                    weaponTypes.Add(EObjectType.Instrument);
                    weaponTypes.Add(EObjectType.Instrument);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Infiltrator:
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.Crossbow);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Scout:
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.Longbow);
                    weaponTypes.Add(EObjectType.Longbow);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Mercenary:
                    weaponTypes.Add(EObjectType.Fired); //shortbow
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.SlashingWeapon);
                    weaponTypes.Add(EObjectType.ThrustWeapon);
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Cleric:
                    weaponTypes.Add(EObjectType.CrushingWeapon);
                    weaponTypes.Add(EObjectType.Staff);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                default:
                    return EObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<EObjectType> outputList = new List<EObjectType>(weaponTypes);

            //add duplicate values
            foreach (EObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);

            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static EObjectType GetAlbionArmorType(EPlayerClass charClass, byte level)
        {

            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Cabalist:
                case EPlayerClass.Necromancer:
                case EPlayerClass.Sorcerer:
                case EPlayerClass.Theurgist:
                case EPlayerClass.Wizard:
                    return EObjectType.Cloth;

                case EPlayerClass.Friar:
                case EPlayerClass.Infiltrator:
                    return EObjectType.Leather;

                case EPlayerClass.Armsman:
                    if (level < 5)
                    {
                        return EObjectType.Studded;
                    }
                    else if (level < 15)
                    {
                        return EObjectType.Chain;
                    }
                    else
                    {
                        return EObjectType.Plate;
                    }

                case EPlayerClass.Paladin:
                    if (level < 10)
                    {
                        return EObjectType.Studded;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Chain;
                    }
                    else
                    {
                        return EObjectType.Plate;
                    }

                case EPlayerClass.Reaver:
                case EPlayerClass.Mercenary:
                    if (level < 10)
                    {
                        return EObjectType.Studded;
                    }
                    else
                    {
                        return EObjectType.Chain;
                    }

                case EPlayerClass.Minstrel:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Studded;
                    }
                    else
                    {
                        return EObjectType.Chain;
                    }

                case EPlayerClass.Scout:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else { return EObjectType.Studded; }

                case EPlayerClass.Cleric:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Studded;
                    }
                    else
                    {
                        return EObjectType.Chain;
                    }
                default:
                    return EObjectType.Cloth;
            }
        }

        public static EObjectType GetMidgardWeapon(EPlayerClass charClass)
        {

            List<EObjectType> weaponTypes = new List<EObjectType>();
            /*
			 * Midgard Weapons
			eObjectType.Sword,
			eObjectType.Hammer,
			eObjectType.Axe,
			eObjectType.Shield,
			eObjectType.Staff,
			eObjectType.Spear,
			eObjectType.CompositeBow,
			eObjectType.LeftAxe,
			eObjectType.HandToHand,
			eObjectType.FistWraps,//Maulers
			eObjectType.MaulerStaff,//Maulers
			*/
            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Bonedancer:
                case EPlayerClass.Runemaster:
                case EPlayerClass.Spiritmaster:
                    weaponTypes.Add(EObjectType.Staff);
                    break;
                case EPlayerClass.Healer:
                case EPlayerClass.Shaman:
                    weaponTypes.Add(EObjectType.Staff);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Hunter:
                    weaponTypes.Add(EObjectType.Spear);
                    weaponTypes.Add(EObjectType.CompositeBow);
                    weaponTypes.Add(EObjectType.Spear);
                    weaponTypes.Add(EObjectType.CompositeBow);
                    weaponTypes.Add(EObjectType.Sword);
                    break;
                case EPlayerClass.Savage:
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.HandToHand);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    break;
                case EPlayerClass.Shadowblade:
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Berserker:
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.LeftAxe);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Thane:
                case EPlayerClass.Warrior:
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Skald:
                    //hi Catkain <3
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Sword);
                    weaponTypes.Add(EObjectType.Axe);
                    weaponTypes.Add(EObjectType.Hammer);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                default:
                    return EObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<EObjectType> outputList = new List<EObjectType>(weaponTypes);

            //add duplicate values
            foreach (EObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);


            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static EObjectType GetMidgardArmorType(EPlayerClass charClass, byte level)
        {

            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Bonedancer:
                case EPlayerClass.Runemaster:
                case EPlayerClass.Spiritmaster:
                    return EObjectType.Cloth;

                case EPlayerClass.Shadowblade:
                    return EObjectType.Leather;

                case EPlayerClass.Hunter:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else
                    {
                        return EObjectType.Studded;
                    }

                case EPlayerClass.Berserker:
                case EPlayerClass.Savage:
                    return EObjectType.Studded;

                case EPlayerClass.Shaman:
                case EPlayerClass.Healer:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Studded;
                    }
                    else
                    {
                        return EObjectType.Chain;
                    }

                case EPlayerClass.Skald:
                    if (level < 20)
                    {
                        return EObjectType.Studded;
                    }
                    else { return EObjectType.Chain; }

                case EPlayerClass.Warrior:
                    if (level < 10)
                    {
                        return EObjectType.Studded;
                    }
                    else { return EObjectType.Chain; }

                case EPlayerClass.Thane:
                    if (level < 12)
                    {
                        return EObjectType.Studded;
                    }
                    else
                    {
                        return EObjectType.Chain;
                    }

                default:
                    return EObjectType.Cloth;
            }
        }

        public static EObjectType GetHiberniaWeapon(EPlayerClass charClass)
        {
            List<EObjectType> weaponTypes = new List<EObjectType>();
            /*
			 * Hibernia Weapons
			eObjectType.Blades,
			eObjectType.Blunt,
			eObjectType.Piercing,
			eObjectType.Shield,
			eObjectType.Staff,
			eObjectType.LargeWeapons,
			eObjectType.CelticSpear,
			eObjectType.Scythe,
			eObjectType.RecurvedBow,
			eObjectType.Instrument,
			eObjectType.FistWraps,//Maulers
			eObjectType.MaulerStaff,//Maulers
			*/
            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Eldritch:
                case EPlayerClass.Enchanter:
                case EPlayerClass.Mentalist:
                case EPlayerClass.Animist:
                    weaponTypes.Add(EObjectType.Staff);
                    break;
                case EPlayerClass.Valewalker:
                    weaponTypes.Add(EObjectType.Scythe);
                    break;
                case EPlayerClass.Nightshade:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Ranger:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.RecurvedBow);
                    weaponTypes.Add(EObjectType.RecurvedBow);
                    weaponTypes.Add(EObjectType.RecurvedBow);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Champion:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.LargeWeapons);
                    weaponTypes.Add(EObjectType.LargeWeapons);
                    weaponTypes.Add(EObjectType.LargeWeapons);
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Hero:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.LargeWeapons);
                    weaponTypes.Add(EObjectType.CelticSpear);
                    weaponTypes.Add(EObjectType.LargeWeapons);
                    weaponTypes.Add(EObjectType.CelticSpear);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Fired); //shortbow
                    break;
                case EPlayerClass.Blademaster:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Piercing);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Fired); //shortbow
                    weaponTypes.Add(EObjectType.Shield);
                    break;
                case EPlayerClass.Warden:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Fired); //shortbow
                    break;
                case EPlayerClass.Druid:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Staff);
                    break;
                case EPlayerClass.Bard:
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Blades);
                    weaponTypes.Add(EObjectType.Blunt);
                    weaponTypes.Add(EObjectType.Shield);
                    weaponTypes.Add(EObjectType.Instrument);
                    weaponTypes.Add(EObjectType.Instrument);
                    break;
                default:
                    return EObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<EObjectType> outputList = new List<EObjectType>(weaponTypes);

            //add duplicate values
            foreach (EObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);


            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static EObjectType GetHiberniaArmorType(EPlayerClass charClass, byte level)
        {

            /* Hib Armor
			eObjectType.Cloth,
			eObjectType.Leather,
			eObjectType.Reinforced,
			eObjectType.Scale,
			 */
            switch (charClass)
            {
                //staff classes
                case EPlayerClass.Valewalker:
                case EPlayerClass.Animist:
                case EPlayerClass.Mentalist:
                case EPlayerClass.Enchanter:
                case EPlayerClass.Eldritch:
                    return EObjectType.Cloth;

                case EPlayerClass.Nightshade:
                    return EObjectType.Leather;

                case EPlayerClass.Blademaster:
                    return EObjectType.Reinforced;

                case EPlayerClass.Ranger:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else
                    {
                        return EObjectType.Reinforced;
                    }

                case EPlayerClass.Champion:
                    if (level < 20)
                    {
                        return EObjectType.Reinforced;
                    }
                    else { return EObjectType.Scale; }

                case EPlayerClass.Hero:
                    if (level < 15)
                    {
                        return EObjectType.Reinforced;
                    }
                    else { return EObjectType.Scale; }

                case EPlayerClass.Warden:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Reinforced;
                    }
                    else { return EObjectType.Scale; }

                case EPlayerClass.Druid:
                    if (level < 10)
                    {
                        return EObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return EObjectType.Reinforced;
                    }
                    else { return EObjectType.Scale; }

                case EPlayerClass.Bard:
                    if (level < 15)
                    {
                        return EObjectType.Leather;
                    }
                    else { return EObjectType.Reinforced; }

                default:
                    return EObjectType.Cloth;
            }
        }

        public static EInventorySlot GenerateItemType(EObjectType type)
        {
            if ((int)type >= (int)EObjectType._FirstArmor && (int)type <= (int)EObjectType._LastArmor)
                return (EInventorySlot)ArmorSlots[Util.Random(0, ArmorSlots.Length - 1)];
            switch (type)
            {
                //left or right standard
                //tolakram - left hand usable now set based on speed
                case EObjectType.HandToHand:
                case EObjectType.Piercing:
                case EObjectType.Blades:
                case EObjectType.Blunt:
                case EObjectType.SlashingWeapon:
                case EObjectType.CrushingWeapon:
                case EObjectType.ThrustWeapon:
                case EObjectType.FistWraps: //Maulers
                case EObjectType.Flexible:
                    return (EInventorySlot)Slot.RIGHTHAND;
                //left or right or twohand
                case EObjectType.Sword:
                case EObjectType.Axe:
                case EObjectType.Hammer:
                    if (Util.Random(100) >= 50)
                        return (EInventorySlot)Slot.RIGHTHAND;
                    else
                        return (EInventorySlot)Slot.TWOHAND;
                //left
                case EObjectType.LeftAxe:
                case EObjectType.Shield:
                    return (EInventorySlot)Slot.LEFTHAND;
                //twohanded
                case EObjectType.LargeWeapons:
                case EObjectType.CelticSpear:
                case EObjectType.PolearmWeapon:
                case EObjectType.Spear:
                case EObjectType.Staff:
                case EObjectType.Scythe:
                case EObjectType.TwoHandedWeapon:
                case EObjectType.MaulerStaff:
                    return (EInventorySlot)Slot.TWOHAND;
                //ranged
                case EObjectType.CompositeBow:
                case EObjectType.Fired:
                case EObjectType.Longbow:
                case EObjectType.RecurvedBow:
                case EObjectType.Crossbow:
                    return (EInventorySlot)Slot.RANGED;
                case EObjectType.Magical:
                    return (EInventorySlot)MagicalSlots[Util.Random(0, MagicalSlots.Length - 1)];
                case EObjectType.Instrument:
                    return (EInventorySlot)Slot.RANGED;
            }
            return EInventorySlot.FirstEmptyBackpack;
        }

        private static EDamageType GenerateDamageType(EObjectType type, EPlayerClass charClass)
        {
            switch (type)
            {
                //all
                case EObjectType.TwoHandedWeapon:
                case EObjectType.PolearmWeapon:
                case EObjectType.Instrument:
                    return (EDamageType)Util.Random(1, 3);
                //slash
                case EObjectType.Axe:
                case EObjectType.Blades:
                case EObjectType.SlashingWeapon:
                case EObjectType.LeftAxe:
                case EObjectType.Sword:
                case EObjectType.Scythe:
                    return EDamageType.Slash;
                //thrust
                case EObjectType.ThrustWeapon:
                case EObjectType.Piercing:
                case EObjectType.CelticSpear:
                case EObjectType.Longbow:
                case EObjectType.RecurvedBow:
                case EObjectType.CompositeBow:
                case EObjectType.Fired:
                case EObjectType.Crossbow:
                    return EDamageType.Thrust;
                //crush
                case EObjectType.Hammer:
                case EObjectType.CrushingWeapon:
                case EObjectType.Blunt:
                case EObjectType.MaulerStaff: //Maulers
                case EObjectType.FistWraps: //Maulers
                case EObjectType.Staff:
                    return EDamageType.Crush;
                //specifics
                case EObjectType.HandToHand:
                case EObjectType.Spear:
                    return (EDamageType)Util.Random(2, 3);
                case EObjectType.LargeWeapons:
                case EObjectType.Flexible:
                    return (EDamageType)Util.Random(1, 2);
                //do shields return the shield size?
                case EObjectType.Shield:
                    return (EDamageType)Util.Random(1, GetMaxShieldSizeFromClass(charClass));
                    //return (eDamageType)Util.Random(1, 3);
            }
            return EDamageType.Natural;
        }

        private static int GetMaxShieldSizeFromClass(EPlayerClass charClass)
        {
            //shield size is based off of damage type
            //1 = small shield
            //2 = medium
            //3 = large
            switch (charClass)
            {
                case EPlayerClass.Berserker:
                case EPlayerClass.Skald:
                case EPlayerClass.Savage:
                case EPlayerClass.Healer:
                case EPlayerClass.Shaman:
                case EPlayerClass.Shadowblade:
                case EPlayerClass.Bard:
                case EPlayerClass.Druid:
                case EPlayerClass.Nightshade:
                case EPlayerClass.Ranger:
                case EPlayerClass.Infiltrator:
                case EPlayerClass.Minstrel:
                case EPlayerClass.Scout:
                    return 1;

                case EPlayerClass.Thane:
                case EPlayerClass.Warden:
                case EPlayerClass.Blademaster:
                case EPlayerClass.Champion:
                case EPlayerClass.Mercenary:
                case EPlayerClass.Cleric:
                    return 2;

                case EPlayerClass.Warrior:
                case EPlayerClass.Hero:
                case EPlayerClass.Armsman:
                case EPlayerClass.Paladin:
                case EPlayerClass.Reaver:
                    return 3;
                default: return 1;
            }
        }

        #endregion

        #region generate item speed and abs

        private static int GetAbsorb(EObjectType type)
        {
            switch (type)
            {
                case EObjectType.Cloth: return 0;
                case EObjectType.Leather: return 10;
                case EObjectType.Studded: return 19;
                case EObjectType.Reinforced: return 19;
                case EObjectType.Chain: return 27;
                case EObjectType.Scale: return 27;
                case EObjectType.Plate: return 34;
                default: return 0;
            }
        }

        private void SetWeaponSpeed()
        {
            // tolakram - reset speeds based on data from allakhazam 1-26-2008
            // removed specific left hand speed - left hand usable set based on speed in GenerateItemNameModel

            switch ((EObjectType)this.Object_Type)
            {
                case EObjectType.SlashingWeapon:
                    {
                        this.SPD_ABS = Util.Random(26, 39);
                        return;
                    }
                case EObjectType.CrushingWeapon:
                    {
                        this.SPD_ABS = Util.Random(30, 40);
                        return;
                    }
                case EObjectType.ThrustWeapon:
                    {
                        this.SPD_ABS = Util.Random(25, 37);
                        return;
                    }
                case EObjectType.Fired:
                    {
                        this.SPD_ABS = Util.Random(40, 46);
                        return;
                    }
                case EObjectType.TwoHandedWeapon:
                    {
                        this.SPD_ABS = Util.Random(43, 51);
                        return;
                    }
                case EObjectType.PolearmWeapon:
                    {
                        this.SPD_ABS = Util.Random(53, 56);
                        return;
                    }
                case EObjectType.Staff:
                    {
                        this.SPD_ABS = Util.Random(30, 50);
                        return;
                    }
                case EObjectType.MaulerStaff: //Maulers
                    {
                        this.SPD_ABS = Util.Random(34, 54);
                        return;
                    }
                case EObjectType.Longbow:
                    {
                        this.SPD_ABS = Util.Random(40, 52);
                        return;
                    }
                case EObjectType.Crossbow:
                    {
                        this.SPD_ABS = Util.Random(33, 54);
                        return;
                    }
                case EObjectType.Flexible:
                    {
                        this.SPD_ABS = Util.Random(33, 39);
                        return;
                    }
                case EObjectType.Sword:
                    if (this.Hand == 1)
                    {
                        this.SPD_ABS = Util.Random(46, 51);  // two handed
                        return;
                    }
                    else
                    {
                        this.SPD_ABS = Util.Random(25, 38); // one handed
                        return;
                    }
                case EObjectType.Hammer:
                    {
                        if (this.Hand == 1)
                        {
                            this.SPD_ABS = Util.Random(49, 52);  // two handed
                            return;
                        }
                        else
                        {
                            this.SPD_ABS = Util.Random(31, 39); // one handed
                            return;
                        }
                    }
                case EObjectType.Axe:
                    {
                        if (this.Hand == 1)
                        {
                            this.SPD_ABS = Util.Random(49, 53);  // two handed
                            return;
                        }
                        else
                        {
                            this.SPD_ABS = Util.Random(37, 40); // one handed
                            return;
                        }
                    }
                case EObjectType.Spear:
                    {
                        this.SPD_ABS = Util.Random(43, 52);
                        return;
                    }
                case EObjectType.CompositeBow:
                    {
                        this.SPD_ABS = Util.Random(40, 47);
                        return;
                    }
                case EObjectType.LeftAxe:
                    {
                        this.SPD_ABS = Util.Random(27, 31);
                        return;
                    }
                case EObjectType.HandToHand:
                    {
                        this.SPD_ABS = Util.Random(27, 37);
                        return;
                    }
                case EObjectType.FistWraps:
                    {
                        this.SPD_ABS = Util.Random(28, 41);
                        return;
                    }
                case EObjectType.RecurvedBow:
                    {
                        this.SPD_ABS = Util.Random(45, 52);
                        return;
                    }
                case EObjectType.Blades:
                    {
                        this.SPD_ABS = Util.Random(27, 39);
                        return;
                    }
                case EObjectType.Blunt:
                    {
                        this.SPD_ABS = Util.Random(30, 40);
                        return;
                    }
                case EObjectType.Piercing:
                    {
                        this.SPD_ABS = Util.Random(25, 36);
                        return;
                    }
                case EObjectType.LargeWeapons:
                    {
                        this.SPD_ABS = Util.Random(47, 53);
                        return;
                    }
                case EObjectType.CelticSpear:
                    {
                        this.SPD_ABS = Util.Random(40, 56);
                        return;
                    }
                case EObjectType.Scythe:
                    {
                        this.SPD_ABS = Util.Random(40, 53);
                        return;
                    }
                case EObjectType.Shield:
                    {
                        switch (this.Type_Damage)
                        {
                            case 1:
                                this.SPD_ABS = 30;
                                return;
                            case 2:
                                this.SPD_ABS = 40;
                                return;
                            case 3:
                                this.SPD_ABS = 50;
                                return;
                        }
                        this.SPD_ABS = 50;
                        return;
                    }
            }
            // for unhandled types
            if (this.Hand == 1)
            {
                this.SPD_ABS = 50;  // two handed
                return;
            }
            else if (this.Hand == 2)
            {
                this.SPD_ABS = 30;  // left hand
                return;
            }
            else
            {
                this.SPD_ABS = 40; // right hand
                return;
            }
        }

        public void GenerateItemWeight()
        {
            EObjectType type = (EObjectType)this.Object_Type;
            EInventorySlot slot = (EInventorySlot)this.Item_Type;

            switch (type)
            {
                case EObjectType.LeftAxe:
                case EObjectType.Flexible:
                case EObjectType.Axe:
                case EObjectType.Blades:
                case EObjectType.HandToHand:
                case EObjectType.FistWraps: //Maulers
                    this.Weight = 20;
                    return;
                case EObjectType.CompositeBow:
                case EObjectType.RecurvedBow:
                case EObjectType.Longbow:
                case EObjectType.Blunt:
                case EObjectType.CrushingWeapon:
                case EObjectType.Fired:
                case EObjectType.Hammer:
                case EObjectType.Piercing:
                case EObjectType.SlashingWeapon:
                case EObjectType.Sword:
                case EObjectType.ThrustWeapon:
                    this.Weight = 30;
                    return;
                case EObjectType.Crossbow:
                case EObjectType.Spear:
                case EObjectType.CelticSpear:
                case EObjectType.Staff:
                case EObjectType.TwoHandedWeapon:
                case EObjectType.MaulerStaff: //Maulers
                    this.Weight = 40;
                    return;
                case EObjectType.Scale:
                case EObjectType.Chain:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: this.Weight = 48; return;
                            case EInventorySlot.FeetArmor: this.Weight = 32; return;
                            case EInventorySlot.HandsArmor: this.Weight = 32; return;
                            case EInventorySlot.HeadArmor: this.Weight = 32; return;
                            case EInventorySlot.LegsArmor: this.Weight = 56; return;
                            case EInventorySlot.TorsoArmor: this.Weight = 80; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case EObjectType.Cloth:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: this.Weight = 8; return;
                            case EInventorySlot.FeetArmor: this.Weight = 8; return;
                            case EInventorySlot.HandsArmor: this.Weight = 8; return;
                            case EInventorySlot.HeadArmor: this.Weight = 32; return;
                            case EInventorySlot.LegsArmor: this.Weight = 14; return;
                            case EInventorySlot.TorsoArmor: this.Weight = 20; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case EObjectType.Instrument:
                    this.Weight = 15;
                    return;
                case EObjectType.LargeWeapons:
                    this.Weight = 50;
                    return;
                case EObjectType.Leather:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: this.Weight = 24; return;
                            case EInventorySlot.FeetArmor: this.Weight = 16; return;
                            case EInventorySlot.HandsArmor: this.Weight = 16; return;
                            case EInventorySlot.HeadArmor: this.Weight = 16; return;
                            case EInventorySlot.LegsArmor: this.Weight = 28; return;
                            case EInventorySlot.TorsoArmor: this.Weight = 40; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case EObjectType.Magical:
                    this.Weight = 5;
                    return;
                case EObjectType.Plate:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: this.Weight = 54; return;
                            case EInventorySlot.FeetArmor: this.Weight = 36; return;
                            case EInventorySlot.HandsArmor: this.Weight = 36; return;
                            case EInventorySlot.HeadArmor: this.Weight = 40; return;
                            case EInventorySlot.LegsArmor: this.Weight = 63; return;
                            case EInventorySlot.TorsoArmor: this.Weight = 90; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case EObjectType.PolearmWeapon:
                    this.Weight = 60;
                    return;
                case EObjectType.Reinforced:
                case EObjectType.Studded:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: this.Weight = 36; return;
                            case EInventorySlot.FeetArmor: this.Weight = 24; return;
                            case EInventorySlot.HandsArmor: this.Weight = 24; return;
                            case EInventorySlot.HeadArmor: this.Weight = 24; return;
                            case EInventorySlot.LegsArmor: this.Weight = 42; return;
                            case EInventorySlot.TorsoArmor: this.Weight = 60; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case EObjectType.Scythe:
                    this.Weight = 40;
                    return;
                case EObjectType.Shield:
                    switch (this.Type_Damage)
                    {
                        case 1:
                            this.Weight = 31;
                            return;
                        case 2:
                            this.Weight = 35;
                            return;
                        case 3:
                            this.Weight = 38;
                            return;
                    }
                    this.Weight = 31;
                    return;
            }
            this.Weight = 10;
            return;
        }

        #endregion
        bool m_named = false;
        #region Naming and Modeling
        public bool WriteMagicalName(EProperty property)
        {
            if (hPropertyToMagicPrefix.ContainsKey(property) && !m_named)
            {
                string str = hPropertyToMagicPrefix[property];
                //Console.WriteLine($"Str: {str}");
                if (str != string.Empty)
                    this.Name = str + " " + this.Name;
                m_named = true;
                //Console.WriteLine("Named = true, name = " + this.Name);
                return true;
            }

            return false;
        }


        private void GenerateItemNameModel()
        {
            EInventorySlot slot = (EInventorySlot)this.Item_Type;
            EDamageType damage = (EDamageType)this.Type_Damage;
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)this.Object_Type;

            string name = "No Name";
            int model = 488;
            bool canAddExtension = false;

            switch (type)
            {
                //armor
                case EObjectType.Cloth:
                    {
                        name = "Cloth " + ArmorSlotToName(slot, type);

                        switch (realm)
                        {
                            case ERealm.Albion:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = 141; break;
                                    case EInventorySlot.LegsArmor: model = 140; break;
                                    case EInventorySlot.FeetArmor: model = 143; break;
                                    case EInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1278; //30% chance of wizard hat
                                        else
                                            model = 822;
                                        break;
                                    case EInventorySlot.HandsArmor: model = 142; break;
                                    case EInventorySlot.TorsoArmor:
                                        if (Util.Chance(60))
                                        {
                                            model = 139;
                                        }
                                        else
                                        {
                                            name = "Cloth Robe";

                                            switch (Util.Random(2))
                                            {
                                                case 0: model = 58; break;
                                                case 1: model = 65; break;
                                                case 2: model = 66; break;
                                            }
                                        }
                                        break;
                                }
                                break;

                            case ERealm.Midgard:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = 247; break;
                                    case EInventorySlot.LegsArmor: model = 246; break;
                                    case EInventorySlot.FeetArmor: model = 249; break;
                                    case EInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1280; //30% chance of wizard hat
                                        else
                                            model = 825;
                                        break;
                                    case EInventorySlot.HandsArmor: model = 248; break;
                                    case EInventorySlot.TorsoArmor:
                                        if (Util.Chance(60))
                                        {
                                            model = 245;
                                        }
                                        else
                                        {
                                            name = "Cloth Robe";

                                            switch (Util.Random(2))
                                            {
                                                case 0: model = 58; break;
                                                case 1: model = 65; break;
                                                case 2: model = 66; break;
                                            }
                                        }
                                        break;
                                }
                                break;

                            case ERealm.Hibernia:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = 380; break;
                                    case EInventorySlot.LegsArmor: model = 379; break;
                                    case EInventorySlot.FeetArmor: model = 382; break;
                                    case EInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1279; //30% chance of wizard hat
                                        else
                                            model = 826;
                                        break;
                                    case EInventorySlot.HandsArmor: model = 381; break;
                                    case EInventorySlot.TorsoArmor:
                                        if (Util.Chance(60))
                                        {
                                            model = 378;
                                        }
                                        else
                                        {
                                            name = "Cloth Robe";

                                            switch (Util.Random(2))
                                            {
                                                case 0: model = 58; break;
                                                case 1: model = 65; break;
                                                case 2: model = 66; break;
                                            }
                                        }
                                        break;
                                }
                                break;

                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Leather:
                    {
                        name = "Leather " + ArmorSlotToName(slot, type);

                        switch (realm)
                        {
                            case ERealm.Albion:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, ERealm.Albion); break;
                                }
                                break;

                            case ERealm.Midgard:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, ERealm.Midgard); break;
                                }
                                break;

                            case ERealm.Hibernia:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, ERealm.Hibernia); break;
                                    case EInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, ERealm.Hibernia); break;
                                    case EInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, ERealm.Hibernia); break;
                                    case EInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, ERealm.Hibernia); break;
                                    case EInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, ERealm.Hibernia); break;
                                    case EInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, ERealm.Hibernia); break;
                                }
                                break;

                        }

                        if (slot != EInventorySlot.HeadArmor
                            && slot != EInventorySlot.ArmsArmor
                            && slot != EInventorySlot.LegsArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Studded:
                    {
                        name = "Studded " + ArmorSlotToName(slot, type);
                        switch (realm)
                        {
                            case ERealm.Albion:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetStuddedSleevesForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.LegsArmor: model = GetStuddedPantsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.FeetArmor: model = GetStuddedBootsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HeadArmor: model = GetStuddedHelmForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.TorsoArmor: model = GetStuddedTorsoForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HandsArmor: model = GetStuddedHandsForLevel(Level, ERealm.Albion); break;
                                }
                                break;

                            case ERealm.Midgard:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetStuddedSleevesForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.LegsArmor: model = GetStuddedPantsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.FeetArmor: model = GetStuddedBootsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HeadArmor: model = GetStuddedHelmForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.TorsoArmor: model = GetStuddedTorsoForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HandsArmor: model = GetStuddedHandsForLevel(Level, ERealm.Midgard); break;
                                }
                                break;
                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Plate:
                    {
                        name = "Plate " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = GetPlateSleevesForLevel(Level, ERealm.Albion); break;
                            case EInventorySlot.LegsArmor: model = GetPlatePantsForLevel(Level, ERealm.Albion); break;
                            case EInventorySlot.FeetArmor: model = GetPlateBootsForLevel(Level, ERealm.Albion); break;
                            case EInventorySlot.HeadArmor:
                                model = GetPlateHelmForLevel(Level, ERealm.Albion);
                                if (model == 93 || model == 95)
                                    name = "Plate Full Helm";
                                break;
                            case EInventorySlot.TorsoArmor: model = GetPlateTorsoForLevel(Level, ERealm.Albion); break;
                            case EInventorySlot.HandsArmor: model = GetPlateHandsForLevel(Level, ERealm.Albion); break;
                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Chain:
                    {
                        name = "Chain " + ArmorSlotToName(slot, type);
                        switch (realm)
                        {
                            case ERealm.Albion:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetChainSleevesForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.LegsArmor: model = GetChainPantsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.FeetArmor: model = GetChainBootsForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HeadArmor: model = GetChainHelmForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.TorsoArmor: model = GetChainTorsoForLevel(Level, ERealm.Albion); break;
                                    case EInventorySlot.HandsArmor: model = GetChainHandsForLevel(Level, ERealm.Albion); break;
                                }
                                break;

                            case ERealm.Midgard:
                                switch (slot)
                                {
                                    case EInventorySlot.ArmsArmor: model = GetChainSleevesForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.LegsArmor: model = GetChainPantsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.FeetArmor: model = GetChainBootsForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HeadArmor: model = GetChainHelmForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.TorsoArmor: model = GetChainTorsoForLevel(Level, ERealm.Midgard); break;
                                    case EInventorySlot.HandsArmor: model = GetChainHandsForLevel(Level, ERealm.Midgard); break;
                                }
                                break;
                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Reinforced:
                    {
                        name = "Reinforced " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = GetReinforcedSleevesForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.LegsArmor: model = GetReinforcedPantsForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.FeetArmor: model = GetReinforcedBootsForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.HeadArmor: model = GetReinforcedHelmForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.TorsoArmor: model = GetReinforcedTorsoForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.HandsArmor: model = GetReinforcedHandsForLevel(Level, ERealm.Hibernia); break;
                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case EObjectType.Scale:
                    {
                        name = "Scale " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = GetScaleSleevesForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.LegsArmor: model = GetScalePantsForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.FeetArmor: model = GetScaleBootsForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.HeadArmor: model = GetScaleHelmForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.TorsoArmor: model = GetScaleTorsoForLevel(Level, ERealm.Hibernia); break;
                            case EInventorySlot.HandsArmor: model = GetScaleHandsForLevel(Level, ERealm.Hibernia); break;
                        }

                        if (slot != EInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }

                //weapons
                case EObjectType.Axe:
                    {
                        if (this.Hand == 1)
                        {
                            model = Get2HAxeModelForLevel(Level, realm);
                            name = GetNameFromId(model);
                        }
                        else // 1 handed axe; speed 28-45; 578 (hand), 316 (Bearded), 319 (War), 315 (Spiked), 573 (Double)
                        {
                            model = GetAxeModelForLevel(Level, realm);
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.Blades:
                    {
                        model = GetBladeModelForLevel(Level, ERealm.Hibernia);
                        // Blades; speed 22 - 45; Short Sword (445), Falcata (444), Broadsword (447), Longsword (446), Bastard Sword (473)
                        if (this.SPD_ABS <= 27)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 32)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.Blunt:
                    {
                        // Blunt; speed 22 - 45; Club (449), Mace (450), Hammer (461), Spiked Mace (451), Pick Hammer (641)
                        model = GetBluntModelForLevel(Level, ERealm.Hibernia);
                        if (this.SPD_ABS < 31)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 35)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 40)
                        {
                            name = GetNameFromId(model);
                        }
                        else if (this.SPD_ABS < 43)
                        {
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }

                        if (Util.Chance(1))
                            model = 3458; //1% chance of being a rolling pin
                        break;
                    }
                case EObjectType.CelticSpear:
                    {
                        model = GetSpearModelForLevel(Level, ERealm.Hibernia);
                        // Short Spear (470), Spear (469), Long Spear (476), War Spear (477)
                        if (this.SPD_ABS < 35)
                        {
                            name = "Short Spear";
                        }
                        else if (this.SPD_ABS < 45)
                        {
                            name = "Spear";
                        }
                        else if (this.SPD_ABS < 50)
                        {
                            name = "Long Spear";
                        }
                        else
                        {
                            name = "War Spear";
                        }
                        break;
                    }
                case EObjectType.CompositeBow:
                    {
                        if (this.SPD_ABS > 40)
                            name = "Great Composite Bow";
                        else
                            name = "Composite Bow";

                        model = GetBowModelForLevel(Level, ERealm.Midgard);
                        break;
                    }
                case EObjectType.Crossbow:
                    {
                        name = "Crossbow";
                        model = GetCrossbowModelForLevel(Level, ERealm.Albion);
                        break;
                    }
                case EObjectType.CrushingWeapon:
                    {
                        model = GetBluntModelForLevel(Level, ERealm.Albion);
                        // Hammer (12), Mace (13), Flanged Mace (14), War Hammer (15)
                        if (this.SPD_ABS < 33)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 35)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 40)
                        {
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.Fired:
                    {
                        if (realm == ERealm.Albion)
                        {
                            name = "Short Bow";
                            model = 569;
                        }
                        else // hibernia
                        {
                            name = "Short Bow";
                            model = 922;
                        }
                        break;
                    }
                case EObjectType.Flexible:
                    {
                        model = GetFlexModelForLevel(Level, ERealm.Albion, damage);
                        switch (damage)
                        {
                            case EDamageType.Crush:
                                {
                                    if (this.SPD_ABS < 33)
                                    {
                                        name = "Morning Star";
                                    }
                                    else if (this.SPD_ABS < 40)
                                    {
                                        name = "Flail";
                                    }
                                    else
                                    {
                                        name = "Weighted Flail";
                                    }
                                    break;
                                }
                            case EDamageType.Slash:
                                {
                                    if (this.SPD_ABS < 33)
                                    {
                                        name = "Whip";
                                    }
                                    else if (this.SPD_ABS < 40)
                                    {
                                        name = "Chain";
                                    }
                                    else
                                    {
                                        name = "War Chain";
                                    }
                                    break;
                                }
                        }
                        break;

                    }
                case EObjectType.Hammer:
                    {
                        if (this.Hand == 1)
                        {
                            model = Get2HHammerForLevel(Level, ERealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            model = GetBluntModelForLevel(Level, ERealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.HandToHand:
                    {
                        model = GetH2HModelForLevel(Level, ERealm.Midgard, damage);
                        switch (damage)
                        {
                            case EDamageType.Slash:
                                {
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case EDamageType.Thrust:
                                {
                                    name = GetNameFromId(model);
                                    break;
                                }
                        }
                        // all hand to hand weapons usable in left hand
                        this.Hand = 2; // allow left hand
                        this.Item_Type = Slot.LEFTHAND;
                        break;
                    }
                case EObjectType.Instrument:
                    {
                        switch (this.DPS_AF)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                {
                                    model = GetInstrumentModelForLevel(Level, realm);
                                    name = GetNameFromId(model);
                                    break;
                                }
                                /*
                                    {
                                        name = "Drum";
                                        model = 228;
                                        break;
                                    }

                                    {
                                        name = "Lute";
                                        model = 227;
                                        break;
                                    }

                                    {
                                        name = "Flute";
                                        model = 325;
                                        break;
                                    }*/
                        }
                        break;
                    }
                case EObjectType.LargeWeapons:
                    {
                        switch (damage)
                        {
                            case EDamageType.Slash:
                                {
                                    model = Get2HSwordForLevel(Level, ERealm.Hibernia);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case EDamageType.Crush:
                                {
                                    model = Get2HHammerForLevel(Level, ERealm.Hibernia);
                                    if (model == 474 || model == 912)
                                    {
                                        name = "Big Shillelagh";
                                    }
                                    else
                                    {
                                        name = GetNameFromId(model);
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case EObjectType.LeftAxe:
                    {
                        model = GetAxeModelForLevel(Level, ERealm.Midgard);
                        if (this.SPD_ABS < 25)
                        {
                            name = "Hand Axe";
                        }
                        else if (this.SPD_ABS < 30)
                        {
                            name = "Bearded Axe";
                        }
                        else
                        {
                            name = "War Axe";
                        }
                        break;
                    }
                case EObjectType.Longbow:
                    {
                        model = GetBowModelForLevel(Level, ERealm.Albion);
                        if (this.SPD_ABS < 44)
                        {
                            name = "Hunting Bow";
                        }
                        else if (this.SPD_ABS < 55)
                        {
                            name = "Longbow";
                        }
                        else
                        {
                            name = "Heavy Longbow";
                        }
                        break;
                    }
                case EObjectType.Magical:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.Cloak:
                                {
                                    if (Util.Chance(50))
                                        name = "Mantle";
                                    else
                                        name = "Cloak";

                                    if (Util.Chance(50))
                                        model = 57;
                                    else if (Util.Chance(50))
                                        model = 559;
                                    else
                                        model = 560;

                                    break;
                                }
                            case EInventorySlot.Waist:
                                {
                                    if (Util.Chance(50))
                                        name = "Belt";
                                    else
                                        name = "Girdle";

                                    model = 597;
                                    break;
                                }
                            case EInventorySlot.Neck:
                                {
                                    if (Util.Chance(50))
                                        name = "Choker";
                                    else
                                        name = "Pendant";

                                    model = 101;
                                    break;
                                }
                            case EInventorySlot.Jewellery:
                                {
                                    if (Util.Chance(50))
                                        name = "Gem";
                                    else
                                        name = "Jewel";

                                    model = Util.Random(110, 119);
                                    break;
                                }
                            case EInventorySlot.LeftBracer:
                            case EInventorySlot.RightBracer:
                                {
                                    if (Util.Chance(50))
                                    {
                                        name = "Bracelet";
                                        model = 619;
                                    }
                                    else
                                    {
                                        name = "Bracer";
                                        model = 598;
                                    }

                                    break;
                                }
                            case EInventorySlot.LeftRing:
                            case EInventorySlot.RightRing:
                                {
                                    if (Util.Chance(50))
                                        name = "Ring";
                                    else
                                        name = "Wrap";

                                    model = 103;
                                    break;
                                }
                        }
                        break;
                    }
                case EObjectType.Piercing:
                    {
                        model = GetThrustModelForLevel(Level, ERealm.Hibernia);
                        if (this.SPD_ABS < 24)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 29)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 30)
                        {
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.PolearmWeapon:
                    {
                        model = GetPolearmModelForLevel(Level, ERealm.Albion, damage);
                        switch (damage)
                        {
                            case EDamageType.Slash:
                                {
                                    name = "Lochaber Axe";
                                    break;
                                }
                            case EDamageType.Thrust:
                                {
                                    name = "Pike";
                                    break;
                                }
                            case EDamageType.Crush:
                                {
                                    name = "Lucerne Hammer";
                                    break;
                                }
                        }
                        break;
                    }
                case EObjectType.RecurvedBow:
                    {
                        model = GetBowModelForLevel(Level, ERealm.Hibernia);
                        if (this.SPD_ABS > 49)
                        {
                            name = "Great Recurve Bow";
                        }
                        else
                        {
                            name = "Recurve Bow";
                        }
                        break;
                    }
                case EObjectType.Scythe:
                    {
                        model = GetScytheModelForLevel(Level, ERealm.Hibernia);
                        if (this.SPD_ABS < 47)
                        {
                            name = "Scythe";
                        }
                        else if (this.SPD_ABS < 51)
                        {
                            name = "Martial Scythe";
                        }
                        else
                        {
                            name = "War Scythe";
                        }
                        break;
                    }
                case EObjectType.Shield:
                    {
                        model = GetShieldModelForLevel(Level, realm, (int)damage);
                        switch ((int)damage)
                        {
                            case 1:
                                {
                                    name = "Small Shield";
                                    break;
                                }
                            case 2:
                                {
                                    name = "Medium Shield";
                                    break;
                                }
                            case 3:
                                {
                                    name = "Large Shield";
                                    break;
                                }
                        }
                        break;
                    }
                case EObjectType.SlashingWeapon:
                    {
                        model = GetBladeModelForLevel(Level, ERealm.Albion);
                        if (this.SPD_ABS < 26)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 30)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 32)
                        {
                            name = GetNameFromId(model);
                        }
                        else if (this.SPD_ABS < 35)
                        {
                            name = GetNameFromId(model);
                        }
                        else if (this.SPD_ABS < 40)
                        {
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.Spear:
                    {
                        model = GetSpearModelForLevel(Level, ERealm.Midgard);
                        name = GetNameFromId(model);
                        break;
                    }
                case EObjectType.MaulerStaff:
                    {
                        name = "Mauler Staff";
                        model = 19;
                        break;
                    }
                case EObjectType.Staff:
                    {
                        model = GetStaffModelForLevel(Level, realm);
                        switch (realm)
                        {
                            case ERealm.Albion:

                                if (Util.Chance(20))
                                {
                                    this.Description = "friar";

                                    if (this.SPD_ABS < 40)
                                    {
                                        name = "Quarterstaff";
                                    }
                                    else if (this.SPD_ABS < 50)
                                    {
                                        name = "Shod Quarterstaff";
                                    }
                                    else
                                    {
                                        name = "Heavy Shod Quarterstaff";
                                    }
                                }
                                else
                                {
                                    name = GetNameFromId(model);
                                }
                                break;

                            case ERealm.Midgard:
                                name = GetNameFromId(model);
                                break;

                            case ERealm.Hibernia:
                                name = GetNameFromId(model);
                                break;
                        }
                        break;
                    }
                case EObjectType.Sword:
                    {
                        if (this.Hand == 1)
                        {
                            model = Get2HSwordForLevel(Level, ERealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            model = GetBladeModelForLevel(Level, ERealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.ThrustWeapon:
                    {
                        model = GetThrustModelForLevel(Level, ERealm.Albion);
                        if (this.SPD_ABS < 24)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 28)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 30)
                        {
                            name = GetNameFromId(model);
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 36)
                        {
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case EObjectType.TwoHandedWeapon:
                    {
                        switch (damage)
                        {
                            case EDamageType.Slash:
                                {
                                    model = Get2HSwordForLevel(Level, ERealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case EDamageType.Crush:
                                {
                                    model = Get2HHammerForLevel(Level, ERealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case EDamageType.Thrust:
                                {
                                    model = Get2HThrustForLevel(Level, ERealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                        }
                        break;
                    }
                case EObjectType.FistWraps: // Maulers
                    {
                        string str = "Fist";

                        if (Util.Chance(50))
                            str = "Hand";

                        if (this.SPD_ABS < 31)
                        {
                            name = str + " Wrap";
                            model = 3476;
                            this.Effect = 102; // smoke
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else if (this.SPD_ABS < 35)
                        {
                            name = "Studded " + str + " Wrap";
                            model = 3477;
                            this.Effect = 48; // fire
                            this.Hand = 2; // allow left hand
                            this.Item_Type = Slot.LEFTHAND;
                        }
                        else
                        {
                            name = "Spiked Fist Wrap";
                            model = 3478;
                            this.Effect = 49; // sparkle fire
                        }

                        break;
                    }
            }

            //each realm has a chance for special helmets during generation
            if (slot == EInventorySlot.HeadArmor)
            {
                switch (realm)
                {
                    case ERealm.Albion:
                        if (Util.Chance(1))
                            model = 1284; //1% chance of tarboosh
                        else if (Util.Chance(1))
                            model = 1281; //1% chance of robin hood hat
                        else if (Util.Chance(1))
                            model = 1287; //1% chance of jester hat
                        break;
                    case ERealm.Hibernia:
                        if (Util.Chance(1))
                            model = 1282; //1% chance of robin hood hat
                        else if (Util.Chance(1))
                            model = 1285; //1% chance of leaf hat
                        else if (Util.Chance(1))
                            model = 1288; //1% chance of stag helm
                        break;
                    case ERealm.Midgard:
                        if (Util.Chance(1))
                            model = 1289; //1% chance of wolf hat
                        else if (Util.Chance(1))
                            model = 1283; //1% chance of fur cap
                        else if (Util.Chance(1))
                            model = 1286; //1% chance of wing hat
                        break;
                }
            }


            this.Name = name;
            this.Model = model;

            //
            if (canAddExtension)
            {
                byte ext = 0;
                if (slot == EInventorySlot.HandsArmor ||
                     slot == EInventorySlot.FeetArmor)
                    ext = GetNonTorsoExtensionForLevel(Level);
                else if (slot == EInventorySlot.TorsoArmor)
                    ext = GetTorsoExtensionForLevel(Level);

                this.Extension = ext;
            }

        }

        #region Leather Model Generation
        private static int GetLeatherTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(31);
                    if (Level > 20)
                        validModels.Add(36);
                    if (Level > 30)
                        validModels.Add(74);
                    if (Level > 40)
                        validModels.Add(134);
                    if (Level > 50)
                        validModels.Add(2797);
                    break;
                case ERealm.Midgard:
                    validModels.Add(240);
                    if (Level > 20)
                        validModels.Add(260);
                    if (Level > 30)
                        validModels.Add(280);
                    if (Level > 40)
                        validModels.Add(300);
                    if (Level > 50)
                        validModels.Add(2859);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(373);
                    if (Level >= 10)
                        validModels.Add(393);
                    if (Level >= 20)
                        validModels.Add(413);
                    if (Level >= 30)
                        validModels.Add(433);
                    if (Level >= 40)
                        validModels.Add(2988);
                    if (Level > 50)
                        validModels.Add(2828);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetLeatherPantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(32);
                    if (Level > 20)
                        validModels.Add(37);
                    if (Level > 30)
                        validModels.Add(75);
                    if (Level > 40)
                        validModels.Add(135);
                    if (Level > 50)
                        validModels.Add(2798);
                    break;
                case ERealm.Midgard:
                    validModels.Add(241);
                    if (Level > 20)
                        validModels.Add(261);
                    if (Level > 30)
                        validModels.Add(281);
                    if (Level > 40)
                        validModels.Add(301);
                    if (Level > 50)
                        validModels.Add(2860);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(374);
                    if (Level >= 10)
                        validModels.Add(394);
                    if (Level >= 20)
                        validModels.Add(414);
                    if (Level >= 30)
                        validModels.Add(434);
                    if (Level >= 40)
                        validModels.Add(1257);
                    if (Level > 50)
                        validModels.Add(2829);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetLeatherSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(33);
                    if (Level > 20)
                        validModels.Add(38);
                    if (Level > 30)
                        validModels.Add(76);
                    if (Level > 40)
                        validModels.Add(136);
                    if (Level > 50)
                        validModels.Add(2799);
                    break;
                case ERealm.Midgard:
                    validModels.Add(242);
                    if (Level > 20)
                        validModels.Add(262);
                    if (Level > 30)
                        validModels.Add(282);
                    if (Level > 40)
                        validModels.Add(302);
                    if (Level > 50)
                        validModels.Add(2861);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(375);
                    if (Level >= 10)
                        validModels.Add(395);
                    if (Level >= 20)
                        validModels.Add(415);
                    if (Level >= 30)
                        validModels.Add(435);
                    if (Level >= 40)
                        validModels.Add(355);
                    if (Level > 50)
                        validModels.Add(2830);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetLeatherHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(34);
                    if (Level > 20)
                        validModels.Add(39);
                    if (Level > 30)
                        validModels.Add(77);
                    if (Level > 40)
                        validModels.Add(137);
                    if (Level > 50)
                        validModels.Add(2802);
                    break;
                case ERealm.Midgard:
                    validModels.Add(243);
                    if (Level > 20)
                        validModels.Add(263);
                    if (Level > 30)
                        validModels.Add(283);
                    if (Level > 40)
                        validModels.Add(303);
                    if (Level > 50)
                        validModels.Add(2864);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(376);
                    if (Level >= 10)
                        validModels.Add(396);
                    if (Level >= 20)
                        validModels.Add(416);
                    if (Level >= 30)
                        validModels.Add(436);
                    if (Level >= 40)
                        validModels.Add(1259);
                    if (Level > 50)
                        validModels.Add(2833);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetLeatherBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(40);
                    if (Level > 20)
                        validModels.Add(133);
                    if (Level > 30)
                        validModels.Add(78);
                    if (Level > 40)
                        validModels.Add(138);
                    if (Level > 50)
                        validModels.Add(2801);
                    break;
                case ERealm.Midgard:
                    validModels.Add(244);
                    if (Level > 20)
                        validModels.Add(264);
                    if (Level > 30)
                        validModels.Add(284);
                    if (Level > 40)
                        validModels.Add(304);
                    if (Level > 50)
                        validModels.Add(2863);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(377);
                    if (Level >= 10)
                        validModels.Add(397);
                    if (Level >= 20)
                        validModels.Add(417);
                    if (Level >= 30)
                        validModels.Add(437);
                    if (Level >= 40)
                        validModels.Add(1260);
                    if (Level > 50)
                        validModels.Add(2832);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetLeatherHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(62);
                    if (Level > 35)
                        validModels.Add(1231);
                    if (Level > 45)
                        validModels.Add(2800);
                    if (Level > 50)
                        validModels.Add(1232);
                    break;
                case ERealm.Midgard:
                    validModels.Add(335);
                    if (Level > 35)
                        validModels.Add(336);
                    if (Level > 45)
                        validModels.Add(337);
                    if (Level > 50)
                        validModels.Add(1214);
                    break;
                case ERealm.Hibernia:
                    validModels.Add(438);
                    if (Level > 35)
                        validModels.Add(439);
                    if (Level > 45)
                        validModels.Add(440);
                    if (Level > 50)
                        validModels.Add(1198);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Studded Model Generation
        private static int GetStuddedTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(51);
                    if (Level > 20)
                        validModels.Add(81);
                    if (Level > 30)
                        validModels.Add(156);
                    if (Level > 40)
                        validModels.Add(216);
                    if (Level > 50)
                        validModels.Add(2803);
                    break;
                case ERealm.Midgard:
                    validModels.Add(230);
                    if (Level > 20)
                        validModels.Add(250);
                    if (Level > 30)
                        validModels.Add(270);
                    if (Level > 40)
                        validModels.Add(3012);
                    if (Level > 50)
                        validModels.Add(2865);
                    break;
                default:
                    validModels.Add(0);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStuddedPantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(52);
                    if (Level > 20)
                        validModels.Add(82);
                    if (Level > 30)
                        validModels.Add(217);
                    if (Level > 40)
                        validModels.Add(157);
                    if (Level > 50)
                        validModels.Add(2804);
                    break;
                case ERealm.Midgard:
                    validModels.Add(231);
                    if (Level > 20)
                        validModels.Add(251);
                    if (Level > 30)
                        validModels.Add(271);
                    if (Level > 40)
                        validModels.Add(291);
                    if (Level > 50)
                        validModels.Add(2866);
                    break;
                default:
                    validModels.Add(52);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStuddedSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(53);
                    if (Level > 20)
                        validModels.Add(83);
                    if (Level > 30)
                        validModels.Add(218);
                    if (Level > 40)
                        validModels.Add(158);
                    if (Level > 50)
                        validModels.Add(2805);
                    break;
                case ERealm.Midgard:
                    validModels.Add(232);
                    if (Level > 20)
                        validModels.Add(252);
                    if (Level > 30)
                        validModels.Add(272);
                    if (Level > 40)
                        validModels.Add(292);
                    if (Level > 50)
                        validModels.Add(2867);
                    break;
                default:
                    validModels.Add(53);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStuddedHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(80);
                    if (Level > 20)
                        validModels.Add(85);
                    if (Level > 30)
                        validModels.Add(219);
                    if (Level > 40)
                        validModels.Add(159);
                    if (Level > 50)
                        validModels.Add(2808);
                    break;
                case ERealm.Midgard:
                    validModels.Add(233);
                    if (Level > 20)
                        validModels.Add(253);
                    if (Level > 30)
                        validModels.Add(273);
                    if (Level > 40)
                        validModels.Add(293);
                    if (Level > 50)
                        validModels.Add(2870);
                    break;
                default:
                    validModels.Add(80);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStuddedBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(54);
                    if (Level > 20)
                        validModels.Add(84);
                    if (Level > 30)
                        validModels.Add(220);
                    if (Level > 40)
                        validModels.Add(160);
                    if (Level > 50)
                        validModels.Add(2807);
                    break;
                case ERealm.Midgard:
                    validModels.Add(234);
                    if (Level > 20)
                        validModels.Add(254);
                    if (Level > 30)
                        validModels.Add(274);
                    if (Level > 40)
                        validModels.Add(294);
                    if (Level > 50)
                        validModels.Add(2869);
                    break;
                default:
                    validModels.Add(54);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStuddedHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(824);
                    if (Level > 35)
                        validModels.Add(1233);
                    if (Level > 45)
                        validModels.Add(1234);
                    if (Level > 50)
                        validModels.Add(1235);
                    break;
                case ERealm.Midgard:
                    validModels.Add(829);
                    if (Level > 35)
                        validModels.Add(830);
                    if (Level > 45)
                        validModels.Add(831);
                    if (Level > 50)
                        validModels.Add(1215);
                    break;
                default:
                    validModels.Add(824);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Chain Model Generation
        private static int GetChainTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(41);
                    if (Level > 10)
                        validModels.Add(181);
                    if (Level > 20)
                        validModels.Add(186);
                    if (Level > 30)
                        validModels.Add(191);
                    if (Level > 40)
                        validModels.Add(1251);
                    if (Level > 50)
                        validModels.Add(1246);
                    break;
                case ERealm.Midgard:
                    validModels.Add(235);
                    if (Level > 10)
                        validModels.Add(255);
                    if (Level > 20)
                        validModels.Add(275);
                    if (Level > 30)
                        validModels.Add(295);
                    if (Level > 40)
                        validModels.Add(999);
                    if (Level > 50)
                        validModels.Add(1262);
                    break;
                default:
                    validModels.Add(41);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetChainPantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(42);
                    if (Level > 10)
                        validModels.Add(1252);
                    if (Level > 20)
                        validModels.Add(182);
                    if (Level > 30)
                        validModels.Add(187);
                    if (Level > 40)
                        validModels.Add(192);
                    if (Level > 50)
                        validModels.Add(1247);
                    break;
                case ERealm.Midgard:
                    validModels.Add(236);
                    if (Level > 20)
                        validModels.Add(256);
                    if (Level > 30)
                        validModels.Add(276);
                    if (Level > 40)
                        validModels.Add(998);
                    if (Level > 50)
                        validModels.Add(1261);
                    break;
                default:
                    validModels.Add(236);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetChainSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(43);
                    if (Level > 20)
                        validModels.Add(183);
                    if (Level > 30)
                        validModels.Add(188);
                    if (Level > 40)
                        validModels.Add(193);
                    if (Level > 50)
                        validModels.Add(1265);
                    break;
                case ERealm.Midgard:
                    validModels.Add(237);
                    if (Level > 20)
                        validModels.Add(257);
                    if (Level > 30)
                        validModels.Add(277);
                    if (Level > 40)
                        validModels.Add(1002);
                    if (Level > 50)
                        validModels.Add(1265);
                    break;
                default:
                    validModels.Add(237);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetChainHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(44);
                    if (Level > 20)
                        validModels.Add(184);
                    if (Level > 30)
                        validModels.Add(189);
                    if (Level > 40)
                        validModels.Add(194);
                    if (Level > 50)
                        validModels.Add(1249);
                    break;
                case ERealm.Midgard:
                    validModels.Add(238);
                    if (Level > 20)
                        validModels.Add(258);
                    if (Level > 30)
                        validModels.Add(278);
                    if (Level > 40)
                        validModels.Add(1000);
                    if (Level > 50)
                        validModels.Add(1263);
                    break;
                default:
                    validModels.Add(44);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetChainBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(45);
                    if (Level > 20)
                        validModels.Add(185);
                    if (Level > 30)
                        validModels.Add(190);
                    if (Level > 40)
                        validModels.Add(1250);
                    if (Level > 50)
                        validModels.Add(1255);
                    break;
                case ERealm.Midgard:
                    validModels.Add(239);
                    if (Level > 20)
                        validModels.Add(259);
                    if (Level > 30)
                        validModels.Add(279);
                    if (Level > 40)
                        validModels.Add(1001);
                    if (Level > 50)
                        validModels.Add(1264);
                    break;
                default:
                    validModels.Add(45);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetChainHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(1236);
                    if (Level > 35)
                        validModels.Add(63);
                    if (Level > 45)
                        validModels.Add(2812);
                    break;
                case ERealm.Midgard:
                    validModels.Add(832);
                    if (Level > 35)
                        validModels.Add(833);
                    if (Level > 45)
                        validModels.Add(834);
                    if (Level > 50)
                        validModels.Add(1216);
                    break;
                default:
                    validModels.Add(1236);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Plate Model Generation
        private static int GetPlateTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(46);
                    if (Level > 20)
                        validModels.Add(86);
                    if (Level > 30)
                        validModels.Add(201);
                    if (Level > 40)
                        validModels.Add(206);
                    if (Level > 50)
                    {
                        validModels.Add(1272);
                        validModels.Add(2815);
                    }
                    break;
                default:
                    validModels.Add(0);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPlatePantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(47);
                    if (Level > 20)
                        validModels.Add(87);
                    if (Level > 30)
                        validModels.Add(202);
                    if (Level > 40)
                        validModels.Add(207);
                    if (Level > 50)
                    {
                        validModels.Add(1273);
                        validModels.Add(2816);
                    }
                    break;
                default:
                    validModels.Add(47);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPlateSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(48);
                    if (Level > 20)
                        validModels.Add(88);
                    if (Level > 30)
                        validModels.Add(203);
                    if (Level > 40)
                        validModels.Add(208);
                    if (Level > 50)
                    {
                        validModels.Add(1274);
                        validModels.Add(2817);
                    }
                    break;
                default:
                    validModels.Add(48);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPlateHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(49);
                    if (Level > 20)
                        validModels.Add(89);
                    if (Level > 30)
                        validModels.Add(204);
                    if (Level > 40)
                        validModels.Add(209);
                    if (Level > 50)
                        validModels.Add(2820);
                    break;
                default:
                    validModels.Add(49);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPlateBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(50);
                    if (Level > 20)
                        validModels.Add(90);
                    if (Level > 30)
                        validModels.Add(205);
                    if (Level > 40)
                        validModels.Add(210);
                    if (Level > 50)
                        validModels.Add(2819);
                    break;
                default:
                    validModels.Add(50);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPlateHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(64);
                    if (Level > 10)
                        validModels.Add(93);
                    if (Level > 35)
                        validModels.Add(1238);
                    if (Level > 45)
                        validModels.Add(1239);
                    if (Level > 50)
                        validModels.Add(95);
                    break;
                default:
                    validModels.Add(64);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Reinforced Model Generation
        private static int GetReinforcedTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(363);
                    if (Level > 10)
                        validModels.Add(383);
                    if (Level > 20)
                        validModels.Add(403);
                    if (Level > 30)
                        validModels.Add(423);
                    if (Level > 40)
                        validModels.Add(1256);
                    if (Level > 50)
                        validModels.Add(3012);
                    break;
                default:
                    validModels.Add(363);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetReinforcedPantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(364);
                    if (Level > 10)
                        validModels.Add(384);
                    if (Level > 20)
                        validModels.Add(404);
                    if (Level > 30)
                        validModels.Add(424);
                    if (Level > 40)
                        validModels.Add(1257);
                    if (Level > 50)
                        validModels.Add(3013);
                    break;
                default:
                    validModels.Add(364);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetReinforcedSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(365);
                    if (Level > 10)
                        validModels.Add(385);
                    if (Level > 20)
                        validModels.Add(405);
                    if (Level > 30)
                        validModels.Add(425);
                    if (Level > 40)
                        validModels.Add(1258);
                    if (Level > 50)
                        validModels.Add(3014);
                    break;
                default:
                    validModels.Add(365);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetReinforcedHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(366);
                    if (Level > 10)
                        validModels.Add(386);
                    if (Level > 20)
                        validModels.Add(406);
                    if (Level > 30)
                        validModels.Add(426);
                    if (Level > 40)
                        validModels.Add(1259);
                    if (Level > 50)
                        validModels.Add(3016);
                    break;
                default:
                    validModels.Add(366);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetReinforcedBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(367);
                    if (Level > 10)
                        validModels.Add(387);
                    if (Level > 20)
                        validModels.Add(407);
                    if (Level > 30)
                        validModels.Add(427);
                    if (Level > 40)
                        validModels.Add(1260);
                    if (Level > 50)
                        validModels.Add(3015);
                    break;
                default:
                    validModels.Add(50);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetReinforcedHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(835);
                    if (Level > 10)
                        validModels.Add(836);
                    if (Level > 35)
                        validModels.Add(837);
                    if (Level > 45)
                        validModels.Add(1199);
                    if (Level > 50)
                        validModels.Add(2837);
                    break;
                default:
                    validModels.Add(64);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Scale Model Generation
        private static int GetScaleTorsoForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(368);
                    if (Level > 10)
                        validModels.Add(388);
                    if (Level > 20)
                        validModels.Add(408);
                    if (Level > 30)
                        validModels.Add(428);
                    if (Level > 40)
                        validModels.Add(988);
                    if (Level > 50)
                        validModels.Add(3000);
                    break;
                default:
                    validModels.Add(368);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScalePantsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(369);
                    if (Level > 10)
                        validModels.Add(389);
                    if (Level > 20)
                        validModels.Add(409);
                    if (Level > 30)
                        validModels.Add(429);
                    if (Level > 40)
                        validModels.Add(989);
                    if (Level > 50)
                        validModels.Add(3001);
                    break;
                default:
                    validModels.Add(369);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScaleSleevesForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(370);
                    if (Level > 10)
                        validModels.Add(390);
                    if (Level > 20)
                        validModels.Add(410);
                    if (Level > 30)
                        validModels.Add(430);
                    if (Level > 40)
                        validModels.Add(990);
                    if (Level > 50)
                        validModels.Add(3002);
                    break;
                default:
                    validModels.Add(365);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScaleHandsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(371);
                    if (Level > 10)
                        validModels.Add(391);
                    if (Level > 20)
                        validModels.Add(411);
                    if (Level > 30)
                        validModels.Add(431);
                    if (Level > 40)
                        validModels.Add(991);
                    if (Level > 50)
                        validModels.Add(3005);
                    break;
                default:
                    validModels.Add(371);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScaleBootsForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(372);
                    if (Level > 10)
                        validModels.Add(392);
                    if (Level > 20)
                        validModels.Add(412);
                    if (Level > 30)
                        validModels.Add(432);
                    if (Level > 40)
                        validModels.Add(992);
                    if (Level > 50)
                        validModels.Add(3004);
                    break;
                default:
                    validModels.Add(372);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScaleHelmForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(838);
                    if (Level > 10)
                        validModels.Add(839);
                    if (Level > 35)
                        validModels.Add(840);
                    if (Level > 45)
                        validModels.Add(1200);
                    if (Level > 50)
                        validModels.Add(2843);
                    break;
                default:
                    validModels.Add(838);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }
        #endregion

        #region Weapon Model Generation

        private static int Get2HAxeModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(9);
                    if (Level > 10)
                        validModels.Add(72);
                    if (Level > 30)
                        validModels.Add(73);
                    break;
                case ERealm.Midgard:
                    validModels.Add(317);
                    if (Level > 10)
                    {
                        validModels.Add(318);
                    }
                    if(Level > 20)
                        validModels.Add(1030);
                    if (Level > 30)
                    {
                        validModels.Add(955);
                        validModels.Add(1033);
                    }
                    if (Level > 40)
                        validModels.Add(1027);

                    if (Level > 50)
                        validModels.Add(660);
                    
                    break;
                default:
                    validModels.Add(2);
                    break;
            }

            
            if(Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3662);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3705);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }
        private static int GetAxeModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(2);
                    if (Level > 10)
                        validModels.Add(878);
                    if (Level > 30)
                        validModels.Add(880);
                    if (Level > 40)
                        validModels.Add(3681);
                    if (Level > 50)
                        validModels.Add(3724);
                    break;
                case ERealm.Midgard:
                    validModels.Add(315);
                    validModels.Add(316);
                    if (Level > 10)
                    {
                        validModels.Add(319);
                        validModels.Add(573);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(951);
                        validModels.Add(953);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(1010);
                        validModels.Add(1011);                      
                    }

                    if (Level > 50)
                    {
                        validModels.Add(1014);
                        validModels.Add(1018);
                        validModels.Add(654);
                    }

                    if (Util.Chance(1) && Level > 40)
                    {
                        validModels.Clear();
                        validModels.Add(3681);
                        validModels.Add(3680);
                    }
                    if (Util.Chance(1) && Level > 50)
                    {
                        validModels.Clear();
                        validModels.Add(3723);
                        validModels.Add(3724);
                    }

                    break;
                default:
                    validModels.Add(2);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3662);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3705);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int Get2HSwordForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(459);
                    if (Level > 10)
                        validModels.Add(448);
                    if (Level > 20)
                        validModels.Add(639);
                    if (Level > 30)
                        validModels.Add(907);
                    if (Level > 40)
                    {
                        validModels.Add(910);
                        validModels.Add(3658);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(911);
                        validModels.Add(3701);
                    }
                    break;
                case ERealm.Albion:
                    validModels.Add(6);
                    if (Level > 10)
                        validModels.Add(7);
                    if (Level > 20)
                        validModels.Add(9);
                    if (Level > 30)
                        validModels.Add(72);
                    if (Level > 40)
                    {
                        validModels.Add(73);
                        validModels.Add(645);
                        validModels.Add(841);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(843);
                        validModels.Add(845);
                        validModels.Add(847);

                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(314);
                    if (Level > 10)
                        validModels.Add(572);
                    if (Level > 20)
                        validModels.Add(658);
                    if (Level > 30)
                        validModels.Add(1035);
                    if (Level > 40)
                    {
                        validModels.Add(957);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(1032);
                    }
                    break;
                default:
                    validModels.Add(6);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3658);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3701);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetBladeModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(444);
                    validModels.Add(445);
                    if (Level > 10)
                    {
                        validModels.Add(446);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(447);

                    }
                    if (Level > 40)
                    {
                        validModels.Add(460);
                    }

                    if (Level > 50)
                    {
                        validModels.Add(473);                        
                    }
                    break;
                case ERealm.Albion:
                    validModels.Add(1);
                    validModels.Add(3);
                    if (Level > 10)
                    {
                        validModels.Add(4);
                        validModels.Add(5);
                    }
                    if (Level > 20)
                        validModels.Add(8);
                    if (Level > 30)
                    {
                        validModels.Add(10);
                        validModels.Add(652);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(877);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(879);
                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(311);
                    if (Level > 10)
                    {
                        validModels.Add(310);
                        validModels.Add(312);
                    }
                    if (Level > 20)
                        validModels.Add(313);
                    if (Level > 30)
                        validModels.Add(949);
                    if (Level > 40)
                    {
                        validModels.Add(948);
                        validModels.Add(952);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(655);
                        validModels.Add(1017);
                        validModels.Add(1015);
                    }
                    break;
                default:
                    validModels.Add(445);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3675);
                validModels.Add(3674);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3717);
                validModels.Add(3718);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int Get2HHammerForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(474);
                    validModels.Add(463);
                    if (Level > 10)
                    {
                        validModels.Add(462);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(640);
                        validModels.Add(912);

                    }
                    if (Level > 40)
                    {
                        validModels.Add(904);
                        validModels.Add(906);
                        validModels.Add(908);
                        validModels.Add(909);
                        validModels.Add(3661);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(905);
                        validModels.Add(917);
                        validModels.Add(905);
                        validModels.Add(3704);
                    }
                    break;
                case ERealm.Albion:
                    validModels.Add(16);

                    if (Level > 20)
                        validModels.Add(17);
                    if (Level > 30)
                        validModels.Add(644);
                    if (Level > 40)
                        validModels.Add(842);
                    if (Level > 50)
                        validModels.Add(844);
                    break;
                case ERealm.Midgard:
                    validModels.Add(574);
                    if (Level > 10)
                        validModels.Add(575);

                    if (Level > 20)
                    {
                        validModels.Add(576);
                        validModels.Add(659);
                    }

                    if (Level > 30)
                        validModels.Add(956);

                    if (Level > 40)
                    {
                        validModels.Add(1031);
                        validModels.Add(1034); 
                    }
                    if (Level > 50)
                        validModels.Add(1028);
                    break;
                default:
                    validModels.Add(449);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3661);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3704);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetBluntModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(449);
                    validModels.Add(450);
                    if (Level > 10)
                        validModels.Add(451);
                    if (Level > 20)
                        validModels.Add(452);
                    if (Level > 30)
                        validModels.Add(461);
                    if (Level > 40)
                    {
                        validModels.Add(913);
                        validModels.Add(914);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(916);
                        validModels.Add(915);
                    }
                    break;
                case ERealm.Albion:
                    validModels.Add(11);
                    validModels.Add(12);
                    if (Level > 10)
                    {
                        validModels.Add(13);
                        validModels.Add(14);
                    }
                    if (Level > 20)
                        validModels.Add(15);
                    if (Level > 30)
                    {
                        validModels.Add(18);
                        validModels.Add(20);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(853);
                        validModels.Add(854);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(855);
                        validModels.Add(856);
                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(320);
                    validModels.Add(321);
                    if (Level > 10)
                    {
                        validModels.Add(322);
                        validModels.Add(323);
                    }
                    if (Level > 20)
                        validModels.Add(324);
                    if (Level > 30)
                    {
                        validModels.Add(950);
                        validModels.Add(954);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(1019);
                        validModels.Add(1016);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(1016);
                        validModels.Add(1009);
                    }
                    break;
                default:
                    validModels.Add(449);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3676);
                validModels.Add(3677);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3719);
                validModels.Add(3720);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int Get2HThrustForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(846);

                    if (Level > 20)
                        validModels.Add(646);
                    if (Level > 30)
                        validModels.Add(2661);
                    if (Level > 40)
                    {
                        
                    }
                    if (Level > 50)
                    {
                        validModels.Add(2208);
                        
                    }
                    break;
                default:
                    validModels.Add(846);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                
                validModels.Add(3657);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3700);
                validModels.Add(3817);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetThrustModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(71);
                    validModels.Add(454);
                    if (Level > 10)
                    {
                        validModels.Add(455);
                        validModels.Add(902);
                    }
                    if (Level > 20)
                    {
                        validModels.Add(456);
                        validModels.Add(898);
                        validModels.Add(940);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(457);
                        validModels.Add(472);
                        validModels.Add(895);
                        validModels.Add(941);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(460);
                        validModels.Add(643);
                        validModels.Add(947);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(453);
                        validModels.Add(942);
                        validModels.Add(943);
                        validModels.Add(944);
                        validModels.Add(945);
                        validModels.Add(946);
                        validModels.Add(2209);
                    }
                    break;
                case ERealm.Albion:
                    validModels.Add(21);
                    validModels.Add(71);
                    if (Level > 10)
                    {
                        validModels.Add(876);
                        validModels.Add(22);
                        //validModels.Add(23);
                    }
                    if (Level > 20)
                    {
                        validModels.Add(889);
                        validModels.Add(25);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(888);
                        validModels.Add(887);
                        //validModels.Add(29);
                        validModels.Add(30);
                    }
                    if (Level > 40)
                        validModels.Add(653);
                    if (Level > 50)
                    {
                        validModels.Add(885);
                        validModels.Add(886);
                        validModels.Add(2209);
                    }
                    break;
                default:
                    validModels.Add(1);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3721);
                validModels.Add(3722);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3721);
                validModels.Add(3722);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetPolearmModelForLevel(int Level, ERealm realm, EDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    switch (dtype)
                    {
                        case EDamageType.Slash:
                            validModels.Add(67);
                            if (Level > 10)
                                validModels.Add(68);
                            if (Level > 20)
                                validModels.Add(648);
                            if (Level > 30)
                                validModels.Add(649);
                            if (Level > 40)
                            {
                                validModels.Add(873);
                                if (Util.Chance(1))
                                {
                                    validModels.Clear();
                                    validModels.Add(3672);
                                }
                            }
                            if (Level > 50)
                            {
                                validModels.Add(874);
                                if (Util.Chance(1))
                                {
                                    validModels.Clear();
                                    validModels.Add(3715);
                                }
                            }
                            break;
                        case EDamageType.Crush:
                            validModels.Add(70);
                            if (Level > 10)
                                validModels.Add(650);
                            if (Level > 20)
                                validModels.Add(870);
                            if (Level > 30)
                                validModels.Add(875);
                            if (Level > 40 &&  Util.Chance(1))
                                validModels.Add(3673);
                            if (Level > 50 && Util.Chance(1))
                            {

                                validModels.Add(3833);
                                validModels.Add(3716);
                            }
                            break;
                        case EDamageType.Thrust:
                            validModels.Add(26);
                            if (Level > 10)
                                validModels.Add(69);
                            if (Level > 20)
                                validModels.Add(458);
                            if (Level > 30)
                                validModels.Add(649);
                            if (Level > 40)
                            {
                                validModels.Add(871);
                                if (Util.Chance(1))
                                {
                                    validModels.Clear();
                                    validModels.Add(3671);
                                }
                            }
                            if (Level > 50)
                            {
                                validModels.Add(872);
                                if (Util.Chance(1))
                                {
                                    validModels.Clear();
                                    validModels.Add(3714);
                                }
                            }
                            break;
                    }
                    break;
                default:
                    validModels.Add(328);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetSpearModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(556);
                    validModels.Add(469);
                    if (Level > 10)
                    {
                        validModels.Add(470);
                        validModels.Add(475);
                    }
                    if (Level > 20)
                    {
                        validModels.Add(476);
                        validModels.Add(477);
                    }
                    if (Level > 30)
                    {
                        validModels.Add(934);
                        validModels.Add(935);
                    }
                    if (Level > 40)
                    {
                        validModels.Add(556);
                        validModels.Add(933);
                        validModels.Add(936);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(937);
                        validModels.Add(938);
                        validModels.Add(939);
                        validModels.Add(2689);
                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(328);
                    if (Level > 10)
                        validModels.Add(329);
                    if (Level > 20)
                        validModels.Add(330);
                    if (Level > 30)
                        validModels.Add(331);
                    if (Level > 40)
                    {
                        validModels.Add(332);
                        validModels.Add(958);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(657);
                        validModels.Add(1029);
                    }
                    break;
                default:
                    validModels.Add(328);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3660);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3703);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetBowModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(471);
                    if (Level > 10)
                        validModels.Add(918);
                    if (Level > 20)
                        validModels.Add(919);
                    if (Level > 30)
                        validModels.Add(920);
                    if (Level > 40)
                    {
                        validModels.Add(921);
                        validModels.Add(922);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(923);
                        validModels.Add(925);
                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(564);
                    if (Level > 30)
                        validModels.Add(1037);
                    if (Level > 40)
                        validModels.Add(1038);
                    if (Level > 50)
                        validModels.Add(1039);
                    break;
                case ERealm.Albion:
                    validModels.Add(132);
                    if (Level > 10)
                        validModels.Add(570);
                    if (Level > 20)
                        validModels.Add(848);
                    if (Level > 30)
                        validModels.Add(849);
                    if (Level > 40)
                        validModels.Add(850);
                    if (Level > 50)
                    {
                        validModels.Add(851);
                        validModels.Add(852); 
                    }
                    break;
                default:
                    validModels.Add(132);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3824);
                validModels.Add(3663);
            }
            /*
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3706);
                validModels.Add(3823);
            }*/

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetFlexModelForLevel(int Level, ERealm realm, EDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    switch (dtype)
                    {
                        case EDamageType.Crush:
                            validModels.Add(861);
                            if (Level > 10)
                                validModels.Add(862);
                            if (Level > 20)
                                validModels.Add(864);
                            if (Level > 30)
                                validModels.Add(869);
                            if (Level > 40)
                            {
                                validModels.Add(2669);
                                if(Util.Chance(1))
                                    validModels.Add(3653);
                            }
                            if (Level > 50 && Util.Chance(1))
                            {
                                validModels.Clear();
                                validModels.Add(3696);
                                validModels.Add(3815);
                                validModels.Add(3952);
                            }
                            break;
                        case EDamageType.Slash:
                            validModels.Add(857);
                            validModels.Add(859);
                            validModels.Add(865);
                            if (Level > 10)
                                validModels.Add(863);
                            if (Level > 20)
                                validModels.Add(867);
                            if (Level > 30)
                                validModels.Add(868);
                            if (Level > 40)
                            {
                                validModels.Add(2670);
                                if (Util.Chance(1))
                                    validModels.Add(3654);
                            }
                            if (Level > 50 && Util.Chance(1))
                            {
                                validModels.Add(3697);
                                validModels.Add(3814);
                                validModels.Add(3951);
                            }
                            break;
                    }
                    break;
                default:
                    validModels.Add(132);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetH2HModelForLevel(int Level, ERealm realm, EDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Midgard:
                    switch (dtype)
                    {
                        case EDamageType.Thrust:
                            validModels.Add(960);
                            validModels.Add(962);
                            validModels.Add(964);
                            if (Level > 10)
                                validModels.Add(966);
                            if (Level > 20)
                                validModels.Add(968);
                            if (Level > 30)
                                validModels.Add(970);
                            if (Level > 40)
                            {
                                validModels.Add(972);
                                validModels.Add(974);
                                validModels.Add(976);
                                if (Util.Chance(1))
                                {
                                    validModels.Add(3686);
                                    validModels.Add(3687);
                                }                                
                            }
                            if (Level > 50)
                            {
                                validModels.Add(978);
                                validModels.Add(980);
                                validModels.Add(982);
                                if (Util.Chance(1))
                                {
                                    validModels.Add(3729);
                                    validModels.Add(3730);
                                }                                
                            }
                            break;
                        case EDamageType.Slash:
                            validModels.Add(959);
                            validModels.Add(961);
                            validModels.Add(963);
                            if (Level > 10)
                                validModels.Add(965);
                            if (Level > 20)
                                validModels.Add(967);
                            if (Level > 30)
                                validModels.Add(969);
                            if (Level > 40)
                            {
                                validModels.Add(971);
                                validModels.Add(973);
                                validModels.Add(975);
                                if (Util.Chance(1))
                                {
                                    validModels.Add(3682);
                                    validModels.Add(3683);
                                }                                
                            }
                            if (Level > 50)
                            {
                                validModels.Add(977);
                                validModels.Add(979);
                                validModels.Add(981);
                                if (Util.Chance(1))
                                {
                                    validModels.Add(3725);
                                    validModels.Add(3726);
                                }
                            }
                            break;
                    }
                    break;
                default:
                    validModels.Add(132);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetCrossbowModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(226);
                    if (Level > 10)
                        validModels.Add(890);
                    if (Level > 20)
                        validModels.Add(891);
                    if (Level > 30)
                        validModels.Add(892);
                    if (Level > 40)
                    {
                        validModels.Add(893);
                        validModels.Add(894); 
                    }
                    break;
                default:
                    validModels.Add(226);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3656);
            }
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3816);
                validModels.Add(3953);
                validModels.Add(3699);
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetScytheModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    validModels.Add(931);
                    if (Level > 10)
                        validModels.Add(929);
                    if (Level > 20)
                        validModels.Add(928);
                    if (Level > 30)
                        validModels.Add(930);
                    if (Level > 40)
                    {
                        validModels.Add(932);
                        validModels.Add(926);
                        validModels.Add(927);
                        if(Util.Chance(1))
                            validModels.Add(3665);
                    }
                    if (Level > 50 && Util.Chance(1))
                    {
                        validModels.Clear();
                        validModels.Add(3825);
                        validModels.Add(3708);
                        validModels.Add(3885);
                    }
                    break;
                default:
                    validModels.Add(931);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetStaffModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Albion:
                    validModels.Add(19);
                    if (Level > 10)
                        validModels.Add(442);
                    if (Level > 20)
                        validModels.Add(567);
                    if (Level > 30)
                        validModels.Add(568);
                    if (Level > 40)
                    {
                        validModels.Add(882);
                        validModels.Add(883);
                        validModels.Add(1166);
                        validModels.Add(1169);

                    }
                    if (Level > 50)
                    {
                        validModels.Add(821);
                        validModels.Add(881);
                        validModels.Add(1168);
                        validModels.Add(1167);
                        validModels.Add(1170); 
                    }
                    break;
                case ERealm.Hibernia:
                    validModels.Add(1180);
                    if (Level > 10)
                        validModels.Add(1181);
                    if (Level > 20)
                        validModels.Add(1185);
                    if (Level > 30)
                        validModels.Add(1184);
                    if (Level > 40)
                    {
                        validModels.Add(1178);
                        validModels.Add(1174);
                        validModels.Add(1175);
                    }
                    if (Level > 50)
                    {
                        validModels.Add(1179);
                        validModels.Add(1173);
                    }
                    break;
                case ERealm.Midgard:
                    validModels.Add(327);
                    if (Level > 10)
                        validModels.Add(565);
                    if (Level > 20)
                        validModels.Add(828);
                    if (Level > 30)
                        validModels.Add(1171);
                    if (Level > 40)
                    {
                        validModels.Add(1172);
                        validModels.Add(1176);
                    }
                    if (Level > 50)
                        validModels.Add(1177);
                    break;
                default:
                    validModels.Add(931);
                    break;
            }

            if (Util.Chance(1) && Level > 40)
            {
                validModels.Clear();
                validModels.Add(3667);
            }
            if (Util.Chance(1) && Level > 50)
            {
                validModels.Clear();
                validModels.Add(3710);
            }
            
            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetShieldModelForLevel(int Level, ERealm realm, int size)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case ERealm.Hibernia:
                    switch (size)
                    {
                        case 1:
                            validModels.Add(1046);
                            validModels.Add(1047);
                            validModels.Add(1048);
                            if (Level > 10)
                            {
                                validModels.Add(1082);
                                validModels.Add(1083);
                                validModels.Add(1084);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1100);
                                validModels.Add(1101);
                                validModels.Add(1102);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1163);
                                validModels.Add(1164);
                                validModels.Add(1165);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3888);
                            break;
                        case 2:
                            validModels.Add(1055);
                            validModels.Add(1056);
                            validModels.Add(1057);
                            if (Level > 10)
                            {
                                validModels.Add(1091);
                                validModels.Add(1092);
                                validModels.Add(1093);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1145);
                                validModels.Add(1146);
                                validModels.Add(1147);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1148);
                                validModels.Add(1149);
                                validModels.Add(1150);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1160);
                                validModels.Add(1161);
                                validModels.Add(1162);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3889);
                            break;
                        case 3:
                            validModels.Add(1082);
                            validModels.Add(1083);
                            validModels.Add(1084);
                            if (Level > 10)
                            {
                                validModels.Add(1073);
                                validModels.Add(1074);
                                validModels.Add(1075);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1064);
                                validModels.Add(1065);
                                validModels.Add(1066);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1151);
                                validModels.Add(1152);
                                validModels.Add(1153);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1154);
                                validModels.Add(1155);
                                validModels.Add(1156);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3890);
                            break;
                    }
                    break;
                case ERealm.Albion:
                    switch (size)
                    {
                        case 1:
                            validModels.Add(1040);
                            validModels.Add(1041);
                            validModels.Add(1042);
                            if (Level > 10)
                            {
                                validModels.Add(1103);
                                validModels.Add(1104);
                                validModels.Add(1105);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1118);
                                validModels.Add(1119);
                                validModels.Add(1120);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3965);
                            break;
                        case 2:
                            validModels.Add(1094);
                            validModels.Add(1095);
                            validModels.Add(1096);
                            if (Level > 10)
                            {
                                validModels.Add(1049);
                                validModels.Add(1050);
                                validModels.Add(1051);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1085);
                                validModels.Add(1086);
                                validModels.Add(1087);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1106);
                                validModels.Add(1107);
                                validModels.Add(1108);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1115);
                                validModels.Add(1116);
                                validModels.Add(1117);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3966);
                            break;
                        case 3:
                            validModels.Add(1058);
                            validModels.Add(1059);
                            validModels.Add(1060);
                            if (Level > 10)
                            {
                                validModels.Add(1067);
                                validModels.Add(1068);
                                validModels.Add(1069);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1112);
                                validModels.Add(1113);
                                validModels.Add(1114);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1109);
                                validModels.Add(1110);
                                validModels.Add(1111);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1121);
                                validModels.Add(1122);
                                validModels.Add(1123);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3967);
                            break;
                    }
                    break;
                case ERealm.Midgard:
                    switch (size)
                    {
                        case 1:
                            validModels.Add(1043);
                            validModels.Add(1044);
                            validModels.Add(1045);
                            if (Level > 10)
                            {
                                validModels.Add(1124);
                                validModels.Add(1125);
                                validModels.Add(1126);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1139);
                                validModels.Add(1140);
                                validModels.Add(1141);
                            }
                            if(Level > 30)
                            {
                                validModels.Add(1130);
                                validModels.Add(1131);
                                validModels.Add(1132);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3929);
                            break;
                        case 2:
                            validModels.Add(1097);
                            validModels.Add(1098);
                            validModels.Add(1099);
                            if (Level > 10)
                            {
                                validModels.Add(1088);
                                validModels.Add(1089);
                                validModels.Add(1090);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1052);
                                validModels.Add(1053);
                                validModels.Add(1054);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1127);
                                validModels.Add(1128);
                                validModels.Add(1129);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3930);
                            break;
                        case 3:
                            validModels.Add(1079);
                            validModels.Add(1080);
                            validModels.Add(1081);
                            if (Level > 10)
                            {
                                validModels.Add(1061);
                                validModels.Add(1062);
                                validModels.Add(1063);
                            }
                            if (Level > 20)
                            {
                                validModels.Add(1133);
                                validModels.Add(1134);
                                validModels.Add(1135);
                            }
                            if (Level > 30)
                            {
                                validModels.Add(1136);
                                validModels.Add(1137);
                                validModels.Add(1138);
                            }
                            if (Level > 40)
                            {
                                validModels.Add(1142);
                                validModels.Add(1143);
                                validModels.Add(1144);
                            }
                            if (Level > 50 && Util.Chance(1))
                                validModels.Add(3931);
                            break;
                    }
                    break;
                default:
                    validModels.Add(59);
                    break;
            }

            return validModels[Util.Random(validModels.Count - 1)];
        }

        private static int GetInstrumentModelForLevel(int Level, ERealm realm)
        {
            List<int> validModels = new List<int>();
            validModels.Add(227);
            validModels.Add(228);
            validModels.Add(325);
            if (Level > 10)
            {
                validModels.Add(2974);
                validModels.Add(2975);
                validModels.Add(2973);
            }
            if (Level > 20)
            {
                validModels.Add(2970);
                validModels.Add(2971);
                validModels.Add(2972);
            }
            if (Level > 30)
            {
                if(realm == ERealm.Albion)
                {
                    validModels.Add(2976);
                    validModels.Add(2977);
                    validModels.Add(2978);
                } else if (realm == ERealm.Hibernia)
                {
                    validModels.Add(2979);
                    validModels.Add(2980);
                    validModels.Add(2981);
                }
               
            }
            if(Level > 40)
            {
                validModels.Add(2114);
                validModels.Add(2115);
                validModels.Add(2116);
                validModels.Add(2117);
            }
            if (Level > 50 && Util.Chance(1))
            {
                validModels.Add(3688);
                validModels.Add(3731);
                validModels.Add(3848);
                if (Util.Chance(50))
                {
                    if (realm == ERealm.Albion)
                        validModels.Add(3985);
                    if (realm == ERealm.Hibernia)
                        validModels.Add(3908);
                }
                if (Util.Chance(5))
                {
                    if (realm == ERealm.Albion)
                        validModels.Add(3280);
                    if (realm == ERealm.Hibernia)
                        validModels.Add(3239);
                }
            }
            return validModels[Util.Random(validModels.Count - 1)];
        }

        #endregion

        #region Naming
        private static string GetNameFromId(int modelId)
        {
            switch (modelId)
            {
                case 1:
                case 23:
                case 25:
                case 28:
                case 454:
                case 457:
                case 472:
                case 571:
                case 885:
                case 887:
                case 895:
                case 898:
                case 902:
                case 943:
                case 944:
                case 949:
                case 1013:
                case 1021:
                case 3678:
                case 3679:
                case 3721:
                case 3722:
                case 3838:
                case 3839:
                    return "Dagger";
                case 21:
                case 876:
                case 889:
                    return "Dirk";
                case 30:
                    return "Gladius";
                case 456:
                case 71:
                    return "Stiletto";
                case 2:
                case 315:
                case 316:
                case 319:
                case 573:
                case 578:
                case 878:
                case 880:
                case 951:
                case 953:
                case 1010:
                case 1011:
                case 1014:
                case 1018:
                case 1023:
                case 1025:
                case 2657:
                case 2672:
                case 3680:
                case 3681:
                case 3723:
                case 3724:
                case 3840:
                case 3841:
                    return "Axe";
                case 654:
                    return "Cleaver";
                case 22:
                case 24:
                case 29:
                case 455:
                case 643:
                case 653:
                case 886:
                case 888:
                case 945:
                case 946:
                case 2686:
                case 2687:
                case 2658:
                    return "Rapier";
                case 3:
                case 4:
                case 5:
                case 10:
                case 310:
                case 311:
                case 312:
                case 313:
                case 445:
                case 446:
                case 447:
                case 473:
                case 655:
                case 877:
                case 879:
                case 896:
                case 897:
                case 899:
                case 900:
                case 901:
                case 903:
                case 948:
                case 952:
                case 1015:
                case 1017:
                case 1020:
                case 1024:
                case 2671:
                case 2682:
                case 3674:
                case 3675:
                case 3717:
                case 3718:
                case 3834:
                case 3835:
                    return "Sword";
                case 460:
                    return "Hooked Sword";
                case 444:
                    return "Falcata";
                case 8:
                case 645:
                    return "Scimitar";
                case 651:
                    return "Jambiya";
                case 652:
                    return "Sabre";
                case 2195:
                    return "Khopesh";
                case 2209:
                    return "Wakazashi";
                case 6:
                case 7:
                case 314:
                case 448:
                case 459:
                case 572:
                case 658:
                case 841:
                case 843:
                case 907:
                case 911:
                case 957:
                case 1032:
                case 1035:
                case 2660:
                case 2674:
                case 2690:
                case 3657:
                case 3658:
                case 3700:
                case 3701:
                case 3817:
                case 3818:
                case 3954:
                case 3955:
                    return "Greatsword";
                case 660:
                    return "War Cleaver";
                case 639:
                    return "Great Falcata";
                case 847:
                    return "Great Falchion";
                case 910:
                    return "Troll Splitter";
                case 2208:
                    return "Katana";
                case 959:
                case 960:
                case 963:
                case 964:
                case 965:
                case 966:
                case 969:
                case 970:
                case 973:
                case 974:
                case 977:
                case 978:
                case 3684:
                case 3685:
                case 3725:
                case 3726:
                    return "Greave";
                case 961:
                case 967:
                case 971:
                case 975:
                case 979:
                case 981:
                case 3682:
                case 3683:
                case 3727:
                case 3728:
                    return "Claw";
                case 962:
                case 968:
                case 972:
                case 976:
                case 980:
                case 982:
                case 3686:
                case 3687:
                case 3729:
                case 3730:
                    return "Fang";
                case 9:
                case 72:
                case 73:
                case 317:
                case 318:
                case 577:
                case 845:
                case 955:
                case 1027:
                case 1030:
                case 1033:
                case 2675:
                case 2985:
                case 3662:
                case 3705:
                case 3822:
                case 3882:
                case 3923:
                case 3959:
                    return "Greataxe";
                case 16:
                case 17:
                case 462:
                case 463:
                case 574:
                case 575:
                case 576:
                case 640:
                case 644:
                case 659:
                case 842:
                case 844:
                case 904:
                case 905:
                case 906:
                case 908:
                case 909:
                case 917:
                case 956:
                case 1028:
                case 1031:
                case 1034:
                case 2215:
                case 2662:
                case 2676:
                case 2691:
                case 3661:
                case 3704:
                case 3821:
                case 3881:
                case 3922:
                    return "Great Hammer";
                case 474:
                case 912:
                    return "Shillelagh";
                case 846:
                case 2661:
                case 646:
                    return "War Mattock";
                case 11:
                case 13:
                case 14:
                case 18:
                case 20:
                case 450:
                case 451:
                case 647:
                case 853:
                case 854:
                case 855:
                case 856:
                case 914:
                case 915:
                case 2659:
                case 2683:
                    return "Mace";
                case 12:
                case 15:
                case 320:
                case 321:
                case 322:
                case 323:
                case 324:
                case 461:
                case 641:
                case 656:
                case 913:
                case 916:
                case 950:
                case 954:
                case 1009:
                case 1012:
                case 1022:
                case 1026:
                case 2673:
                case 3676:
                case 3677:
                case 3836:
                case 3837:
                    return "Hammer";
                case 940:
                case 941:
                case 942:
                case 947:
                case 2684:
                    return "Adze";
                case 449:
                case 452:
                case 1016:
                case 1019:
                    return "Club";
                case 453:
                    return "Sickle";
                case 227:
                case 2117:
                case 2970:
                case 2973:
                case 2976:
                case 2979:
                    return "Lute";
                case 3848:
                    return "Mandolin";
                case 228:
                case 2114:
                case 2971:
                case 2974:
                case 2977:
                case 2980:
                    return "Drum";
                case 325:
                case 2115:
                case 2972:
                case 2975:
                case 2978:
                case 2981:
                    return "Flute";
                case 2116:
                case 3908:
                case 3949:
                case 3985:
                case 3280:
                case 3239:
                case 3688:
                case 3731:
                    return "Harp";
                case 328:
                case 329:
                case 331:
                case 332:
                case 469:
                case 470:
                case 475:
                case 476:
                case 477:
                case 556:
                case 642:
                case 657:
                case 933:
                case 934:
                case 935:
                case 936:
                case 938:
                case 939:
                case 958:
                case 1029:
                case 1036:
                case 1661:
                case 3659:
                case 3660:
                case 3671:
                case 3672:
                case 3673:
                case 3702:
                case 3703:
                case 3714:
                case 3715:
                case 3716:
                case 3819:
                case 3820:
                case 3831:
                case 3832:
                case 3833:
                    return "Spear";
                case 937:
                    return "Harpoon";
                case 330:
                case 458:
                case 1004:
                    return "Trident";
                default:
                    return "Staff";
            }
        }
        #endregion

        private static byte GetTorsoExtensionForLevel(int Level)
        {
            int possibleExtensions = 1;
            byte appliedExtension = 0;

            if (Level > 10)
                possibleExtensions++;
            if (Level > 20)
                possibleExtensions++;
            if (Level > 30)
                possibleExtensions++;
            if (Level > 40)
                possibleExtensions++;
            appliedExtension = (byte)Util.Random(possibleExtensions);
            if (Level > 50)
                appliedExtension++; //increment by 1 to unlock special extension for lvl 51+, as well as remove possibility of getting extension 0
            return appliedExtension;
        }

        private static byte GetNonTorsoExtensionForLevel(int Level)
        {
            List<byte> possibleExt = new List<byte>();
            possibleExt.Add(0);
            if (Level > 10)
                possibleExt.Add(8);
            if (Level > 20)
                possibleExt.Add(7);
            if (Level > 30)
                possibleExt.Add(5);
            if (Level > 40)
                possibleExt.Add(6);
            if (Level > 50)
            {
                possibleExt.Add(4);
                possibleExt.Remove(0);
            }
            byte appliedExtension = possibleExt[Util.Random(possibleExt.Count - 1)];
            return appliedExtension;
        }

        private static string ArmorSlotToName(EInventorySlot slot, EObjectType type)
        {
            switch (slot)
            {
                case EInventorySlot.ArmsArmor:
                    if (type == EObjectType.Plate)
                        return "Arms";
                    else
                        return "Sleeves";

                case EInventorySlot.FeetArmor:
                    return "Boots";

                case EInventorySlot.HandsArmor:
                    if (type == EObjectType.Plate)
                        return "Gauntlets";
                    else
                        return "Gloves";

                case EInventorySlot.HeadArmor:
                    if (type == EObjectType.Cloth)
                        return "Cap";
                    else if (type == EObjectType.Scale)
                        return "Coif";
                    else
                        return "Helm";

                case EInventorySlot.LegsArmor:
                    if (type == EObjectType.Cloth)
                        return "Pants";
                    else if (type == EObjectType.Plate)
                        return "Legs";
                    else
                        return "Leggings";

                case EInventorySlot.TorsoArmor:
                    if (type == EObjectType.Chain || type == EObjectType.Scale)
                        return "Hauberk";
                    else if (type == EObjectType.Plate)
                        return "Breastplate";
                    else if ((type == EObjectType.Leather || type == EObjectType.Studded) && Util.Chance(50))
                        return "Jerkin";
                    else
                        return "Vest";

                default: return GlobalConstants.SlotToName((int)slot);
            }
        }

        private int GetProcFromLevel(byte level)
        {
            int procID = 0;
            if (Util.Chance(50))
                procID = GetLifetapProcFromLevel(Level);
            else
                procID = GetDDProcFromLevel(Level);

            return procID;
        }

        private int GetDDProcFromLevel(int level)
        {
            if (Level <= 10)
                return 8020;
            if (Level <= 15)
                return 8021;
            if (Level <= 20)
                return 8022;
            if (Level <= 25)
                return 8023;
            if (Level <= 30)
                return 8024;
            if (Level <= 35)
                return 8025;
            if (Level <= 40)
                return 8026;
            if (Level <= 43)
                return 8027;

            return 0;
        }

        private int GetLifetapProcFromLevel(int level)
        {
            if (Level <= 10)
                return 8010;
            if (Level <= 15)
                return 8011;
            if (Level <= 20)
                return 8012;
            if (Level <= 25)
                return 8013;
            if (Level <= 30)
                return 8014;
            if (Level <= 35)
                return 8015;
            if (Level <= 40)
                return 8016;
            if (Level <= 43)
                return 8017;

            return 0;

        }

        #endregion

        #region definitions
        public enum eBonusType {
            Stat,
            AdvancedStat,
            Resist,
            Skill,
            Focus,
        }

        public enum eGenerateType {
            Weapon,
            Armor,
            Magical,
            None,
        }

        private static EProperty[] StatBonus = new EProperty[]
        {
            EProperty.Strength,
            EProperty.Dexterity,
            EProperty.Constitution,
            EProperty.Quickness,
			//eProperty.Intelligence,
			//eProperty.Piety,
			//eProperty.Empathy,
			//eProperty.Charisma,
			EProperty.MaxMana,
            EProperty.MaxHealth,
            EProperty.Acuity,
        };

        private static EProperty[] AdvancedStats = new EProperty[]
        {
            EProperty.PowerPool,
            EProperty.PowerPoolCapBonus,
            EProperty.StrCapBonus,
            EProperty.DexCapBonus,
            EProperty.ConCapBonus,
            EProperty.QuiCapBonus,
			//eProperty.IntCapBonus,
			//eProperty.PieCapBonus,
			//eProperty.EmpCapBonus,
			//eProperty.ChaCapBonus,
			EProperty.MaxHealthCapBonus,
            EProperty.AcuCapBonus,
        };

        private static EProperty[] ResistBonus = new EProperty[]
        {
            EProperty.Resist_Body,
            EProperty.Resist_Cold,
            EProperty.Resist_Crush,
            EProperty.Resist_Energy,
            EProperty.Resist_Heat,
            EProperty.Resist_Matter,
            EProperty.Resist_Slash,
            EProperty.Resist_Spirit,
            EProperty.Resist_Thrust,
        };


        private static EProperty[] AlbSkillBonus = new EProperty[]
        {
            EProperty.Skill_Two_Handed,
            EProperty.Skill_Body,
	        //eProperty.Skill_Chants, // bonus not used
	        EProperty.Skill_Critical_Strike,
            EProperty.Skill_Cross_Bows,
            EProperty.Skill_Crushing,
            EProperty.Skill_Death_Servant,
            EProperty.Skill_DeathSight,
            EProperty.Skill_Dual_Wield,
            EProperty.Skill_Earth,
            EProperty.Skill_Enhancement,
            EProperty.Skill_Envenom,
            EProperty.Skill_Fire,
            EProperty.Skill_Flexible_Weapon,
            EProperty.Skill_Cold,
            EProperty.Skill_Instruments,
            EProperty.Skill_Long_bows,
            EProperty.Skill_Matter,
            EProperty.Skill_Mind,
            EProperty.Skill_Pain_working,
            EProperty.Skill_Parry,
            EProperty.Skill_Polearms,
            EProperty.Skill_Rejuvenation,
            EProperty.Skill_Shields,
            EProperty.Skill_Slashing,
            EProperty.Skill_Smiting,
            EProperty.Skill_SoulRending,
            EProperty.Skill_Spirit,
            EProperty.Skill_Staff,
            EProperty.Skill_Stealth,
            EProperty.Skill_Thrusting,
            EProperty.Skill_Wind,
            //eProperty.Skill_Aura_Manipulation, //Maulers
            //eProperty.Skill_FistWraps, //Maulers
            //eProperty.Skill_MaulerStaff, //Maulers
            //eProperty.Skill_Magnetism, //Maulers
            //eProperty.Skill_Power_Strikes, //Maulers
        };


        private static EProperty[] HibSkillBonus = new EProperty[]
        {
            EProperty.Skill_Critical_Strike,
            EProperty.Skill_Envenom,
            EProperty.Skill_Parry,
            EProperty.Skill_Shields,
            EProperty.Skill_Stealth,
            EProperty.Skill_Light,
            EProperty.Skill_Void,
            EProperty.Skill_Mana,
            EProperty.Skill_Blades,
            EProperty.Skill_Blunt,
            EProperty.Skill_Piercing,
            EProperty.Skill_Large_Weapon,
            EProperty.Skill_Mentalism,
            EProperty.Skill_Regrowth,
            EProperty.Skill_Nurture,
            EProperty.Skill_Nature,
            EProperty.Skill_Music,
            EProperty.Skill_Celtic_Dual,
            EProperty.Skill_Celtic_Spear,
            EProperty.Skill_RecurvedBow,
            EProperty.Skill_Valor,
            EProperty.Skill_Verdant,
            EProperty.Skill_Creeping,
            EProperty.Skill_Arboreal,
            EProperty.Skill_Scythe,
            //eProperty.Skill_Nightshade, // bonus not used
            //eProperty.Skill_Pathfinding, // bonus not used
            //eProperty.Skill_Dementia,
            //eProperty.Skill_ShadowMastery,
            //eProperty.Skill_VampiiricEmbrace,
            //eProperty.Skill_EtherealShriek,
            //eProperty.Skill_PhantasmalWail,
            //eProperty.Skill_SpectralForce,
            //eProperty.Skill_Aura_Manipulation, //Maulers
            //eProperty.Skill_FistWraps, //Maulers
            //eProperty.Skill_MaulerStaff, //Maulers
            //eProperty.Skill_Magnetism, //Maulers
            //eProperty.Skill_Power_Strikes, //Maulers
        };

        private static EProperty[] MidSkillBonus = new EProperty[]
        {
            EProperty.Skill_Critical_Strike,
            EProperty.Skill_Envenom,
            EProperty.Skill_Parry,
            EProperty.Skill_Shields,
            EProperty.Skill_Stealth,
            EProperty.Skill_Sword,
            EProperty.Skill_Hammer,
            EProperty.Skill_Axe,
            EProperty.Skill_Left_Axe,
            EProperty.Skill_Spear,
            EProperty.Skill_Mending,
            EProperty.Skill_Augmentation,
	        //Skill_Cave_Magic = 59,
	        EProperty.Skill_Darkness,
            EProperty.Skill_Suppression,
            EProperty.Skill_Runecarving,
            EProperty.Skill_Stormcalling,
	        //eProperty.Skill_BeastCraft, // bonus not used
			EProperty.Skill_Composite,
            EProperty.Skill_Battlesongs,
            EProperty.Skill_Subterranean,
            EProperty.Skill_BoneArmy,
            EProperty.Skill_Thrown_Weapons,
            EProperty.Skill_HandToHand,
    		//eProperty.Skill_Pacification,
	        //eProperty.Skill_Savagery,
	        //eProperty.Skill_OdinsWill,
	        //eProperty.Skill_Cursing,
	        //eProperty.Skill_Hexing,
	        //eProperty.Skill_Witchcraft,
    		EProperty.Skill_Summoning,
            //eProperty.Skill_Aura_Manipulation, //Maulers
            //eProperty.Skill_FistWraps, //Maulers
            //eProperty.Skill_MaulerStaff, //Maulers
            //eProperty.Skill_Magnetism, //Maulers
            //eProperty.Skill_Power_Strikes, //Maulers
        };



        private static int[] ArmorSlots = new int[] { 21, 22, 23, 25, 27, 28, };
        private static int[] MagicalSlots = new int[] { 24, 26, 29, 32, 33, 34, 35, 36 };

        // the following are doubled up to work around an apparent mid-number bias to the random number generator

        // note that weapon array has been adjusted to add weight to more commonly used items
        private static EObjectType[] AlbionWeapons = new EObjectType[]
        {
            EObjectType.ThrustWeapon,
            EObjectType.CrushingWeapon,
            EObjectType.SlashingWeapon,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.TwoHandedWeapon,
            EObjectType.Longbow,
            EObjectType.Flexible,
            EObjectType.PolearmWeapon,
            EObjectType.FistWraps, //Maulers
			EObjectType.MaulerStaff,//Maulers
			EObjectType.Instrument,
            EObjectType.Crossbow,
            EObjectType.ThrustWeapon,
            EObjectType.CrushingWeapon,
            EObjectType.SlashingWeapon,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.TwoHandedWeapon,
            EObjectType.Longbow,
            EObjectType.Flexible,
            EObjectType.PolearmWeapon,
            EObjectType.FistWraps, //Maulers
			EObjectType.MaulerStaff,//Maulers
			EObjectType.Instrument,
            EObjectType.Crossbow,
        };

        private static EObjectType[] AlbionArmor = new EObjectType[]
        {
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Studded,
            EObjectType.Chain,
            EObjectType.Plate,
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Studded,
            EObjectType.Chain,
            EObjectType.Plate,
        };
        private static EObjectType[] MidgardWeapons = new EObjectType[]
        {
            EObjectType.Sword,
            EObjectType.Hammer,
            EObjectType.Axe,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.Spear,
            EObjectType.CompositeBow ,
            EObjectType.LeftAxe,
            EObjectType.HandToHand,
            EObjectType.Sword,
            EObjectType.Hammer,
            EObjectType.Axe,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.Spear,
            EObjectType.CompositeBow ,
            EObjectType.LeftAxe,
            EObjectType.HandToHand,
        };

        private static EObjectType[] MidgardArmor = new EObjectType[]
        {
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Studded,
            EObjectType.Chain,
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Studded,
            EObjectType.Chain,
        };

        private static EObjectType[] HiberniaWeapons = new EObjectType[]
        {
            EObjectType.Blades,
            EObjectType.Blunt,
            EObjectType.Piercing,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.LargeWeapons,
            EObjectType.CelticSpear,
            EObjectType.Scythe,
            EObjectType.RecurvedBow,
            EObjectType.Instrument,
            EObjectType.FistWraps,//Maulers
			EObjectType.MaulerStaff,//Maulers
			EObjectType.Blades,
            EObjectType.Blunt,
            EObjectType.Piercing,
            EObjectType.Shield,
            EObjectType.Staff,
            EObjectType.LargeWeapons,
            EObjectType.CelticSpear,
            EObjectType.Scythe,
            EObjectType.RecurvedBow,
            EObjectType.Instrument,
            EObjectType.FistWraps,//Maulers
			EObjectType.MaulerStaff,//Maulers
        };

        private static EObjectType[] HiberniaArmor = new EObjectType[]
        {
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Reinforced,
            EObjectType.Scale,
            EObjectType.Cloth,
            EObjectType.Leather,
            EObjectType.Reinforced,
            EObjectType.Scale,
        };

        #endregion definitions

        public static void InitializeHashtables()
        {
            // Magic Prefix

            hPropertyToMagicPrefix.Add(EProperty.Strength, "Mighty");
            hPropertyToMagicPrefix.Add(EProperty.Dexterity, "Adroit");
            hPropertyToMagicPrefix.Add(EProperty.Constitution, "Fortifying");
            hPropertyToMagicPrefix.Add(EProperty.Quickness, "Speedy");
            hPropertyToMagicPrefix.Add(EProperty.Intelligence, "Insightful");
            hPropertyToMagicPrefix.Add(EProperty.Piety, "Willful");
            hPropertyToMagicPrefix.Add(EProperty.Empathy, "Attuned");
            hPropertyToMagicPrefix.Add(EProperty.Charisma, "Glib");
            hPropertyToMagicPrefix.Add(EProperty.MaxMana, "Arcane");
            hPropertyToMagicPrefix.Add(EProperty.MaxHealth, "Sturdy");
            hPropertyToMagicPrefix.Add(EProperty.PowerPool, "Arcane");

            hPropertyToMagicPrefix.Add(EProperty.Resist_Body, "Bodybender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Cold, "Icebender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Crush, "Bluntbender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Energy, "Energybender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Heat, "Heatbender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Matter, "Matterbender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Slash, "Edgebender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Spirit, "Spiritbender");
            hPropertyToMagicPrefix.Add(EProperty.Resist_Thrust, "Thrustbender");

            hPropertyToMagicPrefix.Add(EProperty.Skill_Two_Handed, "Sundering");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Body, "Soul Crusher");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Critical_Strike, "Lifetaker");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Cross_Bows, "Truefire");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Crushing, "Battering");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Death_Servant, "Death Binder");
            hPropertyToMagicPrefix.Add(EProperty.Skill_DeathSight, "Minionbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Dual_Wield, "Whirling");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Earth, "Earthborn");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Enhancement, "Fervent");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Envenom, "Venomous");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Fire, "Flameborn");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Flexible_Weapon, "Tensile");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Cold, "Iceborn");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Instruments, "Melodic");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Long_bows, "Winged");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Matter, "Earthsplitter");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Mind, "Dominating");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Pain_working, "Painbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Parry, "Bladeblocker");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Polearms, "Decimator");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Rejuvenation, "Rejuvenating");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Shields, "Protector's");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Slashing, "Honed");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Smiting, "Earthshaker");
            hPropertyToMagicPrefix.Add(EProperty.Skill_SoulRending, "Soul Taker");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Spirit, "Spiritbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Staff, "Thunderer");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Stealth, "Shadowwalker");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Thrusting, "Perforator");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Wind, "Airy");


            hPropertyToMagicPrefix.Add(EProperty.AllMagicSkills, "Mystical");
            hPropertyToMagicPrefix.Add(EProperty.AllMeleeWeaponSkills, "Gladiator");
            hPropertyToMagicPrefix.Add(EProperty.AllSkills, "Skillful");
            hPropertyToMagicPrefix.Add(EProperty.AllDualWieldingSkills, "Duelist");
            hPropertyToMagicPrefix.Add(EProperty.AllArcherySkills, "Bowmaster");


            hPropertyToMagicPrefix.Add(EProperty.Skill_Sword, "Serrated");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Hammer, "Demolishing");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Axe, "Swathe Cutter's");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Left_Axe, "Cleaving");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Spear, "Impaling");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Mending, "Bodymender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Augmentation, "Empowering");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Darkness, "Shadowbender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Suppression, "Spiritbinder");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Runecarving, "Runebender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Stormcalling, "Stormcaller");
            hPropertyToMagicPrefix.Add(EProperty.Skill_BeastCraft, "Lifebender");

            hPropertyToMagicPrefix.Add(EProperty.Skill_Light, "Lightbender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Void, "Voidbender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Mana, "Starbinder");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Enchantments, "Chanter");

            hPropertyToMagicPrefix.Add(EProperty.Skill_Blades, "Razored");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Blunt, "Crushing");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Piercing, "Lancenator");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Large_Weapon, "Sundering");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Mentalism, "Mindbinder");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Regrowth, "Forestbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Nurture, "Plantbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Nature, "Animalbound");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Music, "Resonant");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Celtic_Dual, "Whirling");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Celtic_Spear, "Impaling");
            hPropertyToMagicPrefix.Add(EProperty.Skill_RecurvedBow, "Hawk");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Valor, "Courageous");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Subterranean, "Ancestral");
            hPropertyToMagicPrefix.Add(EProperty.Skill_BoneArmy, "Blighted");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Verdant, "Vale Defender");

            hPropertyToMagicPrefix.Add(EProperty.Skill_Battlesongs, "Motivating");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Composite, "Dragon");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Creeping, "Withering");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Arboreal, "Arbor Defender");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Scythe, "Reaper's");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Thrown_Weapons, "Catapult");
            hPropertyToMagicPrefix.Add(EProperty.Skill_HandToHand, "Martial");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Pacification, "Pacifying");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Savagery, "Savage");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Nightshade, "Nightshade");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Pathfinding, "Trail");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Summoning, "Soulbinder");

            hPropertyToMagicPrefix.Add(EProperty.Skill_Dementia, "Feverish");
            hPropertyToMagicPrefix.Add(EProperty.Skill_ShadowMastery, "Ominous");
            hPropertyToMagicPrefix.Add(EProperty.Skill_VampiiricEmbrace, "Deathly");
            hPropertyToMagicPrefix.Add(EProperty.Skill_EtherealShriek, "Shrill");
            hPropertyToMagicPrefix.Add(EProperty.Skill_PhantasmalWail, "Keening");
            hPropertyToMagicPrefix.Add(EProperty.Skill_SpectralForce, "Uncanny");
            hPropertyToMagicPrefix.Add(EProperty.Skill_OdinsWill, "Ardent");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Cursing, "Infernal");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Hexing, "Bedeviled");
            hPropertyToMagicPrefix.Add(EProperty.Skill_Witchcraft, "Diabolic");

            // Mauler - live mauler prefixes do not exist, as lame as that sounds.
            hPropertyToMagicPrefix.Add(EProperty.Skill_Aura_Manipulation, string.Empty);
            hPropertyToMagicPrefix.Add(EProperty.Skill_FistWraps, string.Empty);
            hPropertyToMagicPrefix.Add(EProperty.Skill_MaulerStaff, string.Empty);
            hPropertyToMagicPrefix.Add(EProperty.Skill_Magnetism, string.Empty);
            hPropertyToMagicPrefix.Add(EProperty.Skill_Power_Strikes, string.Empty);
        }

        private static void CacheProcSpells()
        {
            //LT spells
            DbSpell Level5Lifetap = CoreDb<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8010));
            DbSpell Level10Lifetap = CoreDb<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8011));
            DbSpell Level15Lifetap = CoreDb<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8012));

            ProcSpells.Add(8010, new Spell(Level5Lifetap, 0));
            ProcSpells.Add(8011, new Spell(Level10Lifetap, 0));
            ProcSpells.Add(8012, new Spell(Level15Lifetap, 0));

        }
    }
}
