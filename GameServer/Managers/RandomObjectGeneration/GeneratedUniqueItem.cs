﻿using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    /// <summary>
    /// GeneratedUniqueItem is a subclass of UniqueItem used to create RoG object
    /// Using it as a class is much more extendable to other usage than just loot and inventory
    /// </summary>
    public class GeneratedUniqueItem : DbItemUnique {
        
        //The following properties are weights for each roll
        //It is *not* a direct chance to receive the item. It is instead
        //a chance for that item type to be randomly selected as a valid generation type
        private static int ToaItemChance = Properties.ROG_TOA_ITEM_CHANCE;
        private static int ArmorWeight = Properties.ROG_ARMOR_WEIGHT;
        private static int JewelryWeight = Properties.ROG_MAGICAL_WEIGHT;
        private static int WeaponWeight = Properties.ROG_WEAPON_WEIGHT;
        //The following 5 weights are for EACH roll on an item
        //I do not recommend putting any of them above 45
        private static int ToaStatWeight = Properties.ROG_TOA_STAT_WEIGHT;
        private static int ItemStatWeight = Properties.ROG_ITEM_STAT_WEIGHT;
        private static int ItemResistWeight = Properties.ROG_ITEM_RESIST_WEIGHT;
        private static int ItemSkillWeight = Properties.ROG_ITEM_SKILL_WEIGHT;
        private static int ItemAllSkillWeight = Properties.ROG_STAT_ALLSKILL_WEIGHT;

        //base item quality for all rogs
        private static int RogStartingQual = Properties.ROG_STARTING_QUAL;
        //max possible quality for any rog
        private static int RogCapQuality = Properties.ROG_CAP_QUAL;
        //base Chance to get a magical RoG item, PlayerLevel*2 is added to get final value
        private static int MagicalItemOffset = Properties.ROG_MAGICAL_ITEM_OFFSET;

        private eCharacterClass charClass = eCharacterClass.Unknown;

        private static Dictionary<int,Spell> ProcSpells = new Dictionary<int,Spell>();

        protected static Dictionary<eProperty, string> hPropertyToMagicPrefix = new Dictionary<eProperty, string>();

        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        { 
            InitializeHashtables();
        }

        public GeneratedUniqueItem()
            : this((eRealm)Util.Random(1, 3), (eCharacterClass)Util.Random(1, 32), (byte)Util.Random(1, 50))
        {

        }

        #region Constructor Randomized

        public GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, int minUtility = 15)
            : this(realm, charClass, level, GenerateObjectType(realm, charClass, level), minUtility)
        {

        }

        public GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, eObjectType type, int minUtility = 15)
            : this(realm, charClass, level, type, GenerateItemType(type), minUtility)
        {

        }

        public GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot, int minUtility = 15)
            : this(realm, charClass, level, type, slot, GenerateDamageType(type, charClass), minUtility)
        {

        }

        public GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot, eDamageType dmg, int minUtility = 15)
            : this(false, realm, charClass, level, type, slot, dmg, minUtility)
        {

        }

        public GeneratedUniqueItem(bool toa)
            : this(toa, (eRealm)Util.Random(1, 3), (eCharacterClass)Util.Random(1, 32), (byte)Util.Random(1, 50))
        {

        }

        public GeneratedUniqueItem(bool toa, eRealm realm, eCharacterClass charClass, byte level)
            : this(toa, realm, charClass, level, GenerateObjectType(realm, charClass, level))
        {

        }

        public GeneratedUniqueItem(bool toa, eRealm realm, eCharacterClass charClass, byte level, eObjectType type)
            : this(toa, realm, charClass, level, type, GenerateItemType(type))
        {

        }

        public GeneratedUniqueItem(bool toa, eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot)
            : this(toa, realm, charClass, level, type, slot, GenerateDamageType(type, charClass))
        {

        }

        public GeneratedUniqueItem(bool toa, eRealm realm, eCharacterClass charClass, byte level, eObjectType type, eInventorySlot slot, eDamageType dmg, int utilityMinimum = 15)
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
            this.GenerateItemQuality(Util.Random(0, 6) - 3);

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
        public void GenerateItemQuality(int conlevel)
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

            this.Price = WalletHelper.CalculateAutoPrice(this.Level, this.Quality);
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

            eObjectType type = (eObjectType)this.Object_Type;

            //special property for instrument
            if (type == eObjectType.Instrument)
                this.DPS_AF = Util.Random(0, 3);

            //set hand
            switch (type)
            {
                //two handed weapons
                case eObjectType.CelticSpear:
                case eObjectType.CompositeBow:
                case eObjectType.Crossbow:
                case eObjectType.Fired:
                case eObjectType.Instrument:
                case eObjectType.LargeWeapons:
                case eObjectType.Longbow:
                case eObjectType.PolearmWeapon:
                case eObjectType.RecurvedBow:
                case eObjectType.Scythe:
                case eObjectType.Spear:
                case eObjectType.Staff:
                case eObjectType.TwoHandedWeapon:
                case eObjectType.MaulerStaff: //Maulers
                    {
                        this.Hand = 1;
                        break;
                    }
                //right or left handed weapons
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.CrushingWeapon:
                case eObjectType.HandToHand:
                case eObjectType.Piercing:
                case eObjectType.SlashingWeapon:
                case eObjectType.ThrustWeapon:
                case eObjectType.FistWraps: //Maulers
                    {
                        if ((eInventorySlot)this.Item_Type == eInventorySlot.LeftHandWeapon)
                            this.Hand = 2;
                        break;
                    }
                //left handed weapons
                case eObjectType.LeftAxe:
                case eObjectType.Shield:
                    {
                        this.Hand = 2;
                        break;
                    }
                //right or two handed weapons
                case eObjectType.Sword:
                case eObjectType.Hammer:
                case eObjectType.Axe:
                    {
                        if ((eInventorySlot)this.Item_Type == eInventorySlot.TwoHandWeapon)
                            this.Hand = 1;
                        break;
                    }
            }

            //set dps_af and spd_abs
            if ((int)type >= (int)eObjectType._FirstArmor && (int)type <= (int)eObjectType._LastArmor)
            {
                if (type == eObjectType.Cloth)
                    this.DPS_AF = this.Level;
                else this.DPS_AF = this.Level * 2;
                this.SPD_ABS = GetAbsorb(type);
            }

            switch (type)
            {
                case eObjectType.Axe:
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.CelticSpear:
                case eObjectType.CompositeBow:
                case eObjectType.Crossbow:
                case eObjectType.CrushingWeapon:
                case eObjectType.Fired:
                case eObjectType.Flexible:
                case eObjectType.Hammer:
                case eObjectType.HandToHand:
                case eObjectType.LargeWeapons:
                case eObjectType.LeftAxe:
                case eObjectType.Longbow:
                case eObjectType.Piercing:
                case eObjectType.PolearmWeapon:
                case eObjectType.RecurvedBow:
                case eObjectType.Scythe:
                case eObjectType.Shield:
                case eObjectType.SlashingWeapon:
                case eObjectType.Spear:
                case eObjectType.Staff:
                case eObjectType.Sword:
                case eObjectType.ThrustWeapon:
                case eObjectType.TwoHandedWeapon:
                case eObjectType.MaulerStaff: //Maulers
                case eObjectType.FistWraps: //Maulers
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
            if (this.Object_Type == (int)eObjectType.Magical)
                return;

            this.ProcChance = 10;

            if(((this.Object_Type >= (int)eObjectType._FirstWeapon && this.Object_Type <= (int)eObjectType._LastWeapon) || this.Object_Type == (int)eObjectType.Shield))
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
            else if(this.Object_Type >= (int)eObjectType._FirstArmor && this.Object_Type <= (int)eObjectType._LastArmor && this.Item_Type == Slot.TORSO)
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

        private int GetRandomColorForRealm(eRealm realm)
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
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

            if (Util.Chance(MagicalItemOffset + this.Level * 2) || (eObjectType)Object_Type == eObjectType.Magical) // 100% magical starting at level 40
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
            if (this.Object_Type == (int)eObjectType.Magical && number < 1)
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
                eProperty property = this.GetProperty(type);
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

        private eProperty GetPropertyFromBonusLine(int BonusLine)
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

            return (eProperty)property;
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
            if (Properties.ROG_USE_WEIGHTED_GENERATION)
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
            if (this.Object_Type == (int)eObjectType.Staff)
            {
                if (this.Bonus1Type != 0)
                    return false;

                if (this.Realm == (int)eRealm.Albion && this.Description == "friar")
                    return false;

                return true;
            }

            return false;
        }
        #endregion

        #region check valid stat
        private eProperty GetProperty(eBonusType type)
        {
            switch (type)
            {
                case eBonusType.Focus:
                    {
                        return eProperty.AllFocusLevels;
                    }
                case eBonusType.Resist:
                    {
                        return (eProperty)Util.Random((int)eProperty.Resist_First, (int)eProperty.Resist_Last);
                    }
                case eBonusType.Skill:
                    {
                        // fill valid skills
                        ArrayList validSkills = new ArrayList();

                        bool fIndividualSkill = false;

                        // All Skills is never combined with any other skill
                        if (!BonusExists(eProperty.AllSkills))
                        {
                            // All type skills never combined with individual skills
                            if (!BonusExists(eProperty.AllMagicSkills) &&
                                !BonusExists(eProperty.AllMeleeWeaponSkills) &&
                                !BonusExists(eProperty.AllDualWieldingSkills) &&
                                !BonusExists(eProperty.AllArcherySkills))
                            {
                                // individual realm specific skills
                                if ((eRealm)this.Realm == eRealm.Albion)
                                {
                                    foreach (eProperty property in AlbSkillBonus)
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
                                else if ((eRealm)this.Realm == eRealm.Hibernia)
                                {
                                    foreach (eProperty property in HibSkillBonus)
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
                                else if ((eRealm)this.Realm == eRealm.Midgard)
                                {
                                    foreach (eProperty property in MidSkillBonus)
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
                                    if (SkillIsValidForObjectType(eProperty.AllSkills) && Util.Chance(ItemAllSkillWeight))
                                        validSkills.Add(eProperty.AllSkills);
                                }
                            }

                            // All type skills never combined with individual skills
                            if (!fIndividualSkill)
                            {
                                if (!BonusExists(eProperty.AllMagicSkills) && SkillIsValidForObjectType(eProperty.AllMagicSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(eProperty.AllMagicSkills);

                                if (!BonusExists(eProperty.AllMeleeWeaponSkills) && SkillIsValidForObjectType(eProperty.AllMeleeWeaponSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(eProperty.AllMeleeWeaponSkills);

                                if (!BonusExists(eProperty.AllDualWieldingSkills) && SkillIsValidForObjectType(eProperty.AllDualWieldingSkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(eProperty.AllDualWieldingSkills);

                                if (!BonusExists(eProperty.AllArcherySkills) && SkillIsValidForObjectType(eProperty.AllArcherySkills) && Util.Chance(ItemAllSkillWeight))
                                    validSkills.Add(eProperty.AllArcherySkills);
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
                                    return eProperty.MaxHealth;
                                case 1:
                                    return eProperty.Strength;
                                case 2:
                                    return eProperty.Dexterity;
                                case 3:
                                    return eProperty.Quickness;
                                case 4:
                                    return eProperty.Constitution;
                            }
                        }

                        return (eProperty)validSkills[Util.Random(0, index)];
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
                        foreach (eProperty property in AdvancedStats)
                        {
                            if (!BonusExists(property) && StatIsValidForObjectType(property) && StatIsValidForRealm(property))
                                validStats.Add(property);
                        }
                        return (eProperty)validStats[Util.Random(0, validStats.Count - 1)];
                    }
            }
            return eProperty.MaxHealth;
        }

        private bool IsCompetingSkillLine(eProperty prop)
        {
            List<eProperty> skillsToCheck = new List<eProperty>();
            if(prop == eProperty.Skill_Slashing || prop == eProperty.Skill_Thrusting || prop == eProperty.Skill_Crushing)
            {
                skillsToCheck.Add(eProperty.Skill_Slashing);
                skillsToCheck.Add(eProperty.Skill_Thrusting);
                skillsToCheck.Add(eProperty.Skill_Crushing);
            }
            if (prop == eProperty.Skill_Blades || prop == eProperty.Skill_Piercing || prop == eProperty.Skill_Blunt)
            {
                skillsToCheck.Add(eProperty.Skill_Blades);
                skillsToCheck.Add(eProperty.Skill_Piercing);
                skillsToCheck.Add(eProperty.Skill_Blunt);
            }
            if (prop == eProperty.Skill_Axe || prop == eProperty.Skill_Sword || prop == eProperty.Skill_Hammer)
            {
                skillsToCheck.Add(eProperty.Skill_Axe);
                skillsToCheck.Add(eProperty.Skill_Sword);
                skillsToCheck.Add(eProperty.Skill_Hammer);
            }

            if (prop == eProperty.Skill_Matter || prop == eProperty.Skill_Body || prop == eProperty.Skill_Spirit || prop == eProperty.Skill_Mind)
            {
                skillsToCheck.Add(eProperty.Skill_Matter);
                skillsToCheck.Add(eProperty.Skill_Body);
                skillsToCheck.Add(eProperty.Skill_Spirit);
                skillsToCheck.Add(eProperty.Skill_Mind);
            }
            if (prop == eProperty.Skill_Earth || prop == eProperty.Skill_Cold || prop == eProperty.Skill_Fire || prop == eProperty.Skill_Wind)
            {
                skillsToCheck.Add(eProperty.Skill_Earth);
                skillsToCheck.Add(eProperty.Skill_Cold);
                skillsToCheck.Add(eProperty.Skill_Fire);
                skillsToCheck.Add(eProperty.Skill_Wind);
            }
            if (prop == eProperty.Skill_DeathSight || prop == eProperty.Skill_Death_Servant || prop == eProperty.Skill_Pain_working)
            {
                skillsToCheck.Add(eProperty.Skill_DeathSight);
                skillsToCheck.Add(eProperty.Skill_Death_Servant);
                skillsToCheck.Add(eProperty.Skill_Pain_working);
            }
            if (prop == eProperty.Skill_Light || prop == eProperty.Skill_Mana || prop == eProperty.Skill_Void || prop == eProperty.Skill_Enchantments || prop == eProperty.Skill_Mentalism)
            {
                skillsToCheck.Add(eProperty.Skill_Light);
                skillsToCheck.Add(eProperty.Skill_Mana);
                skillsToCheck.Add(eProperty.Skill_Void);
                skillsToCheck.Add(eProperty.Skill_Enchantments);
                skillsToCheck.Add(eProperty.Skill_Mentalism);
            }
            if (prop == eProperty.Skill_Arboreal || prop == eProperty.Skill_Creeping || prop == eProperty.Skill_Verdant)
            {
                skillsToCheck.Add(eProperty.Skill_Arboreal);
                skillsToCheck.Add(eProperty.Skill_Creeping);
                skillsToCheck.Add(eProperty.Skill_Verdant);
            }
            if (prop == eProperty.Skill_Darkness || prop == eProperty.Skill_Suppression || prop == eProperty.Skill_Runecarving || prop == eProperty.Skill_Summoning || prop == eProperty.Skill_BoneArmy)
            {
                skillsToCheck.Add(eProperty.Skill_Darkness);
                skillsToCheck.Add(eProperty.Skill_Suppression);
                skillsToCheck.Add(eProperty.Skill_Runecarving);
                skillsToCheck.Add(eProperty.Skill_Summoning);
                skillsToCheck.Add(eProperty.Skill_BoneArmy);
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

        private eProperty GetWeightedStatForClass(eCharacterClass charClass)
        {
            if (Util.Chance(10))
                return eProperty.MaxHealth;

            int rand = Util.Random(100);
            switch (charClass)
            {
                case eCharacterClass.Armsman:
                case eCharacterClass.Mercenary:
                case eCharacterClass.Infiltrator:
                case eCharacterClass.Scout:
                case eCharacterClass.Blademaster:
                case eCharacterClass.Hero:
                case eCharacterClass.Berserker:
                case eCharacterClass.Warrior:
                case eCharacterClass.Savage:
                case eCharacterClass.Hunter:
                case eCharacterClass.Shadowblade:
                case eCharacterClass.Nightshade:
                case eCharacterClass.Ranger:
                    //25% chance of getting any useful stat
                    //for classes who do not need mana/acuity/casting stats
                    if (rand <= 25)
                        return eProperty.Strength;
                    else if (rand <= 50)
                        return eProperty.Dexterity;
                    else if (rand <= 75)
                        return eProperty.Constitution;
                    else return eProperty.Quickness;

                case eCharacterClass.Cabalist:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Wizard:
                case eCharacterClass.Necromancer:
                case eCharacterClass.Eldritch:
                case eCharacterClass.Enchanter:
                case eCharacterClass.Mentalist:
                case eCharacterClass.Animist:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    
                    //weight stats for casters towards dex, acu, con
                    //keep some 10% chance of str or quick since useful for carrying/occasional melee
                    if (rand <= 30)
                        return eProperty.Dexterity;
                    else if (rand <= 40)
                        return eProperty.Strength;
                    else if (rand <= 70)
                        return eProperty.Intelligence;
                    else if (rand <= 80)
                        return eProperty.Quickness;
                    else return eProperty.Constitution;

                case eCharacterClass.Runemaster:
                case eCharacterClass.Spiritmaster:
                case eCharacterClass.Bonedancer:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    //weight stats for casters towards dex, acu, con
                    //keep some 10% chance of str or quick since useful for carrying/occasional melee
                    if (rand <= 30)
                        return eProperty.Dexterity;
                    else if (rand <= 40)
                        return eProperty.Strength;
                    else if (rand <= 70)
                        return eProperty.Piety;
                    else if (rand <= 80)
                        return eProperty.Quickness;
                    else return eProperty.Constitution;

                case eCharacterClass.Paladin:
                    if (rand <= 25)
                        return eProperty.Strength;
                    else if (rand <= 40)
                        return eProperty.Dexterity;
                    else if (rand <= 60)
                        return eProperty.Quickness;
                    else if (rand <= 75)
                        return eProperty.Piety;
                    else return eProperty.Constitution;
                
                case eCharacterClass.Cleric:
                case eCharacterClass.Shaman:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    if (rand <= 10)
                        return eProperty.Strength;
                    else if (rand <= 40)
                        return eProperty.Dexterity;
                    else if (rand <= 50)
                        return eProperty.Quickness;
                    else if (rand <= 80)
                        return eProperty.Piety;
                    else return eProperty.Constitution;
                
                case eCharacterClass.Thane:
                case eCharacterClass.Reaver:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    if (rand <= 20)
                        return eProperty.Strength;
                    else if (rand <= 40)
                        return eProperty.Dexterity;
                    else if (rand <= 65)
                        return eProperty.Quickness;
                    else if (rand <= 80)
                        return eProperty.Piety;
                    else return eProperty.Constitution;

                case eCharacterClass.Friar:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    if (rand <= 25)
                        return eProperty.Piety;
                    else if (rand <= 50)
                        return eProperty.Dexterity;
                    else if (rand <= 75)
                        return eProperty.Constitution;
                    else return eProperty.Quickness;

                
                case eCharacterClass.Druid:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    if (rand <= 10)
                        return eProperty.Strength;
                    else if (rand <= 40)
                        return eProperty.Dexterity;
                    else if (rand <= 50)
                        return eProperty.Quickness;
                    else if (rand <= 80)
                        return eProperty.Empathy;
                    else return eProperty.Constitution;

                case eCharacterClass.Warden:
                    if (Util.Chance(10))
                        return eProperty.MaxMana;
                    if (rand <= 20)
                        return eProperty.Strength;
                    else if (rand <= 40)
                        return eProperty.Dexterity;
                    else if (rand <= 60)
                        return eProperty.Quickness;
                    else if (rand <= 80)
                        return eProperty.Empathy;
                    else return eProperty.Constitution;

                case eCharacterClass.Champion:
                case eCharacterClass.Valewalker:
                    if (Util.Chance(10))
                        return eProperty.MaxMana;
                    if (rand <= 22)
                        return eProperty.Strength;
                    else if (rand <= 44)
                        return eProperty.Dexterity;
                    else if (rand <= 66)
                        return eProperty.Quickness;
                    else if (rand <= 88)
                        return eProperty.Constitution;
                    else return eProperty.Intelligence;

                case eCharacterClass.Bard:
                case eCharacterClass.Skald:
                case eCharacterClass.Minstrel:
                    if (Util.Chance(20))
                        return eProperty.MaxMana;
                    if (rand <= 22)
                        return eProperty.Strength;
                    else if (rand <= 44)
                        return eProperty.Dexterity;
                    else if (rand <= 66)
                        return eProperty.Quickness;
                    else if (rand <= 88)
                        return eProperty.Constitution;
                    else return eProperty.Charisma;

                case eCharacterClass.Healer:
                    if (Util.Chance(15))
                        return eProperty.MaxMana;
                    if (rand <= 30)
                        return eProperty.Dexterity;
                    else if (rand <= 60)
                        return eProperty.Piety;
                    else if (rand <= 80)
                        return eProperty.Constitution;
                    else return eProperty.Strength;
            }
            return eProperty.Constitution;

        }

        private bool SkillIsValidForClass(eProperty property)
        {
            switch (charClass)
            {
                case eCharacterClass.Paladin:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Crushing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Two_Handed ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Armsman:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Crushing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Two_Handed ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Polearms ||
                        property == eProperty.Skill_Cross_Bows ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Reaver:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Crushing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Flexible_Weapon ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_SoulRending ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Mercenary:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Crushing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Dual_Wield ||
                        property == eProperty.AllDualWieldingSkills ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Cleric:
                    if (property == eProperty.Skill_Rejuvenation ||
                        property == eProperty.Skill_Enhancement ||
                        property == eProperty.Skill_Smiting ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Friar:
                    if (property == eProperty.Skill_Rejuvenation ||
                        property == eProperty.Skill_Enhancement ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Staff ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Infiltrator:
                    if (property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Envenom ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Critical_Strike ||
                        property == eProperty.Skill_Dual_Wield ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllDualWieldingSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Minstrel:
                    if (property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Instruments ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Scout:
                    if (property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Slashing ||
                        property == eProperty.Skill_Thrusting ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Long_bows ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllArcherySkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Cabalist:
                    if (property == eProperty.Skill_Matter ||
                        property == eProperty.Skill_Body ||
                        property == eProperty.Skill_Spirit ||
                        property == eProperty.Focus_Matter ||
                        property == eProperty.Focus_Body ||
                        property == eProperty.Focus_Spirit ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Sorcerer:
                    if (property == eProperty.Skill_Matter ||
                        property == eProperty.Skill_Body ||
                        property == eProperty.Skill_Mind ||
                        property == eProperty.Focus_Matter ||
                        property == eProperty.Focus_Body ||
                        property == eProperty.Focus_Mind ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Theurgist:
                    if (property == eProperty.Skill_Earth ||
                        property == eProperty.Skill_Cold ||
                        property == eProperty.Skill_Wind ||
                        property == eProperty.Focus_Earth ||
                        property == eProperty.Focus_Cold ||
                        property == eProperty.Focus_Air ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Wizard:
                    if (property == eProperty.Skill_Earth ||
                        property == eProperty.Skill_Cold ||
                        property == eProperty.Skill_Fire ||
                        property == eProperty.Focus_Earth ||
                        property == eProperty.Focus_Cold ||
                        property == eProperty.Focus_Fire ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Necromancer:
                    if (property == eProperty.Skill_DeathSight ||
                        property == eProperty.Skill_Death_Servant ||
                        property == eProperty.Skill_Pain_working ||
                        property == eProperty.Focus_Earth ||
                        property == eProperty.Focus_Cold ||
                        property == eProperty.Focus_Air ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Bard:
                    if (property == eProperty.Skill_Regrowth ||
                        property == eProperty.Skill_Nurture ||
                        property == eProperty.Skill_Music ||
                        property == eProperty.Skill_Blunt ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Druid:
                    if (property == eProperty.Skill_Regrowth ||
                        property == eProperty.Skill_Nurture ||
                        property == eProperty.Skill_Nature ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Warden:
                    if (property == eProperty.Skill_Regrowth ||
                        property == eProperty.Skill_Nurture ||
                        property == eProperty.Skill_Blunt ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Blademaster:
                    if (property == eProperty.Skill_Blunt ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Piercing ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Celtic_Dual ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllDualWieldingSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Hero:
                    if (property == eProperty.Skill_Blunt ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Piercing ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Celtic_Spear ||
                        property == eProperty.Skill_Large_Weapon ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Champion:
                    if (property == eProperty.Skill_Blunt ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Piercing ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.Skill_Valor ||
                        property == eProperty.Skill_Large_Weapon ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Eldritch:
                    if (property == eProperty.Skill_Light ||
                        property == eProperty.Skill_Mana ||
                        property == eProperty.Skill_Void ||
                        property == eProperty.Focus_Light ||
                        property == eProperty.Focus_Mana ||
                        property == eProperty.Focus_Void ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Enchanter:
                    if (property == eProperty.Skill_Light ||
                        property == eProperty.Skill_Mana ||
                        property == eProperty.Skill_Enchantments ||
                        property == eProperty.Focus_Light ||
                        property == eProperty.Focus_Mana ||
                        property == eProperty.Focus_Enchantments ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Mentalist:
                    if (property == eProperty.Skill_Light ||
                        property == eProperty.Skill_Mana ||
                        property == eProperty.Skill_Mentalism ||
                        property == eProperty.Focus_Light ||
                        property == eProperty.Focus_Mana ||
                        property == eProperty.Focus_Mentalism ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Nightshade:
                    if (property == eProperty.Skill_Envenom ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Piercing ||
                        property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Critical_Strike ||
                        property == eProperty.Skill_Celtic_Dual ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllDualWieldingSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Ranger:
                    if (property == eProperty.Skill_RecurvedBow ||
                        property == eProperty.Skill_Blades ||
                        property == eProperty.Skill_Piercing ||
                        property == eProperty.Skill_Celtic_Dual ||
                        property == eProperty.Skill_Stealth ||
                        property == eProperty.AllArcherySkills ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllDualWieldingSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Animist:
                    if (property == eProperty.Skill_Arboreal ||
                        property == eProperty.Skill_Creeping ||
                        property == eProperty.Skill_Verdant ||
                        property == eProperty.Focus_Arboreal ||
                        property == eProperty.Focus_CreepingPath ||
                        property == eProperty.Focus_Verdant ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Valewalker:
                    if (property == eProperty.Skill_Arboreal ||
                        property == eProperty.Skill_Scythe ||
                        property == eProperty.Skill_Parry ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Berserker:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Hammer ||
                        property == eProperty.Skill_Left_Axe ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Warrior:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Hammer ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Skald:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Hammer ||
                        property == eProperty.Skill_Battlesongs ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Thane:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Hammer ||
                        property == eProperty.Skill_Stormcalling ||
                        property == eProperty.Skill_Shields ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Savage:
                    if (property == eProperty.Skill_Parry ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Hammer ||
                        property == eProperty.Skill_Savagery ||
                        property == eProperty.Skill_HandToHand ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Healer:
                    if (property == eProperty.Skill_Mending ||
                        property == eProperty.Skill_Augmentation ||
                        property == eProperty.Skill_Pacification ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Shaman:
                    if (property == eProperty.Skill_Mending ||
                        property == eProperty.Skill_Augmentation ||
                        property == eProperty.Skill_Subterranean ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Hunter:
                    if (property == eProperty.Skill_BeastCraft ||
                        property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Composite ||
                        property == eProperty.Skill_Spear ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Shadowblade:
                    if (property == eProperty.Skill_Envenom ||
                        property == eProperty.Skill_Stealth ||
                        property == eProperty.Skill_Sword ||
                        property == eProperty.Skill_Axe ||
                        property == eProperty.Skill_Left_Axe ||
                        property == eProperty.Skill_Critical_Strike ||
                        property == eProperty.AllMeleeWeaponSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Runemaster:
                    if (property == eProperty.Skill_Darkness ||
                        property == eProperty.Skill_Suppression ||
                        property == eProperty.Skill_Runecarving ||
                        property == eProperty.Focus_Darkness ||
                        property == eProperty.Focus_Suppression ||
                        property == eProperty.Focus_Runecarving ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Spiritmaster:
                    if (property == eProperty.Skill_Darkness ||
                        property == eProperty.Skill_Suppression ||
                        property == eProperty.Skill_Summoning ||
                        property == eProperty.Focus_Darkness ||
                        property == eProperty.Focus_Suppression ||
                        property == eProperty.Focus_Summoning ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
                case eCharacterClass.Bonedancer:
                    if (property == eProperty.Skill_Darkness ||
                        property == eProperty.Skill_Suppression ||
                        property == eProperty.Skill_BoneArmy ||
                        property == eProperty.Focus_Darkness ||
                        property == eProperty.Focus_Suppression ||
                        property == eProperty.Focus_BoneArmy ||
                        property == eProperty.AllFocusLevels ||
                        property == eProperty.AllMagicSkills ||
                        property == eProperty.AllSkills
                        )
                        return true;
                    return false;
            }

            return false;
        }

        private bool StatIsValidForObjectType(eProperty property)
        {
            switch ((eObjectType)this.Object_Type)
            {
                case eObjectType.Magical:
                    return StatIsValidForRealm(property) && StatIsValidForClass(property);
                case eObjectType.Cloth:
                case eObjectType.Leather:
                case eObjectType.Studded:
                case eObjectType.Reinforced:
                case eObjectType.Chain:
                case eObjectType.Scale:
                case eObjectType.Plate:
                    return StatIsValidForArmor(property) && StatIsValidForClass(property);
                case eObjectType.Axe:
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.CelticSpear:
                case eObjectType.CompositeBow:
                case eObjectType.Crossbow:
                case eObjectType.CrushingWeapon:
                case eObjectType.Fired:
                case eObjectType.Flexible:
                case eObjectType.Hammer:
                case eObjectType.HandToHand:
                case eObjectType.Instrument:
                case eObjectType.LargeWeapons:
                case eObjectType.LeftAxe:
                case eObjectType.Longbow:
                case eObjectType.Piercing:
                case eObjectType.PolearmWeapon:
                case eObjectType.RecurvedBow:
                case eObjectType.Scythe:
                case eObjectType.Shield:
                case eObjectType.SlashingWeapon:
                case eObjectType.Spear:
                case eObjectType.Staff:
                case eObjectType.Sword:
                case eObjectType.ThrustWeapon:
                case eObjectType.FistWraps: //Maulers
                case eObjectType.MaulerStaff: //Maulers
                case eObjectType.TwoHandedWeapon:
                    return StatIsValidForWeapon(property) && StatIsValidForClass(property);
            }
            return true;
        }

        private bool StatIsValidForClass(eProperty property)
        {
            switch (property)
            {
                case eProperty.MaxMana: //mana isn't a thing!! >:(
                case eProperty.PowerPool:
                    if (charClass == eCharacterClass.Armsman ||
                        charClass == eCharacterClass.Mercenary ||
                        charClass == eCharacterClass.Infiltrator ||
                        charClass == eCharacterClass.Scout ||
                        charClass == eCharacterClass.Paladin ||
                        charClass == eCharacterClass.Blademaster ||
                        charClass == eCharacterClass.Hero ||
                        charClass == eCharacterClass.Nightshade ||
                        charClass == eCharacterClass.Ranger ||
                        charClass == eCharacterClass.Berserker ||
                        charClass == eCharacterClass.Warrior ||
                        charClass == eCharacterClass.Savage ||
                        charClass == eCharacterClass.Shadowblade)
                    {
                        return false;
                    }
                    return true;

                case eProperty.Acuity:
                    if (charClass == eCharacterClass.Armsman ||
                        charClass == eCharacterClass.Mercenary ||
                        charClass == eCharacterClass.Paladin ||
                        charClass == eCharacterClass.Reaver ||
                        charClass == eCharacterClass.Infiltrator ||
                        charClass == eCharacterClass.Scout ||
                        charClass == eCharacterClass.Warden ||
                        charClass == eCharacterClass.Champion ||
                        charClass == eCharacterClass.Nightshade ||
                        charClass == eCharacterClass.Ranger ||
                        charClass == eCharacterClass.Blademaster ||
                        charClass == eCharacterClass.Hero ||
                        charClass == eCharacterClass.Hunter ||
                        charClass == eCharacterClass.Berserker ||
                        charClass == eCharacterClass.Warrior ||
                        charClass == eCharacterClass.Savage ||
                        charClass == eCharacterClass.Shadowblade)
                    {
                        return false;
                    }
                    return true;
                default:
                    return true;
            }
        }

        private bool SkillIsValidForObjectType(eProperty property)
        {
            switch ((eObjectType)this.Object_Type)
            {
                case eObjectType.Magical:
                    return SkillIsValidForMagical(property);
                case eObjectType.Cloth:
                case eObjectType.Leather:
                case eObjectType.Studded:
                case eObjectType.Reinforced:
                case eObjectType.Chain:
                case eObjectType.Scale:
                case eObjectType.Plate:
                    return SkillIsValidForArmor(property);
                case eObjectType.Axe:
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.CelticSpear:
                case eObjectType.CompositeBow:
                case eObjectType.Crossbow:
                case eObjectType.CrushingWeapon:
                case eObjectType.Fired:
                case eObjectType.Flexible:
                case eObjectType.Hammer:
                case eObjectType.HandToHand:
                case eObjectType.Instrument:
                case eObjectType.LargeWeapons:
                case eObjectType.LeftAxe:
                case eObjectType.Longbow:
                case eObjectType.Piercing:
                case eObjectType.PolearmWeapon:
                case eObjectType.RecurvedBow:
                case eObjectType.Scythe:
                case eObjectType.Shield:
                case eObjectType.SlashingWeapon:
                case eObjectType.Spear:
                case eObjectType.Staff:
                case eObjectType.Sword:
                case eObjectType.ThrustWeapon:
                case eObjectType.MaulerStaff:
                case eObjectType.FistWraps:
                case eObjectType.TwoHandedWeapon:
                    return SkillIsValidForWeapon(property);
            }
            return true;
        }

        private bool SkillIsValidForMagical(eProperty property)
        {
            int level = this.Level;
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;
            eCharacterClass charClass = this.charClass;

            switch (property)
            {
                case eProperty.Skill_Augmentation:
                    {
                        if (charClass != eCharacterClass.Healer &&
                            charClass != eCharacterClass.Shaman)
                        {
                            return false;
                        }
                        else { return true; }

                    }
                case eProperty.Skill_Axe:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Battlesongs:
                    {
                        if (charClass != eCharacterClass.Skald)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Pathfinding:
                case eProperty.Skill_BeastCraft:
                    {
                        if (charClass != eCharacterClass.Hunter)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Blades:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Blunt:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }
                        return true;
                    }
                //Cloth skills
                //witchcraft is unused except as a goto target for cloth checks
                case eProperty.Skill_Arboreal:
                    if (charClass != eCharacterClass.Valewalker &&
                        charClass != eCharacterClass.Animist)
                    {
                        return false;
                    }
                    return true;
                case eProperty.Skill_Matter:
                case eProperty.Skill_Body:
                    {
                        if (charClass != eCharacterClass.Cabalist &&
                            charClass != eCharacterClass.Sorcerer)
                        {
                            return false;
                        }
                        return true;
                    }

                case eProperty.Skill_Earth:
                case eProperty.Skill_Cold:
                    {
                        if (charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard)
                        {
                            return false;
                        }
                        return true;
                    }

                case eProperty.Skill_Suppression:
                case eProperty.Skill_Darkness:
                    {
                        if (charClass != eCharacterClass.Spiritmaster &&
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Bonedancer)
                        {
                            return false;
                        }
                        return true;
                    }

                case eProperty.Skill_Light:
                case eProperty.Skill_Mana:
                    {
                        if (charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Mentalist)
                        {
                            return false;
                        }
                        return true;
                    }


                case eProperty.Skill_Mind:
                    if (charClass != eCharacterClass.Sorcerer) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Spirit:
                    if (charClass != eCharacterClass.Cabalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Wind:
                    if (charClass != eCharacterClass.Theurgist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Fire:
                    if (charClass != eCharacterClass.Wizard) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Death_Servant:
                case eProperty.Skill_DeathSight:
                case eProperty.Skill_Pain_working:
                    if (charClass != eCharacterClass.Necromancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Summoning:
                    if (charClass != eCharacterClass.Spiritmaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Runecarving:
                    if (charClass != eCharacterClass.Runemaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_BoneArmy:
                    if (charClass != eCharacterClass.Bonedancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Void:
                    if (charClass != eCharacterClass.Eldritch) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Enchantments:
                    if (charClass != eCharacterClass.Enchanter) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Mentalism:
                    if (charClass != eCharacterClass.Mentalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Creeping:
                case eProperty.Skill_Verdant:
                    if (charClass != eCharacterClass.Animist) { return false; }
                    goto case eProperty.Skill_Witchcraft;



                case eProperty.Skill_Hexing:
                case eProperty.Skill_Cursing:
                case eProperty.Skill_EtherealShriek:
                case eProperty.Skill_PhantasmalWail:
                case eProperty.Skill_SpectralForce:
                    return false;

                case eProperty.Skill_Witchcraft:
                    {
                        return true;
                    }
                case eProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != eCharacterClass.Hero)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Chants:
                    {
                        if (charClass != eCharacterClass.Paladin)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Composite:
                case eProperty.Skill_RecurvedBow:
                case eProperty.Skill_Long_bows:
                case eProperty.Skill_Archery:
                    {
                        if (charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout &&
                            charClass != eCharacterClass.Hunter)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Critical_Strike:
                case eProperty.Skill_Envenom:
                case eProperty.Skill_Dementia:
                case eProperty.Skill_Nightshade:
                case eProperty.Skill_ShadowMastery:
                case eProperty.Skill_VampiiricEmbrace:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Cross_Bows:
                    {
                        if (charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Crushing:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Dual_Wield:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Enhancement:
                    {
                        if (charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Cleric)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        return true;
                    }
                case eProperty.Skill_Hammer:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_HandToHand:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        return true;
                    }
                case eProperty.Skill_Instruments:
                    {
                        if (charClass != eCharacterClass.Minstrel) { return false; }
                        return true;
                    }
                case eProperty.Skill_Large_Weapon:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Left_Axe:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Music:
                    {
                        if (charClass != eCharacterClass.Bard) { return false; }
                        return true;
                    }
                case eProperty.Skill_Nature:
                    {
                        if (charClass != eCharacterClass.Druid) { return false; }
                        return true;
                    }
                case eProperty.Skill_Nurture:
                case eProperty.Skill_Regrowth:
                    {
                        if (charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Druid)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_OdinsWill:
                    {
                        return false;
                    }
                case eProperty.Skill_Pacification:
                    {
                        if (charClass != eCharacterClass.Healer) { return false; }
                        return true;
                    }
                case eProperty.Skill_Parry:
                    {
                        if (charClass != eCharacterClass.Berserker && //midgard
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Champion && //hibernia
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Piercing:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Polearms:
                    {
                        if (charClass != eCharacterClass.Armsman) { return false; }
                        return true;
                    }
                case eProperty.Skill_Rejuvenation:
                    {
                        if (charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Cleric)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Savagery:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        return true;
                    }
                case eProperty.Skill_Scythe:
                    {
                        if (charClass != eCharacterClass.Valewalker) { return false; }
                        return true;
                    }
                case eProperty.Skill_Shields:
                    {
                        if (charClass != eCharacterClass.Thane &&  //midgard
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Champion && //hibernia
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case eProperty.Skill_Smiting:
                    {
                        if (charClass != eCharacterClass.Cleric) { return false; }
                        return true;
                    }
                case eProperty.Skill_SoulRending:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        return true;
                    }
                case eProperty.Skill_Spear:
                    {
                        if (charClass != eCharacterClass.Hunter) { return false; }
                        return true;
                    }
                case eProperty.Skill_Staff:
                    {
                        if (charClass != eCharacterClass.Friar) { return false; }
                        return true;
                    }
                case eProperty.Skill_Stealth:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Stormcalling:
                    {
                        if (charClass != eCharacterClass.Thane) { return false; }
                        return true;
                    }
                case eProperty.Skill_Subterranean:
                    {
                        if (charClass != eCharacterClass.Shaman) { return false; }
                        return true;
                    }
                case eProperty.Skill_Sword:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Slashing:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Thrusting:
                    {

                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.Skill_Two_Handed:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Paladin)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.Skill_Valor:
                    {
                        if (charClass != eCharacterClass.Champion) { return false; }
                        return true;
                    }
                case eProperty.AllArcherySkills:
                    {
                        if (charClass != eCharacterClass.Scout &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.AllDualWieldingSkills:
                    {
                        if (charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary)
                        {
                            return false;
                        }
                        return true;
                    }
                case eProperty.AllMagicSkills:
                    {
                        if (charClass != eCharacterClass.Cabalist && //albion
                            charClass != eCharacterClass.Cleric &&
                            charClass != eCharacterClass.Necromancer &&
                            charClass != eCharacterClass.Sorcerer &&
                            charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard &&
                            charClass != eCharacterClass.Animist && //hibernia
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Mentalist &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Bonedancer && //midgard
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Spiritmaster)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != eCharacterClass.Berserker &&  //midgard
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Blademaster && //hibernia
                            charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        return true;
                    }
                case eProperty.AllSkills:
                    {
                        return true;
                    }
                case eProperty.Skill_Power_Strikes:
                case eProperty.Skill_Magnetism:
                case eProperty.Skill_MaulerStaff:
                case eProperty.Skill_Aura_Manipulation:
                case eProperty.Skill_FistWraps:
                    {
                        return false;
                    }

            }

            return false;
        }


        private bool SkillIsValidForArmor(eProperty property)
        {
            int level = this.Level;
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;
            eCharacterClass charClass = this.charClass;

            switch (property)
            {
                case eProperty.Skill_Mending:
                case eProperty.Skill_Augmentation:
                    {
                        if (charClass != eCharacterClass.Healer &&
                            charClass != eCharacterClass.Shaman)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Axe:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather || type == eObjectType.Studded)
                            return true;
                        else if (type == eObjectType.Chain && level >= 10)
                            return true;

                        return false;
                    }
                case eProperty.Skill_Battlesongs:
                    {
                        if (charClass != eCharacterClass.Skald)
                        {
                            return false;
                        }
                        if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Pathfinding:
                case eProperty.Skill_BeastCraft:
                    {
                        if (charClass != eCharacterClass.Hunter)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Blades:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }

                        if (type == eObjectType.Leather || type == eObjectType.Reinforced || type == eObjectType.Scale)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Blunt:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }

                        if (type == eObjectType.Leather && level < 10)
                            return true;
                        else if (type == eObjectType.Reinforced || type == eObjectType.Scale)
                            return true;
                        return false;
                    }
                //Cloth skills
                //witchcraft is unused except as a goto target for cloth checks
                case eProperty.Skill_Arboreal:
                    if (charClass != eCharacterClass.Valewalker &&
                        charClass != eCharacterClass.Animist)
                    {
                        return false;
                    }
                    goto case eProperty.Skill_Witchcraft;


                case eProperty.Skill_Matter:
                case eProperty.Skill_Body:
                    {
                        if (charClass != eCharacterClass.Cabalist &&
                            charClass != eCharacterClass.Sorcerer)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Earth:
                case eProperty.Skill_Cold:
                    {
                        if (charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Suppression:
                case eProperty.Skill_Darkness:
                    {
                        if (charClass != eCharacterClass.Spiritmaster &&
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Bonedancer)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Light:
                case eProperty.Skill_Mana:
                    {
                        if (charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Mentalist)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }


                case eProperty.Skill_Mind:
                    if (charClass != eCharacterClass.Sorcerer) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Spirit:
                    if (charClass != eCharacterClass.Cabalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Wind:
                    if (charClass != eCharacterClass.Theurgist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Fire:
                    if (charClass != eCharacterClass.Wizard) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Death_Servant:
                case eProperty.Skill_DeathSight:
                case eProperty.Skill_Pain_working:
                    if (charClass != eCharacterClass.Necromancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Summoning:
                    if (charClass != eCharacterClass.Spiritmaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Runecarving:
                    if (charClass != eCharacterClass.Runemaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_BoneArmy:
                    if (charClass != eCharacterClass.Bonedancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Void:
                    if (charClass != eCharacterClass.Eldritch) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Enchantments:
                    if (charClass != eCharacterClass.Enchanter) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Mentalism:
                    if (charClass != eCharacterClass.Mentalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Creeping:
                case eProperty.Skill_Verdant:
                    if (charClass != eCharacterClass.Animist) { return false; }
                    goto case eProperty.Skill_Witchcraft;



                case eProperty.Skill_Hexing:
                case eProperty.Skill_Cursing:
                case eProperty.Skill_EtherealShriek:
                case eProperty.Skill_PhantasmalWail:
                case eProperty.Skill_SpectralForce:
                    return false;

                case eProperty.Skill_Witchcraft:
                    {
                        if (property == eProperty.Skill_Witchcraft)
                        {
                            return false; //we don't want actual Witchcraft skills
                        }
                        if (type == eObjectType.Cloth)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade)
                        {
                            return false;
                        }

                        if (type == eObjectType.Leather ||
                            type == eObjectType.Reinforced)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != eCharacterClass.Hero)
                        {
                            return false;
                        }
                        if (level < 15)
                        {
                            if (type == eObjectType.Reinforced)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Chants:
                    {
                        if (charClass != eCharacterClass.Paladin)
                        {
                            return false;
                        }
                        return false;
                    }
                case eProperty.Skill_Composite:
                case eProperty.Skill_RecurvedBow:
                case eProperty.Skill_Long_bows:
                case eProperty.Skill_Archery:
                    {
                        if (charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout &&
                            charClass != eCharacterClass.Hunter)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Studded || type == eObjectType.Reinforced)
                                return true;

                            return false;
                        }
                    }
                case eProperty.Skill_Critical_Strike:
                case eProperty.Skill_Envenom:
                case eProperty.Skill_Dementia:
                case eProperty.Skill_Nightshade:
                case eProperty.Skill_ShadowMastery:
                case eProperty.Skill_VampiiricEmbrace:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Cross_Bows:
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
                case eProperty.Skill_Crushing:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver)
                        {
                            return false;
                        }
                        if (realm == eRealm.Albion && type == eObjectType.Cloth) // heretic
                            return true;

                        if (level < 15)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain || type == eObjectType.Plate)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Dual_Wield:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary)
                        {
                            return false;
                        }

                        if (level < 20)
                        {
                            if (type == eObjectType.Leather || type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Leather || type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Enhancement:
                    {
                        if (charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Cleric)
                        {
                            return false;
                        }
                        // friar
                        if (type == eObjectType.Leather)
                            return true;

                        if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        if (type == eObjectType.Cloth) // Heretic
                            return true;

                        if (level < 10)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Hammer:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_HandToHand:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        if (type == eObjectType.Studded)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Instruments:
                    {
                        if (charClass != eCharacterClass.Minstrel) { return false; }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Large_Weapon:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero)
                        {
                            return false;
                        }
                        if (level < 15)
                        {
                            if (type == eObjectType.Reinforced)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Scale)
                                return true;

                            return false;
                        }
                    }
                case eProperty.Skill_Left_Axe:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather || type == eObjectType.Studded)
                            return true;
                        break;
                    }
                case eProperty.Skill_Music:
                    {
                        if (charClass != eCharacterClass.Bard) { return false; }
                        if (level < 15)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Reinforced)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Nature:
                    {
                        if (charClass != eCharacterClass.Druid) { return false; }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == eObjectType.Reinforced)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Nurture:
                case eProperty.Skill_Regrowth:
                    {
                        if (charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Druid)
                        {
                            return false;
                        }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Reinforced || type == eObjectType.Scale)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_OdinsWill:
                    {
                        return false;
                        if (level < 10)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Pacification:
                    {
                        if (charClass != eCharacterClass.Healer) { return false; }
                        if (level < 10)
                        {
                            if (type == eObjectType.Leather)
                                return true;
                            return false;
                        }
                        else if (level < 20)
                        {
                            if (type == eObjectType.Studded)
                                return true;
                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Chain)
                                return true;
                            return false;
                        }
                    }
                case eProperty.Skill_Parry:
                    {
                        if (charClass != eCharacterClass.Berserker && //midgard
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Champion && //hibernia
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver)
                        {
                            return false;
                        }

                        if (type == eObjectType.Cloth && realm == eRealm.Hibernia && level >= 5)
                            return true;
                        else if (realm == eRealm.Hibernia && level < 2)
                            return false;
                        else if (realm == eRealm.Albion && level < 5)
                            return false;
                        else if (realm == eRealm.Albion && level < 10 && type == eObjectType.Studded)
                            return true;
                        else if (realm == eRealm.Albion && level >= 10 && (type == eObjectType.Leather || type == eObjectType.Chain || type == eObjectType.Plate))
                            return true;
                        else if (realm == eRealm.Hibernia && level < 20 && type == eObjectType.Reinforced)
                            return true;
                        else if (realm == eRealm.Hibernia && level >= 15 && type == eObjectType.Scale)
                            return true;
                        else if (realm == eRealm.Midgard && (type == eObjectType.Studded || type == eObjectType.Chain))
                            return true;

                        break;
                    }
                case eProperty.Skill_Piercing:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather || type == eObjectType.Reinforced || type == eObjectType.Scale)
                            return true;
                        return false;
                    }
                case eProperty.Skill_Polearms:
                    {
                        if (charClass != eCharacterClass.Armsman) { return false; }
                        if (level < 5 && type == eObjectType.Studded)
                        {
                            return true;
                        }
                        else if (level < 15)
                        {
                            if (type == eObjectType.Chain)
                                return true;

                            return false;
                        }
                        else
                        {
                            if (type == eObjectType.Plate)
                                return true;

                            return false;
                        }
                    }
                case eProperty.Skill_Rejuvenation:
                    {
                        if (charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Cleric)
                        {
                            return false;
                        }
                        if (type == eObjectType.Cloth)
                            return true;
                        else if (type == eObjectType.Leather)
                            return true;
                        else if (type == eObjectType.Studded && level >= 10 && level < 20)
                            return true;
                        else if (type == eObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case eProperty.Skill_Savagery:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        if (type == eObjectType.Studded)
                            return true;
                        break;
                    }
                case eProperty.Skill_Scythe:
                    {
                        if (charClass != eCharacterClass.Valewalker) { return false; }
                        if (type == eObjectType.Cloth)
                            return true;
                        break;
                    }
                case eProperty.Skill_Shields:
                    {
                        if (charClass != eCharacterClass.Thane &&  //midgard
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Champion && //hibernia
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Cloth && realm == eRealm.Albion)
                            return true;
                        else if (type == eObjectType.Studded || type == eObjectType.Chain || type == eObjectType.Reinforced || type == eObjectType.Scale || type == eObjectType.Plate)
                            return true;
                        break;
                    }
                case eProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case eProperty.Skill_Smiting:
                    {
                        if (charClass != eCharacterClass.Cleric) { return false; }
                        if (type == eObjectType.Leather && level < 10)
                            return true;
                        else if (type == eObjectType.Studded && level < 20)
                            return true;
                        else if (type == eObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case eProperty.Skill_SoulRending:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        if (type == eObjectType.Studded && level < 10)
                            return true;
                        else if (type == eObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case eProperty.Skill_Spear:
                    {
                        if (charClass != eCharacterClass.Hunter) { return false; }
                        if (type == eObjectType.Leather && level < 10)
                            return true;
                        else if (type == eObjectType.Studded)
                            return true;
                        else if (type == eObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case eProperty.Skill_Staff:
                    {
                        if (charClass != eCharacterClass.Friar) { return false; }
                        if (type == eObjectType.Leather && realm == eRealm.Albion)
                            return true;
                        break;
                    }
                case eProperty.Skill_Stealth:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather || type == eObjectType.Studded || type == eObjectType.Reinforced)
                            return true;
                        else if (realm == eRealm.Albion && level >= 20 && type == eObjectType.Chain)
                            return true;
                        break;
                    }
                case eProperty.Skill_Stormcalling:
                    {
                        if (charClass != eCharacterClass.Thane) { return false; }
                        if (type == eObjectType.Studded && level < 10)
                            return true;
                        else if (type == eObjectType.Chain && level >= 10)
                            return true;
                        break;
                    }
                case eProperty.Skill_Subterranean:
                    {
                        if (charClass != eCharacterClass.Shaman) { return false; }
                        if (type == eObjectType.Leather && level < 10)
                            return true;
                        else if (type == eObjectType.Studded && level < 20)
                            return true;
                        else if (type == eObjectType.Chain && level >= 20)
                            return true;
                        break;
                    }
                case eProperty.Skill_Sword:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        if (type == eObjectType.Studded || type == eObjectType.Chain)
                            return true;
                        break;
                    }
                case eProperty.Skill_Slashing:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        if (type == eObjectType.Leather || type == eObjectType.Studded || type == eObjectType.Chain || type == eObjectType.Plate)
                            return true;
                        break;
                    }
                case eProperty.Skill_Thrusting:
                    {

                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        if (type == eObjectType.Leather || type == eObjectType.Studded || type == eObjectType.Chain || type == eObjectType.Plate)
                            return true;
                        break;
                    }
                case eProperty.Skill_Two_Handed:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Paladin)
                        {
                            return false;
                        }
                        if (type == eObjectType.Studded && level < 10)
                            return true;
                        else if (type == eObjectType.Chain && level < 20)
                            return true;
                        else if (type == eObjectType.Plate)
                            return true;
                        break;
                    }
                case eProperty.Skill_Valor:
                    {
                        if (charClass != eCharacterClass.Champion) { return false; }
                        if (type == eObjectType.Reinforced && level < 20)
                            return true;
                        else if (type == eObjectType.Scale)
                            return true;
                        break;
                    }
                case eProperty.AllArcherySkills:
                    {
                        if (charClass != eCharacterClass.Scout &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        if (type == eObjectType.Leather && level < 10)
                            return true;
                        else if (level >= 10 && (type == eObjectType.Reinforced || type == eObjectType.Studded))
                            return true;

                        break;
                    }
                case eProperty.AllDualWieldingSkills:
                    {
                        if (charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary)
                        {
                            return false;
                        }
                        //Dualwielders are always above level 4 and can wear better than cloth from the start.
                        if (type == eObjectType.Cloth)
                            return false;
                        //mercs are the only dualwielder who can wear chain
                        else if (realm == eRealm.Albion && type == eObjectType.Studded && level < 10)
                            return true;
                        else if (realm == eRealm.Albion && type == eObjectType.Chain)
                            return true;
                        //all assassins wear leather, blademasters and zerks wear studded.
                        else if (type == eObjectType.Leather || type == eObjectType.Reinforced || (type == eObjectType.Studded && realm == eRealm.Midgard))
                            return true;
                        break;
                    }
                case eProperty.AllMagicSkills:
                    {
                        if (charClass != eCharacterClass.Cabalist && //albion
                            charClass != eCharacterClass.Cleric &&
                            charClass != eCharacterClass.Necromancer &&
                            charClass != eCharacterClass.Sorcerer &&
                            charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard &&
                            charClass != eCharacterClass.Animist && //hibernia
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Mentalist &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Bonedancer && //midgard
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Spiritmaster)
                        {
                            return false;
                        }

                        // not for scouts
                        if (realm == eRealm.Albion && type == eObjectType.Studded && level >= 20)
                            return false;
                        // Paladins can't use + magic skills
                        if (realm == eRealm.Albion && type == eObjectType.Plate)
                            return false;

                        return true;
                    }
                case eProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != eCharacterClass.Berserker &&  //midgard
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Blademaster && //hibernia
                            charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        if (realm == eRealm.Midgard && type == eObjectType.Cloth)
                            return false;
                        else if (level >= 5)
                            return true;

                        break;
                    }
                case eProperty.AllSkills:
                    {
                        return true;
                    }
                case eProperty.Skill_Power_Strikes:
                case eProperty.Skill_Magnetism:
                case eProperty.Skill_MaulerStaff:
                case eProperty.Skill_Aura_Manipulation:
                case eProperty.Skill_FistWraps:
                    {
                        return false;
                        //Maulers
                        if (type == eObjectType.Leather) //Maulers can only wear leather.
                            return true;

                        break;
                    }

            }

            return false;
        }

        private bool SkillIsValidForWeapon(eProperty property)
        {
            int level = this.Level;
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;

            switch (property)
            {
                case eProperty.Skill_SpectralForce:
                case eProperty.Skill_EtherealShriek:
                case eProperty.Skill_PhantasmalWail:
                case eProperty.Skill_Hexing:
                case eProperty.Skill_Cursing:
                    return false;

                case eProperty.Skill_Arboreal:
                    if (charClass != eCharacterClass.Valewalker &&
                        charClass != eCharacterClass.Animist)
                    {
                        return false;
                    }
                    goto case eProperty.Skill_Witchcraft;


                case eProperty.Skill_Matter:
                case eProperty.Skill_Body:
                    {
                        if (charClass != eCharacterClass.Cabalist &&
                            charClass != eCharacterClass.Sorcerer)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Earth:
                case eProperty.Skill_Cold:
                    {
                        if (charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Suppression:
                case eProperty.Skill_Darkness:
                    {
                        if (charClass != eCharacterClass.Spiritmaster &&
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Bonedancer)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }

                case eProperty.Skill_Light:
                case eProperty.Skill_Mana:
                    {
                        if (charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Mentalist)
                        {
                            return false;
                        }
                        goto case eProperty.Skill_Witchcraft;
                    }


                case eProperty.Skill_Mind:
                    if (charClass != eCharacterClass.Sorcerer) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Spirit:
                    if (charClass != eCharacterClass.Cabalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Wind:
                    if (charClass != eCharacterClass.Theurgist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Fire:
                    if (charClass != eCharacterClass.Wizard) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Death_Servant:
                case eProperty.Skill_DeathSight:
                case eProperty.Skill_Pain_working:
                    if (charClass != eCharacterClass.Necromancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Summoning:
                    if (charClass != eCharacterClass.Spiritmaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Runecarving:
                    if (charClass != eCharacterClass.Runemaster) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_BoneArmy:
                    if (charClass != eCharacterClass.Bonedancer) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Void:
                    if (charClass != eCharacterClass.Eldritch) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Enchantments:
                    if (charClass != eCharacterClass.Enchanter) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Mentalism:
                    if (charClass != eCharacterClass.Mentalist) { return false; }
                    goto case eProperty.Skill_Witchcraft;
                case eProperty.Skill_Creeping:
                case eProperty.Skill_Verdant:
                    if (charClass != eCharacterClass.Animist) { return false; }
                    goto case eProperty.Skill_Witchcraft;

                case eProperty.Skill_Witchcraft:
                    {
                        if (property == eProperty.Skill_Witchcraft)
                        {
                            return false; //we don't want actual Witchcraft skills
                        }
                        if (type == eObjectType.Staff && this.Description != "friar")
                            return true;
                        break;
                    }
                //healer things
                case eProperty.Skill_Smiting:
                    {
                        if (((type == eObjectType.Shield && this.Type_Damage < 3) || type == eObjectType.CrushingWeapon)
                            && charClass == eCharacterClass.Cleric)
                            return true;
                        break;
                    }
                case eProperty.Skill_Enhancement:
                case eProperty.Skill_Rejuvenation:
                    {
                        if (realm != eRealm.Albion || (charClass != eCharacterClass.Cleric && charClass != eCharacterClass.Friar)) { return false; }
                        if ((type == eObjectType.Staff && this.Description == "friar") || (type == eObjectType.Shield && this.Type_Damage < 3) || type == eObjectType.CrushingWeapon)
                            return true;
                        break;
                    }
                case eProperty.Skill_Augmentation:
                case eProperty.Skill_Mending:
                    {
                        if (realm != eRealm.Midgard || (charClass != eCharacterClass.Healer && charClass != eCharacterClass.Shaman)) { return false; }
                        if ((type == eObjectType.Shield && this.Type_Damage < 2) || type == eObjectType.Hammer)
                        {
                            return true;
                        }
                        break;
                    }
                case eProperty.Skill_Subterranean:
                    {
                        if (realm != eRealm.Midgard || charClass != eCharacterClass.Shaman) { return false; }
                        if ((type == eObjectType.Shield && this.Type_Damage < 2) || type == eObjectType.Hammer)
                        {
                            return true;
                        }
                        break;
                    }
                case eProperty.Skill_Nurture:
                case eProperty.Skill_Nature:
                case eProperty.Skill_Regrowth:
                    {
                        if (realm != eRealm.Hibernia) { return false; }
                        if (type == eObjectType.Blunt || type == eObjectType.Blades || (type == eObjectType.Shield && this.Type_Damage < 2))
                            return true;
                        break;
                    }
                //archery things
                case eProperty.Skill_Archery:
                    if (type == eObjectType.CompositeBow || type == eObjectType.RecurvedBow || type == eObjectType.Longbow)
                        return true;
                    break;
                case eProperty.Skill_Composite:
                    {
                        if (type == eObjectType.CompositeBow)
                            return true;
                        break;
                    }
                case eProperty.Skill_RecurvedBow:
                    {
                        if (type == eObjectType.RecurvedBow)
                            return true;
                        break;
                    }
                case eProperty.Skill_Long_bows:
                    {
                        if (type == eObjectType.Longbow)
                            return true;
                        break;
                    }
                //other specifics
                case eProperty.Skill_Staff:
                    {
                        if (type == eObjectType.Staff && this.Description == "friar")
                            return true;
                        break;
                    }
                case eProperty.Skill_Axe:
                    {
                        if (realm != eRealm.Midgard) { return false; }
                        if (type == eObjectType.Axe || type == eObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case eProperty.Skill_Battlesongs:
                    {
                        if (charClass != eCharacterClass.Skald) { return false; }
                        if (type == eObjectType.Sword || type == eObjectType.Axe || type == eObjectType.Hammer || (type == eObjectType.Shield && this.Type_Damage < 3))
                            return true;
                        break;
                    }
                case eProperty.Skill_BeastCraft:
                    {
                        if (charClass != eCharacterClass.Hunter) { return false; }
                        if (type == eObjectType.Spear)
                            return true;
                        break;
                    }
                case eProperty.Skill_Blades:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }

                        if (type == eObjectType.Blades)
                            return true;
                        break;
                    }
                case eProperty.Skill_Blunt:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Bard &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Warden)
                        {
                            return false;
                        }

                        if (type == eObjectType.Blunt)
                            return true;
                        break;
                    }
                case eProperty.Skill_Celtic_Dual:
                    {
                        if (charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster)
                        {
                            return false;
                        }
                        if (type == eObjectType.Piercing || type == eObjectType.Blades || type == eObjectType.Blunt)
                            return true;
                        break;
                    }
                case eProperty.Skill_Celtic_Spear:
                    {
                        if (charClass != eCharacterClass.Hero) { return false; }
                        if (type == eObjectType.CelticSpear)
                            return true;
                        break;
                    }
                case eProperty.Skill_Chants:
                    {
                        return false;
                    }
                case eProperty.Skill_Critical_Strike:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == eObjectType.Piercing || type == eObjectType.SlashingWeapon || type == eObjectType.ThrustWeapon || type == eObjectType.Blades || type == eObjectType.Sword || type == eObjectType.Axe || type == eObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case eProperty.Skill_Cross_Bows:
                    {
                        if (type == eObjectType.Crossbow)
                            return true;
                        break;
                    }
                case eProperty.Skill_Crushing:
                    {
                        if (realm != eRealm.Albion || type == eObjectType.Flexible) { return false; }
                        if (type == eObjectType.CrushingWeapon ||
                            ((type == eObjectType.TwoHandedWeapon || type == eObjectType.PolearmWeapon) && this.Type_Damage == (int)eDamageType.Crush))
                            return true;
                        break;
                    }
                case eProperty.Skill_Dual_Wield:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Berserker)
                        {
                            return false;
                        }

                        if (type == eObjectType.SlashingWeapon || type == eObjectType.ThrustWeapon || type == eObjectType.CrushingWeapon)
                            return true;
                        break;
                    }
                case eProperty.Skill_Envenom:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (type == eObjectType.SlashingWeapon || type == eObjectType.ThrustWeapon)
                            return true;
                        break;
                    }
                case eProperty.Skill_Flexible_Weapon:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        if (type == eObjectType.Flexible || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_Hammer:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        if (type == eObjectType.Hammer)
                            return true;
                        break;
                    }
                case eProperty.Skill_HandToHand:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        if (type == eObjectType.HandToHand)
                            return true;
                        break;
                    }
                case eProperty.Skill_Instruments:
                    {
                        if (charClass != eCharacterClass.Minstrel) { return false; }
                        if (type == eObjectType.Instrument)
                            return true;
                        break;
                    }
                case eProperty.Skill_Large_Weapon:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero)
                        {
                            return false;
                        }
                        if (type == eObjectType.LargeWeapons)
                            return true;
                        break;
                    }
                case eProperty.Skill_Left_Axe:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Shadowblade)
                        {
                            return false;
                        }
                        if (this.Item_Type == Slot.TWOHAND) return false;
                        if (type == eObjectType.Axe || type == eObjectType.LeftAxe)
                            return true;
                        break;
                    }
                case eProperty.Skill_Music:
                    {
                        if (charClass != eCharacterClass.Bard)
                        {
                            return false;
                        }
                        if (type == eObjectType.Blades || type == eObjectType.Blunt || (type == eObjectType.Shield && this.Type_Damage == 1) || type == eObjectType.Instrument)
                            return true;
                        break;
                    }
                case eProperty.Skill_Nightshade:
                    {
                        if (charClass != eCharacterClass.Nightshade)
                        {
                            return false;
                        }
                        if (type == eObjectType.Blades || type == eObjectType.Piercing || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_OdinsWill:
                    {
                        return false;
                        if (type == eObjectType.Sword || type == eObjectType.Spear || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_Parry:
                    if (charClass != eCharacterClass.Berserker &&  //midgard
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Blademaster && //hibernia
                            charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver)
                    {
                        return false;
                    }
                    return true;
                case eProperty.Skill_Pathfinding:
                    {
                        if (charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        if (type == eObjectType.RecurvedBow || type == eObjectType.Piercing || type == eObjectType.Blades)
                            return true;
                        break;
                    }
                case eProperty.Skill_Piercing:
                    {
                        if (charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        if (type == eObjectType.Piercing)
                            return true;
                        break;
                    }
                case eProperty.Skill_Polearms:
                    {
                        if (charClass != eCharacterClass.Armsman) { return false; }
                        if (type == eObjectType.PolearmWeapon)
                            return true;
                        break;
                    }
                case eProperty.Skill_Savagery:
                    {
                        if (charClass != eCharacterClass.Savage) { return false; }
                        if (type == eObjectType.Sword || type == eObjectType.Axe || type == eObjectType.Hammer || type == eObjectType.HandToHand)
                            return true;
                        break;
                    }
                case eProperty.Skill_Scythe:
                    {
                        if (charClass != eCharacterClass.Valewalker) { return false; }
                        if (type == eObjectType.Scythe)
                            return true;
                        break;
                    }

                case eProperty.Skill_VampiiricEmbrace:
                case eProperty.Skill_ShadowMastery:
                    {
                        return false;
                        if (type == eObjectType.Piercing)
                            return true;
                        break;
                    }
                case eProperty.Skill_Shields:
                    {
                        if (charClass != eCharacterClass.Thane &&  //midgard
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Champion && //hibernia
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_ShortBow:
                    {
                        return false;
                    }
                case eProperty.Skill_Slashing:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }

                        if (type == eObjectType.Flexible)
                            return false;
                        if (type == eObjectType.SlashingWeapon ||
                            ((type == eObjectType.TwoHandedWeapon || type == eObjectType.PolearmWeapon) && this.Type_Damage == (int)eDamageType.Slash))
                            return true;
                        break;
                    }
                case eProperty.Skill_SoulRending:
                    {
                        if (charClass != eCharacterClass.Reaver) { return false; }
                        if (type == eObjectType.SlashingWeapon || type == eObjectType.CrushingWeapon || type == eObjectType.ThrustWeapon || type == eObjectType.Flexible || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_Spear:
                    {
                        if (charClass != eCharacterClass.Hunter) { return false; }
                        if (type == eObjectType.Spear)
                            return true;
                        break;
                    }
                case eProperty.Skill_Stealth:
                    {
                        if (charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Longbow || type == eObjectType.RecurvedBow || type == eObjectType.CompositeBow || (realm == eRealm.Albion && type == eObjectType.Shield && this.Type_Damage == 1) || type == eObjectType.Spear || type == eObjectType.Sword || type == eObjectType.Axe || type == eObjectType.LeftAxe || type == eObjectType.SlashingWeapon || type == eObjectType.ThrustWeapon || type == eObjectType.Piercing || type == eObjectType.Blades || (realm == eRealm.Albion && type == eObjectType.Instrument))
                            return true;
                        break;
                    }
                case eProperty.Skill_Stormcalling:
                    {
                        if (charClass != eCharacterClass.Thane) { return false; }
                        if (type == eObjectType.Sword || type == eObjectType.Axe || type == eObjectType.Hammer || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_Sword:
                    {
                        if (charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior)
                        {
                            return false;
                        }
                        if (type == eObjectType.Sword)
                            return true;
                        break;
                    }
                case eProperty.Skill_Thrusting:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Flexible)
                            return false;
                        if (type == eObjectType.ThrustWeapon ||
                            ((type == eObjectType.TwoHandedWeapon || type == eObjectType.PolearmWeapon) && this.Type_Damage == (int)eDamageType.Thrust))
                            return true;
                        break;
                    }
                case eProperty.Skill_Two_Handed:
                    {
                        if (charClass != eCharacterClass.Armsman &&
                            charClass != eCharacterClass.Paladin)
                        {
                            return false;
                        }
                        if (type == eObjectType.TwoHandedWeapon)
                            return true;
                        break;
                    }
                case eProperty.Skill_Valor:
                    {
                        if (charClass != eCharacterClass.Champion) { return false; }
                        if (type == eObjectType.Blades || type == eObjectType.Piercing || type == eObjectType.Blunt || type == eObjectType.LargeWeapons || type == eObjectType.Shield)
                            return true;
                        break;
                    }
                case eProperty.Skill_Thrown_Weapons:
                    {
                        return false;
                    }
                case eProperty.Skill_Pacification:
                    {
                        if (charClass != eCharacterClass.Healer) { return false; }
                        if (type == eObjectType.Hammer)
                            return true;
                        break;
                    }
                case eProperty.Skill_Dementia:
                    {
                        return false;
                        if (type == eObjectType.Piercing)
                            return true;
                        break;
                    }
                case eProperty.AllArcherySkills:
                    {
                        if (charClass != eCharacterClass.Scout &&
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Ranger)
                        {
                            return false;
                        }
                        if (type == eObjectType.CompositeBow || type == eObjectType.Longbow || type == eObjectType.RecurvedBow)
                            return true;
                        break;
                    }
                case eProperty.AllDualWieldingSkills:
                    {
                        if (charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Berserker &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Blademaster &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary)
                        {
                            return false;
                        }
                        if (type == eObjectType.Axe || type == eObjectType.Sword || type == eObjectType.Hammer || type == eObjectType.LeftAxe || type == eObjectType.SlashingWeapon || type == eObjectType.CrushingWeapon || type == eObjectType.ThrustWeapon || type == eObjectType.Piercing || type == eObjectType.Blades || type == eObjectType.Blunt)
                            return true;
                        break;
                    }
                case eProperty.AllMagicSkills:
                    {
                        if (charClass != eCharacterClass.Cabalist && //albion
                            charClass != eCharacterClass.Cleric &&
                            charClass != eCharacterClass.Necromancer &&
                            charClass != eCharacterClass.Sorcerer &&
                            charClass != eCharacterClass.Theurgist &&
                            charClass != eCharacterClass.Wizard &&
                            charClass != eCharacterClass.Animist && //hibernia
                            charClass != eCharacterClass.Eldritch &&
                            charClass != eCharacterClass.Enchanter &&
                            charClass != eCharacterClass.Mentalist &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Bonedancer && //midgard
                            charClass != eCharacterClass.Runemaster &&
                            charClass != eCharacterClass.Spiritmaster)
                        {
                            return false;
                        }
                        //scouts, armsmen, paladins, mercs, blademasters, heroes, zerks, warriors do not need this.
                        if (type == eObjectType.Longbow || type == eObjectType.CelticSpear || type == eObjectType.PolearmWeapon || type == eObjectType.TwoHandedWeapon || type == eObjectType.Crossbow || (type == eObjectType.Shield && this.Type_Damage > 2))
                            return false;
                        else
                            return true;
                    }
                case eProperty.AllMeleeWeaponSkills:
                    {
                        if (charClass != eCharacterClass.Berserker &&  //midgard
                            charClass != eCharacterClass.Hunter &&
                            charClass != eCharacterClass.Savage &&
                            charClass != eCharacterClass.Shadowblade &&
                            charClass != eCharacterClass.Skald &&
                            charClass != eCharacterClass.Thane &&
                            charClass != eCharacterClass.Warrior &&
                            charClass != eCharacterClass.Blademaster && //hibernia
                            charClass != eCharacterClass.Champion &&
                            charClass != eCharacterClass.Hero &&
                            charClass != eCharacterClass.Nightshade &&
                            charClass != eCharacterClass.Ranger &&
                            charClass != eCharacterClass.Valewalker &&
                            charClass != eCharacterClass.Warden &&
                            charClass != eCharacterClass.Armsman && //albion
                            charClass != eCharacterClass.Friar &&
                            charClass != eCharacterClass.Infiltrator &&
                            charClass != eCharacterClass.Mercenary &&
                            charClass != eCharacterClass.Minstrel &&
                            charClass != eCharacterClass.Paladin &&
                            charClass != eCharacterClass.Reaver &&
                            charClass != eCharacterClass.Scout)
                        {
                            return false;
                        }
                        if (type == eObjectType.Staff && realm != eRealm.Albion)
                            return false;
                        else if (type == eObjectType.Staff && this.Description != "friar") // do not add if caster staff
                            return false;
                        else if (type == eObjectType.Longbow || type == eObjectType.CompositeBow || type == eObjectType.RecurvedBow || type == eObjectType.Crossbow || type == eObjectType.Fired || type == eObjectType.Instrument)
                            return false;
                        else
                            return true;
                    }
                case eProperty.Skill_Aura_Manipulation: //Maulers
                    {
                        return false;
                        if (type == eObjectType.MaulerStaff || type == eObjectType.FistWraps)
                            return true;
                        break;
                    }
                case eProperty.Skill_Magnetism: //Maulers
                    {
                        return false;
                        if (type == eObjectType.FistWraps || type == eObjectType.MaulerStaff)
                            return true;
                        break;
                    }
                case eProperty.Skill_MaulerStaff: //Maulers
                    {
                        return false;
                        if (type == eObjectType.MaulerStaff)
                            return true;
                        break;
                    }
                case eProperty.Skill_Power_Strikes: //Maulers
                    {
                        return false;
                        if (type == eObjectType.MaulerStaff || type == eObjectType.FistWraps)
                            return true;
                        break;
                    }
                case eProperty.Skill_FistWraps: //Maulers
                    {
                        return false;
                        if (type == eObjectType.FistWraps)
                            return true;
                        break;
                    }
            }
            return false;
        }

        private bool StatIsValidForRealm(eProperty property)
        {
            switch (property)
            {
                case eProperty.Piety:
                case eProperty.PieCapBonus:
                    {
                        if (this.Realm == (int)eRealm.Hibernia)
                            return false;
                        break;
                    }
                case eProperty.Empathy:
                case eProperty.EmpCapBonus:
                    {
                        if (this.Realm == (int)eRealm.Midgard || this.Realm == (int)eRealm.Albion)
                            return false;
                        break;
                    }
                case eProperty.Intelligence:
                case eProperty.IntCapBonus:
                    {
                        if (this.Realm == (int)eRealm.Midgard)
                            return false;
                        break;
                    }
            }
            return true;
        }

        private bool StatIsValidForArmor(eProperty property)
        {
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;

            switch (property)
            {
                case eProperty.Intelligence:
                case eProperty.IntCapBonus:
                    {
                        if (realm == eRealm.Midgard)
                            return false;

                        if (realm == eRealm.Hibernia && this.Level < 20 && type != eObjectType.Reinforced && type != eObjectType.Cloth)
                            return false;

                        if (realm == eRealm.Hibernia && this.Level >= 20 && type != eObjectType.Scale && type != eObjectType.Cloth)
                            return false;

                        if (type != eObjectType.Cloth)
                            return false;

                        break;
                    }
                case eProperty.Acuity:
                case eProperty.AcuCapBonus:
                case eProperty.PowerPool:
                case eProperty.PowerPoolCapBonus:
                    {
                        if (realm == eRealm.Albion && this.Level >= 20 && type == eObjectType.Studded)
                            return false;

                        if (realm == eRealm.Midgard && this.Level >= 10 && type == eObjectType.Leather)
                            return false;

                        if (realm == eRealm.Midgard && this.Level >= 20 && type == eObjectType.Studded)
                            return false;

                        break;
                    }
                case eProperty.Piety:
                case eProperty.PieCapBonus:
                    {
                        if (realm == eRealm.Albion)
                        {
                            if (type == eObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == eObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == eObjectType.Chain && this.Level < 10)
                                return false;
                        }
                        else if (realm == eRealm.Midgard)
                        {
                            if (type == eObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == eObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == eObjectType.Chain && this.Level < 10)
                                return false;
                        }
                        else if (realm == eRealm.Hibernia)
                        {
                            return false;
                        }
                        break;
                    }
                case eProperty.Charisma:
                case eProperty.ChaCapBonus:
                    {
                        if (realm == eRealm.Albion)
                        {
                            if (type == eObjectType.Leather && this.Level >= 10)
                                return false;

                            if (type == eObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == eObjectType.Chain && this.Level < 20)
                                return false;
                        }
                        if (realm == eRealm.Midgard)
                        {
                            if (type == eObjectType.Studded && this.Level >= 20)
                                return false;

                            if (type == eObjectType.Chain && this.Level < 20)
                                return false;
                        }
                        else if (realm == eRealm.Hibernia)
                        {
                            if (type == eObjectType.Leather && this.Level >= 15)
                                return false;

                            if (type == eObjectType.Reinforced && this.Level < 15)
                                return false;
                        }
                        break;
                    }
                case eProperty.Empathy:
                case eProperty.EmpCapBonus:
                    {
                        if (realm != eRealm.Hibernia)
                            return false;

                        if (type == eObjectType.Leather && this.Level >= 10)
                            return false;

                        if (type == eObjectType.Reinforced && this.Level >= 20)
                            return false;

                        if (type == eObjectType.Scale && this.Level < 20)
                            return false;

                        break;
                    }
            }
            return true;
        }

        private bool StatIsValidForWeapon(eProperty property)
        {
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;

            switch (type)
            {
                case eObjectType.Staff:
                    {
                        if ((property == eProperty.Piety || property == eProperty.PieCapBonus) && realm == eRealm.Hibernia)
                            return false;
                        else if ((property == eProperty.Piety || property == eProperty.PieCapBonus) && realm == eRealm.Albion && this.Description != "friar")
                            return false; // caster staff
                        else if (property == eProperty.Charisma || property == eProperty.Empathy || property == eProperty.ChaCapBonus || property == eProperty.EmpCapBonus)
                            return false;
                        else if ((property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.AcuCapBonus) && this.Description == "friar")
                            return false;
                        break;
                    }

                case eObjectType.Shield:
                    {
                        if ((realm == eRealm.Albion || realm == eRealm.Midgard) && (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus))
                            return false;
                        else if (realm == eRealm.Hibernia && (property == eProperty.Piety || property == eProperty.PieCapBonus))
                            return false;
                        else if ((realm == eRealm.Albion || realm == eRealm.Hibernia) && this.Type_Damage > 1 && (property == eProperty.Charisma || property == eProperty.ChaCapBonus))
                            return false;
                        else if (realm == eRealm.Midgard && this.Type_Damage > 2 && (property == eProperty.Charisma || property == eProperty.ChaCapBonus))
                            return false;
                        else if (this.Type_Damage > 2 && property == eProperty.MaxMana)
                            return false;

                        break;
                    }
                case eObjectType.Blades:
                case eObjectType.Blunt:
                    {
                        if (property == eProperty.Piety || property == eProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case eObjectType.LargeWeapons:
                case eObjectType.Piercing:
                case eObjectType.Scythe:
                    {
                        if (property == eProperty.Piety || property == eProperty.Empathy || property == eProperty.Charisma)
                            return false;
                        break;
                    }
                case eObjectType.CrushingWeapon:
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.Charisma || property == eProperty.ChaCapBonus)
                            return false;
                        break;
                    }
                case eObjectType.SlashingWeapon:
                case eObjectType.ThrustWeapon:
                case eObjectType.Hammer:
                case eObjectType.Sword:
                case eObjectType.Axe:
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.AcuCapBonus || property == eProperty.Acuity)
                            return false;
                        break;
                    }
                case eObjectType.TwoHandedWeapon:
                case eObjectType.Flexible:
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.Charisma || property == eProperty.ChaCapBonus)
                            return false;
                        break;
                    }
                case eObjectType.RecurvedBow:
                case eObjectType.CompositeBow:
                case eObjectType.Longbow:
                case eObjectType.Crossbow:
                case eObjectType.Fired:
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.Charisma || property == eProperty.ChaCapBonus ||
                            property == eProperty.MaxMana || property == eProperty.PowerPool || property == eProperty.PowerPoolCapBonus || property == eProperty.AcuCapBonus || property == eProperty.Acuity || property == eProperty.Piety || property == eProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case eObjectType.Spear:
                case eObjectType.CelticSpear:
                case eObjectType.LeftAxe:
                case eObjectType.PolearmWeapon:
                case eObjectType.HandToHand:
                case eObjectType.FistWraps: //Maulers
                case eObjectType.MaulerStaff: //Maulers
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.Charisma || property == eProperty.ChaCapBonus ||
                            property == eProperty.MaxMana || property == eProperty.PowerPool || property == eProperty.PowerPoolCapBonus || property == eProperty.AcuCapBonus || property == eProperty.Acuity || property == eProperty.Piety || property == eProperty.PieCapBonus)
                            return false;
                        break;
                    }
                case eObjectType.Instrument:
                    {
                        if (property == eProperty.Intelligence || property == eProperty.IntCapBonus || property == eProperty.Empathy || property == eProperty.EmpCapBonus || property == eProperty.Piety || property == eProperty.PieCapBonus)
                            return false;
                        break;
                    }
            }
            return true;
        }

        private void WriteBonus(eProperty property, int amount)
        {
            if (property == eProperty.AllFocusLevels)
            {
                amount = Math.Min(50, amount);
            }

            if (this.Bonus1 == 0)
            {
                this.Bonus1 = amount;
                this.Bonus1Type = (int)property;

                if (property == eProperty.AllFocusLevels)
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

        private bool BonusExists(eProperty property)
        {
            if (this.Bonus1Type == (int)property ||
                this.Bonus2Type == (int)property ||
                this.Bonus3Type == (int)property ||
                this.Bonus4Type == (int)property ||
                this.Bonus5Type == (int)property)
                return true;

            return false;
        }

        private int GetBonusAmount(eBonusType type, eProperty property)
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
                        if (property == eProperty.AllSkills ||
                            property == eProperty.AllMagicSkills ||
                            property == eProperty.AllDualWieldingSkills ||
                            property == eProperty.AllMeleeWeaponSkills ||
                            property == eProperty.AllArcherySkills)
                            max = (int)Math.Ceiling((double)max / 2.0);
                        return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                    }
                case eBonusType.Stat:
                    {
                        if (property == eProperty.MaxHealth)
                        {
                            int max = (int)Math.Ceiling(((double)this.Level * 4.0) / 4);
                            return Util.Random((int)Math.Ceiling((double)max / 2.0), max);
                        }
                        else if (property == eProperty.MaxMana)
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
                        if (property == eProperty.MaxHealthCapBonus)
                            return Util.Random(5, 25); // cap is 400
                        else if (property == eProperty.PowerPoolCapBonus)
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

            //write name of item based off of capped lines
            int utiLine = GetHighestUtilitySingleLine();
            eProperty bonus = GetPropertyFromBonusLine(utiLine);
            WriteMagicalName(bonus);
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

            return Bonus;
        }

        private int IncreaseSingleLineUtility(int BonusType, int Bonus)
        {
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


        private static eObjectType GenerateObjectType(eRealm realm, eCharacterClass charClass, byte level)
        {
            eGenerateType type = GetObjectTypeByWeight(level);

            switch ((eRealm)realm)
            {
                case eRealm.Albion:
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
                            case eGenerateType.Magical: return eObjectType.Magical;
                        }
                        break;
                    }
                case eRealm.Midgard:
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
                            case eGenerateType.Magical: return eObjectType.Magical;
                        }
                        break;
                    }
                case eRealm.Hibernia:
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
                            case eGenerateType.Magical: return eObjectType.Magical;
                        }
                        break;
                    }
            }
            return eObjectType.GenericItem;
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

        public static eObjectType GetAlbionWeapon(eCharacterClass charClass)
        {
            List<eObjectType> weaponTypes = new List<eObjectType>();
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
                case eCharacterClass.Cabalist:
                case eCharacterClass.Necromancer:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Wizard:
                    weaponTypes.Add(eObjectType.Staff);
                    break;
                case eCharacterClass.Friar:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Armsman:
                    weaponTypes.Add(eObjectType.PolearmWeapon);
                    weaponTypes.Add(eObjectType.PolearmWeapon);
                    weaponTypes.Add(eObjectType.PolearmWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.Crossbow);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Paladin:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.TwoHandedWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Reaver:
                    weaponTypes.Add(eObjectType.Flexible);
                    weaponTypes.Add(eObjectType.Flexible);
                    weaponTypes.Add(eObjectType.Flexible);
                    weaponTypes.Add(eObjectType.Flexible);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Minstrel:
                    weaponTypes.Add(eObjectType.Instrument);
                    weaponTypes.Add(eObjectType.Instrument);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Infiltrator:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Crossbow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Scout:
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.Longbow);
                    weaponTypes.Add(eObjectType.Longbow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Mercenary:
                    weaponTypes.Add(eObjectType.Fired); //shortbow
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.SlashingWeapon);
                    weaponTypes.Add(eObjectType.ThrustWeapon);
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Cleric:
                    weaponTypes.Add(eObjectType.CrushingWeapon);
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                default:
                    return eObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<eObjectType> outputList = new List<eObjectType>(weaponTypes);

            //add duplicate values
            foreach (eObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);

            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static eObjectType GetAlbionArmorType(eCharacterClass charClass, byte level)
        {

            switch (charClass)
            {
                //staff classes
                case eCharacterClass.Cabalist:
                case eCharacterClass.Necromancer:
                case eCharacterClass.Sorcerer:
                case eCharacterClass.Theurgist:
                case eCharacterClass.Wizard:
                    return eObjectType.Cloth;

                case eCharacterClass.Friar:
                case eCharacterClass.Infiltrator:
                    return eObjectType.Leather;

                case eCharacterClass.Armsman:
                    if (level < 5)
                    {
                        return eObjectType.Studded;
                    }
                    else if (level < 15)
                    {
                        return eObjectType.Chain;
                    }
                    else
                    {
                        return eObjectType.Plate;
                    }

                case eCharacterClass.Paladin:
                    if (level < 10)
                    {
                        return eObjectType.Studded;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Chain;
                    }
                    else
                    {
                        return eObjectType.Plate;
                    }

                case eCharacterClass.Reaver:
                case eCharacterClass.Mercenary:
                    if (level < 10)
                    {
                        return eObjectType.Studded;
                    }
                    else
                    {
                        return eObjectType.Chain;
                    }

                case eCharacterClass.Minstrel:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Studded;
                    }
                    else
                    {
                        return eObjectType.Chain;
                    }

                case eCharacterClass.Scout:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else { return eObjectType.Studded; }

                case eCharacterClass.Cleric:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Studded;
                    }
                    else
                    {
                        return eObjectType.Chain;
                    }
                default:
                    return eObjectType.Cloth;
            }
        }

        public static eObjectType GetMidgardWeapon(eCharacterClass charClass)
        {

            List<eObjectType> weaponTypes = new List<eObjectType>();
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
                case eCharacterClass.Bonedancer:
                case eCharacterClass.Runemaster:
                case eCharacterClass.Spiritmaster:
                    weaponTypes.Add(eObjectType.Staff);
                    break;
                case eCharacterClass.Healer:
                case eCharacterClass.Shaman:
                    weaponTypes.Add(eObjectType.Staff);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Hunter:
                    weaponTypes.Add(eObjectType.Spear);
                    weaponTypes.Add(eObjectType.CompositeBow);
                    weaponTypes.Add(eObjectType.Spear);
                    weaponTypes.Add(eObjectType.CompositeBow);
                    weaponTypes.Add(eObjectType.Sword);
                    break;
                case eCharacterClass.Savage:
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.HandToHand);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    break;
                case eCharacterClass.Shadowblade:
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Berserker:
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.LeftAxe);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Thane:
                case eCharacterClass.Warrior:
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Skald:
                    //hi Catkain <3
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Sword);
                    weaponTypes.Add(eObjectType.Axe);
                    weaponTypes.Add(eObjectType.Hammer);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                default:
                    return eObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<eObjectType> outputList = new List<eObjectType>(weaponTypes);

            //add duplicate values
            foreach (eObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);


            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static eObjectType GetMidgardArmorType(eCharacterClass charClass, byte level)
        {

            switch (charClass)
            {
                //staff classes
                case eCharacterClass.Bonedancer:
                case eCharacterClass.Runemaster:
                case eCharacterClass.Spiritmaster:
                    return eObjectType.Cloth;

                case eCharacterClass.Shadowblade:
                    return eObjectType.Leather;

                case eCharacterClass.Hunter:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else
                    {
                        return eObjectType.Studded;
                    }

                case eCharacterClass.Berserker:
                case eCharacterClass.Savage:
                    return eObjectType.Studded;

                case eCharacterClass.Shaman:
                case eCharacterClass.Healer:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Studded;
                    }
                    else
                    {
                        return eObjectType.Chain;
                    }

                case eCharacterClass.Skald:
                    if (level < 20)
                    {
                        return eObjectType.Studded;
                    }
                    else { return eObjectType.Chain; }

                case eCharacterClass.Warrior:
                    if (level < 10)
                    {
                        return eObjectType.Studded;
                    }
                    else { return eObjectType.Chain; }

                case eCharacterClass.Thane:
                    if (level < 12)
                    {
                        return eObjectType.Studded;
                    }
                    else
                    {
                        return eObjectType.Chain;
                    }

                default:
                    return eObjectType.Cloth;
            }
        }

        public static eObjectType GetHiberniaWeapon(eCharacterClass charClass)
        {
            List<eObjectType> weaponTypes = new List<eObjectType>();
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
                case eCharacterClass.Eldritch:
                case eCharacterClass.Enchanter:
                case eCharacterClass.Mentalist:
                case eCharacterClass.Animist:
                    weaponTypes.Add(eObjectType.Staff);
                    break;
                case eCharacterClass.Valewalker:
                    weaponTypes.Add(eObjectType.Scythe);
                    break;
                case eCharacterClass.Nightshade:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Ranger:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.RecurvedBow);
                    weaponTypes.Add(eObjectType.RecurvedBow);
                    weaponTypes.Add(eObjectType.RecurvedBow);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Champion:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Hero:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.CelticSpear);
                    weaponTypes.Add(eObjectType.LargeWeapons);
                    weaponTypes.Add(eObjectType.CelticSpear);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Fired); //shortbow
                    break;
                case eCharacterClass.Blademaster:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Piercing);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Fired); //shortbow
                    weaponTypes.Add(eObjectType.Shield);
                    break;
                case eCharacterClass.Warden:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Fired); //shortbow
                    break;
                case eCharacterClass.Druid:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Staff);
                    break;
                case eCharacterClass.Bard:
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Blades);
                    weaponTypes.Add(eObjectType.Blunt);
                    weaponTypes.Add(eObjectType.Shield);
                    weaponTypes.Add(eObjectType.Instrument);
                    weaponTypes.Add(eObjectType.Instrument);
                    break;
                default:
                    return eObjectType.Staff;
            }

            //this list nonsense is kind of weird but we need to duplicate the 
            //items in the list to avoid apparent mid-number bias for random number gen

            //clone existing list
            List<eObjectType> outputList = new List<eObjectType>(weaponTypes);

            //add duplicate values
            foreach (eObjectType type in weaponTypes)
            {
                outputList.Add(type);
            }

            //get our random value from the list
            int randomGrab = Util.Random(0, outputList.Count - 1);


            //return a random type from our list of valid weapons
            return outputList[randomGrab];

        }

        public static eObjectType GetHiberniaArmorType(eCharacterClass charClass, byte level)
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
                case eCharacterClass.Valewalker:
                case eCharacterClass.Animist:
                case eCharacterClass.Mentalist:
                case eCharacterClass.Enchanter:
                case eCharacterClass.Eldritch:
                    return eObjectType.Cloth;

                case eCharacterClass.Nightshade:
                    return eObjectType.Leather;

                case eCharacterClass.Blademaster:
                    return eObjectType.Reinforced;

                case eCharacterClass.Ranger:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else
                    {
                        return eObjectType.Reinforced;
                    }

                case eCharacterClass.Champion:
                    if (level < 20)
                    {
                        return eObjectType.Reinforced;
                    }
                    else { return eObjectType.Scale; }

                case eCharacterClass.Hero:
                    if (level < 15)
                    {
                        return eObjectType.Reinforced;
                    }
                    else { return eObjectType.Scale; }

                case eCharacterClass.Warden:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Reinforced;
                    }
                    else { return eObjectType.Scale; }

                case eCharacterClass.Druid:
                    if (level < 10)
                    {
                        return eObjectType.Leather;
                    }
                    else if (level < 20)
                    {
                        return eObjectType.Reinforced;
                    }
                    else { return eObjectType.Scale; }

                case eCharacterClass.Bard:
                    if (level < 15)
                    {
                        return eObjectType.Leather;
                    }
                    else { return eObjectType.Reinforced; }

                default:
                    return eObjectType.Cloth;
            }
        }

        public static eInventorySlot GenerateItemType(eObjectType type)
        {
            if ((int)type >= (int)eObjectType._FirstArmor && (int)type <= (int)eObjectType._LastArmor)
                return (eInventorySlot)ArmorSlots[Util.Random(0, ArmorSlots.Length - 1)];
            switch (type)
            {
                //left or right standard
                //tolakram - left hand usable now set based on speed
                case eObjectType.HandToHand:
                case eObjectType.Piercing:
                case eObjectType.Blades:
                case eObjectType.Blunt:
                case eObjectType.SlashingWeapon:
                case eObjectType.CrushingWeapon:
                case eObjectType.ThrustWeapon:
                case eObjectType.FistWraps: //Maulers
                case eObjectType.Flexible:
                    return (eInventorySlot)Slot.RIGHTHAND;
                //left or right or twohand
                case eObjectType.Sword:
                case eObjectType.Axe:
                case eObjectType.Hammer:
                    if (Util.Random(100) >= 50)
                        return (eInventorySlot)Slot.RIGHTHAND;
                    else
                        return (eInventorySlot)Slot.TWOHAND;
                //left
                case eObjectType.LeftAxe:
                case eObjectType.Shield:
                    return (eInventorySlot)Slot.LEFTHAND;
                //twohanded
                case eObjectType.LargeWeapons:
                case eObjectType.CelticSpear:
                case eObjectType.PolearmWeapon:
                case eObjectType.Spear:
                case eObjectType.Staff:
                case eObjectType.Scythe:
                case eObjectType.TwoHandedWeapon:
                case eObjectType.MaulerStaff:
                    return (eInventorySlot)Slot.TWOHAND;
                //ranged
                case eObjectType.CompositeBow:
                case eObjectType.Fired:
                case eObjectType.Longbow:
                case eObjectType.RecurvedBow:
                case eObjectType.Crossbow:
                    return (eInventorySlot)Slot.RANGED;
                case eObjectType.Magical:
                    return (eInventorySlot)MagicalSlots[Util.Random(0, MagicalSlots.Length - 1)];
                case eObjectType.Instrument:
                    return (eInventorySlot)Slot.RANGED;
            }
            return eInventorySlot.FirstEmptyBackpack;
        }

        private static eDamageType GenerateDamageType(eObjectType type, eCharacterClass charClass)
        {
            switch (type)
            {
                //all
                case eObjectType.TwoHandedWeapon:
                case eObjectType.PolearmWeapon:
                case eObjectType.Instrument:
                    return (eDamageType)Util.Random(1, 3);
                //slash
                case eObjectType.Axe:
                case eObjectType.Blades:
                case eObjectType.SlashingWeapon:
                case eObjectType.LeftAxe:
                case eObjectType.Sword:
                case eObjectType.Scythe:
                    return eDamageType.Slash;
                //thrust
                case eObjectType.ThrustWeapon:
                case eObjectType.Piercing:
                case eObjectType.CelticSpear:
                case eObjectType.Longbow:
                case eObjectType.RecurvedBow:
                case eObjectType.CompositeBow:
                case eObjectType.Fired:
                case eObjectType.Crossbow:
                    return eDamageType.Thrust;
                //crush
                case eObjectType.Hammer:
                case eObjectType.CrushingWeapon:
                case eObjectType.Blunt:
                case eObjectType.MaulerStaff: //Maulers
                case eObjectType.FistWraps: //Maulers
                case eObjectType.Staff:
                    return eDamageType.Crush;
                //specifics
                case eObjectType.HandToHand:
                case eObjectType.Spear:
                    return (eDamageType)Util.Random(2, 3);
                case eObjectType.LargeWeapons:
                case eObjectType.Flexible:
                    return (eDamageType)Util.Random(1, 2);
                //do shields return the shield size?
                case eObjectType.Shield:
                    return (eDamageType)Util.Random(1, GetMaxShieldSizeFromClass(charClass));
                    //return (eDamageType)Util.Random(1, 3);
            }
            return eDamageType.Natural;
        }

        private static int GetMaxShieldSizeFromClass(eCharacterClass charClass)
        {
            //shield size is based off of damage type
            //1 = small shield
            //2 = medium
            //3 = large
            switch (charClass)
            {
                case eCharacterClass.Berserker:
                case eCharacterClass.Skald:
                case eCharacterClass.Savage:
                case eCharacterClass.Healer:
                case eCharacterClass.Shaman:
                case eCharacterClass.Shadowblade:
                case eCharacterClass.Bard:
                case eCharacterClass.Druid:
                case eCharacterClass.Nightshade:
                case eCharacterClass.Ranger:
                case eCharacterClass.Infiltrator:
                case eCharacterClass.Minstrel:
                case eCharacterClass.Scout:
                    return 1;

                case eCharacterClass.Thane:
                case eCharacterClass.Warden:
                case eCharacterClass.Blademaster:
                case eCharacterClass.Champion:
                case eCharacterClass.Mercenary:
                case eCharacterClass.Cleric:
                    return 2;

                case eCharacterClass.Warrior:
                case eCharacterClass.Hero:
                case eCharacterClass.Armsman:
                case eCharacterClass.Paladin:
                case eCharacterClass.Reaver:
                    return 3;
                default: return 1;
            }
        }

        #endregion

        #region generate item speed and abs

        private static int GetAbsorb(eObjectType type)
        {
            switch (type)
            {
                case eObjectType.Cloth: return 0;
                case eObjectType.Leather: return 10;
                case eObjectType.Studded: return 19;
                case eObjectType.Reinforced: return 19;
                case eObjectType.Chain: return 27;
                case eObjectType.Scale: return 27;
                case eObjectType.Plate: return 34;
                default: return 0;
            }
        }

        private void SetWeaponSpeed()
        {
            // tolakram - reset speeds based on data from allakhazam 1-26-2008
            // removed specific left hand speed - left hand usable set based on speed in GenerateItemNameModel

            switch ((eObjectType)this.Object_Type)
            {
                case eObjectType.SlashingWeapon:
                    {
                        this.SPD_ABS = Util.Random(26, 39);
                        return;
                    }
                case eObjectType.CrushingWeapon:
                    {
                        this.SPD_ABS = Util.Random(30, 40);
                        return;
                    }
                case eObjectType.ThrustWeapon:
                    {
                        this.SPD_ABS = Util.Random(25, 37);
                        return;
                    }
                case eObjectType.Fired:
                    {
                        this.SPD_ABS = Util.Random(40, 46);
                        return;
                    }
                case eObjectType.TwoHandedWeapon:
                    {
                        this.SPD_ABS = Util.Random(43, 51);
                        return;
                    }
                case eObjectType.PolearmWeapon:
                    {
                        this.SPD_ABS = Util.Random(53, 56);
                        return;
                    }
                case eObjectType.Staff:
                    {
                        this.SPD_ABS = Util.Random(30, 50);
                        return;
                    }
                case eObjectType.MaulerStaff: //Maulers
                    {
                        this.SPD_ABS = Util.Random(34, 54);
                        return;
                    }
                case eObjectType.Longbow:
                    {
                        this.SPD_ABS = Util.Random(40, 52);
                        return;
                    }
                case eObjectType.Crossbow:
                    {
                        this.SPD_ABS = Util.Random(33, 54);
                        return;
                    }
                case eObjectType.Flexible:
                    {
                        this.SPD_ABS = Util.Random(33, 39);
                        return;
                    }
                case eObjectType.Sword:
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
                case eObjectType.Hammer:
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
                case eObjectType.Axe:
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
                case eObjectType.Spear:
                    {
                        this.SPD_ABS = Util.Random(43, 52);
                        return;
                    }
                case eObjectType.CompositeBow:
                    {
                        this.SPD_ABS = Util.Random(40, 47);
                        return;
                    }
                case eObjectType.LeftAxe:
                    {
                        this.SPD_ABS = Util.Random(27, 31);
                        return;
                    }
                case eObjectType.HandToHand:
                    {
                        this.SPD_ABS = Util.Random(27, 37);
                        return;
                    }
                case eObjectType.FistWraps:
                    {
                        this.SPD_ABS = Util.Random(28, 41);
                        return;
                    }
                case eObjectType.RecurvedBow:
                    {
                        this.SPD_ABS = Util.Random(45, 52);
                        return;
                    }
                case eObjectType.Blades:
                    {
                        this.SPD_ABS = Util.Random(27, 39);
                        return;
                    }
                case eObjectType.Blunt:
                    {
                        this.SPD_ABS = Util.Random(30, 40);
                        return;
                    }
                case eObjectType.Piercing:
                    {
                        this.SPD_ABS = Util.Random(25, 36);
                        return;
                    }
                case eObjectType.LargeWeapons:
                    {
                        this.SPD_ABS = Util.Random(47, 53);
                        return;
                    }
                case eObjectType.CelticSpear:
                    {
                        this.SPD_ABS = Util.Random(40, 56);
                        return;
                    }
                case eObjectType.Scythe:
                    {
                        this.SPD_ABS = Util.Random(40, 53);
                        return;
                    }
                case eObjectType.Shield:
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
            eObjectType type = (eObjectType)this.Object_Type;
            eInventorySlot slot = (eInventorySlot)this.Item_Type;

            switch (type)
            {
                case eObjectType.LeftAxe:
                case eObjectType.Flexible:
                case eObjectType.Axe:
                case eObjectType.Blades:
                case eObjectType.HandToHand:
                case eObjectType.FistWraps: //Maulers
                    this.Weight = 20;
                    return;
                case eObjectType.CompositeBow:
                case eObjectType.RecurvedBow:
                case eObjectType.Longbow:
                case eObjectType.Blunt:
                case eObjectType.CrushingWeapon:
                case eObjectType.Fired:
                case eObjectType.Hammer:
                case eObjectType.Piercing:
                case eObjectType.SlashingWeapon:
                case eObjectType.Sword:
                case eObjectType.ThrustWeapon:
                    this.Weight = 30;
                    return;
                case eObjectType.Crossbow:
                case eObjectType.Spear:
                case eObjectType.CelticSpear:
                case eObjectType.Staff:
                case eObjectType.TwoHandedWeapon:
                case eObjectType.MaulerStaff: //Maulers
                    this.Weight = 40;
                    return;
                case eObjectType.Scale:
                case eObjectType.Chain:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: this.Weight = 48; return;
                            case eInventorySlot.FeetArmor: this.Weight = 32; return;
                            case eInventorySlot.HandsArmor: this.Weight = 32; return;
                            case eInventorySlot.HeadArmor: this.Weight = 32; return;
                            case eInventorySlot.LegsArmor: this.Weight = 56; return;
                            case eInventorySlot.TorsoArmor: this.Weight = 80; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case eObjectType.Cloth:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: this.Weight = 8; return;
                            case eInventorySlot.FeetArmor: this.Weight = 8; return;
                            case eInventorySlot.HandsArmor: this.Weight = 8; return;
                            case eInventorySlot.HeadArmor: this.Weight = 32; return;
                            case eInventorySlot.LegsArmor: this.Weight = 14; return;
                            case eInventorySlot.TorsoArmor: this.Weight = 20; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case eObjectType.Instrument:
                    this.Weight = 15;
                    return;
                case eObjectType.LargeWeapons:
                    this.Weight = 50;
                    return;
                case eObjectType.Leather:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: this.Weight = 24; return;
                            case eInventorySlot.FeetArmor: this.Weight = 16; return;
                            case eInventorySlot.HandsArmor: this.Weight = 16; return;
                            case eInventorySlot.HeadArmor: this.Weight = 16; return;
                            case eInventorySlot.LegsArmor: this.Weight = 28; return;
                            case eInventorySlot.TorsoArmor: this.Weight = 40; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case eObjectType.Magical:
                    this.Weight = 5;
                    return;
                case eObjectType.Plate:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: this.Weight = 54; return;
                            case eInventorySlot.FeetArmor: this.Weight = 36; return;
                            case eInventorySlot.HandsArmor: this.Weight = 36; return;
                            case eInventorySlot.HeadArmor: this.Weight = 40; return;
                            case eInventorySlot.LegsArmor: this.Weight = 63; return;
                            case eInventorySlot.TorsoArmor: this.Weight = 90; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case eObjectType.PolearmWeapon:
                    this.Weight = 60;
                    return;
                case eObjectType.Reinforced:
                case eObjectType.Studded:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: this.Weight = 36; return;
                            case eInventorySlot.FeetArmor: this.Weight = 24; return;
                            case eInventorySlot.HandsArmor: this.Weight = 24; return;
                            case eInventorySlot.HeadArmor: this.Weight = 24; return;
                            case eInventorySlot.LegsArmor: this.Weight = 42; return;
                            case eInventorySlot.TorsoArmor: this.Weight = 60; return;
                        }
                        this.Weight = 0;
                        return;
                    }
                case eObjectType.Scythe:
                    this.Weight = 40;
                    return;
                case eObjectType.Shield:
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
        public bool WriteMagicalName(eProperty property)
        {
            if (hPropertyToMagicPrefix.TryGetValue(property, out string prefix) && !m_named)
            {
                if (!string.IsNullOrEmpty(prefix))
                    Name = $"{prefix} {Name}";

                m_named = true;
                return true;
            }

            return false;
        }


        private void GenerateItemNameModel()
        {
            eInventorySlot slot = (eInventorySlot)this.Item_Type;
            eDamageType damage = (eDamageType)this.Type_Damage;
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)this.Object_Type;

            string name = "No Name";
            int model = 488;
            bool canAddExtension = false;

            switch (type)
            {
                //armor
                case eObjectType.Cloth:
                    {
                        name = "Cloth " + ArmorSlotToName(slot, type);

                        switch (realm)
                        {
                            case eRealm.Albion:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = 141; break;
                                    case eInventorySlot.LegsArmor: model = 140; break;
                                    case eInventorySlot.FeetArmor: model = 143; break;
                                    case eInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1278; //30% chance of wizard hat
                                        else
                                            model = 822;
                                        break;
                                    case eInventorySlot.HandsArmor: model = 142; break;
                                    case eInventorySlot.TorsoArmor:
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

                            case eRealm.Midgard:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = 247; break;
                                    case eInventorySlot.LegsArmor: model = 246; break;
                                    case eInventorySlot.FeetArmor: model = 249; break;
                                    case eInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1280; //30% chance of wizard hat
                                        else
                                            model = 825;
                                        break;
                                    case eInventorySlot.HandsArmor: model = 248; break;
                                    case eInventorySlot.TorsoArmor:
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

                            case eRealm.Hibernia:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = 380; break;
                                    case eInventorySlot.LegsArmor: model = 379; break;
                                    case eInventorySlot.FeetArmor: model = 382; break;
                                    case eInventorySlot.HeadArmor:
                                        if (Util.Chance(30))
                                            model = 1279; //30% chance of wizard hat
                                        else
                                            model = 826;
                                        break;
                                    case eInventorySlot.HandsArmor: model = 381; break;
                                    case eInventorySlot.TorsoArmor:
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

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Leather:
                    {
                        name = "Leather " + ArmorSlotToName(slot, type);

                        switch (realm)
                        {
                            case eRealm.Albion:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, eRealm.Albion); break;
                                }
                                break;

                            case eRealm.Midgard:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, eRealm.Midgard); break;
                                }
                                break;

                            case eRealm.Hibernia:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetLeatherSleevesForLevel(Level, eRealm.Hibernia); break;
                                    case eInventorySlot.LegsArmor: model = GetLeatherPantsForLevel(Level, eRealm.Hibernia); break;
                                    case eInventorySlot.FeetArmor: model = GetLeatherBootsForLevel(Level, eRealm.Hibernia); break;
                                    case eInventorySlot.HeadArmor: model = GetLeatherHelmForLevel(Level, eRealm.Hibernia); break;
                                    case eInventorySlot.TorsoArmor: model = GetLeatherTorsoForLevel(Level, eRealm.Hibernia); break;
                                    case eInventorySlot.HandsArmor: model = GetLeatherHandsForLevel(Level, eRealm.Hibernia); break;
                                }
                                break;

                        }

                        if (slot != eInventorySlot.HeadArmor
                            && slot != eInventorySlot.ArmsArmor
                            && slot != eInventorySlot.LegsArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Studded:
                    {
                        name = "Studded " + ArmorSlotToName(slot, type);
                        switch (realm)
                        {
                            case eRealm.Albion:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetStuddedSleevesForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.LegsArmor: model = GetStuddedPantsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.FeetArmor: model = GetStuddedBootsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HeadArmor: model = GetStuddedHelmForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.TorsoArmor: model = GetStuddedTorsoForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HandsArmor: model = GetStuddedHandsForLevel(Level, eRealm.Albion); break;
                                }
                                break;

                            case eRealm.Midgard:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetStuddedSleevesForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.LegsArmor: model = GetStuddedPantsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.FeetArmor: model = GetStuddedBootsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HeadArmor: model = GetStuddedHelmForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.TorsoArmor: model = GetStuddedTorsoForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HandsArmor: model = GetStuddedHandsForLevel(Level, eRealm.Midgard); break;
                                }
                                break;
                        }

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Plate:
                    {
                        name = "Plate " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = GetPlateSleevesForLevel(Level, eRealm.Albion); break;
                            case eInventorySlot.LegsArmor: model = GetPlatePantsForLevel(Level, eRealm.Albion); break;
                            case eInventorySlot.FeetArmor: model = GetPlateBootsForLevel(Level, eRealm.Albion); break;
                            case eInventorySlot.HeadArmor:
                                model = GetPlateHelmForLevel(Level, eRealm.Albion);
                                if (model == 93 || model == 95)
                                    name = "Plate Full Helm";
                                break;
                            case eInventorySlot.TorsoArmor: model = GetPlateTorsoForLevel(Level, eRealm.Albion); break;
                            case eInventorySlot.HandsArmor: model = GetPlateHandsForLevel(Level, eRealm.Albion); break;
                        }

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Chain:
                    {
                        name = "Chain " + ArmorSlotToName(slot, type);
                        switch (realm)
                        {
                            case eRealm.Albion:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetChainSleevesForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.LegsArmor: model = GetChainPantsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.FeetArmor: model = GetChainBootsForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HeadArmor: model = GetChainHelmForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.TorsoArmor: model = GetChainTorsoForLevel(Level, eRealm.Albion); break;
                                    case eInventorySlot.HandsArmor: model = GetChainHandsForLevel(Level, eRealm.Albion); break;
                                }
                                break;

                            case eRealm.Midgard:
                                switch (slot)
                                {
                                    case eInventorySlot.ArmsArmor: model = GetChainSleevesForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.LegsArmor: model = GetChainPantsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.FeetArmor: model = GetChainBootsForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HeadArmor: model = GetChainHelmForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.TorsoArmor: model = GetChainTorsoForLevel(Level, eRealm.Midgard); break;
                                    case eInventorySlot.HandsArmor: model = GetChainHandsForLevel(Level, eRealm.Midgard); break;
                                }
                                break;
                        }

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Reinforced:
                    {
                        name = "Reinforced " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = GetReinforcedSleevesForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.LegsArmor: model = GetReinforcedPantsForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.FeetArmor: model = GetReinforcedBootsForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.HeadArmor: model = GetReinforcedHelmForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.TorsoArmor: model = GetReinforcedTorsoForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.HandsArmor: model = GetReinforcedHandsForLevel(Level, eRealm.Hibernia); break;
                        }

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }
                case eObjectType.Scale:
                    {
                        name = "Scale " + ArmorSlotToName(slot, type);
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = GetScaleSleevesForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.LegsArmor: model = GetScalePantsForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.FeetArmor: model = GetScaleBootsForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.HeadArmor: model = GetScaleHelmForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.TorsoArmor: model = GetScaleTorsoForLevel(Level, eRealm.Hibernia); break;
                            case eInventorySlot.HandsArmor: model = GetScaleHandsForLevel(Level, eRealm.Hibernia); break;
                        }

                        if (slot != eInventorySlot.HeadArmor)
                            canAddExtension = true;

                        break;
                    }

                //weapons
                case eObjectType.Axe:
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
                case eObjectType.Blades:
                    {
                        model = GetBladeModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.Blunt:
                    {
                        // Blunt; speed 22 - 45; Club (449), Mace (450), Hammer (461), Spiked Mace (451), Pick Hammer (641)
                        model = GetBluntModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.CelticSpear:
                    {
                        model = GetSpearModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.CompositeBow:
                    {
                        if (this.SPD_ABS > 40)
                            name = "Great Composite Bow";
                        else
                            name = "Composite Bow";

                        model = GetBowModelForLevel(Level, eRealm.Midgard);
                        break;
                    }
                case eObjectType.Crossbow:
                    {
                        name = "Crossbow";
                        model = GetCrossbowModelForLevel(Level, eRealm.Albion);
                        break;
                    }
                case eObjectType.CrushingWeapon:
                    {
                        model = GetBluntModelForLevel(Level, eRealm.Albion);
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
                case eObjectType.Fired:
                    {
                        if (realm == eRealm.Albion)
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
                case eObjectType.Flexible:
                    {
                        model = GetFlexModelForLevel(Level, eRealm.Albion, damage);
                        switch (damage)
                        {
                            case eDamageType.Crush:
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
                            case eDamageType.Slash:
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
                case eObjectType.Hammer:
                    {
                        if (this.Hand == 1)
                        {
                            model = Get2HHammerForLevel(Level, eRealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            model = GetBluntModelForLevel(Level, eRealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case eObjectType.HandToHand:
                    {
                        model = GetH2HModelForLevel(Level, eRealm.Midgard, damage);
                        switch (damage)
                        {
                            case eDamageType.Slash:
                                {
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case eDamageType.Thrust:
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
                case eObjectType.Instrument:
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
                case eObjectType.LargeWeapons:
                    {
                        switch (damage)
                        {
                            case eDamageType.Slash:
                                {
                                    model = Get2HSwordForLevel(Level, eRealm.Hibernia);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case eDamageType.Crush:
                                {
                                    model = Get2HHammerForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.LeftAxe:
                    {
                        model = GetAxeModelForLevel(Level, eRealm.Midgard);
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
                case eObjectType.Longbow:
                    {
                        model = GetBowModelForLevel(Level, eRealm.Albion);
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
                case eObjectType.Magical:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.Cloak:
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
                            case eInventorySlot.Waist:
                                {
                                    if (Util.Chance(50))
                                        name = "Belt";
                                    else
                                        name = "Girdle";

                                    model = 597;
                                    break;
                                }
                            case eInventorySlot.Neck:
                                {
                                    if (Util.Chance(50))
                                        name = "Choker";
                                    else
                                        name = "Pendant";

                                    model = 101;
                                    break;
                                }
                            case eInventorySlot.Jewelry:
                                {
                                    if (Util.Chance(50))
                                        name = "Gem";
                                    else
                                        name = "Jewel";

                                    model = Util.Random(110, 119);
                                    break;
                                }
                            case eInventorySlot.LeftBracer:
                            case eInventorySlot.RightBracer:
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
                            case eInventorySlot.LeftRing:
                            case eInventorySlot.RightRing:
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
                case eObjectType.Piercing:
                    {
                        model = GetThrustModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.PolearmWeapon:
                    {
                        model = GetPolearmModelForLevel(Level, eRealm.Albion, damage);
                        switch (damage)
                        {
                            case eDamageType.Slash:
                                {
                                    name = "Lochaber Axe";
                                    break;
                                }
                            case eDamageType.Thrust:
                                {
                                    name = "Pike";
                                    break;
                                }
                            case eDamageType.Crush:
                                {
                                    name = "Lucerne Hammer";
                                    break;
                                }
                        }
                        break;
                    }
                case eObjectType.RecurvedBow:
                    {
                        model = GetBowModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.Scythe:
                    {
                        model = GetScytheModelForLevel(Level, eRealm.Hibernia);
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
                case eObjectType.Shield:
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
                case eObjectType.SlashingWeapon:
                    {
                        model = GetBladeModelForLevel(Level, eRealm.Albion);
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
                case eObjectType.Spear:
                    {
                        model = GetSpearModelForLevel(Level, eRealm.Midgard);
                        name = GetNameFromId(model);
                        break;
                    }
                case eObjectType.MaulerStaff:
                    {
                        name = "Mauler Staff";
                        model = 19;
                        break;
                    }
                case eObjectType.Staff:
                    {
                        model = GetStaffModelForLevel(Level, realm);
                        switch (realm)
                        {
                            case eRealm.Albion:

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

                            case eRealm.Midgard:
                                name = GetNameFromId(model);
                                break;

                            case eRealm.Hibernia:
                                name = GetNameFromId(model);
                                break;
                        }
                        break;
                    }
                case eObjectType.Sword:
                    {
                        if (this.Hand == 1)
                        {
                            model = Get2HSwordForLevel(Level, eRealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        else
                        {
                            model = GetBladeModelForLevel(Level, eRealm.Midgard);
                            name = GetNameFromId(model);
                        }
                        break;
                    }
                case eObjectType.ThrustWeapon:
                    {
                        model = GetThrustModelForLevel(Level, eRealm.Albion);
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
                case eObjectType.TwoHandedWeapon:
                    {
                        switch (damage)
                        {
                            case eDamageType.Slash:
                                {
                                    model = Get2HSwordForLevel(Level, eRealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case eDamageType.Crush:
                                {
                                    model = Get2HHammerForLevel(Level, eRealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                            case eDamageType.Thrust:
                                {
                                    model = Get2HThrustForLevel(Level, eRealm.Albion);
                                    name = GetNameFromId(model);
                                    break;
                                }
                        }
                        break;
                    }
                case eObjectType.FistWraps: // Maulers
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
            if (slot == eInventorySlot.HeadArmor)
            {
                switch (realm)
                {
                    case eRealm.Albion:
                        if (Util.Chance(1))
                            model = 1284; //1% chance of tarboosh
                        else if (Util.Chance(1))
                            model = 1281; //1% chance of robin hood hat
                        else if (Util.Chance(1))
                            model = 1287; //1% chance of jester hat
                        break;
                    case eRealm.Hibernia:
                        if (Util.Chance(1))
                            model = 1282; //1% chance of robin hood hat
                        else if (Util.Chance(1))
                            model = 1285; //1% chance of leaf hat
                        else if (Util.Chance(1))
                            model = 1288; //1% chance of stag helm
                        break;
                    case eRealm.Midgard:
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
                if (slot == eInventorySlot.HandsArmor ||
                     slot == eInventorySlot.FeetArmor)
                    ext = GetNonTorsoExtensionForLevel(Level);
                else if (slot == eInventorySlot.TorsoArmor)
                    ext = GetTorsoExtensionForLevel(Level);

                this.Extension = ext;
            }

        }

        #region Leather Model Generation
        private static int GetLeatherTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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
                case eRealm.Hibernia:
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

        private static int GetLeatherPantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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
                case eRealm.Hibernia:
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

        private static int GetLeatherSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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
                case eRealm.Hibernia:
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

        private static int GetLeatherHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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
                case eRealm.Hibernia:
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

        private static int GetLeatherBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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
                case eRealm.Hibernia:
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

        private static int GetLeatherHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    validModels.Add(62);
                    if (Level > 35)
                        validModels.Add(1231);
                    if (Level > 45)
                        validModels.Add(2800);
                    if (Level > 50)
                        validModels.Add(1232);
                    break;
                case eRealm.Midgard:
                    validModels.Add(335);
                    if (Level > 35)
                        validModels.Add(336);
                    if (Level > 45)
                        validModels.Add(337);
                    if (Level > 50)
                        validModels.Add(1214);
                    break;
                case eRealm.Hibernia:
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
        private static int GetStuddedTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetStuddedPantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetStuddedSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetStuddedHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetStuddedBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetStuddedHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    validModels.Add(824);
                    if (Level > 35)
                        validModels.Add(1233);
                    if (Level > 45)
                        validModels.Add(1234);
                    if (Level > 50)
                        validModels.Add(1235);
                    break;
                case eRealm.Midgard:
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
        private static int GetChainTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetChainPantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetChainSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetChainHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetChainBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetChainHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    validModels.Add(1236);
                    if (Level > 35)
                        validModels.Add(63);
                    if (Level > 45)
                        validModels.Add(2812);
                    break;
                case eRealm.Midgard:
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
        private static int GetPlateTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetPlatePantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetPlateSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetPlateHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetPlateBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetPlateHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
        private static int GetReinforcedTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetReinforcedPantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetReinforcedSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetReinforcedHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetReinforcedBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetReinforcedHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
        private static int GetScaleTorsoForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetScalePantsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetScaleSleevesForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetScaleHandsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetScaleBootsForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetScaleHelmForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int Get2HAxeModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    validModels.Add(9);
                    if (Level > 10)
                        validModels.Add(72);
                    if (Level > 30)
                        validModels.Add(73);
                    break;
                case eRealm.Midgard:
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
        private static int GetAxeModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int Get2HSwordForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetBladeModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int Get2HHammerForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetBluntModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int Get2HThrustForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetThrustModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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

        private static int GetPolearmModelForLevel(int Level, eRealm realm, eDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    switch (dtype)
                    {
                        case eDamageType.Slash:
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
                        case eDamageType.Crush:
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
                        case eDamageType.Thrust:
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

        private static int GetSpearModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Midgard:
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

        private static int GetBowModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Midgard:
                    validModels.Add(564);
                    if (Level > 30)
                        validModels.Add(1037);
                    if (Level > 40)
                        validModels.Add(1038);
                    if (Level > 50)
                        validModels.Add(1039);
                    break;
                case eRealm.Albion:
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

        private static int GetFlexModelForLevel(int Level, eRealm realm, eDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
                    switch (dtype)
                    {
                        case eDamageType.Crush:
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
                        case eDamageType.Slash:
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

        private static int GetH2HModelForLevel(int Level, eRealm realm, eDamageType dtype)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Midgard:
                    switch (dtype)
                    {
                        case eDamageType.Thrust:
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
                        case eDamageType.Slash:
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

        private static int GetCrossbowModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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

        private static int GetScytheModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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

        private static int GetStaffModelForLevel(int Level, eRealm realm)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Albion:
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
                case eRealm.Hibernia:
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
                case eRealm.Midgard:
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

        private static int GetShieldModelForLevel(int Level, eRealm realm, int size)
        {
            List<int> validModels = new List<int>();
            switch (realm)
            {
                case eRealm.Hibernia:
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
                case eRealm.Albion:
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
                case eRealm.Midgard:
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

        private static int GetInstrumentModelForLevel(int Level, eRealm realm)
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
                if(realm == eRealm.Albion)
                {
                    validModels.Add(2976);
                    validModels.Add(2977);
                    validModels.Add(2978);
                } else if (realm == eRealm.Hibernia)
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
                    if (realm == eRealm.Albion)
                        validModels.Add(3985);
                    if (realm == eRealm.Hibernia)
                        validModels.Add(3908);
                }
                if (Util.Chance(5))
                {
                    if (realm == eRealm.Albion)
                        validModels.Add(3280);
                    if (realm == eRealm.Hibernia)
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

        private static string ArmorSlotToName(eInventorySlot slot, eObjectType type)
        {
            switch (slot)
            {
                case eInventorySlot.ArmsArmor:
                    if (type == eObjectType.Plate)
                        return "Arms";
                    else
                        return "Sleeves";

                case eInventorySlot.FeetArmor:
                    return "Boots";

                case eInventorySlot.HandsArmor:
                    if (type == eObjectType.Plate)
                        return "Gauntlets";
                    else
                        return "Gloves";

                case eInventorySlot.HeadArmor:
                    if (type == eObjectType.Cloth)
                        return "Cap";
                    else if (type == eObjectType.Scale)
                        return "Coif";
                    else
                        return "Helm";

                case eInventorySlot.LegsArmor:
                    if (type == eObjectType.Cloth)
                        return "Pants";
                    else if (type == eObjectType.Plate)
                        return "Legs";
                    else
                        return "Leggings";

                case eInventorySlot.TorsoArmor:
                    if (type == eObjectType.Chain || type == eObjectType.Scale)
                        return "Hauberk";
                    else if (type == eObjectType.Plate)
                        return "Breastplate";
                    else if ((type == eObjectType.Leather || type == eObjectType.Studded) && Util.Chance(50))
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

        private static eProperty[] StatBonus = new eProperty[]
        {
            eProperty.Strength,
            eProperty.Dexterity,
            eProperty.Constitution,
            eProperty.Quickness,
			//eProperty.Intelligence,
			//eProperty.Piety,
			//eProperty.Empathy,
			//eProperty.Charisma,
			eProperty.MaxMana,
            eProperty.MaxHealth,
            eProperty.Acuity,
        };

        private static eProperty[] AdvancedStats = new eProperty[]
        {
            eProperty.PowerPool,
            eProperty.PowerPoolCapBonus,
            eProperty.StrCapBonus,
            eProperty.DexCapBonus,
            eProperty.ConCapBonus,
            eProperty.QuiCapBonus,
			//eProperty.IntCapBonus,
			//eProperty.PieCapBonus,
			//eProperty.EmpCapBonus,
			//eProperty.ChaCapBonus,
			eProperty.MaxHealthCapBonus,
            eProperty.AcuCapBonus,
        };

        private static eProperty[] ResistBonus = new eProperty[]
        {
            eProperty.Resist_Body,
            eProperty.Resist_Cold,
            eProperty.Resist_Crush,
            eProperty.Resist_Energy,
            eProperty.Resist_Heat,
            eProperty.Resist_Matter,
            eProperty.Resist_Slash,
            eProperty.Resist_Spirit,
            eProperty.Resist_Thrust,
        };


        private static eProperty[] AlbSkillBonus = new eProperty[]
        {
            eProperty.Skill_Two_Handed,
            eProperty.Skill_Body,
	        //eProperty.Skill_Chants, // bonus not used
	        eProperty.Skill_Critical_Strike,
            eProperty.Skill_Cross_Bows,
            eProperty.Skill_Crushing,
            eProperty.Skill_Death_Servant,
            eProperty.Skill_DeathSight,
            eProperty.Skill_Dual_Wield,
            eProperty.Skill_Earth,
            eProperty.Skill_Enhancement,
            eProperty.Skill_Envenom,
            eProperty.Skill_Fire,
            eProperty.Skill_Flexible_Weapon,
            eProperty.Skill_Cold,
            eProperty.Skill_Instruments,
            eProperty.Skill_Long_bows,
            eProperty.Skill_Matter,
            eProperty.Skill_Mind,
            eProperty.Skill_Pain_working,
            eProperty.Skill_Parry,
            eProperty.Skill_Polearms,
            eProperty.Skill_Rejuvenation,
            eProperty.Skill_Shields,
            eProperty.Skill_Slashing,
            eProperty.Skill_Smiting,
            eProperty.Skill_SoulRending,
            eProperty.Skill_Spirit,
            eProperty.Skill_Staff,
            eProperty.Skill_Stealth,
            eProperty.Skill_Thrusting,
            eProperty.Skill_Wind,
            //eProperty.Skill_Aura_Manipulation, //Maulers
            //eProperty.Skill_FistWraps, //Maulers
            //eProperty.Skill_MaulerStaff, //Maulers
            //eProperty.Skill_Magnetism, //Maulers
            //eProperty.Skill_Power_Strikes, //Maulers
        };


        private static eProperty[] HibSkillBonus = new eProperty[]
        {
            eProperty.Skill_Critical_Strike,
            eProperty.Skill_Envenom,
            eProperty.Skill_Parry,
            eProperty.Skill_Shields,
            eProperty.Skill_Stealth,
            eProperty.Skill_Light,
            eProperty.Skill_Void,
            eProperty.Skill_Mana,
            eProperty.Skill_Blades,
            eProperty.Skill_Blunt,
            eProperty.Skill_Piercing,
            eProperty.Skill_Large_Weapon,
            eProperty.Skill_Mentalism,
            eProperty.Skill_Regrowth,
            eProperty.Skill_Nurture,
            eProperty.Skill_Nature,
            eProperty.Skill_Music,
            eProperty.Skill_Celtic_Dual,
            eProperty.Skill_Celtic_Spear,
            eProperty.Skill_RecurvedBow,
            eProperty.Skill_Valor,
            eProperty.Skill_Verdant,
            eProperty.Skill_Creeping,
            eProperty.Skill_Arboreal,
            eProperty.Skill_Scythe,
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

        private static eProperty[] MidSkillBonus = new eProperty[]
        {
            eProperty.Skill_Critical_Strike,
            eProperty.Skill_Envenom,
            eProperty.Skill_Parry,
            eProperty.Skill_Shields,
            eProperty.Skill_Stealth,
            eProperty.Skill_Sword,
            eProperty.Skill_Hammer,
            eProperty.Skill_Axe,
            eProperty.Skill_Left_Axe,
            eProperty.Skill_Spear,
            eProperty.Skill_Mending,
            eProperty.Skill_Augmentation,
	        //Skill_Cave_Magic = 59,
	        eProperty.Skill_Darkness,
            eProperty.Skill_Suppression,
            eProperty.Skill_Runecarving,
            eProperty.Skill_Stormcalling,
	        //eProperty.Skill_BeastCraft, // bonus not used
			eProperty.Skill_Composite,
            eProperty.Skill_Battlesongs,
            eProperty.Skill_Subterranean,
            eProperty.Skill_BoneArmy,
            eProperty.Skill_Thrown_Weapons,
            eProperty.Skill_HandToHand,
    		//eProperty.Skill_Pacification,
	        //eProperty.Skill_Savagery,
	        //eProperty.Skill_OdinsWill,
	        //eProperty.Skill_Cursing,
	        //eProperty.Skill_Hexing,
	        //eProperty.Skill_Witchcraft,
    		eProperty.Skill_Summoning,
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
        private static eObjectType[] AlbionWeapons = new eObjectType[]
        {
            eObjectType.ThrustWeapon,
            eObjectType.CrushingWeapon,
            eObjectType.SlashingWeapon,
            eObjectType.Shield,
            eObjectType.Staff,
            eObjectType.TwoHandedWeapon,
            eObjectType.Longbow,
            eObjectType.Flexible,
            eObjectType.PolearmWeapon,
            eObjectType.FistWraps, //Maulers
			eObjectType.MaulerStaff,//Maulers
			eObjectType.Instrument,
            eObjectType.Crossbow,
            eObjectType.ThrustWeapon,
            eObjectType.CrushingWeapon,
            eObjectType.SlashingWeapon,
            eObjectType.Shield,
            eObjectType.Staff,
            eObjectType.TwoHandedWeapon,
            eObjectType.Longbow,
            eObjectType.Flexible,
            eObjectType.PolearmWeapon,
            eObjectType.FistWraps, //Maulers
			eObjectType.MaulerStaff,//Maulers
			eObjectType.Instrument,
            eObjectType.Crossbow,
        };

        private static eObjectType[] AlbionArmor = new eObjectType[]
        {
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Studded,
            eObjectType.Chain,
            eObjectType.Plate,
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Studded,
            eObjectType.Chain,
            eObjectType.Plate,
        };
        private static eObjectType[] MidgardWeapons = new eObjectType[]
        {
            eObjectType.Sword,
            eObjectType.Hammer,
            eObjectType.Axe,
            eObjectType.Shield,
            eObjectType.Staff,
            eObjectType.Spear,
            eObjectType.CompositeBow ,
            eObjectType.LeftAxe,
            eObjectType.HandToHand,
            eObjectType.Sword,
            eObjectType.Hammer,
            eObjectType.Axe,
            eObjectType.Shield,
            eObjectType.Staff,
            eObjectType.Spear,
            eObjectType.CompositeBow ,
            eObjectType.LeftAxe,
            eObjectType.HandToHand,
        };

        private static eObjectType[] MidgardArmor = new eObjectType[]
        {
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Studded,
            eObjectType.Chain,
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Studded,
            eObjectType.Chain,
        };

        private static eObjectType[] HiberniaWeapons = new eObjectType[]
        {
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
        };

        private static eObjectType[] HiberniaArmor = new eObjectType[]
        {
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Reinforced,
            eObjectType.Scale,
            eObjectType.Cloth,
            eObjectType.Leather,
            eObjectType.Reinforced,
            eObjectType.Scale,
        };

        #endregion definitions

        public static void InitializeHashtables()
        {
            // Magic Prefix

            hPropertyToMagicPrefix.Add(eProperty.Strength, "Mighty");
            hPropertyToMagicPrefix.Add(eProperty.Dexterity, "Adroit");
            hPropertyToMagicPrefix.Add(eProperty.Constitution, "Fortifying");
            hPropertyToMagicPrefix.Add(eProperty.Quickness, "Speedy");
            hPropertyToMagicPrefix.Add(eProperty.Intelligence, "Insightful");
            hPropertyToMagicPrefix.Add(eProperty.Piety, "Willful");
            hPropertyToMagicPrefix.Add(eProperty.Empathy, "Attuned");
            hPropertyToMagicPrefix.Add(eProperty.Charisma, "Glib");
            hPropertyToMagicPrefix.Add(eProperty.MaxMana, "Arcane");
            hPropertyToMagicPrefix.Add(eProperty.MaxHealth, "Sturdy");
            hPropertyToMagicPrefix.Add(eProperty.PowerPool, "Arcane");

            hPropertyToMagicPrefix.Add(eProperty.Resist_Body, "Bodybender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Cold, "Icebender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Crush, "Bluntbender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Energy, "Energybender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Heat, "Heatbender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Matter, "Matterbender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Slash, "Edgebender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Spirit, "Spiritbender");
            hPropertyToMagicPrefix.Add(eProperty.Resist_Thrust, "Thrustbender");

            hPropertyToMagicPrefix.Add(eProperty.Skill_Two_Handed, "Sundering");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Body, "Soul Crusher");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Critical_Strike, "Lifetaker");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Cross_Bows, "Truefire");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Crushing, "Battering");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Death_Servant, "Death Binder");
            hPropertyToMagicPrefix.Add(eProperty.Skill_DeathSight, "Minionbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Dual_Wield, "Whirling");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Earth, "Earthborn");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Enhancement, "Fervent");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Envenom, "Venomous");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Fire, "Flameborn");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Flexible_Weapon, "Tensile");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Cold, "Iceborn");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Instruments, "Melodic");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Long_bows, "Winged");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Matter, "Earthsplitter");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Mind, "Dominating");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Pain_working, "Painbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Parry, "Bladeblocker");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Polearms, "Decimator");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Rejuvenation, "Rejuvenating");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Shields, "Protector's");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Slashing, "Honed");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Smiting, "Earthshaker");
            hPropertyToMagicPrefix.Add(eProperty.Skill_SoulRending, "Soul Taker");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Spirit, "Spiritbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Staff, "Thunderer");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Stealth, "Shadowwalker");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Thrusting, "Perforator");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Wind, "Airy");


            hPropertyToMagicPrefix.Add(eProperty.AllMagicSkills, "Mystical");
            hPropertyToMagicPrefix.Add(eProperty.AllMeleeWeaponSkills, "Gladiator");
            hPropertyToMagicPrefix.Add(eProperty.AllSkills, "Skillful");
            hPropertyToMagicPrefix.Add(eProperty.AllDualWieldingSkills, "Duelist");
            hPropertyToMagicPrefix.Add(eProperty.AllArcherySkills, "Bowmaster");


            hPropertyToMagicPrefix.Add(eProperty.Skill_Sword, "Serrated");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Hammer, "Demolishing");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Axe, "Swathe Cutter's");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Left_Axe, "Cleaving");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Spear, "Impaling");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Mending, "Bodymender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Augmentation, "Empowering");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Darkness, "Shadowbender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Suppression, "Spiritbinder");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Runecarving, "Runebender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Stormcalling, "Stormcaller");
            hPropertyToMagicPrefix.Add(eProperty.Skill_BeastCraft, "Lifebender");

            hPropertyToMagicPrefix.Add(eProperty.Skill_Light, "Lightbender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Void, "Voidbender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Mana, "Starbinder");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Enchantments, "Chanter");

            hPropertyToMagicPrefix.Add(eProperty.Skill_Blades, "Razored");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Blunt, "Crushing");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Piercing, "Lancenator");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Large_Weapon, "Sundering");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Mentalism, "Mindbinder");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Regrowth, "Forestbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Nurture, "Plantbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Nature, "Animalbound");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Music, "Resonant");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Celtic_Dual, "Whirling");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Celtic_Spear, "Impaling");
            hPropertyToMagicPrefix.Add(eProperty.Skill_RecurvedBow, "Hawk");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Valor, "Courageous");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Subterranean, "Ancestral");
            hPropertyToMagicPrefix.Add(eProperty.Skill_BoneArmy, "Blighted");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Verdant, "Vale Defender");

            hPropertyToMagicPrefix.Add(eProperty.Skill_Battlesongs, "Motivating");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Composite, "Dragon");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Creeping, "Withering");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Arboreal, "Arbor Defender");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Scythe, "Reaper's");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Thrown_Weapons, "Catapult");
            hPropertyToMagicPrefix.Add(eProperty.Skill_HandToHand, "Martial");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Pacification, "Pacifying");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Savagery, "Savage");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Nightshade, "Nightshade");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Pathfinding, "Trail");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Summoning, "Soulbinder");

            hPropertyToMagicPrefix.Add(eProperty.Skill_Dementia, "Feverish");
            hPropertyToMagicPrefix.Add(eProperty.Skill_ShadowMastery, "Ominous");
            hPropertyToMagicPrefix.Add(eProperty.Skill_VampiiricEmbrace, "Deathly");
            hPropertyToMagicPrefix.Add(eProperty.Skill_EtherealShriek, "Shrill");
            hPropertyToMagicPrefix.Add(eProperty.Skill_PhantasmalWail, "Keening");
            hPropertyToMagicPrefix.Add(eProperty.Skill_SpectralForce, "Uncanny");
            hPropertyToMagicPrefix.Add(eProperty.Skill_OdinsWill, "Ardent");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Cursing, "Infernal");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Hexing, "Bedeviled");
            hPropertyToMagicPrefix.Add(eProperty.Skill_Witchcraft, "Diabolic");

            // Mauler - live mauler prefixes do not exist, as lame as that sounds.
            hPropertyToMagicPrefix.Add(eProperty.Skill_Aura_Manipulation, string.Empty);
            hPropertyToMagicPrefix.Add(eProperty.Skill_FistWraps, string.Empty);
            hPropertyToMagicPrefix.Add(eProperty.Skill_MaulerStaff, string.Empty);
            hPropertyToMagicPrefix.Add(eProperty.Skill_Magnetism, string.Empty);
            hPropertyToMagicPrefix.Add(eProperty.Skill_Power_Strikes, string.Empty);
        }

        private static void CacheProcSpells()
        {
            //LT spells
            DbSpell Level5Lifetap = DOLDB<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8010));
            DbSpell Level10Lifetap = DOLDB<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8011));
            DbSpell Level15Lifetap = DOLDB<DbSpell>.SelectObject(DB.Column("Spell_ID").IsEqualTo(8012));

            ProcSpells.Add(8010, new Spell(Level5Lifetap, 0));
            ProcSpells.Add(8011, new Spell(Level10Lifetap, 0));
            ProcSpells.Add(8012, new Spell(Level15Lifetap, 0));

        }
    }
}
