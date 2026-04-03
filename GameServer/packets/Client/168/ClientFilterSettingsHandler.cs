using System;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ClientFilterSettings, "Client chat and effect filter settings handler.", eClientStatus.PlayerInGame)]
    public class ClientFilterSettingsHandler : PacketHandler
    {
        /*
         * Server side implementation aiming at:
         * Reducing the amount of packets send to clients, when those are going to be ignored by it anyway.
         * Fixing broken client side implementation of /effects, not working for some values (1.127).
         * Caveats:
         * A player re-enabling previously disabled chat types won't see the previous ones appear in their log, as those were never sent to begin with.
         * The effect filter can only be used in SendSpellEffectAnimation (not in SendSpellCastAnimation),
         * this means that spell casting will still show visual effects for the filters that are broken client side.
         */

        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            // Bytes 0-15: Chat message filter mask. Maps to eChatType values, starting from CT_Spell (0x10) to CT_SpellPulse (0x1F).
            for (eChatType chatType = eChatType.CT_Spell; chatType <= eChatType.CT_SpellPulse; chatType++)
            {
                bool isEnabled = packet.ReadByte() == 0x00;

                if (isEnabled)
                    client.DisabledChatTypes.Remove(chatType);
                else
                    client.DisabledChatTypes.Add(chatType);
            }

            // Byte 16: /effects command.
            byte effectSettingByte = (byte) packet.ReadByte();

            if (Enum.IsDefined(typeof(eEffectFilter), effectSettingByte))
                client.EffectFilter = (eEffectFilter) effectSettingByte;

            // Bytes 17-19: Unknown state flags.
        }
    }
}
