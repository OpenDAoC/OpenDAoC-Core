using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Commands;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles spell cast requests from client
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.UseSpell, "Handles Player Use Spell Request.", eClientStatus.PlayerInGame)]
    public class UseSpellHandler : AbstractCommandHandler, IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int flagSpeedData;
            int spellLevel;
            int spellLineIndex;

            if (client.Version >= GameClient.eClientVersion.Version1124)
            {
                client.Player.X = (int) packet.ReadFloatLowEndian();
                client.Player.Y = (int) packet.ReadFloatLowEndian();
                client.Player.Z = (int) packet.ReadFloatLowEndian();
                client.Player.CurrentSpeed = (short) packet.ReadFloatLowEndian();
                client.Player.Heading = packet.ReadShort();
                flagSpeedData = packet.ReadShort();
                spellLevel = packet.ReadByte();
                spellLineIndex = packet.ReadByte();
                // Two bytes at end, not sure what for.
            }
            else
            {
                flagSpeedData = packet.ReadShort();
                ushort heading = packet.ReadShort();

                if (client.Version > GameClient.eClientVersion.Version171)
                {
                    int xOffsetInZone = packet.ReadShort();
                    int yOffsetInZone = packet.ReadShort();
                    int currentZoneID = packet.ReadShort();
                    int realZ = packet.ReadShort();

                    Zone newZone = WorldMgr.GetZone((ushort) currentZoneID);

                    if (newZone == null)
                        Log.Warn($"Unknown zone in UseSpellHandler: {currentZoneID} player: {client.Player.Name}");
                    else
                    {
                        client.Player.X = newZone.XOffset + xOffsetInZone;
                        client.Player.Y = newZone.YOffset + yOffsetInZone;
                        client.Player.Z = realZ;
                    }
                }

                spellLevel = packet.ReadByte();
                spellLineIndex = packet.ReadByte();
                client.Player.Heading = heading;
            }

            GamePlayer player = client.Player;

            // Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
            //if ((flagSpeedData & 0x200) != 0)
            //	player.CurrentSpeed = (short)(-(flagSpeedData & 0x1ff)); // backward movement
            //else
            //	player.CurrentSpeed = (short)(flagSpeedData & 0x1ff); // forward movement

            player.IsStrafing = (flagSpeedData & 0x4000) != 0;
            player.TargetInView = (flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
            player.GroundTargetInView = (flagSpeedData & 0x1000) != 0;

            List<Tuple<SpellLine, List<Skill>>> snap = player.GetAllUsableListSpells();
            Skill sk = null;
            SpellLine sl = null;

            // is spelline in index ?
            if (spellLineIndex < snap.Count)
            {
                int index = snap[spellLineIndex].Item2.FindIndex(s => s is Spell ? s.Level == spellLevel :
                                                                (s is Styles.Style style ? style.SpecLevelRequirement == spellLevel :
                                                                (s is Ability ability ? ability.SpecLevelRequirement == spellLevel :
                                                                false)));

                if (index > -1)
                    sk = snap[spellLineIndex].Item2[index];

                sl = snap[spellLineIndex].Item1;
            }

            if (sk is Spell spell && sl != null)
                player.CastSpell(spell, sl);
            else if (sk is Styles.Style style)
                player.styleComponent.ExecuteWeaponStyle(style);
            else if (sk is Ability ability)
                player.castingComponent.RequestStartUseAbility(ability);
            else
            {
                if (Log.IsWarnEnabled)
                    Log.Warn($"Client <{player.Client.Account.Name}> requested incorrect spell at level {spellLevel} in spell-line {(sl == null || sl.Name == null ? "unknown" : sl.Name)}");

                player.Out.SendMessage($"Error : Spell (Line {spellLineIndex}, Level {spellLevel}) can't be resolved...", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
            }
        }
    }
}
