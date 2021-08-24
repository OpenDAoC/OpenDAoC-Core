//===============================================
// '/ngg' command fixed to work with latest revision - Trick
// added custom save command - Unty
// Color chart http://herald.uthgard.net/daoc/list.php?view=item%20colors
// thanks to original author :)
//===============================================
using System;
using System.Linq;
using DOL.Events;
using DOL.Database;
using System.Collections.Generic;

namespace DOL.GS.Commands
{
    [Cmd("&ngg", //command to handle
       ePrivLevel.GM, //minimum privelege level
       "NPC Gear Generator", //command description
       "'/ngg random [color]' Create a completely random equipment template",
       "'/ngg cloth [color]' Create a random set of cloth",
       "'/ngg leather [color]' Create a random set of leather",
       "'/ngg studded [color]' Create a random set of studded",
       "'/ngg chain [color]' Create a random set of chain",
       "'/ngg scale [color]' Create a random set of scale",
       "'/ngg plate [color]' Create a random set of plate",
       "'/ngg cloak [color]' Add a random cloak to the NPC",
       "'/ngg epic [class] [color]' create the class epic armor to the NPC",
       "'/ngg equip [slotNumber] [color]' Add a random equipement to the NPC slot",
       "'/ngg save' Save the template and NPC to database",
       "---------------------------------",
       "[class] : class name",
       "exemple : '/ngg epic shadowblade'",
       "---------------------------------",
       "[color] : one number to select the same color for all the item's parts (if empty : colors random for each item's parts)",
       "or [CloakColor] [HeadColor] [HandsColor] [ArmsColor] [TorsoColor] [LegsColor] [BootsColor] to choose the color of each part (all arguments needed).",
       "exemple : '/ngg random 43' all the armor parts are in radom and take the black cloth color.",
       "or : '/ngg plate 0 1 2 3 4 5 6' all the armor is in plate and each part take a fixed color",
       "or : '/ngg plate' all the armor is in plate and each part take a random color",
       "---------------------------------",
       "[slotNumber] :",
       "- for weapons : 10 = 'right hand', 11 = 'left hand', 12 = 'two handed', 13 = 'distance'",
       "- for armors : 21 = 'head', 22 = 'hands', 23 = 'boots', 25 = 'torso', 26 = 'cloak', 27 = 'legs', 28 = 'arms'",
       "exemple : '/ngg equip 12 ' Add a two handed weapon to the NPC with a random color.",
       "---------------------------------",
       "don't forget to use '/ngg save' when you have finish the npc template."
       )]
    public class NGGCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        /*
           armor[0][][] = cloth;
           armor[1][][] = leather;
           armor[2][][] = studded;
           armor[3][][] = chain;
           armor[4][][] = scale;
           armor[5][][] = plate;
             
           armor[][0][] = head;
           armor[][1][] = hands;
           armor[][2][] = arms;
           armor[][3][] = torso;
           armor[][4][] = legs;
           armor[][5][] = boots;

           equip[0][]  = head;
           equip[1][]  = hands;
           equip[2][]  = arms;
           equip[3][]  = torso;
           equip[4][]  = legs;
           equip[5][]  = boots;
           equip[6][]  = cloak;
           equip[7][]  = right;
           equip[8][]  = left;
           equip[9][]  = 2h;
           equip[10][] = distance;
             
           armor[][][x] = Model IDs;
           equip[][x] = Model IDs;
           cloak[x] = Model IDs;
        */
        private static ushort[][][] armor = new ushort[6][][];
        private static ushort[][] equip = new ushort[11][];
        private static ushort[] cloak;

        //private static String[] slots = {"head", "hands", "arms", "torso", "legs", "boots", "cloak", "right", "left", "2H", "distance"};
        private static byte[] slots = { 0x15, 0x16, 0x1C, 0x19, 0x1B, 0x17, 0x1A, 0x0A, 0x0B, 0x0C, 0x0D };

