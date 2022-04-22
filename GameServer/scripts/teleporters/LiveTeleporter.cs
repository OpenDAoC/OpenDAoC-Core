// I don't know the original author but this
// file has been modified by clait for Atlas Freeshard

using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

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
        /// <summary>
        /// The type of teleporter; this is used in order to be able to handle
        /// identical TeleportIDs differently, depending on the actual teleporter.
        /// </summary>
        protected virtual String Type
        {
            get { return ""; }
        }

        /// <summary>
        /// The destination realm. 
        /// </summary>
        protected virtual eRealm DestinationRealm
        {
            get { return Realm; }
        }

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
            if (!base.Interact(player) || GameRelic.IsPlayerCarryingRelic(player)) return false;

            if (player.Realm != this.Realm && player.Client.Account.PrivLevel == 1) return false;

            TurnTo(player, 10000);
            
            var message = "";

            switch (Realm)
            {
                case eRealm.Albion:

                    message = "Greetings, " + player.Name +
                              " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                              "[Castle Sauvage] in Camelot Hills or \n[Snowdonia Fortress] in Black Mtns. North,\n" +
                              "[Avalon Marsh] wharf,\n" +
                              "[Gothwaite Harbor] in the [Shrouded Isles],\n" +
                              "[Camelot] our glorious capital,\n" +
                              "[Entrance] to the areas of [Housing]\n\n" +
                              "or one of the many [towns] throughout Albion.";
                              //"For this event duration, I can send you to [Darkness Falls]";
                    break;

                case eRealm.Midgard:
                    
                    message = "Greetings, " + player.Name +
                              " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                              "[Svasud Faste] in Mularn or \n[Vindsaul Faste] in West Svealand,\n" +
                              "Beaches of [Gotar] near Nailiten,\n" +
                              "[Aegirhamn] in the [Shrouded Isles],\n" +
                              "Our glorious city of [Jordheim],\n" +
                              "[Entrance] to the areas of [Housing]\n\n" +
                              "or one of the many [towns] throughout Midgard.";
                    break;

                case eRealm.Hibernia:
                    
                    message = "Greetings, " + player.Name +
                              " I am able to channel energy to transport you to distant lands. I can send you to the following locations:\n\n" +
                              "[Druim Ligen] in Connacht or \n[Druim Cain] in Bri Leith,\n" +
                              "[Shannon Estuary] watchtower,\n" +
                              "[Domnann] Grove in the [Shrouded Isles],\n" +
                              "[Tir na Nog] our glorious capital,\n" +
                              "[Entrance] to the areas of [Housing]\n\n" +
                              "or one of the many [towns] throughout Hibernia.";
                    break;

                default:
                    SayTo(player, "I have no Realm set, so don't know what locations to offer..");
                    break;
            }
            
            message += "\n\n" +
                       "Perhaps you would like the challenge of the [Epic Dungeon]?";
            
            SayTo(player, message);

            return true;
        }

        public override bool WhisperReceive(GameLiving source, string str) // What to do when a player whispers me
        {
            if (!base.WhisperReceive(source, str)) return false;

            GamePlayer player = source as GamePlayer;
            if (player == null)
                return false;

            if (GameRelic.IsPlayerCarryingRelic(player))
                return false;

            return GetTeleportLocation(player, str);

        }

        protected virtual bool GetTeleportLocation(GamePlayer player, string text)
        {
            switch (Realm) // Only offer locations based on what realm i am set at.
            {
                case eRealm.Albion:
                    
                    if (text.ToLower() == "shrouded isles")
                    {
                        String reply = String.Format("The isles of Avalon are an excellent choice. {0} {1}",
                            "Would you prefer [Gothwaite] or perhaps one of the outlying towns",
                            "like [Wearyall Village], Fort [Gwyntell], or [Caer Diogel]?");
                        SayTo(player, reply);
                        return false;
                    }
                    
                    if (text.ToLower() == "housing")
                    {
                        SayTo(player,
                            "I can send you to your [personal] or [guild] house. If you do not have a personal house, I can teleport you to the housing [entrance] or your housing [hearth] bindstone.");
                        return false;
                    }
                    
                    if (text.ToLower() == "towns")
                    {
                        SayTo(player, "I can send you to:\n" +
                                      "[Cotswold Village]\n" +
                                      "[Prydwen Keep]\n" +
                                      "[Caer Ulfwych]\n" +
                                      "[Campacorentin Station]\n" +
                                      "[Adribard's Retreat]\n" +
                                      "[Yarley's Farm]");
                        return false;
                    }

                    /*
                    if (text.ToLower() == "darkness falls")
                    {
                        IGameLocation location = new GameLocation("df", 249, 249, 23122, 19634, 22897, 3074);
                        
                        Teleport teleport = new Teleport();
                        teleport.TeleportID = "Darkness Falls";
                        teleport.Realm = (int) DestinationRealm;
                        teleport.RegionID = location.RegionID;
                        teleport.X = location.X;
                        teleport.Y = location.Y;
                        teleport.Z = location.Z;
                        teleport.Heading = location.Heading;
                        OnDestinationPicked(player, teleport);
                        return true;
                    }*/
                    
                    break;
                
                case eRealm.Midgard:
                    
                    if (text.ToLower() == "shrouded isles")
                    {
                        String reply = String.Format("The isles of Aegir are an excellent choice. {0} {1}",
                            "Would you prefer the city of [Aegirhamn] or perhaps one of the outlying towns",
                            "like [Bjarken], [Hagall], or [Knarr]?");
                        SayTo(player, reply);
                        
                        return false;
                    }

                    if (text.ToLower() == "housing")
                    {
                        SayTo(player,
                            "I can send you to your [personal] or [guild] house. If you do not have a personal house, I can teleport you to the housing [entrance] or your housing [hearth] bindstone.");
                        return false;
                    }

                    if (text.ToLower() == "towns")
                    {
                        SayTo(player,
                            "I can send you to:\n" +
                            "[Mularn]\n" +
                            "[Fort Veldon]\n" +
                            "[Audliten]\n" +
                            "[Huginfell]\n" +
                            "[Fort Atla]\n" +
                            "[West Skona]");
                        return false;
                    }
                    
                    break;
                
                case eRealm.Hibernia:
                    
                    if (text.ToLower() == "shrouded isles")
                    {
                        SayTo(player,
                            "The isles of Hy Brasil are an excellent choice. Would you prefer the grove of [Domnann] or perhaps one of the outlying towns like [Droighaid], [Aalid Feie], or [Necht]?");
                        return false;
                    }

                    if (text.ToLower() == "housing")
                    {
                        SayTo(player,
                            "I can send you to your [personal] or [guild] house. If you do not have a personal house, I can teleport you to the housing [entrance] or your housing [hearth] bindstone.");
                        return false;
                    }

                    if (text.ToLower() == "towns")
                    {
                        SayTo(player,
                            "I can send you to:\n" +
                            "[Mag Mell]\n" +
                            "[Tir na mBeo]\n" +
                            "[Ardagh]\n" +
                            "[Howth]\n" +
                            "[Connla]\n" +
                            "[Innis Carthaig]");
                        return false;
                    }

                    break;
            }

            // Another special case is personal house, as there is no location
            // that will work for every player.
            if (text.ToLower() == "personal")
            {
                House house = HouseMgr.GetHouseByPlayer(player);

                if (house == null)
                {
                    text = "entrance"; // Fall through, port to housing entrance.
                }
                else
                {
                    IGameLocation location = house.OutdoorJumpPoint;
                    Teleport teleport = new Teleport();
                    teleport.TeleportID = "your house";
                    teleport.Realm = (int) DestinationRealm;
                    teleport.RegionID = location.RegionID;
                    teleport.X = location.X;
                    teleport.Y = location.Y;
                    teleport.Z = location.Z;
                    teleport.Heading = location.Heading;
                    OnDestinationPicked(player, teleport);
                    return true;
                }
            }

            // Yet another special case the port to the 'hearth' what means
            // that the player will be ported to the defined house bindstone
            if (text.ToLower() == "hearth")
            {
                // Check if player has set a house bind
                if (!(player.BindHouseRegion > 0))
                {
                    SayTo(player, "Sorry, you haven't set any house bind point yet.");
                    return false;
                }

                // Check if the house at the player's house bind location still exists
                ArrayList houses = (ArrayList) HouseMgr.GetHousesCloseToSpot((ushort) player.BindHouseRegion,
                    player.BindHouseXpos, player.BindHouseYpos, 700);
                if (houses.Count == 0)
                {
                    SayTo(player, "I'm afraid I can't teleport you to your hearth since the house at your " +
                                  "house bind location has been torn down.");
                    return false;
                }

                // Check if the house at the player's house bind location contains a bind stone
                House targetHouse = (House) houses[0];
                IDictionary<uint, DBHouseHookpointItem> hookpointItems = targetHouse.HousepointItems;
                Boolean hasBindstone = false;

                foreach (KeyValuePair<uint, DBHouseHookpointItem> targetHouseItem in hookpointItems)
                {
                    if (((GameObject) targetHouseItem.Value.GameObject).GetName(0, false).ToLower()
                        .EndsWith("bindstone"))
                    {
                        hasBindstone = true;
                        break;
                    }
                }

                if (!hasBindstone)
                {
                    SayTo(player, "I'm sorry to tell that the bindstone of your current house bind location " +
                                  "has been removed, so I'm not able to teleport you there.");
                    return false;
                }

                // Check if the player has the permission to bind at the house bind stone
                if (!targetHouse.CanBindInHouse(player))
                {
                    SayTo(player, "You're no longer allowed to bind at the house bindstone you've previously " +
                                  "chosen, hence I'm not allowed to teleport you there.");
                    return false;
                }

                Teleport teleport = new Teleport();
                teleport.TeleportID = "hearth";
                teleport.Realm = (int) DestinationRealm;
                teleport.RegionID = player.BindHouseRegion;
                teleport.X = player.BindHouseXpos;
                teleport.Y = player.BindHouseYpos;
                teleport.Z = player.BindHouseZpos;
                teleport.Heading = player.BindHouseHeading;
                OnDestinationPicked(player, teleport);
                return true;
            }

            if (text.ToLower() == "guild")
            {
                House house = HouseMgr.GetGuildHouseByPlayer(player);

                if (house == null)
                {
                    SayTo(player, $"I'm sorry but {player.Guild.Name} doesn't own a Guild House.");
                    return false;
                    return false; // no teleport when guild house not found
                }
                else
                {
                    IGameLocation location = house.OutdoorJumpPoint;
                    Teleport teleport = new Teleport();
                    teleport.TeleportID = "guild house";
                    teleport.Realm = (int) DestinationRealm;
                    teleport.RegionID = location.RegionID;
                    teleport.X = location.X;
                    teleport.Y = location.Y;
                    teleport.Z = location.Z;
                    teleport.Heading = location.Heading;
                    OnDestinationPicked(player, teleport);
                    return true;
                }
            }

            if (text.ToLower() == "epic dungeon")
            {
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        GetTeleportLocation(player, "Caer Sidi");
                        return true;
                    case eRealm.Midgard:
                        GetTeleportLocation(player, "Tuscaran Glacier");
                        return true;
                    case eRealm.Hibernia:
                        GetTeleportLocation(player, "Galladoria");
                        return false;
                }
            }

            // Find the teleport location in the database.
            Teleport port = WorldMgr.GetTeleportLocation(DestinationRealm, String.Format("{0}:{1}", Type, text));
            if (port != null)
            {
                if (port.RegionID == 0 && port.X == 0 && port.Y == 0 && port.Z == 0)
                {
                    OnSubSelectionPicked(player, port);
                }
                else
                {
                    OnDestinationPicked(player, port);
                }

                return false;
            }

            return true; // Needs further processing.
        }

        /// <summary>
        /// Player has picked a destination.
        /// Override if you need the teleporter to say something to the player
        /// before porting him.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnDestinationPicked(GamePlayer player, Teleport destination)
        {
            Region region = WorldMgr.GetRegion((ushort) destination.RegionID);

            if (region == null || region.IsDisabled)
            {
                player.Out.SendMessage("This destination is not available.", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return;
            }
            
            Say("I'm now teleporting you to " + destination.TeleportID + ".");
            OnTeleportSpell(player, destination);
        }

        /// <summary>
        /// Player has picked a subselection.
        /// Override to pass teleport options on to the player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="subSelection"></param>
        protected virtual void OnSubSelectionPicked(GamePlayer player, Teleport subSelection)
        {
        }

        /// <summary>
        /// Teleport the player to the designated coordinates using the
        /// portal spell.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnTeleportSpell(GamePlayer player, Teleport destination)
        {
            SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
            List<Spell> spellList = SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells);
            Spell spell = SkillBase.GetSpellByID(5999); // UniPortal spell.

            if (spell != null)
            {
                TargetObject = player;
                UniPortal portalHandler = new UniPortal(this, spell, spellLine, destination);
                portalHandler.CastSpell();
                return;
            }

            // Spell not found in the database, fall back on default procedure.

            if (player.Client.Account.PrivLevel > 1)
                player.Out.SendMessage("Uni-Portal spell not found.",
                    eChatType.CT_Skill, eChatLoc.CL_SystemWindow);


            this.OnTeleport(player, destination);
        }

        /// <summary>
        /// Teleport the player to the designated coordinates. 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnTeleport(GamePlayer player, Teleport destination)
        {
            if (player.InCombat == false && GameRelic.IsPlayerCarryingRelic(player) == false)
            {
                player.LeaveHouse();
                GameLocation currentLocation =
                    new GameLocation("TeleportStart", player.CurrentRegionID, player.X, player.Y, player.Z);
                player.MoveTo((ushort) destination.RegionID, destination.X, destination.Y, destination.Z,
                    (ushort) destination.Heading);
                GameServer.ServerRules.OnPlayerTeleport(player, currentLocation, destination);
            }
        }
    }
}