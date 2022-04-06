using DOL.Database;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;

namespace DOL.GS {
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
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Slash, double.Parse(splitText[1]), 1, false);
                        break;
                    case "thrust":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Thrust, double.Parse(splitText[1]), 1, false);
                        break;
                    case "crush":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Crush, double.Parse(splitText[1]), 1, false);
                        break;
                    case "body":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Body, double.Parse(splitText[1]), 1, false);
                        break;
                    case "cold":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Cold, double.Parse(splitText[1]), 1, false);
                        break;
                    case "energy":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Energy, double.Parse(splitText[1]), 1, false);
                        break;
                    case "heat":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Heat, double.Parse(splitText[1]), 1, false);
                        break;
                    case "matter":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Matter, double.Parse(splitText[1]), 1, false);
                        break;
                    case "spirit":
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Spirit, double.Parse(splitText[1]), 1, false);
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
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Slash, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Thrust, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Crush, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Body, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Cold, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Energy, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Heat, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Matter, double.Parse(splitText[1]), 1, false);
                        ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Spirit, double.Parse(splitText[1]), 1, false);
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
                        CreateArmorSetOfType(eObjectType.Cloth);
                        
                        break;
                    case "leather":
                        CreateArmorSetOfType(eObjectType.Leather);
                        break;
                    case "studded":
                        CreateArmorSetOfType(eObjectType.Studded);
                        break;
                    case "chain":
                        CreateArmorSetOfType(eObjectType.Chain);
                        break;
                    case "plate":
                        CreateArmorSetOfType(eObjectType.Plate);
                        break;
                    case "reinforced":
                        CreateArmorSetOfType(eObjectType.Reinforced);
                        break;
                    case "scale":
                        CreateArmorSetOfType(eObjectType.Scale);
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

        private void CreateArmorSetOfType(eObjectType armorType)
        {
            ClearAFAndABSBuffs();
            Inventory.ClearInventory();
            List<int> invSlots = new List<int>();
            invSlots.Add(Slot.ARMS);
            invSlots.Add(Slot.FEET);
            invSlots.Add(Slot.HANDS);
            invSlots.Add(Slot.HELM);
            invSlots.Add(Slot.LEGS);
            invSlots.Add(Slot.TORSO);

            foreach (var slot in invSlots)
            {
                InventoryItem invItem = new InventoryItem();
                invItem.Item_Type = slot;
                invItem.Object_Type = (int)armorType;
                invItem = GenerateItemNameModel(invItem);
                invItem = GenerateArmorStats(invItem);
                this.ItemBonus[eProperty.ArmorFactor] += invItem.DPS_AF;
                this.ItemBonus[eProperty.ArmorAbsorption] += invItem.SPD_ABS;
                //Console.WriteLine($"AF item prop{ItemBonus[eProperty.ArmorFactor]}");
                ApplyBonus(this, eBuffBonusCategory.Other, eProperty.ArmorFactor, invItem.DPS_AF, 1, false);
                //ApplyBonus(this, eBuffBonusCategory.Other, eProperty.ArmorAbsorption, invItem.SPD_ABS, 1, false);
                Inventory.AddItem((eInventorySlot)slot, invItem);
            }

            this.ItemBonus[eProperty.ArmorAbsorption] += GetAbsorb(armorType);
            BroadcastLivingEquipmentUpdate();
            BroadcastUpdate();
        }

        private InventoryItem GenerateArmorStats(InventoryItem item)
        {
            eObjectType type = (eObjectType)item.Object_Type;

            //set dps_af and spd_abs
            if ((int)type >= (int)eObjectType._FirstArmor && (int)type <= (int)eObjectType._LastArmor)
            {
                if (type == eObjectType.Cloth)
                    item.DPS_AF = Level;
                else item.DPS_AF = Level * 2;
                //item.SPD_ABS = GetAbsorb(type);
            }
            item.Quality = 99;
            item.Condition = 100;
            return item;
        }

        private void ClearAFAndABSBuffs()
        {
            
            if (GetModified(eProperty.ArmorFactor) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.Other, eProperty.ArmorFactor, GetModified(eProperty.ArmorFactor), 1, true);
            }
            if (GetModified(eProperty.ArmorAbsorption) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.Other, eProperty.ArmorFactor, GetModified(eProperty.ArmorAbsorption), 1, true);
            }
            if (this.ItemBonus[eProperty.ArmorFactor] > 0)
            {
                ItemBonus[eProperty.ArmorFactor] = 0;
            }
            if (this.ItemBonus[eProperty.ArmorAbsorption] > 0)
            {
                ItemBonus[eProperty.ArmorAbsorption] = 0;
            }
        }

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

        private void ResetResists()
        {
            if (GetResist(eDamageType.Slash) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Slash, GetResist(eDamageType.Slash), 1, true);
            }
            if (GetResist(eDamageType.Crush) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Crush, GetResist(eDamageType.Crush), 1, true);
            }
            if (GetResist(eDamageType.Thrust) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Thrust, GetResist(eDamageType.Thrust), 1, true);
            }
            if (GetResist(eDamageType.Natural) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Natural, GetResist(eDamageType.Natural), 1, true);
            }
            if (GetResist(eDamageType.Body) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Body, GetResist(eDamageType.Body), 1, true);
            }
            if (GetResist(eDamageType.Cold) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Cold, GetResist(eDamageType.Cold), 1, true);
            }
            if (GetResist(eDamageType.Energy) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Energy, GetResist(eDamageType.Energy), 1, true);
            }
            if (GetResist(eDamageType.Heat) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Heat, GetResist(eDamageType.Heat), 1, true);
            }
            if (GetResist(eDamageType.Matter) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Matter, GetResist(eDamageType.Matter), 1, true);
            }
            if (GetResist(eDamageType.Spirit) > 0)
            {
                ApplyBonus(this, eBuffBonusCategory.BaseBuff, (eProperty)eResist.Spirit, GetResist(eDamageType.Spirit), 1, true);
            }
        }

        private void ResetArmor()
        {
            ClearAFAndABSBuffs();
            CreateArmorSetOfType(eObjectType.GenericArmor);
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
            GuildName = "Atlas Dummy Union";
            Model = 34;
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            return base.AddToWorld(); // Finish up and add him to the world.
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_Merchant, eChatLoc.CL_PopupWindow);
        }

        private InventoryItem GenerateItemNameModel(InventoryItem item)
        {
            eInventorySlot slot = (eInventorySlot)item.Item_Type;
            eDamageType damage = (eDamageType)item.Type_Damage;
            eRealm realm = (eRealm)this.Realm;
            eObjectType type = (eObjectType)item.Object_Type;

            int model = 488;
            switch (type)
            {
                //armor
                case eObjectType.Cloth:
                    {

                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 141; break;
                            case eInventorySlot.LegsArmor: model = 140; break;
                            case eInventorySlot.FeetArmor: model = 143; break;
                            case eInventorySlot.HeadArmor: model = 822; break;
                            case eInventorySlot.HandsArmor: model = 142; break;
                            case eInventorySlot.TorsoArmor:
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
                case eObjectType.Leather:
                    {

                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 38; break;
                            case eInventorySlot.LegsArmor: model = 37; break;
                            case eInventorySlot.FeetArmor: model = 40; break;
                            case eInventorySlot.HeadArmor: model = 62; break;
                            case eInventorySlot.TorsoArmor: model = 36; break;
                            case eInventorySlot.HandsArmor: model = 39; break;
                        }
                        break;
                    }
                case eObjectType.Studded:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 83; break;
                            case eInventorySlot.LegsArmor: model = 82; break;
                            case eInventorySlot.FeetArmor: model = 84; break;
                            case eInventorySlot.HeadArmor: model = 824; break;
                            case eInventorySlot.TorsoArmor: model = 81; break;
                            case eInventorySlot.HandsArmor: model = 85; break;
                        }
                        break;
                    }
                case eObjectType.Plate:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 48; break;
                            case eInventorySlot.LegsArmor: model = 47; break;
                            case eInventorySlot.FeetArmor: model = 50; break;
                            case eInventorySlot.HandsArmor: model = 49; break;
                            case eInventorySlot.HeadArmor:
                                if (Util.Chance(25))
                                {
                                    model = 93;
                                }
                                else
                                    model = 64;

                                break;

                            case eInventorySlot.TorsoArmor:
                                model = 46;
                                break;
                        }

                        break;
                    }
                case eObjectType.Chain:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 43; break;
                            case eInventorySlot.LegsArmor: model = 42; break;
                            case eInventorySlot.FeetArmor: model = 45; break;
                            case eInventorySlot.HeadArmor: model = 63; break;
                            case eInventorySlot.TorsoArmor: model = 41; break;
                            case eInventorySlot.HandsArmor: model = 44; break;
                        }
                        break;
                    }
                case eObjectType.Reinforced:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 385; break;
                            case eInventorySlot.LegsArmor: model = 384; break;
                            case eInventorySlot.FeetArmor: model = 387; break;
                            case eInventorySlot.HeadArmor: model = 835; break;
                            case eInventorySlot.TorsoArmor: model = 383; break;
                            case eInventorySlot.HandsArmor: model = 386; break;
                        }

                        break;
                    }
                case eObjectType.Scale:
                    {
                        switch (slot)
                        {
                            case eInventorySlot.ArmsArmor: model = 390; break;
                            case eInventorySlot.LegsArmor: model = 389; break;
                            case eInventorySlot.FeetArmor: model = 392; break;
                            case eInventorySlot.HeadArmor: model = 838; break;
                            case eInventorySlot.TorsoArmor: model = 388; break;
                            case eInventorySlot.HandsArmor: model = 391; break;
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