        [ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            IList<ItemTemplate> temp; 
            int[] ot = { 32, 33, 34, 35, 38, 36 };
            int x;

            for (int a = 0; a < 6; a++)
            {
                armor[a] = new ushort[6][];
                for (int b = 0; b < 6; b++)
                {
                    x = 0;
                   // temp = GameServer.Database.SelectObjects<ItemTemplate>("Object_Type = " + ot[a] + " AND Item_Type = " + slots[b]);
                   temp = GameServer.Database.SelectObjects<ItemTemplate>("`Object_Type` = @Object AND `Item_Type` = @Item", new [] { new QueryParameter("@Object", ot[a]), new QueryParameter("@Item", slots[b]) });
                  	armor[a][b] = new ushort[temp.Count];
                    foreach (DataObject item in temp)
                    {
                        armor[a][b][x++] = (ushort)(item as ItemTemplate).Model;
                    }
                }
            }

            x = 0;
            temp = GameServer.Database.SelectObjects<ItemTemplate>("Item_Type = @Item", new QueryParameter("@Item", slots[6]));
            cloak = new ushort[temp.Count];
            foreach (DataObject item in temp)
            {
                cloak[x++] = (ushort)(item as ItemTemplate).Model;
            }

            for (int a = 0; a < 11; a++)
            {
                x = 0;
                temp = GameServer.Database.SelectObjects<ItemTemplate>( "Item_Type = @Item", new QueryParameter("@Item", slots[a]));
                equip[a] = new ushort[temp.Count];
                foreach (DataObject item in temp)
                {
                    equip[a][x++] = (ushort)(item as ItemTemplate).Model;
                }
            }
        }

