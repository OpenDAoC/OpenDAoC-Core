// I don't know the original author but this
// file has been modified by clait for Atlas Freeshard

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using DOL;
using DOL.Events;
using DOL.GS;
using DOL.GS.Housing;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;

using log4net;
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
                    break;
                case eRealm.Midgard:
                    Name = "Stor Gothi Annark";
                    GuildName = "Teleporter";
                    Model = 215;
                    break;
                case eRealm.Hibernia:
                    Name = "Channeler Glasny";
                    GuildName = "Teleporter";
                    Model = 342;
                    break;
                default:
                    break;
            }
                    Level = 60;
                    Size = 50;
                    Flags |= GameNPC.eFlags.PEACE;
            //Fix Templates Alb is this below mid and hib are different

                    GameNpcInventoryTemplate template = new GameNpcInventoryTemplate(); // This line creates a new Template for this npc, so now we can add items for him to wear.
                    /// Add equipment to the teleporter.
                    template.AddNPCEquipment(eInventorySlot.Cloak, 57, 66);
                    template.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005, 86);
                    template.AddNPCEquipment(eInventorySlot.LegsArmor, 140, 6);
                    template.AddNPCEquipment(eInventorySlot.ArmsArmor, 141, 6);
                    template.AddNPCEquipment(eInventorySlot.HandsArmor, 142, 6);
                    template.AddNPCEquipment(eInventorySlot.FeetArmor, 143, 6);
                    template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 1166);
                    /// How to pick items not explained in this document.
                    Inventory = template.CloseTemplate(); // Close the template after hes dressed
                    return base.AddToWorld(); // Finish up and add him to the world.
        }

		/// <summary>
		/// Display the teleport indicator around this teleporters feet
		/// </summary>
		public override bool ShowTeleporterIndicator
		{
			get
			{
				return true;
			}
		}


        public override bool Interact(GamePlayer player) // What to do when a player clicks on me
        {
            if (!base.Interact(player)) return false;
            switch (Realm)
            {
                case eRealm.Albion:
                    SayTo(player, "Greetings, " + player.Name + " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                                    //"[Forest Sauvage] or [Snowdonia] in the Frontiers\n" +
                                    "[Castle Sauvage] in Camelot Hills or [Snowdonia Fortress] in Black Mtns. North\n" +
                                    "[Avalon Marsh] wharf\n" +
                                    "[Gothwaite Harbor] in the [Shrouded Isles]\n" +
                                    // "[Stygia] haven in the lost lands of Atlantis\n" +
                                    // "[The Inconnu Crypt] in the Catacombs\n" +
                                    "[Camelot] our glorious capital\n" +
                                    // "[Entrance] to the areas of [Housing]\n" +
                                    // "A [Battleground] appropriate to your season\n\n" +
                                    "Or one of the many [towns] throughout Albion");
                    // if (player.Level < 15) // Add server rule check for tutorial
                    // {
                    //     SayTo(player, "You are also eligible for passage to [Holtham] in Constantine's Sound.");
                    // }
                    // else
                    // {
                    //     // Add Check for Svarhamr
                    //     SayTo(player, "In addition to the other locations to which you may travel, you are eligible to teleport to the [NEEDALB], the Svartalf village in Malmohus.");
                    // }
                    break;

                case eRealm.Midgard:
                    SayTo(player, "Greetings, " + player.Name + " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                                    //"[Uppland] or [Yggdra Forest] in the Frontiers\n" +
                                    "[Svasud Faste] in Mularn or [Vindsaul Faste] in West Svealand\n" +
                                    "Beaches of [Gotar] near Nailiten\n" +
                                    "[Aegirhamn] in the [Shrouded Isles]\n" +
                                    // "[Stygia] haven in the lost lands of Atlantis\n" +
                                    // "[Kobold Undercity] in the Catacombs\n" +
                                    "Our glorious city of [Jordheim]\n" +
                                    //"[Entrance] to the areas of [Housing]\n" +
                                    // "A [Battleground] appropriate to your season\n\n" +
                                    "Or one of the many [towns] throughout Midgard");
                    // if (player.Level < 15) // Add server rule check for tutorial
                    // {
                    //     SayTo(player, "You are also eligible for passage to [Hafheim] in Grenlock's Sound.");
                    // }
                    // else
                    // {
                    //     // Add Check for Svarhamr
                    //     SayTo(player, "In addition to the other locations to which you may travel, you are eligible to teleport to the [Svarhamr], the Svartalf village in Malmohus.");
                    // }
                    break;

                case eRealm.Hibernia:
                    SayTo(player, "Greetings, I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                                    //"[Cruachan Gorge] or [Mount Collory] in the Frontiers\n" +
                                    "[Druim Ligen] in Connacht or [Druim Cain] in Bri Leith\n" +
                                    "[Shannon Estuary] watchtower\n" +
                                    "[Domnann] Grove in the [Shrouded Isles]\n" +
                                    // "[Stygia] haven in the lost lands of Atlantis\n" +
                                    // "[Shar Labyrinth] in the Catacombs\n" +
                                    "[Tir na Nog] our glorious capital\n" +
                                    // "[Entrance] to the areas of [Housing]\n" +
                                    // "A [Battleground] appropriate to your season\n\n" +
                                    "Or one of the many [towns] throughout Hibernia");
                    // if (player.Level < 15) // Add server rule check for tutorial
                    // {
                    //     SayTo(player, "You are also eligible for passage to [Fintain] in Lamfhota's Sound.");
                    // }
                    // else
                    // {
                    //     // Add Check for Azure refuge
                    //     SayTo(player,"In adition to the other locations to which you may travel, you are eligible to teleport to the Azure refuge [Tailtiu] in Sheeroe Hills.");
                    // }
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
            GamePlayer t = (GamePlayer)source;
            TurnTo(t.X, t.Y); // Turn to face the player
            // This is where we handle what is said to the NPC, we do it by using case switches.
            // it follows this format
            // switch(str){
            // case "talks":
            //       break;
            switch (Realm) // Only offer locations based on what realm i am set at.
            {
                case eRealm.Albion:
                    switch (str.ToLower())
                    {
                        //Begin Main
                        /*case "forest sauvage":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Forest Sauvage");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 585382, 479095, 2604, 2019);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "snowdonia":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Snowdonia");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 530844, 356929, 8320, 2182);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            */
                        case "castle sauvage":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Castle Sauvage");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 584151, 477177, 2600, 3058);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "snowdonia fortress":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Snowdonia Fortress");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 527543, 358900, 8320, 3074);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "avalon marsh":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Avalon Marsh");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 470613, 630585, 1712, 2500);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "gothwaite harbor":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Gothwaite Harbor.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 526580, 542058, 3168, 406);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "shrouded isles":
                            SayTo(t, "The isles of Avalon are  an excellent choice. Would you prefer the harbor of [Gothwaite] or perhaps one of the outlying towns like [Wearyall] Village, Fort [Gwyntell], or Cear [Diogel]?");
                            break;
                        case "camelot":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Camelot");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(10, 36209, 29843, 7971, 18);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        /*case "housing":
                            SayTo(t, "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area. I can also send you to your [guild] house. If your guild does not own a house then you will not be transported. You may go to your [Hearth] bind as well if you are bound inside a house");
                            break;
                        case "battleground":
                            if (!ServerProperties.Properties.BG_ZONES_OPENED && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player)
                            {
                                SayTo(t, ServerProperties.Properties.BG_ZONES_CLOSED_MESSAGE);
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I will send you to the appropriate Battleground for your level, Good Luck.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 6);
                                    if (t.Level < 5)
                                    {
                                        // Move to The Proving Grounds area 234
                                        // Need to check realm rank
                                        t.MoveTo(234, 573154, 549877, 8640, 1389);
                                    }

                                    if (t.Level > 4 && t.Level < 10)
                                    {
                                        // Move to the Lions Den area 235
                                        t.MoveTo(235, 536907, 535991, 5056, 3965);
                                    }

                                    if (t.Level > 9 && t.Level < 15)
                                    {
                                        // Move to the Hills of Claret area 236
                                        t.MoveTo(236, 541032, 577287, 8008, 3083);
                                    }

                                    if (t.Level > 14 && t.Level < 20)
                                    {
                                        // Move to Killaloe area 237
                                        t.MoveTo(237, 544935, 582399, 8288, 2632);
                                    }

                                    if (t.Level > 19 && t.Level < 25)
                                    {
                                        // Move to Thidranki area 238
                                        t.MoveTo(238, 562805, 574005, 5408, 2796);
                                    }

                                    if (t.Level > 24 && t.Level < 30)
                                    {
                                        // Move to Breamear area 239
                                        t.MoveTo(239, 553703, 584974, 6952, 2619);
                                    }

                                    if (t.Level > 29 && t.Level < 35)
                                    {
                                        // Move to Wilton are 240
                                        t.MoveTo(240, 553692, 583983, 6952, 2632);
                                    }

                                    if (t.Level > 34 && t.Level < 40)
                                    {
                                        // Move to Molvik area 241
                                        t.MoveTo(241, 531997, 541272, 5992, 4031);
                                    }

                                    if (t.Level > 39 && t.Level < 45)
                                    {
                                        // Move to Leirvik area 242
                                        t.MoveTo(242, 322174, 284521, 10128, 1283);
                                    }

                                    if (t.Level > 44 && t.Level < 50)
                                    {
                                        // Move to Cathal Valley area 165
                                        t.MoveTo(165, 583347, 585349, 4896, 2330);
                                    }
                                    if (t.Level > 49)
                                    {
                                        // Tell them oops
                                        Say("Those who have reached their 50th season use the Frontiers as their Battlegrounds.");
                                    }
                                    // Nothing else to check for

                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                                break;
                            }
                             break;
                             */
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
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "wearyall":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Wearyall Village");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 435140, 493260, 3088, 921);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "gwyntell":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Gwyntell");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 427322, 416538, 5712, 689);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "diogel":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cear Diogel.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(51, 403525, 502582, 4680, 561);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        /*case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(2, 584461, 561355, 3576, 2256);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            //End Si
                            //Begin Towns
                            */
                        case "cotswold":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cottswold.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 559613, 511843, 2289, 3200);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "prydwen keep":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Prydwen Keep");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 573994, 529009, 2870, 2206);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "cear ulfwych":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cear Ulfwych.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 522479, 615826, 1818, 4);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "campacorentin station":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Campacorentin Station.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 493010, 591806, 1806, 3881);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "adribard's retreat":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Adribard's Retreat.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 473036, 628049, 2048, 3142);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "yarley's farm":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Yarley's Farm.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 6);
                                t.MoveTo(1, 369874, 679659, 5538, 3893);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            //End Towns
                            // Only offer tutorial if player is under 15 and its enabled, must add this otherwise player can /whisper the npc
                            // And be teleported even if they dont meet the level requirements.
                        /*case "holtham":
                            if (ServerProperties.Properties.DISABLE_TUTORIAL && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player && t.Level <15)
                            {
                                SayTo(t, "The Tutorial is disabled.");
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I'm now teleporting you to Holtham.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 6);
                                    t.MoveTo(27, 97636, 91606, 5696, 4025);
                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            }
                            break;
                            */
                        // Stonecrush Dragonsworn place for alb, put check in like for tutorial, but dont know what to check for yet, so just open it.
                        /*case "stonecrush":
                            if (t.Level < 100) // and a && check for the players flag for this, must complete a quest.
                            {
                                SayTo(t, "Stonecrush. - Not in DB att.");
                            }
                            break;*/
                        default:
                            // Clicked nothing
                            break;
                    }
                    break;
                case eRealm.Midgard:
                    switch (str.ToLower())
                    {
                        /*case "uppland":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Uppland");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 766553, 667846, 5736, 2218);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "yggdra forest":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Yggdra Forest");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 704363, 735047, 5704, 2147);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            */
                        case "svasud faste":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Svasud Faste");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 767242, 669591, 5736, 1198);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "vindsaul faste":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Vindsaul Faste");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 703389, 738621, 5704, 3097);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "gotar":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Gotar");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 771081, 836721, 4624, 167);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "shrouded isles":
                            SayTo(t, "The isles of Aegir are an excellent choice. Would you prefer the city of [Aegirhamn] or perhaps one of the outlying towns like [Bjarken], [Hagall], or [Knarr]?");
                            break;
                        case "jordheim":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Jordheim");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(101, 31619, 28768, 8800, 2201);
                                //MoveTo(regionid, x , y, z, heading)
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        /*case "housing":
                            SayTo(t, "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area. I can also send you to your [guild] house. If your guild does not own a house then you will not be transported. You may go to your [Hearth] bind as well if you are bound inside a house");
                            break;                           
                        case "battleground":
                            if (!ServerProperties.Properties.BG_ZONES_OPENED && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player)
                            {
                                SayTo(t, ServerProperties.Properties.BG_ZONES_CLOSED_MESSAGE);
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I will send you to the appropriate Battleground for your level, Good Luck.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 6);
                                    if (t.Level < 5)
                                    {
                                        // Move to The Proving Grounds area 234
                                        // Need to check realm rank
                                        t.MoveTo(234, 556216, 574739, 8640, 2761);
                                    }

                                    if (t.Level > 4 && t.Level < 10)
                                    {
                                        // Move to the Lions Den area 235
                                        t.MoveTo(235, 543729, 575471, 50556, 2965);
                                    }

                                    if (t.Level > 9 && t.Level < 15)
                                    {
                                        // Move to the Hills of Claret area 236
                                        t.MoveTo(236, 582679, 554408, 8008, 1654);
                                    }

                                    if (t.Level > 14 && t.Level < 20)
                                    {
                                        // Move to Killaloe area 237
                                        t.MoveTo(237, 585970, 559111, 8288, 835);
                                    }

                                    if (t.Level > 19 && t.Level < 25)
                                    {
                                        // Move to Thidranki area 238
                                        t.MoveTo(238, 570913, 540584, 5408, 478);
                                    }

                                    if (t.Level > 24 && t.Level < 30)
                                    {
                                        // Move to Breamear area 239
                                        t.MoveTo(239, 582186, 539260, 6776, 1431);
                                    }

                                    if (t.Level > 29 && t.Level < 35)
                                    {
                                        // Move to Wilton are 240
                                        t.MoveTo(240, 534127, 534463, 6728, 3945);
                                    }

                                    if (t.Level > 34 && t.Level < 40)
                                    {
                                        // Move to Molvik area 241
                                        t.MoveTo(241, 549468, 577418, 5992, 2552);
                                    }

                                    if (t.Level > 39 && t.Level < 45)
                                    {
                                        // Move to Leirvik area 242
                                        t.MoveTo(242, 272810, 272742, 10128, 360);
                                    }

                                    if (t.Level > 44 && t.Level < 50)
                                    {
                                        // Move to Cathal Valley area 165
                                        t.MoveTo(165, 575260, 538161, 4832, 1134);
                                    }
                                    if (t.Level > 49)
                                    {
                                        // Tell them oops
                                        Say("Those who have reached their 50th season use the Frontiers as their Battlegrounds.");
                                    }
                                    // Nothing else to check for

                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                                break;
                            }
                            break;
                            */
                        case "towns":
                            SayTo(t, "I can send you to:\n" +
                                        "[Mularn]\n" +
                                        "[Fort Veldon]\n" +
                                        "[Audliten]\n" +
                                        "[Huginfel]\n" +
                                        "[Fort Atla]");
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
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "audliten":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Audliten");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 725682, 760401, 4528, 1150);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "fort veldon":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Veldon.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 800200, 678003, 5304, 204);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "huginfel":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Huginfel.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 711788, 784084, 4672, 2579);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "fort atla":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Fort Atla.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(100, 749237, 816443, 4408, 2033);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        /*case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(102, 527051, 561559, 3638, 102);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            // End Towns
                            //Begin Si
                            */
                        case "aegirhamn":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Aegirhamn.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 294213, 355955, 3570, 4070);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "bjarken":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Bjarken.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 289626, 301652, 4160, 2804);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "hagall":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Hagall.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 379055, 386013, 7752, 2187);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "knarr":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Knarr.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(151, 302660, 433690, 3214, 2103);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            // End SI
                        /*case "hafheim":
                            if (ServerProperties.Properties.DISABLE_TUTORIAL && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player && t.Level < 15)
                            {
                                SayTo(t, "The Tutorial is disabled.");
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I'm now teleporting you to Hafheim.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 3);
                                    t.MoveTo(27, 228981, 222130, 5696, 41);
                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            }
                            break;
                            */
                        // Svarhamr Dragonsworn place for mid, put check in like for tutorial, but dont know what to check for yet, so just open it.
                        /*case "svarhamr":
                            if (t.Level < 100) // and a && check for the players flag for this, must complete a quest.
                            {
                                if (!t.InCombat)
                                {
                                    Say("I'm now teleporting you to Svarhamr.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 3);
                                    t.MoveTo(100, 742842, 978919, 3920, 1680);
                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            }
                            break;*/
                        default:
                            // Clicked nothing
                            break;
                    }
                    break;
                case eRealm.Hibernia:
                    switch (str.ToLower())
                    {
                        /*case "cruachan gorge":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cruachan Gorge");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 333355, 414423, 5254, 2144);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "mount collory":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Cruachan Gorge");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 420917, 482990, 1688, 1981);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                            */
                        case "druim ligen":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Druim Ligen");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 334600, 419997, 5184, 479);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "shannon estuary":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Shannon Estuary");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 310320, 645327, 4855, 1441);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "domnann":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Domann Grove.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 423157, 442474, 5952, 2046);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "shrouded isles":
                            SayTo(t, "The isles of Hy Brasil are an excellent choice. Would you prefer the grove of [Domnann] or perhaps one of the outlying towns like [Droighaid], [Aalid Feie], or [Necht]?");
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
                        /*case "housing":
                            SayTo(t, "I can send you to your [personal] house. If you do not have a personal house or wish to be sent to the housing [entrance] then you will arrive just inside the housing area. I can also send you to your [guild] house. If your guild does not own a house then you will not be transported. You may go to your [Hearth] bind as well if you are bound inside a house");
                            break;
                        case "battleground":
                            if (!ServerProperties.Properties.BG_ZONES_OPENED && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player)
                            {
                                SayTo(t, ServerProperties.Properties.BG_ZONES_CLOSED_MESSAGE);
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I will send you to the appropriate Battleground for your level, Good Luck.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 6);
                                    if (t.Level < 5)
                                    {
                                        // Move to The Proving Grounds area 234
                                        // Need to check realm rank
                                        t.MoveTo(234, 541540, 549326, 8640, 2707);
                                    }

                                    if (t.Level > 4 && t.Level < 10)
                                    {
                                        // Move to the Lions Den area 235
                                        t.MoveTo(235, 580335, 555282, 5056, 1127);
                                    }

                                    if (t.Level > 9 && t.Level < 15)
                                    {
                                        // Move to the Hills of Claret area 236
                                        t.MoveTo(236, 538416, 539050, 8008, 3917);
                                    }

                                    if (t.Level > 14 && t.Level < 20)
                                    {
                                        // Move to Killaloe area 237
                                        t.MoveTo(237, 534289, 536526, 8288, 3532);
                                    }

                                    if (t.Level > 19 && t.Level < 25)
                                    {
                                        // Move to Thidranki area 238
                                        t.MoveTo(238, 534248, 533333, 5408, 3985);
                                    }

                                    if (t.Level > 24 && t.Level < 30)
                                    {
                                        // Move to Breamear area 239
                                        t.MoveTo(239, 533569, 533068, 6768, 3759);
                                    }

                                    if (t.Level > 29 && t.Level < 35)
                                    {
                                        // Move to Wilton are 240
                                        t.MoveTo(240, 581353, 539099, 6736, 917);
                                    }

                                    if (t.Level > 34 && t.Level < 40)
                                    {
                                        // Move to Molvik area 241
                                        t.MoveTo(241, 576254, 544246, 5992, 1462);
                                    }

                                    if (t.Level > 39 && t.Level < 45)
                                    {
                                        // Move to Leirvik area 242
                                        t.MoveTo(242, 279389, 319874, 10128, 2470);
                                    }

                                    if (t.Level > 44 && t.Level < 50)
                                    {
                                        // Move to Cathal Valley area 165
                                        t.MoveTo(165, 536222, 585564, 5800, 1958);
                                    }
                                    if (t.Level > 49)
                                    {
                                        // Tell them oops
                                        Say("Those who have reached their 50th season use the Frontiers as their Battlegrounds.");
                                    }
                                    // Nothing else to check for

                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                                break;
                            }
                            break;
                            */
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
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "tir na mbeo":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Tir na mBeo.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 344519, 527771, 4061, 1178);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "ardagh":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Ardagh.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 351533, 553440, 5102, 3054);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "howth":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Howth.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(200, 342575, 591967, 5456, 1014);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
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
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
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
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "aalid feie":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Aalid Feie");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 313648, 352530, 3592, 942);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        case "necht":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Necht.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(181, 429507, 318578, 3458, 716);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;
                        /*case "entrance":
                            if (!t.InCombat)
                            {
                                Say("I'm now teleporting you to Housing.");
                                foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                    player.Out.SendSpellCastAnimation(this, 4953, 3);
                                t.MoveTo(202, 555396, 526607, 3008, 1309);
                            }
                            else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            break;                         
                            // End SI
                        case "fintain":
                            if (ServerProperties.Properties.DISABLE_TUTORIAL && t.Client.Account.PrivLevel == (uint)ePrivLevel.Player && t.Level < 15)
                            {
                                SayTo(t, "The Tutorial is disabled.");
                            }
                            else
                            {
                                if (!t.InCombat)
                                {
                                    Say("I'm now teleporting you to Fintain.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 3);
                                    t.MoveTo(27, 359574, 353555, 5688, 18);
                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            }
                            break;
                            */
                        // Tailtiu Dragonsworn place for hib, put check in like for tutorial, but dont know what to check for yet, so just open it.
                        /*case "tailtiu":
                            if (t.Level < 100) // and a && check for the players flag for this, must complete a quest.
                            {
                                if (!t.InCombat)
                                {
                                    Say("I'm now teleporting you to Tailtiu.");
                                    foreach (GamePlayer player in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                        player.Out.SendSpellCastAnimation(this, 4953, 3);
                                    t.MoveTo(200, 369715, 651594, 3693, 1882);
                                }
                                else { t.Client.Out.SendMessage("You can't port while in combat.", eChatType.CT_Say, eChatLoc.CL_PopupWindow); }
                            }
                            break;*/
                        default:
                            // Clicked nothing
                            break;
                    }
                    break;
                default:
                    // Npc has no realm set, and therefore will not work.
                    break;
            }
            //trying a fall through
            switch (str.ToLower())
            {
                case "personal":
                    SayTo(t, "Personal House recall not yet implemented.");
                    break;
                case "guild":
                    SayTo(t, "Guild House recall not yet implemented..");
                    break;
                case "hearth":
                    SayTo(t, "I shall return you to your Hearthstone.");
                    t.MoveToBind();
                    break;
                default:
                    break;
            }


            return true;
        }
    }
}