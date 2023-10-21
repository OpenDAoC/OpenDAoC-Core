using System.Reflection;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS
{
    public class InstantRrNpc : GameNpc
    {
        #region Options
        
        int freeRR = 13;
        protected readonly bool AllowGMChangeAmount = true;

        private static new readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        public override bool AddToWorld()
        {
			Name = "Free Realm Ranks";
            Model = 1194;
            Size = 70;
			Flags |= ENpcFlags.PEACE;
			Level = 75;

            return base.AddToWorld();
        }

        public override bool Interact(GamePlayer player)
        {

            if (!base.Interact(player)) return false;
            TurnTo(player, 100);

            #region Player Responses

            if (freeRR > 0)
            {
                player.Out.SendMessage("Greetings, " + player.Name + ", during the alpha test I can give you free [Realmrank " + freeRR + "]!", EChatType.CT_System, EChatLoc.CL_PopupWindow);
            }
            if (freeRR == 0)
            {
                player.Out.SendMessage("I'm sorry, " + player.Name + " I've been told not to grant free realmranks anymore!", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
            }

            #endregion
            #region GM / Admin Responses

            if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
            {
                player.Out.SendMessage("What would you like the Free Realmrank to be? \n" +
                "[disable]\n" +
                "[2L0]\n" +
                "[3L0]\n" +
                "[4L0]\n" +
                "[5L0]\n" +
                "[6L0]\n" +
                "[7L0]\n" +
                "[8L0]\n" +
                "[9L0]\n" +
                "[10L0]\n" +
                "[11L0]\n" +
                "[12L0]\n" +
                "[13L0]\n", EChatType.CT_System, EChatLoc.CL_PopupWindow);
            }
            if (player.Client.Account.PrivLevel == 3)
            {
                player.Out.SendMessage("What would you like the Free Realmrank to be? \n" +
                "[disable]\n" +
                "[2L0]\n" +
                "[3L0]\n" +
                "[4L0]\n" +
                "[5L0]\n" +
                "[6L0]\n" +
                "[7L0]\n" +
                "[8L0]\n" +
                "[9L0]\n" +
                "[10L0]\n" +
                "[11L0]\n" +
                "[12L0]\n" +
                "[13L0]\n", EChatType.CT_System, EChatLoc.CL_PopupWindow);
            }

            #endregion

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {

            #region Realmrank Amounts

            int RR2 = 7125;
            int RR3 = 61750;
            int RR4 = 213875;
            int RR5 = 513500;
            int RR6 = 1010625;
            int RR7 = 1755250;
            int RR8 = 2797375;
            int RR9 = 4187000;
            int RR10 = 5974125;
            int RR11 = 8208750;
            int RR12 = 23308097;
            int RR13 = 66181501;

            #endregion

            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer player = (GamePlayer)source;
            TurnTo(player.X, player.Y);
            switch (str)
            {
                #region Give Free Realmranks
                case "Realmrank 2":
                    {
                        if (player.RealmPoints < RR2 && freeRR == 2)
                        {
                            player.GainRealmPoints((long)(double)(RR2 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 3":
                    {
                        if (player.RealmPoints < RR3 && freeRR == 3)
                        {
                            player.GainRealmPoints((long)(double)(RR3 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 4":
                    {
                        if (player.RealmPoints < RR4 && freeRR == 4)
                        {
                            player.GainRealmPoints((long)(double)(RR4 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 5":
                    {
                        if (player.RealmPoints < RR5 && freeRR == 5)
                        {
                            player.GainRealmPoints((long)(double)(RR5 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 6":
                    {
                        if (player.RealmPoints < RR6 && freeRR == 6)
                        {
                            player.GainRealmPoints((long)(double)(RR6 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 7":
                    {
                        if (player.RealmPoints < RR7 && freeRR == 7)
                        {
                            player.GainRealmPoints((long)(double)(RR3 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 8":
                    {
                        if (player.RealmPoints < RR8 && freeRR == 8)
                        {
                            player.GainRealmPoints((long)(double)(RR8 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 9":
                    {
                        if (player.RealmPoints < RR9 && freeRR == 9)
                        {
                            player.GainRealmPoints((long)(double)(RR9 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 10":
                    {
                        if (player.RealmPoints < RR10 && freeRR == 10)
                        {
                            player.GainRealmPoints((long)(double)(RR10 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 11":
                    {
                        if (player.RealmPoints < RR11 && freeRR == 11)
                        {
                            player.GainRealmPoints((long)(double)(RR11 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 12":
                    {
                        if (player.RealmPoints < RR12 && freeRR == 12)
                        {
                            player.GainRealmPoints((long)(double)(RR12 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                case "Realmrank 13":
                    {
                        if (player.RealmPoints < RR13 && freeRR == 13)
                        {
                            player.GainRealmPoints((long)(double)(RR13 - player.RealmPoints / ServerProperties.Properties.RP_RATE));
                        }
                    }
                    break;
                #endregion
                #region GM/Admin Set Realmranks

                case "disable":
                    {
                        freeRR = 0;
                    }
                    break;
                case "2L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 2;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR2");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 2;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR2");
                        }
                    }
                    break;
                case "3L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 3;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR3");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 3;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR3");
                        }
                    }
                    break;
                case "4L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 4;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR4");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 4;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR4");
                        }
                    }
                    break;
                case "5L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 5;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR5");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 5;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR5");
                        }
                    }
                    break;
                case "6L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 6;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR6");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 6;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR6");
                        }
                    }
                    break;
                case "7L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 7;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR7");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 7;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR7");
                        }
                    }
                    break;
                case "8L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 8;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR8");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 8;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR8");
                        }
                    }
                    break;
                case "9L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 9;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR9");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 9;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR9");
                        }
                    }
                    break;
                case "10L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 10;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR10");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 10;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR10");
                        }
                    }
                    break;
                case "11L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 11;

                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR11");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 11;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR11");
                        }
                    }
                    break;
                case "12L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 12;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR12");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 12;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR12");
                        }
                    }
                    break;
                case "13L0":
                    {
                        if (player.Client.Account.PrivLevel == 2 && AllowGMChangeAmount == true)
                        {
                            freeRR = 13;
                            GameServer.Instance.LogGMAction("GM: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR13");
                        }
                        if (player.Client.Account.PrivLevel == 3)
                        {
                            freeRR = 13;
                            GameServer.Instance.LogGMAction("ADMIN: " + player.Name + "(" + player.Client.Account.Name + ") Changed the Free Realmranks to RR13");
                        }
                    }
                    break;
                #endregion

                default: break;
            }
            return true;
        }
        private void SendReply(GamePlayer target, string msg)
        {
            target.Out.SendMessage(msg, EChatType.CT_System, EChatLoc.CL_PopupWindow);
        }
    }
}


