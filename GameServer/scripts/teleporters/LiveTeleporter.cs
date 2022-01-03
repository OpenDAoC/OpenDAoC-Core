// I don't know the original author but this
// file has been modified by clait for Atlas Freeshard

using DOL.GS.PacketHandler;

/* Need to fix
 * EquipTemplate for Hib and Mid
 * Oceanus for all realms.
 * Kobold Undercity for Mid
 * personal guild and hearth teleports
 */
namespace DOL.GS.Scripts
{
    public class LiveTeleporter : GameNPC
    {
        public override bool AddToWorld()
        {
            switch (Realm)
            {
                case eRealm.Albion:
                    Name = "Master Visur";
                    GuildName = "Teleporter";
                    Model = 61;

                    GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
                    templateAlb.AddNPCEquipment(eInventorySlot.Cloak, 57, 66);
                    templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005, 86);
                    templateAlb.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 6);
                    templateAlb.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 6);
                    templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 6);
                    templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 6);
                    templateAlb.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166);
                    Inventory = templateAlb.CloseTemplate();
                    break;
                case eRealm.Midgard:
                    Name = "Stor Gothi Annark";
                    GuildName = "Teleporter";
                    Model = 215;

                    GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
                    templateMid.AddNPCEquipment(eInventorySlot.Cloak, 57, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.TorsoArmor, 245, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.LegsArmor, 246, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.HandsArmor, 248, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.FeetArmor, 249, 26);
                    Inventory = templateMid.CloseTemplate();
                    break;
                case eRealm.Hibernia:
                    Name = "Channeler Glasny";
                    GuildName = "Teleporter";
                    Model = 342;

                    GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
                    templateHib.AddNPCEquipment(eInventorySlot.TorsoArmor, 1008);
                    templateHib.AddNPCEquipment(eInventorySlot.HandsArmor, 396);
                    templateHib.AddNPCEquipment(eInventorySlot.FeetArmor, 402);
                    templateHib.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468);
                    Inventory = templateHib.CloseTemplate();
                    break;
            }

            Level = 60;
            Size = 50;
            Flags |= GameNPC.eFlags.PEACE;

            return base.AddToWorld();
        }

        /// <summary>
        /// Display the teleport indicator around this teleporters feet
        /// </summary>
        public override bool ShowTeleporterIndicator
        {
            get { return true; }
        }


        public override bool Interact(GamePlayer player) // What to do when a player clicks on me
        {
            if (!base.Interact(player)) return false;

            if (player.Realm != this.Realm && player.Client.Account.PrivLevel == 1) return false;

            switch (Realm)
            {
                case eRealm.Albion:
                    SayTo(player, "Greetings, " + player.Name +
                                  " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                                  "[Castle Sauvage] in Camelot Hills or \n[Snowdonia Fortress] in Black Mtns. North\n" +
                                  "[Avalon Marsh] wharf\n" +
                                  "[Gothwaite Harbor] in the [Shrouded Isles]\n" +
                                  "[Camelot] our glorious capital\n" +
                                  "[Entrance] to the areas of [Housing]\n\n" +
                                  "Or one of the many [towns] throughout Albion");
                    break;

                case eRealm.Midgard:
                    SayTo(player, "Greetings, " + player.Name +
                                  " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                                  "[Svasud Faste] in Mularn or \n[Vindsaul Faste] in West Svealand\n" +
                                  "Beaches of [Gotar] near Nailiten\n" +
                                  "[Aegirhamn] in the [Shrouded Isles]\n" +
                                  "Our glorious city of [Jordheim]\n" +
                                  "[Entrance] to the areas of [Housing]\n\n" +
                                  "Or one of the many [towns] throughout Midgard");
                    break;

                case eRealm.Hibernia:
                    SayTo(player,
                        "Greetings, " + player.Name +
                        " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                        "[Druim Ligen] in Connacht or \n[Druim Cain] in Bri Leith\n" +
                        "[Shannon Estuary] watchtower\n" +
                        "[Domnann] Grove in the [Shrouded Isles]\n" +
                        "[Tir na Nog] our glorious capital\n" +
                        "[Entrance] to the areas of [Housing]\n\n" +
                        "Or one of the many [towns] throughout Hibernia");
                    break;

                default:
                    SayTo(player, "I have no Realm set, so don't know what locations to offer..");
                    break;
            }

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str) // What to do when a player whispers me
        {
            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer t = (GamePlayer) source;
            TurnTo(t.X, t.Y); // Turn to face the player

            switch (Realm) // Only offer locations based on what realm i am set at.
            {
                case eRealm.Albion:
                    switch (str.ToLower())
                    {
                        case "castle sauvage":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Castle Sauvage");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 584151, 477177, 2600, 3058);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "snowdonia fortress":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Snowdonia Fortress");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 527543, 358900, 8320, 3074);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "avalon marsh":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Avalon Marsh");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 470613, 630585, 1712, 2500);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "gothwaite harbor":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Gothwaite Harbor.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 526580, 542058, 3168, 406);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "shrouded isles":
                            SayTo(t,
                                "The isles of Avalon are  an excellent choice. Would you prefer the harbor of [Gothwaite] or perhaps one of the outlying towns like [Wearyall] Village, Fort [Gwyntell], or Cear [Diogel]?");
                            break;
                        case "camelot":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Camelot");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(10, 36209, 29843, 7971, 18);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "housing":
                            SayTo(t,
                                "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area.");
                            break;
                        case "towns":
                            SayTo(t, "I can send you to:\n" +
                                     "[Cotswold]\n" +
                                     "[Prydwen Keep]\n" +
                                     "[Cear Ulfwych]\n" +
                                     "[Campacorentin Station]\n" +
                                     "[Adribard's Retreat]\n" +
                                     "[Yarley's Farm]");
                            break;
                        //End Main
                        //Begin SI
                        case "gothwaite":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Gothwaite");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 535512, 547448, 4800, 82);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "wearyall":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Wearyall Village");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 435140, 493260, 3088, 921);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "gwyntell":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Gwyntell");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 427322, 416538, 5712, 689);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "diogel":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cear Diogel.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 403525, 502582, 4680, 561);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(2, 584461, 561355, 3576, 2256);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;

                        case "cotswold":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cottswold.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 559613, 511843, 2289, 3200);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "prydwen keep":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Prydwen Keep");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 573994, 529009, 2870, 2206);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "cear ulfwych":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cear Ulfwych.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 522479, 615826, 1818, 4);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "campacorentin station":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Campacorentin Station.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 493010, 591806, 1806, 3881);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "adribard's retreat":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Adribard's Retreat.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 473036, 628049, 2048, 3142);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "yarley's farm":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Yarley's Farm.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 369874, 679659, 5538, 3893);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                    }

                    break;
                case eRealm.Midgard:
                    switch (str.ToLower())
                    {
                        case "svasud faste":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Svasud Faste");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 767242, 669591, 5736, 1198);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "vindsaul faste":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Vindsaul Faste");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 703389, 738621, 5704, 3097);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "gotar":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Gotar");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 771081, 836721, 4624, 167);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "shrouded isles":
                            SayTo(t,
                                "The isles of Aegir are an excellent choice. Would you prefer the city of [Aegirhamn] or perhaps one of the outlying towns like [Bjarken], [Hagall], or [Knarr]?");
                            break;
                        case "jordheim":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Jordheim");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(101, 31619, 28768, 8800, 2201);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "housing":
                            SayTo(t,
                                "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area.");
                            break;
                        case "towns":
                            SayTo(t, "I can send you to:\n" +
                                     "[Mularn]\n" +
                                     "[Fort Veldon]\n" +
                                     "[Audliten]\n" +
                                     "[Huginfel]\n" +
                                     "[Fort Atla]\n" +
                                     "[West Skona]");
                            break;
                        // Begin Towns
                        case "mularn":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Mularn");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 804292, 726509, 4696, 842);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "audliten":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Audliten");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 725682, 760401, 4528, 1150);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "fort veldon":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Veldon.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 800200, 678003, 5304, 204);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "huginfel":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Huginfel.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 711788, 784084, 4672, 2579);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "fort atla":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Atla.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 749237, 816443, 4408, 2033);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "west skona":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to West Skona.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 712345, 923847, 5043, 3898);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(102, 527051, 561559, 3638, 102);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "aegirhamn":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Aegirhamn.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 294213, 355955, 3570, 4070);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "bjarken":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Bjarken.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 289626, 301652, 4160, 2804);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "hagall":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Hagall.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 379055, 386013, 7752, 2187);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "knarr":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Knarr.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 302660, 433690, 3214, 2103);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                    }

                    break;
                case eRealm.Hibernia:
                    switch (str.ToLower())
                    {
                        case "druim ligen":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Druim Ligen");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 334600, 419997, 5184, 479);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "shannon estuary":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Shannon Estuary");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 310320, 645327, 4855, 1441);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "domnann":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Domann Grove.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 423157, 442474, 5952, 2046);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "shrouded isles":
                            SayTo(t,
                                "The isles of Hy Brasil are an excellent choice. Would you prefer the grove of [Domnann] or perhaps one of the outlying towns like [Droighaid], [Aalid Feie], or [Necht]?");
                            break;
                        case "tir na nog":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Tir na Nog");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(201, 30011, 33138, 7916, 3079);
                                //MoveTo(regionid, x , y, z, heading)
                            }

                            break;
                        case "housing":
                            SayTo(t,
                                "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area.");
                            break;
                        case "towns":
                            SayTo(t, "I can send you to:\n" +
                                     "[Mag Mell]\n" +
                                     "[Tir na mBeo]\n" +
                                     "[Ardagh]\n" +
                                     "[Howth]\n" +
                                     "[Connla]\n" +
                                     "[Innis Carthaig]");
                            break;
                        //Begin Towns
                        case "mag mell":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Mag Mell");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 348073, 489646, 5200, 643);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "tir na mbeo":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Tir na mBeo.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 344519, 527771, 4061, 1178);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "ardagh":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Ardagh.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 351533, 553440, 5102, 3054);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "howth":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Howth.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 342575, 591967, 5456, 1014);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "connla":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Connla");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 297173, 642141, 4848, 3814);
                            }

                            break;
                        case "innis carthaig":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Innis Carthaig");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 333939, 719890, 4296, 3142);
                            }

                            break;
                        case "druim cain":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Druim Cain");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 421838, 486293, 1824, 1109);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        // End Towns
                        //Begin SI
                        case "droighaid":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Droighaid.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 379767, 421216, 5528, 1720);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "aalid feie":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Aalid Feie");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 313648, 352530, 3592, 942);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "necht":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Necht.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 429507, 318578, 3458, 716);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                        case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(202, 555396, 526607, 3008, 1309);
                            }
                            else
                            {
                                t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                                    eChatLoc.CL_PopupWindow);
                            }

                            break;
                    }

                    break;
            }

            //trying a fall through
            switch (str.ToLower())
            {
                case "personal":
                    if (t.BindHouseRegion == 0)
                    {
                        SayTo(t, "You don't own a house!");
                        break;
                    }

                    if (!t.InCombat)
                    {
                        SayTo(t, "I'm now teleporting you to your personal house.");
                        foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendSpellCastAnimation(this, 4953, 3);
                        t.MoveTo((ushort) t.BindHouseRegion, t.BindHouseXpos, t.BindHouseYpos, t.BindHouseZpos,
                            (ushort) t.BindHouseHeading);
                    }
                    else
                    {
                        t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say,
                            eChatLoc.CL_PopupWindow);
                    }

                    break;
                // case "guild":
                //     SayTo(t, "Guild House recall not yet implemented..");
                //     break;
                // case "hearth":
                //     SayTo(t, "I shall return you to your Hearthstone.");
                //     t.MoveToBind();
                //     break;
            }


            return true;
        }
    }
}