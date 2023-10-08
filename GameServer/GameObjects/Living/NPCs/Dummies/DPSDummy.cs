using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class DPSDummy : GameTrainingDummy {
        Int32 Damage = 0;
        DateTime StartTime;
        TimeSpan TimePassed;
        Boolean StartCheck = true;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }



            Damage = 0;
            StartCheck = true;
            Name = "Total: 0 DPS: 0";
            ResetArmorAndResists();

            SendReply(player, "Hello, you can change my [armor] and [resistances] with ease, you need but ask. Right click me to reset them back to 0. \n\n" +
                "You can also whisper me with the following format: \n\n\n" +
                "To change individual resists: /whisper <resist> <percent> (e.g., /whisper body 10) \n\n" +
                "The resist percent must be between 0 and 70 \n\n" +
                "Resist values accepted include: body, cold, crush, energy, heat, matter, slash, spirit, and thrust \n\n" +
                "To change all resists: / whisper allresist <percent> (e.g., /whisper allresist 20) \n\n" +
                "\n\n" +
                "To change my individual defenses: /whisper <defense> <percent> (e.g., /whisper evade 30) \n\n" +
                "Defense values accepted include: evade, block, parry \n\n" +
                "To change all defenses: /whisper alldefense <percent> (e.g., /whisper alldefense 20) \n\n" + 
                "");
            return true;
        }


        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text)) return false;
            if (!(source is GamePlayer player)) return false;

            string[] splitText = text.Split(' ');
            if (splitText.Length > 1)
            {
                if (!double.TryParse(splitText[1], out _) || double.Parse(splitText[1]) < 0 || double.Parse(splitText[1]) > 70)
                {
                    SendReply(player, "Number must be between 0 and 70");
                    return false;
                }
                switch (splitText[0].ToLower())
                {
                    case "slash":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Slash, double.Parse(splitText[1]), 1, false);
                        break;
                    case "thrust":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Thrust, double.Parse(splitText[1]), 1, false);
                        break;
                    case "crush":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Crush, double.Parse(splitText[1]), 1, false);
                        break;
                    case "body":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Body, double.Parse(splitText[1]), 1, false);
                        break;
                    case "cold":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Cold, double.Parse(splitText[1]), 1, false);
                        break;
                    case "energy":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Energy, double.Parse(splitText[1]), 1, false);
                        break;
                    case "heat":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Heat, double.Parse(splitText[1]), 1, false);
                        break;
                    case "matter":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Matter, double.Parse(splitText[1]), 1, false);
                        break;
                    case "spirit":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Spirit, double.Parse(splitText[1]), 1, false);
                        break;
                    case "block":
                        BlockChance = byte.Parse(splitText[1]);
                        break;
                    case "parry":
                        ParryChance = byte.Parse(splitText[1]);
                        break;
                    case "evade":
                        EvadeChance = byte.Parse(splitText[1]);
                        break;
                    case "allresist":
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Slash, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Thrust, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Crush, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Body, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Cold, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Energy, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Heat, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Matter, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Spirit, double.Parse(splitText[1]), 1, false);
                        break;
                    case "alldefense":
                        BlockChance = byte.Parse(splitText[1]);
                        EvadeChance = byte.Parse(splitText[1]);
                        ParryChance = byte.Parse(splitText[1]);
                        break;
                }
            }
            else
            {
                switch (splitText[0].ToLower())
                {
                    
                    case "armor":
                        SendReply(player, "Would you like me to don a set of \n" +
                            "[cloth] \n" +
                            "[leather] \n" +
                            "[studded] \n" +
                            "[chain] \n" +
                            "[plate] \n" +
                            "[reinforced] \n" +
                            "or [scale]?" +
                            "");
                        break;
                    
                    case "cloth":
                        CreateArmorSetOfType(EObjectType.Cloth);
                        
                        break;
                    case "leather":
                        CreateArmorSetOfType(EObjectType.Leather);
                        break;
                    case "studded":
                        CreateArmorSetOfType(EObjectType.Studded);
                        break;
                    case "chain":
                        CreateArmorSetOfType(EObjectType.Chain);
                        break;
                    case "plate":
                        CreateArmorSetOfType(EObjectType.Plate);
                        break;
                    case "reinforced":
                        CreateArmorSetOfType(EObjectType.Reinforced);
                        break;
                    case "scale":
                        CreateArmorSetOfType(EObjectType.Scale);
                        break;

                    case "resistances":
                        SendReply(player, "Whisper me the resist type and value you'd like. Example: '/whisper Body 10' will give me +10% Body resist. \n" +
                            "Additionally, you can whisper me 'allresist #' to set all resistances to the number provided." +
                            "");
                        break;
                    default:
                        ResetArmorAndResists();
                        break;
                }
            }

           
            return true;
        }

        private void CreateArmorSetOfType(EObjectType armorType)
        {
            
            Inventory.ClearInventory();
            ClearAFAndABSBuffs();
            List<int> invSlots = new List<int>();
            invSlots.Add(Slot.ARMS);
            invSlots.Add(Slot.FEET);
            invSlots.Add(Slot.HANDS);
            invSlots.Add(Slot.HELM);
            invSlots.Add(Slot.LEGS);
            invSlots.Add(Slot.TORSO);

            foreach (var slot in invSlots)
            {
                DbInventoryItem invItem = new DbInventoryItem();
                invItem.Item_Type = slot;
                invItem.Object_Type = (int)armorType;
                invItem = GenerateItemNameModel(invItem);
                invItem = GenerateArmorStats(invItem);
                this.ItemBonus[EProperty.ArmorFactor] += invItem.DPS_AF;
                //this.ItemBonus[eProperty.ArmorAbsorption] += invItem.SPD_ABS;
                //Console.WriteLine($"AF item prop{ItemBonus[eProperty.ArmorFactor]}");
                ApplyBonus(this, EBuffBonusCategory.Other, EProperty.ArmorFactor, invItem.DPS_AF, 1, false);
                //ApplyBonus(this, eBuffBonusCategory.Other, eProperty.ArmorAbsorption, invItem.SPD_ABS, 1, false);
                Inventory.AddItem((EInventorySlot)slot, invItem);
            }

            this.ItemBonus[EProperty.ArmorAbsorption] += GetAbsorb(armorType);
            BroadcastLivingEquipmentUpdate();
            ClientService.UpdateObjectForPlayers(this);
        }

        private DbInventoryItem GenerateArmorStats(DbInventoryItem item)
        {
            EObjectType type = (EObjectType)item.Object_Type;

            //set dps_af and spd_abs
            if ((int)type >= (int)EObjectType._FirstArmor && (int)type <= (int)EObjectType._LastArmor)
            {
                if (type == EObjectType.GenericArmor)
                    item.DPS_AF = 0;
                else if (type == EObjectType.Cloth)
                    item.DPS_AF = Level;
                else item.DPS_AF = Level * 2;
                item.SPD_ABS = GetAbsorb(type);
            }
            item.Quality = 99;
            item.Condition = 100;
            return item;
        }

        private void ClearAFAndABSBuffs()
        {
            //Console.WriteLine($"af {GetModified(eProperty.ArmorFactor)} abs {GetModified(eProperty.ArmorAbsorption)} itemB AF {this.ItemBonus[eProperty.ArmorFactor]} itemb ABS {this.ItemBonus[eProperty.ArmorAbsorption]}");
            if (GetModified(EProperty.ArmorFactor) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.Other, EProperty.ArmorFactor, ItemBonus[EProperty.ArmorFactor], 1, true);
            }
            if (GetModified(EProperty.ArmorAbsorption) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.Other, EProperty.ArmorAbsorption, ItemBonus[EProperty.ArmorAbsorption], 1, true);
            }
            if (this.ItemBonus[EProperty.ArmorFactor] > 0)
            {
                ItemBonus[EProperty.ArmorFactor] = 0;
            }
            if (this.ItemBonus[EProperty.ArmorAbsorption] > 0)
            {
                ItemBonus[EProperty.ArmorAbsorption] = 0;
            }
        }

        public override double GetArmorAF(EArmorSlot slot)
        {
            return base.GetArmorAF(slot)/6;
        }

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

        private void ResetResists()
        {
            if (GetResist(EDamageType.Slash) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Slash, GetResist(EDamageType.Slash), 1, true);
            }
            if (GetResist(EDamageType.Crush) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Crush, GetResist(EDamageType.Crush), 1, true);
            }
            if (GetResist(EDamageType.Thrust) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Thrust, GetResist(EDamageType.Thrust), 1, true);
            }
            if (GetResist(EDamageType.Natural) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Natural, GetResist(EDamageType.Natural), 1, true);
            }
            if (GetResist(EDamageType.Body) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Body, GetResist(EDamageType.Body), 1, true);
            }
            if (GetResist(EDamageType.Cold) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Cold, GetResist(EDamageType.Cold), 1, true);
            }
            if (GetResist(EDamageType.Energy) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Energy, GetResist(EDamageType.Energy), 1, true);
            }
            if (GetResist(EDamageType.Heat) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Heat, GetResist(EDamageType.Heat), 1, true);
            }
            if (GetResist(EDamageType.Matter) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Matter, GetResist(EDamageType.Matter), 1, true);
            }
            if (GetResist(EDamageType.Spirit) > 0)
            {
                ApplyBonus(this, EBuffBonusCategory.BaseBuff, (EProperty)EResist.Spirit, GetResist(EDamageType.Spirit), 1, true);
            }
        }

        private void ResetArmor()
        {
            CreateArmorSetOfType(EObjectType.GenericArmor);
        }

        private void ResetArmorAndResists()
        {
            ResetResists();
            ResetArmor();
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (StartCheck)
            {
                StartTime = DateTime.Now;
                StartCheck = false;
            }

            Damage += ad.Damage + ad.CriticalDamage;
            TimePassed = (DateTime.Now - StartTime);
            Name = "Total: " + Damage.ToString() + " DPS: " + (Damage / (TimePassed.TotalSeconds + 1)).ToString("0");
        }

        public override bool AddToWorld()
        {
            Name = "Total: 0 DPS: 0";
            GuildName = "Dummy Union";
            Model = 34;
            Inventory = new GameNpcInventory(GameNpcInventoryTemplate.EmptyTemplate);
            return base.AddToWorld(); // Finish up and add him to the world.
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, EChatType.CT_Merchant, EChatLoc.CL_PopupWindow);
        }

        private DbInventoryItem GenerateItemNameModel(DbInventoryItem item)
        {
            EInventorySlot slot = (EInventorySlot)item.Item_Type;
            EDamageType damage = (EDamageType)item.Type_Damage;
            ERealm realm = (ERealm)this.Realm;
            EObjectType type = (EObjectType)item.Object_Type;

            int model = 488;
            switch (type)
            {
                //armor
                case EObjectType.Cloth:
                    {

                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 141; break;
                            case EInventorySlot.LegsArmor: model = 140; break;
                            case EInventorySlot.FeetArmor: model = 143; break;
                            case EInventorySlot.HeadArmor: model = 822; break;
                            case EInventorySlot.HandsArmor: model = 142; break;
                            case EInventorySlot.TorsoArmor:
                                if (Util.Chance(60))
                                {
                                    model = 139;
                                }
                                else
                                {
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
                case EObjectType.Leather:
                    {

                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 38; break;
                            case EInventorySlot.LegsArmor: model = 37; break;
                            case EInventorySlot.FeetArmor: model = 40; break;
                            case EInventorySlot.HeadArmor: model = 62; break;
                            case EInventorySlot.TorsoArmor: model = 36; break;
                            case EInventorySlot.HandsArmor: model = 39; break;
                        }
                        break;
                    }
                case EObjectType.Studded:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 83; break;
                            case EInventorySlot.LegsArmor: model = 82; break;
                            case EInventorySlot.FeetArmor: model = 84; break;
                            case EInventorySlot.HeadArmor: model = 824; break;
                            case EInventorySlot.TorsoArmor: model = 81; break;
                            case EInventorySlot.HandsArmor: model = 85; break;
                        }
                        break;
                    }
                case EObjectType.Plate:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 48; break;
                            case EInventorySlot.LegsArmor: model = 47; break;
                            case EInventorySlot.FeetArmor: model = 50; break;
                            case EInventorySlot.HandsArmor: model = 49; break;
                            case EInventorySlot.HeadArmor:
                                if (Util.Chance(25))
                                {
                                    model = 93;
                                }
                                else
                                    model = 64;

                                break;

                            case EInventorySlot.TorsoArmor:
                                model = 46;
                                break;
                        }

                        break;
                    }
                case EObjectType.Chain:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 43; break;
                            case EInventorySlot.LegsArmor: model = 42; break;
                            case EInventorySlot.FeetArmor: model = 45; break;
                            case EInventorySlot.HeadArmor: model = 63; break;
                            case EInventorySlot.TorsoArmor: model = 41; break;
                            case EInventorySlot.HandsArmor: model = 44; break;
                        }
                        break;
                    }
                case EObjectType.Reinforced:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 385; break;
                            case EInventorySlot.LegsArmor: model = 384; break;
                            case EInventorySlot.FeetArmor: model = 387; break;
                            case EInventorySlot.HeadArmor: model = 835; break;
                            case EInventorySlot.TorsoArmor: model = 383; break;
                            case EInventorySlot.HandsArmor: model = 386; break;
                        }

                        break;
                    }
                case EObjectType.Scale:
                    {
                        switch (slot)
                        {
                            case EInventorySlot.ArmsArmor: model = 390; break;
                            case EInventorySlot.LegsArmor: model = 389; break;
                            case EInventorySlot.FeetArmor: model = 392; break;
                            case EInventorySlot.HeadArmor: model = 838; break;
                            case EInventorySlot.TorsoArmor: model = 388; break;
                            case EInventorySlot.HandsArmor: model = 391; break;
                        }


                        break;
                    }
                default:
                    model = 0;
                    break;
            }

            item.Model = model;

            return item;

        }


    }
}
