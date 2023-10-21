using System;
using System.Collections;
using Core.Database;
using Core.Database.UniqueID;
using Core.GS.Keeps;
using Core.GS.Movement;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS.Commands
{
	/// <summary>
	/// Various keep guard commands
	/// </summary>
	[Command(
		"&keepguard",
		EPrivLevel.GM,
		"GMCommands.KeepGuard.Description",
		"GMCommands.KeepGuard.Information",
		"GMCommands.KeepGuard.Usage.Create",
		"GMCommands.KeepGuard.Usage.Position.Add",
		"GMCommands.KeepGuard.Usage.Position.Remove",
		"GMCommands.KeepGuard.Usage.Path.Create",
		"GMCommands.KeepGuard.Usage.Path.Add",
		"GMCommands.KeepGuard.Usage.Path.Save")]
	public class KeepGuardCommand : ACommandHandler, ICommandHandler
	{
		/// <summary>
		/// The command handler itself
		/// </summary>
		/// <param name="client">The client using the command</param>
		/// <param name="args">The command arguments</param>
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1].ToLower())
			{
				#region Create
				case "create":
					{
						GameKeepGuard guard = null;
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						
						switch (args[2].ToLower())
						{
							#region Lord
							case "lord":
								{
									guard = new GuardLord();
									break;
								}
							#endregion Lord
							#region Fighter
							case "fighter":
								{
									guard = new GuardFighter();
									break;
								}
							#endregion Fighter
							#region Commander
							case "commander":
								{
									guard = new GuardCommander();
									break;
								}
							#endregion Commander
							#region Archer
							case "archer":
								{
									if (args.Length > 3)
										guard = new GuardStaticArcher();
									else
										guard = new GuardArcher();
									break;
								}
							#endregion Archer
							#region Healer
							case "healer":
								{
									guard = new GuardHealer();
									break;
								}
							#endregion Healer
							#region Stealther
							case "stealther":
								{
									guard = new GuardStealther();
									break;
								}
							#endregion Stealther
							#region Caster
							case "caster":
								{
									if (args.Length > 3)
										guard = new GuardStaticCaster();
									else
										guard = new GuardCaster();
									break;
								}
							#endregion Caster
							#region Merchant
							case "merchant":
							{
								guard = new GuardMerchant();
								break;
							}
							#endregion Merchant
							#region CurrencyMerchant
							case "currencymerchant":
							{
								guard = new GuardCurrencyMerchant();
								break;
							}
							#endregion CurrencyMerchant
							#region Hastener
							case "hastener":
								{
									guard = new FrontierHastener();
									break;
								}
							#endregion Hastener
							#region Mission
							case "mission":
								{
									guard = new MissionMaster();
									break;
								}
							#endregion Mission
							#region Patrol
							case "patrol":
								{
									if (args.Length < 4)
									{
										DisplayMessage(client, "You need to provide a name for this patrol.");
										return;
									}

									AGameKeep.eKeepType keepType = AGameKeep.eKeepType.Any;

									if (args.Length < 5)
									{
										DisplayMessage(client, "You need to provide the type of keep this patrol works with.");
										int i = 0;
										foreach (string str in Enum.GetNames(typeof(Keeps.AGameKeep.eKeepType)))
										{
											DisplayMessage(client, "#" + i + ": " + str);
											i++;
										}
										return;
									}

									try
									{
										keepType = (AGameKeep.eKeepType)Convert.ToInt32(args[4]);
									}
									catch
									{
										DisplayMessage(client, "Type of keep specified was not valid.");
										return;
									}


									if (client.Player.TargetObject is GameKeepComponent == false)
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Create.NoKCompTarget"));
										return;
									}
									GameKeepComponent c = client.Player.TargetObject as GameKeepComponent;;
									KeepGuardPatrol p = new KeepGuardPatrol(c);
									p.PatrolID = args[3];
									p.KeepType = keepType;
									p.SpawnPosition = GuardPositionMgr.CreatePatrolPosition(p.PatrolID, c, client.Player, keepType);
									p.PatrolID = p.SpawnPosition.TemplateID;
									p.InitialiseGuards();
									DisplayMessage(client, "Patrol created for Keep Type " + Enum.GetName(typeof(AGameKeep.eKeepType), keepType));
									return;
								}
							#endregion Patrol
							#region CorpseSummoner
                            case "corpsesummoner":
                                {
                                    guard = new GuardCorpseSummoner();
                                    break;
                                }
                            #endregion CorpseSummoner
                            #region GateKeeper
                            case "gatekeeperin":
                                {
                                    guard = new GateKeeperIn();
                                    break;
                                }
                            case "gatekeeperout":
                                {
                                    guard = new GateKeeperOut();
                                    break;
                                }
                            #endregion GateKeeper
						}

						if (guard == null)
						{
							DisplaySyntax(client);
							return;
						}

						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component != null)
						{
							int height = component.Height;
							if (args.Length > 4)
								int.TryParse(args[4], out height);

							DbKeepPosition pos = GuardPositionMgr.CreatePosition(guard.GetType(), height, client.Player, Guid.NewGuid().ToString(), component);
							//PositionMgr.AddPosition(pos);
							//PositionMgr.FillPositions();
							DbKeepPosition[] list = component.Positions[pos.TemplateID] as DbKeepPosition[];
							if (list == null)
							{
								list = new DbKeepPosition[4];
								component.Positions[pos.TemplateID] = list;
							}
								
							list[pos.Height] = pos;
							component.LoadPositions();
							component.FillPositions();
						}
						else
						{
							guard.CurrentRegion = client.Player.CurrentRegion;
							guard.X = client.Player.X;
							guard.Y = client.Player.Y;
							guard.Z = client.Player.Z;
							guard.Heading = client.Player.Heading;
							guard.Realm = guard.CurrentZone.Realm;
                            guard.LoadedFromScript = false;
                            guard.SaveIntoDatabase();
							
							foreach (AbstractArea area in guard.CurrentAreas)
							{
								if (area is KeepArea)
								{
									AGameKeep keep = (area as KeepArea).Keep;
									guard.Component = new GameKeepComponent();
									guard.Component.Keep = keep;
									break;
								}
							}

							guard.RefreshTemplate();
							guard.AddToWorld();

							if (guard.Component != null && guard.Component.Keep != null)
								guard.Component.Keep.Guards.Add(IdGenerator.GenerateID(), guard);
						}

						GuardPositionMgr.FillPositions();

						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Create.GuardAdded"));
						break;
					}
				#endregion Create
				#region Position
				case "position":
					{
						switch (args[2].ToLower())
						{
							#region Add
							case "add":
								{
									if (!(client.Player.TargetObject is GameKeepGuard))
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Position.TargetGuard"));
										return;
									}

									if (args.Length != 4)
									{
										DisplaySyntax(client);
										return;
									}

									byte height = byte.Parse(args[3]);
									//height = KeepMgr.GetHeightFromLevel(height);
									GameKeepGuard guard = client.Player.TargetObject as GameKeepGuard;
									
									if (GuardPositionMgr.GetPosition(guard) != null)
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Position.PAlreadyAss", height));
										return;
									}

									DbKeepPosition pos = GuardPositionMgr.CreatePosition(guard.GetType(), height, client.Player, guard.TemplateID, guard.Component);
									GuardPositionMgr.AddPosition(pos);
									GuardPositionMgr.FillPositions();

									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Position.GuardPAdded"));
									break;
								}
							#endregion Add
							#region Remove
							case "remove":
								{
									if (!(client.Player.TargetObject is GameKeepGuard))
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Position.TargetGuard"));
										return;
									}

									GameKeepGuard guard = client.Player.TargetObject as GameKeepGuard;
									DbKeepPosition pos = guard.Position;
									if (pos != null)
									{
										GuardPositionMgr.RemovePosition(pos);

										if (guard.LoadedFromScript)
										{
											if (guard.PatrolGroup != null)
											{
												foreach (GameKeepGuard g in guard.PatrolGroup.PatrolGuards)
												{
													g.Delete();
												}
											}
											else
											{
												guard.Delete();
											}
										}
									}

									GuardPositionMgr.FillPositions();

									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Position.GuardRemoved"));
									break;
								}
							#endregion Remove
							#region Default
							default:
								{
									DisplaySyntax(client);
									return;
								}
							#endregion Default
						}
						break;
					}
				#endregion Position
				#region Path
				case "path":
					{
						switch (args[2].ToLower())
						{
							#region Create
							case "create":
								{
									RemoveAllTempPathObjects(client);

									PathPoint startpoint = new PathPoint(client.Player.X, client.Player.Y, client.Player.Z, short.MaxValue, EPathType.Once);
									client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, startpoint);
									client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, startpoint);
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.CreationStarted"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									CreateTempPathObject(client, startpoint, "TMP PP 1");
									break;
								}
							#endregion Create
							#region Add
							case "add":
								{
									PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
									if (path == null)
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.NoPathCreatedYet"));
										return;
									}

									short speedlimit = 1000;
									if (args.Length == 4)
									{
										try
										{
											speedlimit = short.Parse(args[3]);
										}
										catch
										{
											DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.NoValidSpLimit", args[2]));
											return;
										}
									}

									PathPoint newpp = new PathPoint(client.Player.X, client.Player.Y, client.Player.Z, speedlimit, path.Type);
									path.Next = newpp;
									newpp.Prev = path;
									client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, newpp);

									int len = 0;
									while (path.Prev != null)
									{
										len++;
										path = path.Prev;
									}
									len += 2;

									CreateTempPathObject(client, newpp, "TMP PP " + len);
									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.PPAdded", len));
									break;
								}
							#endregion Add
							#region Save
							case "save":
								{
									PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
									if (path == null)
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.NoPathCreatedYet"));
										return;
									}

									GameKeepGuard guard = client.Player.TargetObject as GameKeepGuard;
									if (guard == null || guard.PatrolGroup == null)
									{
										DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.TargPatrolGuard"));
										return;
									}

									path.Type = EPathType.Loop;
									GuardPositionMgr.SavePatrolPath(guard.TemplateID, path, guard.Component);
									DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.Saved"));
									RemoveAllTempPathObjects(client);
									guard.PatrolGroup.InitialiseGuards();

									GuardPositionMgr.FillPositions();

									DisplayMessage(client, "Patrol groups initialized!");

									break;
								}
							#endregion Save
							#region Default
							default:
								{
									DisplaySyntax(client);
									return;
								}
							#endregion Default
						}
						break;
					}
				#endregion Path
				#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
				#endregion Default
			}
		}

		protected string TEMP_PATH_FIRST = "TEMP_PATH_FIRST";
		protected string TEMP_PATH_LAST = "TEMP_PATH_LAST";
		protected string TEMP_PATH_OBJS = "TEMP_PATH_OBJS";

		private void CreateTempPathObject(GameClient client, PathPoint pp, string name)
		{
			GameStaticItem obj = new GameStaticItem();
			obj.X = pp.X;
			obj.Y = pp.Y;
			obj.Z = pp.Z;
			obj.CurrentRegion = client.Player.CurrentRegion;
			obj.Heading = client.Player.Heading;
			obj.Name = name;
			obj.Model = 488;
			obj.Emblem = 0;
			obj.AddToWorld();
			ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS, null);
			if (objs == null)
				objs = new ArrayList();
			objs.Add(obj);
			client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, objs);
		}

		private void RemoveAllTempPathObjects(GameClient client)
		{
			ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS, null);
			if (objs == null)
				return;
			foreach (GameStaticItem obj in objs)
				obj.Delete();
			client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, null);
		}
	}
}
