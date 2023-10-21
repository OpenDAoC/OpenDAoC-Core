using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Expansions.LabyrinthOfTheMinotaur;
using Core.GS.Languages;
using Core.GS.Skills;

namespace Core.GS.Commands
{
    [Command(
     "&minorelic",
     EPrivLevel.GM,
     "GMCommands.MinoRelic.Description",
     "GMCommands.MinoRelic.Usage.Create",
     "GMCommands.MinoRelic.Usage.MoveHere",
     "GMCommands.MinoRelic.Usage.Name",
	 "GMCommands.MinoRelic.Usage.Spell",
     "GMCommands.MinoRelic.Usage.Model",
     "GMCommands.MinoRelic.Usage.Effect",
     "GMCommands.MinoRelic.Usage.Info",
     "GMCommands.MinoRelic.Usage.DeSpawn",
     "GMCommands.MinoRelic.Usage.Remove",
	 "GMCommands.MinoRelic.Usage.XP",
     "GMCommands.MinoRelic.Usage.ShowAll",
     "GMCommands.MinoRelic.Usage.Spawn")]
    public class MinoRelicCommand : ACommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
			{
				#region Create
				case "create":
                    {
                        if (args.Length != 7 || (!args[4].ToLower().Equals("group") && !args[4].ToLower().Equals("self") && !args[4].ToLower().Equals("realm")))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        DbMinotaurRelic relic = new DbMinotaurRelic();

                        relic.Name = args[2];

                        relic.SpawnHeading = client.Player.Heading;
                        relic.SpawnX = client.Player.X;
                        relic.SpawnY = client.Player.Y;
                        relic.SpawnZ = client.Player.Z;
                        relic.SpawnRegion = client.Player.CurrentRegionID;

                        relic.relicTarget = args[4].ToLower();

                        try
                        {
                            relic.relicSpell = Convert.ToInt32(args[5]); 
                            relic.Model = Convert.ToUInt16(args[3]);
                            relic.Effect = Convert.ToInt32(args[6]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.RelicID = MinotaurRelicMgr.GetRelicCount() + 1;
                        GameServer.Database.AddObject(relic);

                        MinotaurRelic rrelic = new MinotaurRelic(relic);
                        rrelic.AddToWorld();

                        MinotaurRelicMgr.AddRelic(rrelic);

						break;
					}
				#endregion Create
				#region MoveHere
				case "movehere":
                    {
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.Heading = client.Player.Heading;
                        relic.X = client.Player.X;
                        relic.Y = client.Player.Y;
                        relic.Z = client.Player.Z;
                        relic.CurrentRegionID = client.Player.CurrentRegionID;

                        relic.SpawnHeading = client.Player.Heading;
                        relic.SpawnX = client.Player.X;
                        relic.SpawnY = client.Player.Y;
                        relic.SpawnZ = client.Player.Z;
                        relic.SpawnRegion = client.Player.CurrentRegionID;

                        relic.SaveIntoDatabase();

						break;
					}
				#endregion MoveHere
				#region Model
				case "model":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.Model = Convert.ToUInt16(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

						break;
					}
				#endregion Model
				#region Name
				case "name":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.Name = args[2];

                        relic.SaveIntoDatabase();

						break;
					}
				#endregion Name
				#region Spell
				case "spell":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.RelicSpellID = Convert.ToInt32(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

						break;
					}
				#endregion Spell
				#region Effect
				case "effect":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.Effect = Convert.ToInt32(args[2]);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        relic.SaveIntoDatabase();

						break;
					}
				#endregion Effect
				#region Info
				case "info":
					{
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

						var info = new List<string>();
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.RelicInfo"));
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.Name", relic.Name));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.ID", relic.RelicID));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.CurrentXP", relic.XP));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.Level", relic.Level));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.Effect", relic.Effect));
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.PositionInfo"));
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnX", relic.SpawnX));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnY", relic.SpawnX));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnZ", relic.SpawnZ));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnHeading" + relic.SpawnHeading));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpawnRegion", relic.SpawnRegion));
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellInfo"));
                        info.Add("===========================");
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellID", relic.RelicSpell));
						info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellTarget", relic.RelicTarget));

                        Spell spell = SkillBase.GetSpellByID(relic.RelicSpellID);
                        if (spell != null)
                        {
							info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellName", spell.Name));
							info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellType", spell.SpellType));
							info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Info.SpellDuration", (spell.Duration / 1000)));
                        }

						client.Out.SendCustomTextWindow("[ " + relic.Name + " ]", info);
						break;
					}
				#endregion Info
				#region DeSpawn
				case "despawn":
                    {
                        if (!(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        relic.XP = 0;
                        relic.RemoveFromWorld();
                        relic.RelicDispose();
						break;
					}
				#endregion DeSpawn
				#region Remove
				case "remove":
                    {
                        //Lifeflight: Added the option to remove a minorelic by supplying an ID.
                        if (args.Length == 3)
                        {
                            int minorelicID = 0;
                            try
                            {
                                minorelicID = Convert.ToInt32(args[2]);
                            }
                            catch (Exception)
                            {

                            }

                            if (minorelicID == 0)
                            {
                                DisplaySyntax(client);
                                return;
                            }
                            else
                            {

                                foreach (MinotaurRelic relic in MinotaurRelicMgr.m_minotaurrelics.Values)
                                {
                                    if (relic != null)
                                    {
                                        if (relic.RelicID == minorelicID)
                                        {
                                            //there is a match!
                                            //remove it from the world
                                            relic.RemoveFromWorld();
                                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the world", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            
                                            //remove it from the hashtable
                                            MinotaurRelicMgr.RemoveRelic(relic);
                                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the Minorelic Hash Table", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            
                                            DataObject obj = GameServer.Database.FindObjectByKey<DbMinotaurRelic>(relic.RelicID);
                                            if (obj != null)
                                            {
                                                GameServer.Database.DeleteObject(obj);
                                                client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the database!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            
                                            }
                                            
                                            break;
                                        }
                                    }
                                }
                        

                            }

                        }
                        else
                        {
                            if (!(client.Player.TargetObject is MinotaurRelic))
                            {
                                DisplaySyntax(client);
                                return;
                            }

                            MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                            relic.RemoveFromWorld();
                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the world", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            
                            //remove it from the hashtable
                            MinotaurRelicMgr.RemoveRelic(relic);
                            client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the Minorelic Hash Table", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            

                            DataObject obj = GameServer.Database.FindObjectByKey<DbMinotaurRelic>(relic.RelicID);
                            if (obj != null)
                            {
                                GameServer.Database.DeleteObject(obj);
                                client.Player.Out.SendMessage("Relic " + relic.RelicID + " has been removed from the database!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                                            
                            }
                        }
						break;
					}
				#endregion Remove
				#region XP
				case "xp":
                    {
                        if (args.Length != 3 || !(client.Player.TargetObject is MinotaurRelic))
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = client.Player.TargetObject as MinotaurRelic;

                        try
                        {
                            relic.XP += Convert.ToInt32(args[2]);
                            if (relic.Owner != null)
                                relic.Owner.Out.SendMinotaurRelicBarUpdate(relic.Owner, (int)relic.XP);
                        }
                        catch (Exception)
                        {
                            DisplaySyntax(client);
                            return;
                        }

						break;
					}
				#endregion XP
				#region ShowAll
				case "showall":
                    {
                    	var info = new List<string>();

                        if (args.Length > 2)
                        {
                            ushort region = 0;
                            try
                            {
                                region = Convert.ToUInt16(args[2]);
                            }
                            catch (Exception)
                            {
                                return;
                            }

                            foreach (MinotaurRelic relic in MinotaurRelicMgr.m_minotaurrelics.Values)
                            {
                                if (relic != null && relic.CurrentRegionID == region)
                                {
                                    info.Add(relic.ToString());
                                    info.Add("===========================");
                                }
                            }

							info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.ShowAll.Count", info.Count));

							client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.ShowAll.Infos"), info);

                            return;
                        }

                        foreach (MinotaurRelic relic in MinotaurRelicMgr.m_minotaurrelics.Values)
                        {
                            if (relic != null)
                            {
                                info.Add(relic.ToString());
                                info.Add("===========================");
                            }
                        }

                        info.Add(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.ShowAll.Count", MinotaurRelicMgr.m_minotaurrelics.Count));

						client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.ShowAll.Infos"), info);

						break;
					}
				#endregion ShowAll
				#region Spawn
				case "spawn":
                    {
                        if (args.Length != 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        MinotaurRelic relic = MinotaurRelicMgr.GetRelic(Convert.ToInt32(args[2]));
                        
                        if (relic == null)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (relic.respawntimer == null)
                        {
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.MinoRelic.Spawn.AlreadySpawned"));
                            return;
                        }

                        relic.ManualRespawn();

						break;
					}
				#endregion Spawn
			}
        }
    }
}
