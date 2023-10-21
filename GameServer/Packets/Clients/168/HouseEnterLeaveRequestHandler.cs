using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.GameUtils;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseEnterLeave, "Housing Enter Leave Request.", EClientStatus.PlayerInGame)]
public class HouseEnterLeaveHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		int pid = packet.ReadShort();
		int housenumber = packet.ReadShort();
		int enter = packet.ReadByte();

		// house is null, return
		House house = HouseMgr.GetHouse(housenumber);
		if (house == null)
			return;

		new EnterLeaveHouseAction(client.Player, house, enter).Start(1);
	}

	/// <summary>
	/// Handles house enter/leave events
	/// </summary>
	private class EnterLeaveHouseAction : EcsGameTimerWrapperBase
	{
		/// <summary>
		/// The enter house flag
		/// </summary>
		private readonly int _enter;

		/// <summary>
		/// The target house
		/// </summary>
		private readonly House _house;

		/// <summary>
		/// Constructs a new EnterLeaveHouseAction
		/// </summary>
		/// <param name="actionSource">The actions source</param>
		/// <param name="house">The target house</param>
		/// <param name="enter">The enter house flag</param>
		public EnterLeaveHouseAction(GamePlayer actionSource, House house, int enter) : base(actionSource)
		{
			_house = house;
			_enter = enter;
		}

		/// <summary>
		/// Called on every timer tick
		/// </summary>
		protected override int OnTick(EcsGameTimer timer)
		{
			GamePlayer player = (GamePlayer) timer.Owner;

			switch (_enter)
			{
				case 0:
					player.LeaveHouse();
					break;

				case 1:
					if (!player.IsWithinRadius(_house, 1000) || (player.CurrentRegionID != _house.RegionID))
					{
						ChatUtil.SendSystemMessage(player, string.Format("You are too far away to enter house {0}.", _house.HouseNumber));
						return 0;
					}

					// make sure player can enter
					if (_house.CanEnterHome(player))
					{
						player.CurrentHouse = _house;

						_house.Enter(player);
					}
					else
					{
						ChatUtil.SendSystemMessage(player, string.Format("You can't enter house {0}.", _house.HouseNumber));
						return 0;
					}

					break;
			}

			return 0;
		}
	}
}