        public void OnCommand(GameClient client, string[] args)
        {
            bool getcloak;
            if (Util.Random(1) == 0) getcloak = false;
            else getcloak = true;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            string ClasseEpic = "";
            ushort color = 0;
            ushort color1 = 0;
            ushort color2 = 0;
            ushort color3 = 0;
            ushort color4 = 0;
            ushort color5 = 0;
            ushort color6 = 0;
            ushort sloti = 999;
            byte slotb = 0x00;

            if (args.Length >= 2)
            {
                // Epic Argument
                if (args[1] == "epic")
                {
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (args.Length > 4 && args.Length < 10)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    ClasseEpic = args[2];
                    if (args.Length >= 4)
                    {
                        color = Convert.ToUInt16(args[3]);
                    }
                    if (args.Length > 4)
                    {
                        color1 = Convert.ToUInt16(args[4]);
                        color2 = Convert.ToUInt16(args[5]);
                        color3 = Convert.ToUInt16(args[6]);
                        color4 = Convert.ToUInt16(args[7]);
                        color5 = Convert.ToUInt16(args[8]);
                        color6 = Convert.ToUInt16(args[9]);
                    }
                }
                else if (args[1] == "equip")
                {
                    // equip Argument
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    slotb = Convert.ToByte(Convert.ToUInt16(args[2]));
                    ushort z = 0;
                    foreach (byte finded in slots)
                    {
                        if (finded == slotb) { sloti = z; }
                        z++;
                    }
                    if (sloti == 999)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (args.Length > 3)
                    {
                        color = Convert.ToUInt16(args[3]);
                    }
                }
                else
                {
                    // Armor and Claok Argument
                    if (args.Length > 3 && args.Length < 9)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    if (args.Length >= 3 && args[1] != "save")
                    {
                        color = Convert.ToUInt16(args[2]);
                    }
                    if (args.Length > 3)
                    {
                        color1 = Convert.ToUInt16(args[3]);
                        color2 = Convert.ToUInt16(args[4]);
                        color3 = Convert.ToUInt16(args[5]);
                        color4 = Convert.ToUInt16(args[6]);
                        color5 = Convert.ToUInt16(args[7]);
                        color6 = Convert.ToUInt16(args[8]);
                    }
                }
            }
            int[] colorArray = { color1, color2, color3, color4, color5, color6 };

            GameNPC npc = null;
            if (client.Player.TargetObject is GameNPC)
                npc = (GameNPC)client.Player.TargetObject;

            GameNpcInventoryTemplate template = npc.Inventory as GameNpcInventoryTemplate;
            if (template == null) template = new GameNpcInventoryTemplate();

            if (args[1] != "epic")
            {
                switch (args[1])
                {
                    case "random":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            for (int i = 0; i < 6; i++)
                            {
                                if (args.Length > 2)
                                {
                                    if (args.Length == 3)
                                    {
                                        switch (Util.Random(5))
                                        {
                                            case 0:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[0][i][Util.Random(armor[0][i].Length - 1)], color, 0);
                                                break;
                                            case 1:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[1][i][Util.Random(armor[1][i].Length - 1)], color, 0);
                                                break;
                                            case 2:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[2][i][Util.Random(armor[2][i].Length - 1)], color, 0);
                                                break;
                                            case 3:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[3][i][Util.Random(armor[3][i].Length - 1)], color, 0);
                                                break;
                                            case 4:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[4][i][Util.Random(armor[4][i].Length - 1)], color, 0);
                                                break;
                                            case 5:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[5][i][Util.Random(armor[5][i].Length - 1)], color, 0);
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        switch (Util.Random(5))
                                        {
                                            case 0:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[0][i][Util.Random(armor[0][i].Length - 1)], colorArray[i], 0);
                                                break;
                                            case 1:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[1][i][Util.Random(armor[1][i].Length - 1)], colorArray[i], 0);
                                                break;
                                            case 2:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[2][i][Util.Random(armor[2][i].Length - 1)], colorArray[i], 0);
                                                break;
                                            case 3:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[3][i][Util.Random(armor[3][i].Length - 1)], colorArray[i], 0);
                                                break;
                                            case 4:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[4][i][Util.Random(armor[4][i].Length - 1)], colorArray[i], 0);
                                                break;
                                            case 5:
                                                template.AddNPCEquipment((eInventorySlot)slots[i], armor[5][i][Util.Random(armor[5][i].Length - 1)], colorArray[i], 0);
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    switch (Util.Random(5))
                                    {
                                        case 0:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[0][i][Util.Random(armor[0][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                        case 1:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[1][i][Util.Random(armor[1][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                        case 2:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[2][i][Util.Random(armor[2][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                        case 3:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[3][i][Util.Random(armor[3][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                        case 4:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[4][i][Util.Random(armor[4][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                        case 5:
                                            template.AddNPCEquipment((eInventorySlot)slots[i], armor[5][i][Util.Random(armor[5][i].Length - 1)], Util.Random(86), 0);
                                            break;
                                    }
                                }
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "cloth":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[0][0][Util.Random(armor[0][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[0][1][Util.Random(armor[0][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[0][2][Util.Random(armor[0][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[0][3][Util.Random(armor[0][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[0][4][Util.Random(armor[0][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[0][5][Util.Random(armor[0][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[0][0][Util.Random(armor[0][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[0][1][Util.Random(armor[0][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[0][2][Util.Random(armor[0][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[0][3][Util.Random(armor[0][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[0][4][Util.Random(armor[0][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[0][5][Util.Random(armor[0][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[0][0][Util.Random(armor[0][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[0][1][Util.Random(armor[0][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[0][2][Util.Random(armor[0][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[0][3][Util.Random(armor[0][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[0][4][Util.Random(armor[0][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[0][5][Util.Random(armor[0][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "leather":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[1][0][Util.Random(armor[1][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[1][1][Util.Random(armor[1][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[1][2][Util.Random(armor[1][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[1][3][Util.Random(armor[1][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[1][4][Util.Random(armor[1][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[1][5][Util.Random(armor[1][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[1][0][Util.Random(armor[1][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[1][1][Util.Random(armor[1][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[1][2][Util.Random(armor[1][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[1][3][Util.Random(armor[1][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[1][4][Util.Random(armor[1][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[1][5][Util.Random(armor[1][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[1][0][Util.Random(armor[1][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[1][1][Util.Random(armor[1][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[1][2][Util.Random(armor[1][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[1][3][Util.Random(armor[1][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[1][4][Util.Random(armor[1][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[1][5][Util.Random(armor[1][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "studded":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[2][0][Util.Random(armor[2][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[2][1][Util.Random(armor[2][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[2][2][Util.Random(armor[2][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[2][3][Util.Random(armor[2][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[2][4][Util.Random(armor[2][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[2][5][Util.Random(armor[2][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[2][0][Util.Random(armor[2][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[2][1][Util.Random(armor[2][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[2][2][Util.Random(armor[2][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[2][3][Util.Random(armor[2][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[2][4][Util.Random(armor[2][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[2][5][Util.Random(armor[2][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[2][0][Util.Random(armor[2][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[2][1][Util.Random(armor[2][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[2][2][Util.Random(armor[2][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[2][3][Util.Random(armor[2][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[2][4][Util.Random(armor[2][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[2][5][Util.Random(armor[2][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "chain":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[3][0][Util.Random(armor[3][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[3][1][Util.Random(armor[3][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[3][2][Util.Random(armor[3][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[3][3][Util.Random(armor[3][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[3][4][Util.Random(armor[3][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[3][5][Util.Random(armor[3][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[3][0][Util.Random(armor[3][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[3][1][Util.Random(armor[3][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[3][2][Util.Random(armor[3][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[3][3][Util.Random(armor[3][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[3][4][Util.Random(armor[3][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[3][5][Util.Random(armor[3][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[3][0][Util.Random(armor[3][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[3][1][Util.Random(armor[3][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[3][2][Util.Random(armor[3][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[3][3][Util.Random(armor[3][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[3][4][Util.Random(armor[3][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[3][5][Util.Random(armor[3][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "scale":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[4][0][Util.Random(armor[4][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[4][1][Util.Random(armor[4][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[4][2][Util.Random(armor[4][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[4][3][Util.Random(armor[4][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[4][4][Util.Random(armor[4][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[4][5][Util.Random(armor[4][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[4][0][Util.Random(armor[4][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[4][1][Util.Random(armor[4][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[4][2][Util.Random(armor[4][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[4][3][Util.Random(armor[4][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[4][4][Util.Random(armor[4][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[4][5][Util.Random(armor[4][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[4][0][Util.Random(armor[4][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[4][1][Util.Random(armor[4][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[4][2][Util.Random(armor[4][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[4][3][Util.Random(armor[4][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[4][4][Util.Random(armor[4][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[4][5][Util.Random(armor[4][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "plate":
                        {
                            Clear(npc);
                            template = new GameNpcInventoryTemplate();
                            if (args.Length > 2)
                            {
                                if (args.Length == 3)
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[5][0][Util.Random(armor[5][0].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[5][1][Util.Random(armor[5][1].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[5][2][Util.Random(armor[5][2].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[5][3][Util.Random(armor[5][3].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[5][4][Util.Random(armor[5][4].Length - 1)], color, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[5][5][Util.Random(armor[5][5].Length - 1)], color, 0);
                                }
                                else
                                {
                                    template.AddNPCEquipment((eInventorySlot)slots[0], armor[5][0][Util.Random(armor[5][0].Length - 1)], color1, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[1], armor[5][1][Util.Random(armor[5][1].Length - 1)], color2, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[2], armor[5][2][Util.Random(armor[5][2].Length - 1)], color3, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[3], armor[5][3][Util.Random(armor[5][3].Length - 1)], color4, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[4], armor[5][4][Util.Random(armor[5][4].Length - 1)], color5, 0);
                                    template.AddNPCEquipment((eInventorySlot)slots[5], armor[5][5][Util.Random(armor[5][5].Length - 1)], color6, 0);
                                }
                            }
                            else
                            {
                                template.AddNPCEquipment((eInventorySlot)slots[0], armor[5][0][Util.Random(armor[5][0].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[1], armor[5][1][Util.Random(armor[5][1].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[2], armor[5][2][Util.Random(armor[5][2].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[3], armor[5][3][Util.Random(armor[5][3].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[4], armor[5][4][Util.Random(armor[5][4].Length - 1)], Util.Random(86), 0);
                                template.AddNPCEquipment((eInventorySlot)slots[5], armor[5][5][Util.Random(armor[5][5].Length - 1)], Util.Random(86), 0);
                            }
                            if (getcloak)
                                goto case "cloak";
                            else
                                break;
                        }
                    case "cloak":
                        {
                            if (args.Length > 2)
                            {
                                template.RemoveNPCEquipment((eInventorySlot)slots[6]);
                                template.AddNPCEquipment((eInventorySlot)slots[6], cloak[Util.Random(cloak.Length - 1)], color, 0);
                            }
                            else
                            {
                                template.RemoveNPCEquipment((eInventorySlot)slots[6]);
                                template.AddNPCEquipment((eInventorySlot)slots[6], cloak[Util.Random(cloak.Length - 1)], Util.Random(86), 0);
                            }
                            break;
                        }
                    case "equip":
                        {
                            if (args.Length > 3)
                            {
                                template.RemoveNPCEquipment((eInventorySlot)slots[sloti]);
                                template.AddNPCEquipment((eInventorySlot)slots[sloti], equip[sloti][Util.Random(equip[sloti].Length - 1)], color, 0);
                            }
                            else
                            {
                                template.RemoveNPCEquipment((eInventorySlot)slots[sloti]);
                                template.AddNPCEquipment((eInventorySlot)slots[sloti], equip[sloti][Util.Random(equip[sloti].Length - 1)], Util.Random(86), 0);
                            }
                            if (sloti == 7 || sloti == 8)
                            {
                                npc.SwitchWeapon(eActiveWeaponSlot.Standard);
                            }
                            if (sloti == 9)
                            {
                                npc.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
                            }
                            if (sloti == 10)
                            {
                                npc.SwitchWeapon(eActiveWeaponSlot.Distance);
                            }
                            break;
                        }
                    case "save":
                        {
                			if (args.Length == 3)
                            {
                				Save(client, npc, args[2]);
                				break;
                			}
                            if (args.Length == 2)
                            {
                            	Save(npc);
                				break;
                			}
                            DisplaySyntax(client); break;
                        }
                    default: DisplaySyntax(client); break;
                }
            }
            else
            {
                // Epic
                Clear(npc);
                template = new GameNpcInventoryTemplate();

                ItemTemplate tgeneric0 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicHelm");
                ItemTemplate tgeneric1 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicGloves");
                ItemTemplate tgeneric2 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicArms");
                ItemTemplate tgeneric3 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicVest");
                ItemTemplate tgeneric4 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicLegs");
                ItemTemplate tgeneric5 = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate> (ClasseEpic + "EpicBoots");

                if (args.Length > 3)
                {
                    if (args.Length == 4)
                    {
                        template.AddNPCEquipment((eInventorySlot)slots[0], Convert.ToUInt16(tgeneric0.Model), color, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[1], Convert.ToUInt16(tgeneric1.Model), color, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[2], Convert.ToUInt16(tgeneric2.Model), color, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[3], Convert.ToUInt16(tgeneric3.Model), color, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[4], Convert.ToUInt16(tgeneric4.Model), color, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[5], Convert.ToUInt16(tgeneric5.Model), color, 0);
                    }
                    else
                    {
                        template.AddNPCEquipment((eInventorySlot)slots[0], Convert.ToUInt16(tgeneric0.Model), color1, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[1], Convert.ToUInt16(tgeneric1.Model), color2, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[2], Convert.ToUInt16(tgeneric2.Model), color3, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[3], Convert.ToUInt16(tgeneric3.Model), color4, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[4], Convert.ToUInt16(tgeneric4.Model), color5, 0);
                        template.AddNPCEquipment((eInventorySlot)slots[5], Convert.ToUInt16(tgeneric5.Model), color6, 0);
                    }
                }
                else
                {
                    template.AddNPCEquipment((eInventorySlot)slots[0], Convert.ToUInt16(tgeneric0.Model), Util.Random(86), 0);
                    template.AddNPCEquipment((eInventorySlot)slots[1], Convert.ToUInt16(tgeneric1.Model), Util.Random(86), 0);
                    template.AddNPCEquipment((eInventorySlot)slots[2], Convert.ToUInt16(tgeneric2.Model), Util.Random(86), 0);
                    template.AddNPCEquipment((eInventorySlot)slots[3], Convert.ToUInt16(tgeneric3.Model), Util.Random(86), 0);
                    template.AddNPCEquipment((eInventorySlot)slots[4], Convert.ToUInt16(tgeneric4.Model), Util.Random(86), 0);
                    template.AddNPCEquipment((eInventorySlot)slots[5], Convert.ToUInt16(tgeneric5.Model), Util.Random(86), 0);
                }
                if (args.Length > 3)
                {
                    template.RemoveNPCEquipment((eInventorySlot)slots[6]);
                    template.AddNPCEquipment((eInventorySlot)slots[6], cloak[Util.Random(cloak.Length - 1)], color, 0);
                }
                else
                {
                    template.RemoveNPCEquipment((eInventorySlot)slots[6]);
                    template.AddNPCEquipment((eInventorySlot)slots[6], cloak[Util.Random(cloak.Length - 1)], Util.Random(86), 0);
                }
            }
            npc.Inventory = template;
            npc.BroadcastLivingEquipmentUpdate();
            return;
        }

        private void Clear(GameNPC target)
        {
            target.Inventory = null;
            target.EquipmentTemplateID = null;
        }

        private void Save(GameNPC target)
        {
            String tn;
            do
            {
                tn = Guid.NewGuid().ToString();
            } while (!target.Inventory.SaveIntoDatabase(tn));
            target.EquipmentTemplateID = tn;
            target.SaveIntoDatabase();
        }
        
        private void Save(GameClient client, GameNPC target, string saveName)
        {
            String tn;
			do
            {
                tn = saveName;
            } while (!target.Inventory.SaveIntoDatabase(tn));
            target.EquipmentTemplateID = tn;
            target.SaveIntoDatabase();
            client.Player.Out.SendMessage("Equipment template saved as: " +saveName, DOL.GS.PacketHandler.eChatType.CT_System, DOL.GS.PacketHandler.eChatLoc.CL_SystemWindow);
        }
    }
}
