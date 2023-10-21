using System;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[Command(
		 "&keepcomponent",
		 EPrivLevel.GM,
		 "GMCommands.KeepComponents.Description",
		 "GMCommands.KeepComponents.Usage.Create.TID",
		 "GMCommands.KeepComponents.Usage.Create.T",
		 "GMCommands.KeepComponents.Usage.Skin",
		 "/keepcomponent move - move to your position",
		 "/keepcomponent rotate [0 - 3]",
		 "/keepcomponent reload",
		 "'/keepcomponent save' to save the component in the DB",
		 "GMCommands.KeepComponents.Usage.Delete")]
	public class KeepComponentCommand : ACommandHandler, ICommandHandler
	{
		private readonly ushort INVISIBLE_MODEL = 150;

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			AGameKeep myKeep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, WorldMgr.OBJ_UPDATE_DISTANCE);

			if (myKeep == null)
			{
				DisplayMessage(client, "You are not near a keep.");
			}

			switch (args[1])
			{
				#region Create
				case "create":
					{
						if (args.Length < 3)
						{
							int i = 0;
							foreach (string str in Enum.GetNames(typeof(GameKeepComponent.eComponentSkin)))
							{
								client.Out.SendMessage("#" + i + ": " + str, EChatType.CT_System, EChatLoc.CL_SystemWindow);
								i++;
							}
							DisplaySyntax(client);
							return;
						}

						if (myKeep == null)
						{
							DisplaySyntax(client);
							return;
						}

						int skin = 0;
						try
						{
							skin = Convert.ToInt32(args[2]);
						}
						catch
						{
							int i = 0;
							foreach (string str in Enum.GetNames(typeof(GameKeepComponent.eComponentSkin)))
							{
								client.Out.SendMessage("#" + i + ": " + str, EChatType.CT_System, EChatLoc.CL_SystemWindow);
								i++;
							}
							DisplaySyntax(client);
							return;
						}

						if (args.Length >= 4)
						{
							int keepid = 0;
							try
							{
								keepid = Convert.ToInt32(args[3]);
								myKeep = GameServer.KeepManager.GetKeepByID(keepid);
							}
							catch
							{
								DisplaySyntax(client);
								return;
							}
						}

						GameKeepComponent component = new GameKeepComponent();
						component.X = client.Player.X;
						component.Y = client.Player.Y;
						component.Z = client.Player.Z;
						component.ComponentHeading = (client.Player.Heading - myKeep.Heading) / 1024;
						component.Heading = (ushort)(component.ComponentHeading * 1024 + myKeep.Heading);
						component.Keep = myKeep;
						//todo good formula
						//component.ComponentX = (component.X - myKeep.X) / 148;
						//component.ComponentY = (component.Y - myKeep.Y) / 148;

                        double angle = myKeep.Heading * ((Math.PI * 2) / 360); // angle*2pi/360;

                        //component.ComponentX = (int)((148 * Math.Sin(angle) * myKeep.X - 148 * Math.Sin(angle) * client.Player.X + client.Player.Y - myKeep.Y)
                        //    / (148 * Math.Sin(angle) - 148 * 148 * 2 * Math.Sin(angle) * Math.Cos(angle)));
                        //component.ComponentY = (int)((myKeep.Y - client.Player.Y + 148 * Math.Sin(angle) * component.ComponentX) / (148 * Math.Cos(angle)));

                        component.ComponentX = CalcCX(client.Player, myKeep, angle);
                        component.ComponentY = CalcCY(client.Player, myKeep, angle);

						/*
						x = (component.X-myKeep.X)/148 = a*cos(t) - b*sin(t)
						y = (component.Y-myKeep.Y)/148 = a*sin(t) + b*cos(t)
						a = sqrt((x+b*sin(t))^2 + (y-b*cos(t))^2)
						a = sqrt(x�+y�+b� +2*x*b*sin(t)-2*y*b*cos(t))
						b = sqrt((x-a*cos(t))^2 + (y-a*sin(t))^2)
						b = sqrt(x�+y�+a�-2*x*a*cos(t)-2*y*a*sin(t))
						0 = 2x�+2y�-2*x*a*cos(t)-2*y*a*sin(t)+2*x*sqrt(x�+y�+a�-2*x*a*cos(t)-2*y*a*sin(t))*sin(t)-2*y*sqrt(x�+y�+a�-2*x*a*cos(t)-2*y*a*sin(t))*cos(t)
						pfff
						so must find an other way to find it....
						*/
						component.Name = myKeep.Name;
						component.Model = INVISIBLE_MODEL;
						component.Skin = skin;
						component.Level = (byte)myKeep.Level;
						component.CurrentRegion = client.Player.CurrentRegion;
						component.Health = component.MaxHealth;
						component.ID = myKeep.KeepComponents.Count;
						component.Keep.KeepComponents.Add(component);
						component.SaveInDB = true;
						component.AddToWorld();
						component.SaveIntoDatabase();
						client.Out.SendKeepInfo(myKeep);
						client.Out.SendKeepComponentInfo(component);
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepComponents.Create.KCCreated"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					} break;
				#endregion Create
                #region Move
                case "move":
                    {
                        GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;

                        component.X = client.Player.X;
                        component.Y = client.Player.Y;
                        component.Z = client.Player.Z;
                        component.ComponentHeading = (client.Player.Heading - myKeep.Heading) / 1024;
                        component.Heading = (ushort)(component.ComponentHeading * 1024 + myKeep.Heading);
                        component.Keep = myKeep;
                        //todo good formula
                        //component.ComponentX = (component.X - myKeep.X) / 148;
                        //component.ComponentY = (myKeep.Y - component.Y) / 148;
                        double angle = myKeep.Heading * ((Math.PI * 2) / 360); // angle*2pi/360;

                        //component.ComponentX = (int)((148 * Math.Sin(angle) * myKeep.X - 148 * Math.Sin(angle) * client.Player.X + client.Player.Y - myKeep.Y)
                        //    / (148 * Math.Sin(angle) - 148 * 148 * 2 * Math.Sin(angle) * Math.Cos(angle)));
                        //component.ComponentY = (int)((myKeep.Y - client.Player.Y + 148 * Math.Sin(angle) * component.ComponentX) / (148 * Math.Cos(angle)));

                        component.ComponentX = CalcCX(client.Player, myKeep, angle);
                        component.ComponentY = CalcCY(client.Player, myKeep, angle);

                        client.Out.SendKeepInfo(myKeep);
                        client.Out.SendKeepComponentInfo(component);
						client.Out.SendKeepComponentDetailUpdate(component);
						client.Out.SendMessage("Component moved.  Use /keepcomponent save to save, or reload to reload the original position.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    } break;
                #endregion
				#region Rotate
				case "rotate":
					{
						try
						{
							ushort amount = Convert.ToUInt16(args[2]);

							if (amount > 3)
								amount = 3;

							GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;

							component.ComponentHeading = amount;
							component.Heading = (ushort)(component.ComponentHeading * 1024 + myKeep.Heading);

							client.Out.SendKeepInfo(myKeep);
							client.Out.SendKeepComponentInfo(component);
							client.Out.SendKeepComponentDetailUpdate(component);
							client.Out.SendMessage("Component rotated.  Use /keepcomponent save to save, or reload to reload the original position.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
						catch
						{
							DisplayMessage(client, "/keepcomponent rotate [0 - 3]");
						}
					} break;
				#endregion
				#region Skin
                case "skin":
					{
						if (args.Length < 3)
						{
							int i = 0;
							foreach (string str in Enum.GetNames(typeof(GameKeepComponent.eComponentSkin)))
							{
								client.Out.SendMessage("#" + i + ": " + str, EChatType.CT_System, EChatLoc.CL_SystemWindow);
								i++;
							}
							DisplaySyntax(client);
							return;
						}

						int skin = 0;
						try
						{
							skin = Convert.ToInt32(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component == null)
						{
							DisplaySyntax(client);
							return;
						}
						component.Skin = skin;

						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepComponentInfo(component);
							otherPlayer.Out.SendKeepComponentDetailUpdate(component);
						}

						//client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepComponents.Skin.YChangeSkin"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
						client.Out.SendMessage("Component skin updated.  Use /keepcomponent save to save, or reload to reload the original skin.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					} break;
				#endregion Skin
				#region Delete
				case "delete":
					{
						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component == null)
						{
							DisplaySyntax(client);
							return;
						}
						component.RemoveFromWorld();
						component.Delete();
						component.DeleteFromDatabase();
						client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepComponents.Delete.YDeleteKC"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

					} break;
				#endregion Delete
				#region Save
				case "save":
					{
						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component == null)
						{
							DisplaySyntax(client);
							return;
						}
						component.SaveIntoDatabase();
						client.Out.SendMessage(string.Format("Saved ComponentID: {0}, KeepID: {1}, Skin: {2}, Health: {3}%", 
															component.ID, 
															(component.Keep == null ? "0" : component.Keep.KeepID.ToString()), 
															component.Skin, 
															component.HealthPercent), EChatType.CT_System, EChatLoc.CL_SystemWindow);

					} break;
				#endregion Save
				#region Reload
				case "reload":
                    {

                        GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
                        if (component == null)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        var dbcomponent = CoreDb<DbKeepComponent>.SelectObject(DB.Column("KeepID").IsEqualTo(component.Keep.KeepID).And(DB.Column("ID").IsEqualTo(component.ID)));
                        component.ComponentX = dbcomponent.X;
                        component.ComponentY = dbcomponent.Y;
                        component.ComponentHeading = dbcomponent.Heading;
						component.Skin = dbcomponent.Skin;

						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepComponentInfo(component);
							otherPlayer.Out.SendKeepComponentDetailUpdate(component);
						}

                        client.Out.SendMessage("Component Reloaded", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        break;
					}
				#endregion Reload
				#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
				#endregion Default
			}
		}

        public int CalcCX(GamePlayer player, AGameKeep myKeep, double angle)
        {
            if (Math.Abs(Math.Sin(angle)) < 0.0001) //for approximations, == 0 wont work.
            {
                return (player.X - myKeep.X) / 148;
            }
            else
            {
                return (int)((148 * Math.Sin(angle) * myKeep.X - 148 * Math.Sin(angle) * player.X + player.Y - myKeep.Y)
                            / (148 * Math.Sin(angle) - 148 * 148 * 2 * Math.Sin(angle) * Math.Cos(angle)));
            }
        }

        public int CalcCY(GamePlayer player, AGameKeep myKeep, double angle)
        {
            if (Math.Abs(Math.Sin(angle)) < 0.0001)
            {
                return (myKeep.Y - player.Y) / 148;
            }
            else
            {
                int cx = (int)((148 * Math.Sin(angle) * myKeep.X - 148 * Math.Sin(angle) * player.X + player.Y - myKeep.Y)
                            / (148 * Math.Sin(angle) - 148 * 148 * 2 * Math.Sin(angle) * Math.Cos(angle)));
                return (int)((myKeep.Y - player.Y + 148 * Math.Sin(angle) * cx) / (148 * Math.Cos(angle)));
            }
        }
	}
}
