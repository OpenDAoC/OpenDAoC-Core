/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerTarget, "Handle Player Target Change.", eClientStatus.PlayerInGame)]
    public class PlayerTargetHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort targetID = packet.ReadShort();
            ushort flags = packet.ReadShort();

            /*
             * 0x8000 = 'examine' bit
             * 0x4000 = LOS1 bit; is 0 if no LOS
             * 0x2000 = LOS2 bit; is 0 if no LOS
             * 0x0001 = players attack mode bit (not targets!)
             */

            ChangeTarget(client.Player, targetID, (flags & (0x4000 | 0x2000)) != 0, (flags & 0x8000) != 0);
        }

        private static void ChangeTarget(GamePlayer actionSource, ushort newTargetId, bool targetInView, bool examineTarget)
        {
            GameObject target = actionSource.CurrentRegion.GetObject(newTargetId);

            if (target != null && !actionSource.IsWithinRadius(target, WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                actionSource.Out.SendObjectDelete(newTargetId);
                target = null;
            }

            actionSource.TargetObject = target;
            actionSource.TargetInView = targetInView;

            if (target != null)
            {
                // Send target message text only if 'examine' bit is set.
                if (examineTarget)
                {
                    foreach (string message in target.GetExamineMessages(actionSource))
                        actionSource.Out.SendMessage(message, eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                }

                // No LOS message. Not sure which bit to use so use both.
                if (!targetInView)
                    actionSource.Out.SendMessage("Target is not in view.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (target is not GamePlayer)
                    ClientService.UpdateObjectForPlayer(actionSource, target);
            }

            if (actionSource.IsPraying)
            {
                if (target is not GameGravestone gravestone || !gravestone.InternalID.Equals(actionSource.InternalID))
                {
                    actionSource.Out.SendMessage("You are no longer targetting your grave. Your prayers fail.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    actionSource.PrayTimerStop();
                }
            }
        }
    }
}
