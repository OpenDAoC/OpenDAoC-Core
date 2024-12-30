using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class DPSDummy : GameTrainingDummy
    {
        private int _damage;
        private DateTime _startTime;
        private TimeSpan _timePassed;
        private bool _startCheck = true;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            _damage = 0;
            _startCheck = true;
            Name = "Total: 0 DPS: 0";

            SetDefaultResists();
            SetDefaultArmor();

            SendReply(player, "Hello, you can change my [armor] and [resistances] with ease, you need but ask. Right click me to reset them back to default.\n\n" +
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
            if (!base.WhisperReceive(source, text))
                return false;

            if (source is not GamePlayer player)
                return false;

            string[] splitText = text.Split(' ');

            if (splitText.Length > 1)
            {
                if (!int.TryParse(splitText[1], out int value))
                {
                    SendReply(player, "Invalid number format");
                    return false;
                }
                else if (value is < 0 or > 100)
                {
                    SendReply(player, "Number must be between 0 and 100");
                    return false;
                }

                switch (splitText[0].ToLower())
                {
                    case "slash":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Slash, value);
                        break;
                    }
                    case "thrust":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Thrust, value);
                        break;
                    }
                    case "crush":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Crush, value);
                        break;
                    }
                    case "body":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Body, value);
                        break;
                    }
                    case "cold":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Cold, value);
                        break;
                    }
                    case "energy":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Energy, value);
                        break;
                    }
                    case "heat":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Heat, value);
                        break;
                    }
                    case "matter":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Matter, value);
                        break;
                    }
                    case "spirit":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Spirit, value);
                        break;
                    }
                    case "block":
                    {
                        BlockChance = (byte) value;
                        break;
                    }
                    case "parry":
                    {
                        ParryChance = (byte) value;
                        break;
                    }
                    case "evade":
                    {
                        EvadeChance = (byte) value;
                        break;
                    }
                    case "allresist":
                    {
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Slash, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Thrust, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Crush, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Body, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Cold, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Energy, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Heat, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Matter, value);
                        ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) eResist.Spirit, value);
                        break;
                    }
                    case "alldefense":
                    {
                        BlockChance = (byte) value;
                        EvadeChance = (byte) value;
                        ParryChance = (byte) value;
                        break;
                    }
                }
            }
            else
            {
                switch (splitText[0].ToLower())
                {
                    case "armor":
                    {
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
                    }
                    case "cloth":
                    {
                        CreateArmorSetOfType(eObjectType.Cloth);
                        break;
                    }
                    case "leather":
                    {
                        CreateArmorSetOfType(eObjectType.Leather);
                        break;
                    }
                    case "studded":
                    {
                        CreateArmorSetOfType(eObjectType.Studded);
                        break;
                    }
                    case "chain":
                    {
                        CreateArmorSetOfType(eObjectType.Chain);
                        break;
                    }
                    case "plate":
                    {
                        CreateArmorSetOfType(eObjectType.Plate);
                        break;
                    }
                    case "reinforced":
                    {
                        CreateArmorSetOfType(eObjectType.Reinforced);
                        break;
                    }
                    case "scale":
                    {
                        CreateArmorSetOfType(eObjectType.Scale);
                        break;
                    }

                    case "resistances":
                    {
                        SendReply(player, "Whisper me the resist type and value you'd like. Example: '/whisper Body 10' will give me +10% Body resist. \n" +
                            "Additionally, you can whisper me 'allresist #' to set all resistances to the number provided." +
                            "");
                        break;
                    }
                    default:
                        break;
                }
            }

            return true;
        }

        private void CreateArmorSetOfType(eObjectType armorType)
        {
            Inventory.ClearInventory();
            ClearAFAndABSBuffs();

            List<int> invSlots =
            [
                Slot.ARMS,
                Slot.FEET,
                Slot.HANDS,
                Slot.HELM,
                Slot.LEGS,
                Slot.TORSO,
            ];

            foreach (int slot in invSlots)
            {
                DbInventoryItem invItem = new()
                {
                    Item_Type = slot,
                    Object_Type = (int) armorType
                };

                invItem = GenerateItemNameModel(invItem);
                invItem = GenerateArmorStats(invItem);
                Inventory.AddItem((eInventorySlot) slot, invItem);
            }

            BroadcastLivingEquipmentUpdate();
            ClientService.UpdateNpcForPlayers(this);
        }

        private DbInventoryItem GenerateArmorStats(DbInventoryItem item)
        {
            eObjectType type = (eObjectType)item.Object_Type;

            if (type is >= eObjectType._FirstArmor and <= eObjectType._LastArmor)
            {
                if (type is eObjectType.GenericArmor)
                    item.DPS_AF = 0;
                else if (type is eObjectType.Cloth)
                    item.DPS_AF = Level;
                else
                    item.DPS_AF = Level * 2;

                item.SPD_ABS = type switch
                {
                    eObjectType.Cloth => 0,
                    eObjectType.Leather => 10,
                    eObjectType.Studded => 19,
                    eObjectType.Reinforced => 19,
                    eObjectType.Chain => 27,
                    eObjectType.Scale => 27,
                    eObjectType.Plate => 34,
                    _ => 0,
                };
            }

            item.Quality = 100;
            item.Condition = 100;
            return item;
        }

        private void ClearAFAndABSBuffs()
        {
            effectListComponent.CancelAll();
        }

        public override double GetArmorAF(eArmorSlot slot)
        {
            // Mostly a copy paste of `GamePlayer.GetArmorAF`. but we ignore the slot.
            DbInventoryItem item = Inventory.GetItem(eInventorySlot.TorsoArmor);

            if (item == null)
                return 0;

            int characterLevel = Level;

            if (Level > 50)
                characterLevel++;

            int armorFactorCap = characterLevel * 2;
            double armorFactor = Math.Min(item.DPS_AF, (eObjectType) item.Object_Type is eObjectType.Cloth ? characterLevel : armorFactorCap);
            armorFactor += BaseBuffBonusCategory[(int) eProperty.ArmorFactor] / 6.0; // Base AF buff.
            armorFactor *= item.Quality * 0.01 * item.Condition / item.MaxCondition; // Apply condition and quality before the second cap. Maybe incorrect, but it makes base AF buffs a little more useful.
            armorFactor = Math.Min(armorFactor, armorFactorCap);
            armorFactor += base.GetArmorAF(slot);
            return armorFactor;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // Mostly a copy paste of `GamePlayer.GetArmorAF`. but we ignore the slot.
            DbInventoryItem item = Inventory.GetItem(eInventorySlot.TorsoArmor);

            if (item == null)
                return 0;

            return Math.Clamp((item.SPD_ABS + GetModified(eProperty.ArmorAbsorption)) * 0.01, 0, 1);
        }

        private void SetDefaultResists()
        {
            foreach (eResist resist in Enum.GetValues<eResist>())
                ApplyBonus(eBuffBonusCategory.BaseBuff, (eProperty) resist, 26);
        }

        private void SetDefaultArmor()
        {
            // Skip generic armor.
            int armorIndex = Util.Random(eObjectType._LastArmor - eObjectType._FirstArmor + 1) + (int) eObjectType._FirstArmor;
            CreateArmorSetOfType((eObjectType) armorIndex);
        }

        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (_startCheck)
            {
                _startTime = DateTime.Now;
                _startCheck = false;
            }

            _damage += ad.Damage + ad.CriticalDamage;
            _timePassed = DateTime.Now - _startTime;
            Name = "Total: " + _damage.ToString() + " DPS: " + (_damage / (_timePassed.TotalSeconds + 1)).ToString("0");
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            Name = "Total: 0 DPS: 0";
            GuildName = "Dummy Union";
            Model = 34;
            Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            SetDefaultResists();
            SetDefaultArmor();
            return true;
        }

        private static void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_Merchant, eChatLoc.CL_PopupWindow);
        }

        private static DbInventoryItem GenerateItemNameModel(DbInventoryItem item)
        {
            eInventorySlot slot = (eInventorySlot) item.Item_Type;

            int model = 0;

            switch ((eObjectType) item.Object_Type)
            {
                case eObjectType.Cloth:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 141;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 140;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 143;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 822;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 142;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            if (Util.Chance(60))
                                model = 139;
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
                    }

                    break;
                }
                case eObjectType.Leather:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 38;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 37;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 40;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 62;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 36;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 39;
                            break;
                        }
                    }

                    break;
                }
                case eObjectType.Studded:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 83;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 82;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 84;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 824;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 81;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 85;
                            break;
                        }
                    }

                break;
                }
                case eObjectType.Plate:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 48;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 47;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 50;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 49;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            if (Util.Chance(25))
                                model = 93;
                            else
                                model = 64;

                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 46;
                            break;
                        }
                    }

                    break;
                }
                case eObjectType.Chain:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 43;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 42;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 45;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 63;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 41;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 44;
                            break;
                        }
                    }

                    break;
                }
                case eObjectType.Reinforced:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 385;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 384;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 387;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 835;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 383;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 386;
                            break;
                        }
                    }

                    break;
                }
                case eObjectType.Scale:
                {
                    switch (slot)
                    {
                        case eInventorySlot.ArmsArmor:
                        {
                            model = 390;
                            break;
                        }
                        case eInventorySlot.LegsArmor:
                        {
                            model = 389;
                            break;
                        }
                        case eInventorySlot.FeetArmor:
                        {
                            model = 392;
                            break;
                        }
                        case eInventorySlot.HeadArmor:
                        {
                            model = 838;
                            break;
                        }
                        case eInventorySlot.TorsoArmor:
                        {
                            model = 388;
                            break;
                        }
                        case eInventorySlot.HandsArmor:
                        {
                            model = 391;
                            break;
                        }
                    }

                    break;
                }
                default:
                    break;
            }

            item.Model = model;
            return item;
        }
    }
}
