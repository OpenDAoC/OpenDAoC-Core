using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
       "&faction",
       ePrivLevel.GM,
       "GMCommands.Faction.Description",
       "GMCommands.Faction.Usage.Create",
       "GMCommands.Faction.Usage.Assign",
       "GMCommands.Faction.Usage.AddFriend",
       "GMCommands.Faction.Usage.AddEnemy",
       "GMCommands.Faction.Usage.Relations",
       "GMCommands.Faction.Usage.List",
       "GMCommands.Faction.Usage.Select")]
    public class FactionCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        protected string TEMP_FACTION_LAST = "TEMP_FACTION_LAST";

        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            Faction selectedFaction = client.Player.TempProperties.GetProperty<Faction>(TEMP_FACTION_LAST, null);

            switch (args[1])
            {
                case "create":
                {
                    if (args.Length < 4)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    int baseAggro;

                    try
                    {
                        baseAggro = Convert.ToInt32(args[3]);
                    }
                    catch
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Create.BAMustBeNumber"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    int max = 0;

                    if (FactionMgr.Factions.Count != 0)
                    {
                        IEnumerator enumerator = FactionMgr.Factions.Keys.GetEnumerator();

                        while (enumerator.MoveNext())
                            max = Math.Max(max, (int)enumerator.Current);
                    }

                    DbFaction dbFaction = new()
                    {
                        BaseAggroLevel = baseAggro,
                        Name = args[2],
                        ID = max + 1
                    };
                    GameServer.Database.AddObject(dbFaction);
                    selectedFaction = new Faction();
                    selectedFaction.LoadFromDatabase(dbFaction);
                    FactionMgr.Factions.Add(dbFaction.ID, selectedFaction);
                    client.Player.TempProperties.SetProperty(TEMP_FACTION_LAST, selectedFaction);
                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Create.NewCreated"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "assign":
                {
                    if (selectedFaction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (client.Player.TargetObject is not GameNPC npc)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Assign.MustSelectMob"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    npc.Faction = selectedFaction;
                    client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Assign.MobHasJoinedFact", npc.Name, selectedFaction.Name), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                    break;
                }
                case "addfriend":
                {
                    if (selectedFaction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    int id;

                    try
                    {
                        id = Convert.ToInt32(args[2]);
                    }
                    catch
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    Faction faction = FactionMgr.GetFactionByID(id);

                    if (faction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    DbFactionLinks dbFactionLinks = new()
                    {
                        FactionID = selectedFaction.Id,
                        LinkedFactionID = faction.Id,
                        IsFriend = true
                    };
                    GameServer.Database.AddObject(dbFactionLinks);
                    selectedFaction.FriendFactions.Add(faction);
                    break;
                }
                case "addenemy":
                {
                    if (selectedFaction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    int id;

                    try
                    {
                        id = Convert.ToInt32(args[2]);
                    }
                    catch
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    Faction faction = FactionMgr.GetFactionByID(id);

                    if (faction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    DbFactionLinks dbFactionLinks = new()
                    {
                        FactionID = selectedFaction.Id,
                        LinkedFactionID = faction.Id,
                        IsFriend = false
                    };
                    GameServer.Database.AddObject(dbFactionLinks);
                    selectedFaction.EnemyFactions.Add(faction);
                    break;
                }
                case "relations":
                {
                    if (selectedFaction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    client.Player.Out.SendMessage($"Relations for #{selectedFaction.Id}: {selectedFaction.Name } ({selectedFaction._baseAggroLevel}):", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                    HashSet<Faction> otherFactions = selectedFaction.FriendFactions;

                    if (otherFactions.Count > 0)
                    {
                        client.Player.Out.SendMessage($" Is friend with:", eChatType.CT_Say, eChatLoc.CL_SystemWindow);

                        foreach (Faction otherFaction in otherFactions)
                            SendOtherFactionInfo(client, otherFaction, IsHardcoded(selectedFaction.Id, otherFaction.Id, true), otherFaction.FriendFactions.Contains(selectedFaction));
                    }

                    otherFactions = selectedFaction.EnemyFactions;

                    if (otherFactions.Count > 0)
                    {
                        client.Player.Out.SendMessage($" Is hostile with:", eChatType.CT_Say, eChatLoc.CL_SystemWindow);

                        foreach (Faction otherFaction in selectedFaction.EnemyFactions)
                            SendOtherFactionInfo(client, otherFaction, IsHardcoded(selectedFaction.Id, otherFaction.Id, false), otherFaction.EnemyFactions.Contains(selectedFaction));
                    }

                    static bool IsHardcoded(int selectedFactionId, int otherFactionId, bool checkForFriendly)
                    {
                        return GameServer.Database.SelectObject<DbFactionLinks>(DB.Column("FactionID").IsEqualTo(selectedFactionId).And(DB.Column("LinkedFactionID").IsEqualTo(otherFactionId).And(DB.Column("IsFriend").IsEqualTo(checkForFriendly)))) == null;
                    }

                    static void SendOtherFactionInfo(GameClient client, Faction otherFaction, bool hardcoded, bool reciprocal)
                    {
                        string extraInfo = string.Empty;

                        if (hardcoded || !reciprocal)
                        {
                            extraInfo = "  ";

                            if (hardcoded)
                                extraInfo += "[hardcoded]";

                            if (!reciprocal)
                                extraInfo += "[not reciprocal]";
                        }

                        client.Out.SendMessage($"  #{otherFaction.Id}: {otherFaction.Name } ({otherFaction._baseAggroLevel}){extraInfo}", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                    }

                    break;
                }
                case "list":
                {
                    foreach (Faction faction in FactionMgr.Factions.Values)
                        client.Player.Out.SendMessage($"#{faction.Id}: {faction.Name } ({faction._baseAggroLevel})", eChatType.CT_Say, eChatLoc.CL_SystemWindow);

                    return;
                }
                case "select":
                {
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }

                    int id;

                    try
                    {
                        id = Convert.ToInt32(args[2]);
                    }
                    catch
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    Faction faction = FactionMgr.GetFactionByID(id);

                    if (faction == null)
                    {
                        client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    client.Player.TempProperties.SetProperty(TEMP_FACTION_LAST, faction);
                    client.Player.Out.SendMessage($"Selected faction #{faction.Id}: {faction.Name } ({faction._baseAggroLevel})", eChatType.CT_Say, eChatLoc.CL_SystemWindow);
                    break;
                }
                default:
                {
                    DisplaySyntax(client);
                    return;
                }
            }
        }
    }
}
