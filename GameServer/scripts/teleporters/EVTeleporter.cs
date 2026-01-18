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
    public class EVTeleporter : GameNPC
    {
        /// <summary>
        /// The type of teleporter; this is used in order to be able to handle
        /// identical TeleportIDs differently, depending on the actual teleporter.
        /// </summary>
        protected virtual String Type
        {
            get { return string.Empty; }
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
                    Name = "Master Vaughn";
                    GuildName = "";
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
                    Name = "Stor Gothi Dagny";
                    GuildName = "";
                    Model = 177;

                    GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
                    templateMid.AddNPCEquipment(eInventorySlot.Cloak, 57, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.TorsoArmor, 245, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.LegsArmor, 246, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.HandsArmor, 248, 26);
                    templateMid.AddNPCEquipment(eInventorySlot.FeetArmor, 249, 26);
                    Inventory = templateMid.CloseTemplate();
                    break;
                case eRealm.Hibernia:
                    Name = "Brerend";
                    GuildName = "";
                    Model = 334;

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
            
            var message = string.Empty;

            switch (Realm)
            {
                case eRealm.Albion:
                    {
                        message = "Me and my assistants have the power to teleport you to the magical island of [Ellan Vannin].\n";
                        break;
                    }

                case eRealm.Midgard:
                    {
                        message = "Me and my assistants have the power to teleport you to the magical island of [Ellan Vannin].\n";
                        break;
                    }
                case eRealm.Hibernia:
                    {
                        message = "Me and my assistants have the power to teleport you to the magical island of [Ellan Vannin].\n";
                        break;
                    }

                default:
                    SayTo(player, "I have no Realm set, so don't know what locations to offer..");
                    break;
            }

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
            if (text.ToLower() == "ellan vannin")
            {
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        GetTeleportLocation(player, "EV_Alb");
                        return true;
                    case eRealm.Midgard:
                        GetTeleportLocation(player, "EV_Mid");
                        return true;
                    case eRealm.Hibernia:
                        GetTeleportLocation(player, "EV_Hib");
                        return false;
                }
            }

            // Find the teleport location in the database.
            DbTeleport port = WorldMgr.GetTeleportLocation(DestinationRealm, String.Format("{0}:{1}", Type, text));
            if (port != null)
            {
                OnDestinationPicked(player, port);
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
        protected virtual void OnDestinationPicked(GamePlayer player, DbTeleport destination)
        {
            Region region = WorldMgr.GetRegion((ushort) destination.RegionID);

            if (region == null || region.IsDisabled)
            {
                player.Out.SendMessage("This destination is not available.", eChatType.CT_System,
                    eChatLoc.CL_SystemWindow);
                return;
            }
            
            var message = $"{Name} says, \"I'm now teleporting you to {region.Description}.\"";
            
            player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            
            OnTeleportSpell(player, destination);
        }

        /// <summary>
        /// Teleport the player to the designated coordinates using the
        /// portal spell.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected virtual void OnTeleportSpell(GamePlayer player, DbTeleport destination)
        {
            SpellLine spellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);
            List<Spell> spellList = SkillBase.GetSpellList(GlobalSpellsLines.Mob_Spells);
            Spell spell = SkillBase.GetSpellByID(5999); // UniPortal spell.

            if (spell != null)
            {
                UniPortal portalHandler = new UniPortal(this, spell, spellLine, destination);
                portalHandler.StartSpell(player);
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
        protected virtual void OnTeleport(GamePlayer player, DbTeleport destination)
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