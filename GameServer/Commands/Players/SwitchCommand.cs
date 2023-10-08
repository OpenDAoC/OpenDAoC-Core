﻿using DOL.Database;

namespace DOL.GS.Commands;

[Command("&switch", EPrivLevel.Player,
    "Equip Weapons from bag. (/switch 1h 1, will replace your mainhand weapon with the first slot in your backpack)",
    "/switch 1h <slot>",
    "/switch offhand <slot>",
    "/switch 2h <slot>",
    "/switch range <slot>")]
public class SwitchCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 3)
        {
            DisplaySyntax(client);
            return;
        }

        eInventorySlot ToSlot = eInventorySlot.FirstBackpack;

        switch (args[1])
        {
            case "1h":
                ToSlot = eInventorySlot.RightHandWeapon;
                break;
            case "2h":
                ToSlot = eInventorySlot.TwoHandWeapon;
                break;
            case "offhand":
                ToSlot = eInventorySlot.LeftHandWeapon;
                break;
            case "range":
                ToSlot = eInventorySlot.DistanceWeapon;
                break;
        }

        //The first backpack.
        int FromSlot = 40;

        if (int.TryParse(args[2], out FromSlot))
        {
            FromSlot = int.Parse(args[2]);
            SwitchItem(client.Player, ToSlot, (eInventorySlot)FromSlot + 39);
        }
        else
        {
            DisplayMessage(client, "There seems to have been a problem. Please try again.");
            DisplaySyntax(client);
            return;
        }

    }
    public void SwitchItem(GamePlayer player, eInventorySlot ToSlot, eInventorySlot FromSlot)
    {
        if (player.Inventory.GetItem(FromSlot) != null)
        {
            DbInventoryItem item = player.Inventory.GetItem(FromSlot);

            if (!GlobalConstants.IsWeapon(item.Object_Type) && item.Object_Type != (int)EObjectType.Instrument)
            {
                DisplayMessage(player.Client, "That is not a weapon!");
                DisplaySyntax(player.Client);
                return;
            }

            if (!player.Inventory.MoveItem(FromSlot, ToSlot, 1))
            {
                DisplayMessage(player.Client, "There seems to have been a problem. Please try again.");
                DisplaySyntax(player.Client);
                return;
            }
        }
    }
}