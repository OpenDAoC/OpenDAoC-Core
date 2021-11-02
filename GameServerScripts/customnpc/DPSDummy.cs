using DOL.GS.PacketHandler;
using System;

namespace DOL.GS
{
    public class DPSDummy : GameTrainingDummy
    {
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

            SendReply(player, "Hello, you can change my armor and [resistances] with ease, you need but ask. Right click me to reset them back to 0.");
            return true;
        }


        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text)) return false;
            if (!(source is GamePlayer player)) return false;

            string[] splitText = text.Split(' ');
            if(splitText.Length > 1)
            {
                if(!double.TryParse(splitText[1], out _) || double.Parse(splitText[1]) < 0 || double.Parse(splitText[1]) > 70) { 
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
                }
            }
            else
            {
                switch (splitText[0].ToLower())
                {
                    case "resistances":
                        SendReply(player, "Whisper me the resist type and value you'd like. Example: '/whisper Body 10' will give me +10% Body resist");
                        break;
                    default:
                        ResetArmorAndResists();
                        break;
                }
            }
            

            return true;
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
            //we will figure out armor next 
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
            Name = "Total: " + Damage.ToString() +" DPS: " + (Damage / (TimePassed.TotalSeconds + 1)).ToString("0");
        }

        public override bool AddToWorld()
        {
            Name = "Total: 0 DPS: 0";
            GuildName = "DPS Dummy";
            Model = 34;
            return base.AddToWorld(); // Finish up and add him to the world.
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_Merchant, eChatLoc.CL_PopupWindow);
        }

    }
}
