using System.Reflection;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.GameUtils;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.WithDrawMerchantMoney, "Withdraw GameConsignmentMerchant Merchant Money", EClientStatus.PlayerInGame)]
    public class PlayerWithdrawMerchantMoney : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
			// player is null, return
            if (client.Player == null)
                return;

			// active consignment merchant is null, return
            GameConsignmentMerchant conMerchant = client.Player.ActiveInventoryObject as GameConsignmentMerchant;
            if (conMerchant == null)
                return;

			// current house is null, return
            House house = HouseMgr.GetHouse(conMerchant.HouseNumber);
            if (house == null)
                return;

			// make sure player has permissions to withdraw from the consignment merchant
            if (!house.CanUseConsignmentMerchant(client.Player, EConsignmentPermissions.Withdraw))
            {
                client.Player.Out.SendMessage("You don't have permission to withdraw money from this merchant!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
                return;
            }

			lock (conMerchant.LockObject())
			{
				long totalConMoney = conMerchant.TotalMoney;

				if (totalConMoney > 0)
				{
					if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
					{
						client.Player.Out.SendMessage("You withdraw " + totalConMoney.ToString() + " BountyPoints from your Merchant.", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
						client.Player.BountyPoints += totalConMoney;
						client.Player.Out.SendUpdatePoints();
					}
					else
					{
						ChatUtil.SendMerchantMessage(client, "GameMerchant.OnPlayerWithdraw", MoneyMgr.GetString(totalConMoney));
						client.Player.AddMoney(totalConMoney);
						InventoryLogging.LogInventoryAction(conMerchant, client.Player, EInventoryActionType.Merchant, totalConMoney);
					}

					conMerchant.TotalMoney -= totalConMoney;

					if (ServerProperties.Properties.MARKET_ENABLE_LOG)
					{
						log.DebugFormat("CM: [{0}:{1}] withdraws {2} from CM on lot {3}.", client.Player.Name, client.Account.Name, totalConMoney, conMerchant.HouseNumber);
					}

					client.Out.SendConsignmentMerchantMoney(conMerchant.TotalMoney);
				}
			}
        }
    }
}