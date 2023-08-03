
using System;
using System.Collections;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
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
	public class KeepGuardCommand : AbstractCommandHandler, ICommandHandler
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
										guard = new GuardArcherStatic();
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
										guard = new GuardCasterStatic();
									else
										guard = new GuardCaster();
									break;
								}
							#endregion Caster
							#region Merchant
							case "merchant":
							{
								guard = new RvrCraftingMerchant();
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

									AbstractGameKeep.eKeepType keepType = AbstractGameKeep.eKeepType.Any;

									if (args.Length < 5)
									{
										DisplayMessage(client, "You need to provide the type of keep this patrol works with.");
										int i = 0;
										foreach (string str in Enum.GetNames(typeof(Keeps.AbstractGameKeep.eKeepType)))
										{
											DisplayMessage(client, "#" + i + ": " + str);
											i++;
										}
										return;
									}

									try
									{
										keepType = (AbstractGameKeep.eKeepType)Convert.ToInt32(args[4]);
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
									GuardPatrol p = new GuardPatrol(c);
									p.PatrolID = args[3];
									p.KeepType = keepType;
									p.SpawnPosition = GuardPositionMgr.CreatePatrolPosition(p.PatrolID, c, client.Player, keepType);
									p.PatrolID = p.SpawnPosition.TemplateID;
									p.InitialiseGuards();
									DisplayMessage(client, "Patrol created for Keep Type " + Enum.GetName(typeof(AbstractGameKeep.eKeepType), keepType));
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
                                    guard = new GateKeeper();
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

							DbKeepPositions pos = GuardPositionMgr.CreatePosition(guard.GetType(), height, client.Player, Guid.NewGuid().ToString(), component);
							//PositionMgr.AddPosition(pos);
							//PositionMgr.FillPositions();
							DbKeepPositions[] list = component.Positions[pos.TemplateID] as DbKeepPositions[];
							if (list == null)
							{
								list = new DbKeepPositions[4];
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
									AbstractGameKeep keep = (area as KeepArea).Keep;
									guard.Component = new GameKeepComponent();
									guard.Component.Keep = keep;
									break;
								}
							}

							guard.RefreshTemplate();
							guard.AddToWorld();

							if (guard.Component != null && guard.Component.Keep != null)
								guard.Component.Keep.Guards.Add(DOL.Database.UniqueID.IdGenerator.GenerateId(), guard);
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

									DbKeepPositions pos = GuardPositionMgr.CreatePosition(guard.GetType(), height, client.Player, guard.TemplateID, guard.Component);
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
									DbKeepPositions pos = guard.Position;
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

									PathPointUtil startpoint = new PathPointUtil(client.Player.X, client.Player.Y, client.Player.Z, short.MaxValue, ePathType.Once);
									client.Player.TempProperties.setProperty(TEMP_PATH_FIRST, startpoint);
									client.Player.TempProperties.setProperty(TEMP_PATH_LAST, startpoint);
									client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.KeepGuard.Path.CreationStarted"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
									CreateTempPathObject(client, startpoint, "TMP PP 1");
									break;
								}
							#endregion Create
							#region Add
							case "add":
								{
									PathPointUtil path = (PathPointUtil)client.Player.TempProperties.getProperty<object>(TEMP_PATH_LAST, null);
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

									PathPointUtil newpp = new PathPointUtil(client.Player.X, client.Player.Y, client.Player.Z, speedlimit, path.Type);
									path.Next = newpp;
									newpp.Prev = path;
									client.Player.TempProperties.setProperty(TEMP_PATH_LAST, newpp);

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
									PathPointUtil path = (PathPointUtil)client.Player.TempProperties.getProperty<object>(TEMP_PATH_LAST, null);
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

									path.Type = ePathType.Loop;
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

		private void CreateTempPathObject(GameClient client, PathPointUtil pp, string name)
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
			ArrayList objs = (ArrayList)client.Player.TempProperties.getProperty<object>(TEMP_PATH_OBJS, null);
			if (objs == null)
				objs = new ArrayList();
			objs.Add(obj);
			client.Player.TempProperties.setProperty(TEMP_PATH_OBJS, objs);
		}

		private void RemoveAllTempPathObjects(GameClient client)
		{
			ArrayList objs = (ArrayList)client.Player.TempProperties.getProperty<object>(TEMP_PATH_OBJS, null);
			if (objs == null)
				return;
			foreach (GameStaticItem obj in objs)
				obj.Delete();
			client.Player.TempProperties.setProperty(TEMP_PATH_OBJS, null);
		}
	}
}