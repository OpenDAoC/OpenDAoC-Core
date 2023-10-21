using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
		"&npc",
		EPrivLevel.GM,
		"Various npc commands",
		"/npc say <text>",
		"/npc yell <text>",
		"/npc action <text>",
		"/npc emote <emote>",
		"/npc face <name>",
		"/npc follow <name>",
		"/npc stopfollow",
		"/npc walkto <name> [speed]",
		"/npc target <name>",
		"/npc weapon <slot>",
		"/npc cast <spellLine> <spellID>")]

	public class NpcCommand : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			if (!(client.Player.TargetObject is GameNpc))
			{
				client.Out.SendMessage("You must target an NPC.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			GameNpc npc = client.Player.TargetObject as GameNpc;

			switch (args[1].ToLower())
			{
				case "say":
					{
						if (args.Length < 3)
						{
							client.Player.Out.SendMessage("Usage: /npc say <message>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						string message = string.Join(" ", args, 2, args.Length - 2);
						npc.Say(message);
						break;
					}
				case "yell":
					{
						if (args.Length < 3)
						{
							client.Player.Out.SendMessage("Usage: /npc yell <message>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						string message = string.Join(" ", args, 2, args.Length - 2);
						npc.Yell(message);
						break;
					}
				case "action":
					{
						if (args.Length < 3)
						{
							client.Player.Out.SendMessage("Usage: /npc action <action message>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}
						string action = string.Join(" ", args, 2, args.Length - 2);
						action = "<" + npc.Name + " " + action + " >";
						foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.SAY_DISTANCE))
						{
							player.Out.SendMessage(action, EChatType.CT_Emote, EChatLoc.CL_ChatWindow);
						}
						break;
					}
				case "emote":
					{
						if (args.Length != 3)
						{
							client.Player.Out.SendMessage("Usage: /npc emote <emote>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						EEmote emoteID;
						switch (args[2].ToLower())
						{
							case "angry": emoteID = EEmote.Angry; break;
							case "bang": emoteID = EEmote.BangOnShield; break;
							case "beckon": emoteID = EEmote.Beckon; break;
							case "beg": emoteID = EEmote.Beg; break;
							case "bindalb": emoteID = EEmote.BindAlb; break;
							case "bindhib": emoteID = EEmote.BindHib; break;
							case "bindmid": emoteID = EEmote.BindMid; break;
							case "blush": emoteID = EEmote.Blush; break;
							case "bow": emoteID = EEmote.Bow; break;
							case "charge": emoteID = EEmote.LetsGo; break;
							case "cheer": emoteID = EEmote.Cheer; break;
							case "clap": emoteID = EEmote.Clap; break;
							case "confuse": emoteID = EEmote.Confused; break;
							case "cry": emoteID = EEmote.Cry; break;
							case "curtsey": emoteID = EEmote.Curtsey; break;
							case "dance": emoteID = EEmote.Dance; break;
							case "dismiss": emoteID = EEmote.Dismiss; break;
							case "distract": emoteID = EEmote.Distract; break;
							case "drink": emoteID = EEmote.Drink; break;
							case "flex": emoteID = EEmote.Flex; break;
							case "horsecourbette": emoteID = EEmote.Horse_Courbette; break;
							case "horsegraze": emoteID = EEmote.Horse_Graze; break;
							case "horsenod": emoteID = EEmote.Horse_Nod; break;
							case "horserear": emoteID = EEmote.Horse_rear; break;
							case "horsestartle": emoteID = EEmote.Horse_Startle; break;
							case "horsewhistle": emoteID = EEmote.Horse_whistle; break;
							case "hug": emoteID = EEmote.Hug; break;
							case "induct": emoteID = EEmote.Induct; break;
							case "knock": emoteID = EEmote.Knock; break;
							case "kiss": emoteID = EEmote.BlowKiss; break;
							case "laugh": emoteID = EEmote.Laugh; break;
							case "levelup": emoteID = EEmote.LvlUp; break;
							case "meditate": emoteID = EEmote.Meditate; break;
							case "mememe": emoteID = EEmote.Mememe; break;
							case "berzerkerfrenzy": emoteID = EEmote.MidgardFrenzy; break;
							case "military": emoteID = EEmote.Military; break;
							case "no": emoteID = EEmote.No; break;
							case "listen": emoteID = EEmote.PlayerListen; break;
							case "pickup": emoteID = EEmote.PlayerPickup; break;
							case "prepare": emoteID = EEmote.PlayerPrepare; break;
							case "point": emoteID = EEmote.Point; break;
							case "ponder": emoteID = EEmote.Ponder; break;
							case "pray": emoteID = EEmote.Pray; break;
							case "present": emoteID = EEmote.Present; break;
							case "raise": emoteID = EEmote.Raise; break;
							case "riderhalt": emoteID = EEmote.Rider_Halt; break;
							case "riderlook": emoteID = EEmote.Rider_LookFar; break;
							case "riderstench": emoteID = EEmote.Rider_Stench; break;
							case "riderpet": emoteID = EEmote.Rider_pet; break;
							case "ridertrick": emoteID = EEmote.Rider_Trick; break;
							case "roar": emoteID = EEmote.Roar; break;
							case "rofl": emoteID = EEmote.Rofl; break;
							case "rude": emoteID = EEmote.Rude; break;
							case "salute": emoteID = EEmote.Salute; break;
							case "shrug": emoteID = EEmote.Shrug; break;
							case "slap": emoteID = EEmote.Slap; break;
							case "slit": emoteID = EEmote.Slit; break;
							case "smile": emoteID = EEmote.Smile; break;
							case "boom": emoteID = EEmote.SpellGoBoom; break;
							case "herofrenzy": emoteID = EEmote.StagFrenzy; break;
							case "stagger": emoteID = EEmote.Stagger; break;
							case "surrender": emoteID = EEmote.Surrender; break;
							case "taunt": emoteID = EEmote.Taunt; break;
							case "throwdirt": emoteID = EEmote.ThrowDirt; break;
							case "victory": emoteID = EEmote.Victory; break;
							case "wave": emoteID = EEmote.Wave; break;
							case "worship": emoteID = EEmote.Worship; break;
							case "yawn": emoteID = EEmote.Yawn; break;
							case "yes": emoteID = EEmote.Yes; break;
							default: return;
						}

						npc.Emote(emoteID);
						break;
					}
				case "walkto":
					{
						if (args.Length < 3 || args.Length > 4)
						{
							client.Out.SendMessage("Usage: /npc walkto <targetname> [speed]", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						short speed = 200;
						if (args.Length == 4)
						{
							speed = Convert.ToInt16(args[3]);
						}

						int X = 0;
						int Y = 0;
						int Z = 0;
						switch (args[2].ToLower())
						{
							case "me":
								{
									X = client.Player.X;
									Y = client.Player.Y;
									Z = client.Player.Z;
									break;
								}

							default:
								{
									//Check for players by name in visibility distance
									foreach (GamePlayer targetplayer in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetplayer.Name.ToLower() == args[2].ToLower())
										{
											X = targetplayer.X;
											Y = targetplayer.Y;
											Z = targetplayer.Z;
											break;
										}
									}
									//Check for NPCs by name in visibility distance
									foreach (GameNpc target in npc.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (target.Name.ToLower() == args[2].ToLower())
										{
											X = target.X;
											Y = target.Y;
											Z = target.Z;
											break;
										}
									}
									break;
								}
						}

						if (X == 0 && Y == 0 && Z == 0)
						{
							client.Out.SendMessage("Can't find name " + args[2].ToLower() + " near your target.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						npc.WalkTo(new Point3D(X, Y, Z), speed);
						client.Out.SendMessage("Your target is walking to your location!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						break;
					}
				case "face":
					{
						if (args.Length != 3)
						{
							client.Player.Out.SendMessage("Usage: /npc face <targetname>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						GameLiving target = null;
						switch (args[2].ToLower())
						{
							case "me":
								{
									target = client.Player;
									break;
								}

							default:
								{
									//Check for players by name in visibility distance
									foreach (GamePlayer targetplayer in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetplayer.Name.ToLower() == args[2].ToLower())
										{
											target = targetplayer;
											break;
										}
									}
									//Check for NPCs by name in visibility distance
									foreach (GameNpc targetNpc in npc.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetNpc.Name.ToLower() == args[2].ToLower())
										{
											target = targetNpc;
											break;
										}
									}
									break;
								}
						}

						if (target == null)
						{
							client.Out.SendMessage("Can't find name " + args[2].ToLower() + " near your target.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						npc.TurnTo(target);
						break;
					}
				case "follow":
					{
						if (args.Length != 3)
						{
							client.Player.Out.SendMessage("Usage: /npc follow <targetname>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						GameLiving target = null;
						switch (args[2].ToLower())
						{
							case "me":
								{
									target = client.Player;
									break;
								}

							default:
								{
									//Check for players by name in visibility distance
									foreach (GamePlayer targetplayer in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetplayer.Name.ToLower() == args[2].ToLower())
										{
											target = targetplayer;
											break;
										}
									}
									//Check for NPCs by name in visibility distance
									foreach (GameNpc targetNpc in npc.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetNpc.Name.ToLower() == args[2].ToLower())
										{
											target = targetNpc;
											break;
										}
									}
									break;
								}
						}

						if (target == null)
						{
							client.Out.SendMessage("Can't find name " + args[2].ToLower() + " near your target.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						npc.Follow(target, 128, short.MaxValue);
						break;
					}
				case "stopfollow":
					{
						if (args.Length != 2)
						{
							client.Player.Out.SendMessage("Usage: /npc stopfollow", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						npc.StopFollowing();
						break;
					}
				case "target":
					{
						if (args.Length != 3)
						{
							client.Player.Out.SendMessage("Usage: /npc target <targetName>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						GameLiving target = null;
						switch (args[2].ToLower())
						{
							case "self":
								{
									target = npc;
								}
								break;

							case "me":
								{
									target = client.Player;
								}
								break;

							default:
								{
									//Check for players by name in visibility distance
									foreach (GamePlayer targetplayer in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetplayer.Name.ToLower() == args[2].ToLower())
										{
											target = targetplayer;
											break;
										}
									}
									//Check for NPCs by name in visibility distance
									foreach (GameNpc targetNpc in npc.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE))
									{
										if (targetNpc.Name.ToLower() == args[2].ToLower())
										{
											target = targetNpc;
											break;
										}
									}
									break;
								}
						}

						if (target == null)
						{
							client.Out.SendMessage("Can't find name " + args[2].ToLower() + " near your target.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						npc.TargetObject = target;
						client.Out.SendMessage(npc.Name + " now target " + target.Name + ".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						break;
					}


				case "cast":
					{
						if (args.Length != 4)
						{
							client.Player.Out.SendMessage("Usage: /npc cast <spellLine> <spellID>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							client.Player.Out.SendMessage("(Be sure the npc target something to be able to cast)", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						SpellLine line = SkillBase.GetSpellLine(args[2]);
						List<Spell> spells = SkillBase.GetSpellList(line.KeyName);
						if (spells.Count <= 0)
						{
							client.Out.SendMessage("No spells found in line " + args[2] + "!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						if (spells != null)
						{
							foreach (Spell spl in spells)
							{
								if (spl.ID == Convert.ToInt32(args[3]))
								{
									npc.CastSpell(spl, line);
									return;
								}
							}
						}

						client.Out.SendMessage("Spell with id " + Convert.ToInt16(args[3]) + " not found in db!", EChatType.CT_System, EChatLoc.CL_SystemWindow);

						break;
					}
				case "weapon":
					{
						if (args.Length != 3)
						{
							client.Player.Out.SendMessage("Usage: /npc weapon <activeWeaponSlot>", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						if (Convert.ToInt16(args[2]) < 0 || Convert.ToInt16(args[2]) > 2)
						{
							client.Player.Out.SendMessage("The activeWeaponSlot must be between 0 and 2.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						EActiveWeaponSlot slot = (EActiveWeaponSlot)Convert.ToInt16(args[2]);
						npc.SwitchWeapon(slot);
						client.Player.Out.SendMessage(npc.Name + " will now use its " + Enum.GetName(typeof(EActiveWeaponSlot), slot) + " weapon to attack.", EChatType.CT_System, EChatLoc.CL_SystemWindow);

						break;
					}
				default:
					{
						client.Out.SendMessage("Type /npc for command overview.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					break;
			}
		}
	}
}
