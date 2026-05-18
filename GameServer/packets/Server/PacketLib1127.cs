using System;
using DOL.GS.Commands;
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1127, GameClient.eClientVersion.Version1127)]
    public class PacketLib1127 : PacketLib1126
    {
        public PacketLib1127(GameClient client) : base(client) { }

        /// 1127 login granted packet unchanged, work around for server type
        public override void SendLoginGranted(byte color)
        {
            // work around for character screen bugs when server type sent as 00 but player doesnt have a realm
            // 0x07 allows for characters in all realms
            using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.LoginGranted)))
            {
                pak.WritePascalString(m_gameClient.Account.Name);
                pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
                pak.WriteByte(0x05); //Server ID, seems irrelevant
                var type = color == 0 ? 7 : color;
                pak.WriteByte((byte)type); // 00 normal type?, 01 mordred type, 03 gaheris type, 07 ywain type
                pak.WriteByte(0x00); // Trial switch 0x00 - subbed, 0x01 - trial acc
                SendTCP(pak);
            }
        }

        public override void SendMessage(string msg, eChatType type, eChatLoc loc)
        {
            SnoopManager.CheckAndBroadcast(m_gameClient.Player, msg, type, loc);

            if (m_gameClient.DisabledChatTypes.Contains(type))
                return;

            SendRawMessage(msg, type, loc);
        }

        public override void SendRawMessage(string msg, eChatType type, eChatLoc loc)
        {
            if (m_gameClient.ClientState is GameClient.eClientState.CharScreen)
                return;

            var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.Message));
            pak.WriteByte((byte) type);

            // The @@ prefix seems to be technically needed only for the send reply feature.
            // Otherwise the client is able to print to the correct window based on eChatType.
            // We're keeping it here in case something else still needs it (more research needed).
            if (type is eChatType.CT_Send)
                pak.WriteNonNullTerminatedString("@@");
            else if (loc is eChatLoc.CL_PopupWindow)
                pak.WriteNonNullTerminatedString("##");

            pak.WriteString(msg);
            SendTCP(pak);
        }

        public override void SendHookPointStore(GameKeepHookPoint hookPoint)
        {
            using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.KeepComponentHookpointStore)))
            {
                HookPointInventory inventory;

                if (hookPoint.ID > 0x80)
                    inventory = HookPointInventory.YellowHPInventory; // oil
                else if (hookPoint.ID > 0x60)
                    inventory = HookPointInventory.GreenHPInventory; // big siege
                else if (hookPoint.ID > 0x40)
                    inventory = HookPointInventory.LightGreenHPInventory; // small siege
                else if (hookPoint.ID > 0x20)
                    inventory = HookPointInventory.BlueHPInventory; // npc
                else
                    inventory = HookPointInventory.RedHPInventory; // guard

                var items = inventory.GetAllItems();
                int count = Math.Min(items.Count, 30); // client caps at 0x1e

                // 1127 header: count, unknown/ref byte, argA, argB, argC, keepId, componentId, hookPointId
                pak.WriteByte((byte)count);
                pak.WriteByte(0);    // unknown/ref
                pak.WriteByte(1);    // argA
                pak.WriteByte(1);    // argB
                pak.WriteByte(1);    // argC
                pak.WriteShortLowEndian((ushort)hookPoint.Component.Keep.KeepID);
                pak.WriteShortLowEndian((ushort)hookPoint.Component.ID);
                pak.WriteShortLowEndian((ushort)hookPoint.ID);

                int i = 0;
                foreach (HookPointItem item in items)
                {
                    if (i >= count) break;

                    // Row format matches the 1125+ merchant item serialization.
                    // HookPointItem fields are mapped as follows:
                    //   level        = upper byte of Flag (encodes display/type class)
                    //   value1       = lower byte of Flag
                    //   SPD_ABS      = 0 (not applicable)
                    //   hand         = 0 (not applicable)
                    //   type_damage|objecttype = 0 (not applicable)
                    //   usable       = 0 (0 = usable/purchasable in 1127 client)
                    //   weight       = 0
                    //   price        = Gold
                    //   model        = Icon
                    //   name         = Name
                    pak.WriteByte((byte)i);
                    pak.WriteByte((byte)(item.Flag >> 8));       // level field — upper byte of Flag
                    pak.WriteByte((byte)(item.Flag & 0xFF));     // value1/DPS_AF field — lower byte of Flag
                    pak.WriteByte(0);                            // SPD_ABS
                    pak.WriteByte(0);                            // hand
                    pak.WriteByte(0);                            // type_damage | object_type
                    pak.WriteByte(0);                            // usable/display flag (0 = usable)
                    pak.WriteShortLowEndian(0);                  // weight
                    pak.WriteIntLowEndian((uint)item.Gold);      // price
                    pak.WriteShortLowEndian(item.Icon);          // model
                    pak.WritePascalStringIntLE(item.Name); // fixed 48-byte null-padded name

                    i++;
                }

                SendTCP(pak);
            }
        }
    }
}
