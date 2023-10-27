﻿using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.Scripts.Custom;

public class SummonedVaultKeeper : GameVaultKeeper
{
    
    public override bool AddToWorld()
    {
        
        switch (Realm)
        {
            case ERealm.Albion: Model = 10;break;
            case ERealm.Hibernia: Model = 307;break;
            case ERealm.Midgard: Model = 158;break;
            case ERealm.None: Model = 10;break;
        }
        // adding dark blue robe
        GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
        template.AddNPCEquipment(EInventorySlot.TorsoArmor, 58, 26);
        //LoadEquipmentTemplateFromDatabase("SummonedVaultkeeper");
        template.CloseTemplate();
        
        GuildName = "Temp Worker";
        Realm = ERealm.None;
        return base.AddToWorld();
    }
    
    public override bool Interact(GamePlayer player)
    {
        if (!base.Interact(player)) return false;
        if (player.InCombat)
        {
            player.Out.SendMessage("Vaultkeeper says \"stop your combat if you want me speak with me!\"", EChatType.CT_Say,
                EChatLoc.CL_ChatWindow);
            return false;
        }

        if (GetDistanceTo(player) > WorldMgr.INTERACT_DISTANCE)
        {
            player.Out.SendMessage("You are too far away " + GetName(0, false) + ".", EChatType.CT_System,
                EChatLoc.CL_SystemWindow);
            return false;
        }

        TurnTo(player, 3000);

        return true;
    }
}