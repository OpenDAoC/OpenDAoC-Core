using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
    [CmdAttribute("&rearrange", ePrivLevel.Player, "Allows you to rearrange your character overview.",
        "/rearrange list - Shows a list with all to this account assigned characters and their slots.",
        "/rearrange setslot [source slot] [target slot] - Sets the given source slot to the given target slot.")]
    public class RearrangeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCommand(GameClient client, string[] args)
        {
            if (ServerProperties.Properties.DISABLED_COMMANDS.Contains("/rearrange"))
                return;

            if (IsSpammingCommand(client.Player, "rearrange"))
                return;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "list":
                    {
                        SendCharacterListWindow(client);
                        return;
                    }
                case "setslot":
                    {
                        if (args.Length < 4)
                            goto default;

                        int sourceSlotIndex = -1;
                        int targetSlotIndex = -1;

                        try
                        {
                            sourceSlotIndex = Convert.ToInt32(args[2]);
                            targetSlotIndex = Convert.ToInt32(args[3]);
                        }
                        catch
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (!IsValidSlot(sourceSlotIndex) || !IsValidSlot(targetSlotIndex))
                        {
                            if (!IsValidSlot(sourceSlotIndex) && !IsValidSlot(targetSlotIndex))
                                InvalidSlot(client, new int[] { sourceSlotIndex, targetSlotIndex });
                            else
                            {
                                if (!IsValidSlot(sourceSlotIndex))
                                    InvalidSlot(client, new int[] { sourceSlotIndex });

                                if (!IsValidSlot(targetSlotIndex))
                                    InvalidSlot(client, new int[] { targetSlotIndex });
                            }

                            return;
                        }

                        if (sourceSlotIndex == targetSlotIndex)
                        {
                            InvalidSlot(client, new int[] { sourceSlotIndex, targetSlotIndex });
                            return;
                        }

                        // Do not allow changing slots between two different realms.
                        if (!SameRealmSlots(sourceSlotIndex, targetSlotIndex))
                        {
                            NotSameRealm(client, sourceSlotIndex, targetSlotIndex);
                            return;
                        }

                        SetSlot(client, sourceSlotIndex, targetSlotIndex);
                        return;
                    }
                default:
                    {
                        DisplaySyntax(client);
                        return;
                    }
            }
        }

        #region Helpers
        private string GetRealmBySlotIndex(int slot)
        {
            string realm = string.Empty;

            if (slot >= 100 && slot <= 109)
                realm = "Albion";
            else if (slot >= 200 && slot <= 209)
                realm = "Midgard";
            else if (slot >= 300 && slot <= 309)
                realm = "Hibernia";

            return realm;
        }

        private bool IsValidSlot(int value)
        {
            if (value >= 100 && value <= 109)
                return true;

            if (value >= 200 && value <= 209)
                return true;

            if (value >= 300 && value <= 309)
                return true;

            return false;
        }

        private bool SameRealmSlots(int slot1, int slot2)
        {
            if (Math.Abs(slot1 - slot2) < 10)
                return true;

            return false;
        }
        #endregion Helpers

        #region Messages
        private void EmptySlot(GameClient client, int slot)
        {
            client.Out.SendMessage("The given source slot (" + slot + ") is empty.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private void InvalidSlot(GameClient client, int[] slots)
        {
            string str = string.Empty;

            foreach (int slot in slots)
            {
                if (str.Length == 0)
                    str = slot.ToString();
                else
                    str += ", " + slot.ToString();
            }

            client.Out.SendMessage("Invalid character slot" + (slots.Length > 1 ? "s" : "") + ": " + str, eChatType.CT_System, eChatLoc.CL_SystemWindow); 
        }

        private void NotSameRealm(GameClient client, int sourceSlot, int targetSlot)
        {
            client.Out.SendMessage("You cannot set a slot to a different realm! (source realm = " + GetRealmBySlotIndex(sourceSlot) +
                                   ", target realm = " + GetRealmBySlotIndex(targetSlot) + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }

        private void SlotChanged(GameClient client, string name, int oldSlot, int newSlot)
        {
            client.Out.SendMessage("The character slot for " + name + " has been successfully changed. (old slot = " + oldSlot +
                                   ", new slot = " + newSlot + ")", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        #endregion Messages

        #region SendCharacterListWindow
        private static readonly int[] m_firstCharacterSlotByRealm = new int[3] { 100, 200, 300 };

        private void SendCharacterListWindow(GameClient client)
        {
            Dictionary<int, string> slots = new Dictionary<int, string>();

            foreach (int firstSlot in m_firstCharacterSlotByRealm)
            {
                switch (firstSlot)
                {
                    case 100:
                        slots.Add(-1, "Albion:");
                        break;
                    case 200:
                        slots.Add(-2, "Midgard:");
                        break;
                    case 300:
                        slots.Add(-3, "Hibernia:");
                        break;
                }

                for (int i = firstSlot; i <= (firstSlot + 9); i++)
                {
                    slots.Add(i, "Empty slot");

                    if (i == (firstSlot + 9))
                    {
                        if (i >= m_firstCharacterSlotByRealm[2])
                            continue;

                        try
                        {
                            slots.Add(Convert.ToInt32("-" + firstSlot), "\n");
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                        }
                    }
                }
            }

            foreach (DbCoreCharacter character in client.Account.Characters)
            {
                if (slots.ContainsKey(character.AccountSlot))
                    slots[character.AccountSlot] = character.Name;
                else
                    slots.Add(character.AccountSlot, character.Name); // ???
            }

            List<string> data = new List<string>();
            foreach (KeyValuePair<int, string> slot in slots)
            {
                if (slot.Key < 0)
                    data.Add(slot.Value);
                else
                    data.Add("(" + slot.Key + ") " + slot.Value);
            }

            client.Out.SendCustomTextWindow("Character slots", data);
        }
        #endregion SendCharacterListWindow

        #region SetSlot
        private void SetSlot(GameClient client, int sourceSlot, int targetSlot)
        {
            DbCoreCharacter source = null;
            DbCoreCharacter target = null;

            foreach (DbCoreCharacter character in client.Account.Characters)
            {
                if (source == null)
                {
                    if (character.AccountSlot == sourceSlot)
                        source = character;
                }

                if (target == null)
                {
                    if (character.AccountSlot == targetSlot)
                        target = character;
                }

                if (source != null && target != null)
                    break;
            }

            if (source == null)
            {
                EmptySlot(client, sourceSlot);
                return;
            }

            // It's important that we create a backup of each character before we start. If an error
            // occurs and / or the server crashes, we always have an character backup.
            DbCoreCharacterBackup sourceBackup = new DbCoreCharacterBackup(source);
            sourceBackup.DOLCharacters_ID += "-Rearranged"; // Easier for admins to find it.
            GameServer.Database.AddObject(sourceBackup);

            DbCoreCharacterBackup targetBackup = null;
            if (target != null)
            {
                targetBackup = new DbCoreCharacterBackup(target);
                targetBackup.DOLCharacters_ID += "-Rearranged"; // Easier for admins to find it.
                GameServer.Database.AddObject(targetBackup);
            }

            // Time to modify the slots.
            GameServer.Database.DeleteObject(source);
            if (target != null)
                GameServer.Database.DeleteObject(target);

            source.AccountSlot = targetSlot;
            if (target != null)
                target.AccountSlot = sourceSlot;

            GameServer.Database.AddObject(source);
            if (target != null)
                GameServer.Database.AddObject(target);

            GameServer.Database.DeleteObject(sourceBackup);
            if (targetBackup != null)
                GameServer.Database.DeleteObject(targetBackup);

            SlotChanged(client, source.Name, sourceSlot, source.AccountSlot);
        }
        #endregion SetSlot
    }
}
