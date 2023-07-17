namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerGroundTarget, "Handles Player Ground Target Settings", eClientStatus.PlayerInGame)]
	public class PlayerGroundTargetHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			var groundX = (int) packet.ReadInt();
			var groundY = (int) packet.ReadInt();
			var groundZ = (int) packet.ReadInt();
			ushort flag = packet.ReadShort();
//			ushort unk2 = packet.ReadShort();

			new ChangeGroundTargetHandler(client.Player, groundX, groundY, groundZ, flag).Start(1);
		}

		/// <summary>
		/// Handles ground target changes
		/// </summary>
		protected class ChangeGroundTargetHandler : RegionAction
		{
			protected readonly ushort m_flag;

			/// <summary>
			/// The new ground X
			/// </summary>
			protected readonly int m_x;

			/// <summary>
			/// The new ground Y
			/// </summary>
			protected readonly int m_y;

			/// <summary>
			/// The new ground Z
			/// </summary>
			protected readonly int m_z;

			/// <summary>
			/// Constructs a new ChangeGroundTargetHandler
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="x">The new ground X</param>
			/// <param name="y">The new ground Y</param>
			/// <param name="z">The new ground Z</param>
			/// <param name="flag"></param>
			public ChangeGroundTargetHandler(GamePlayer actionSource, int x, int y, int z, ushort flag) : base(actionSource)
			{
				m_x = x;
				m_y = y;
				m_z = z;
				m_flag = flag;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				var player = (GamePlayer) m_actionSource;
				player.GroundTargetInView = ((m_flag & 0x100) != 0);
				player.SetGroundTarget(m_x, m_y, (ushort) m_z);

				if (!player.GroundTargetInView)
					player.Out.SendMessage("Your ground target is not visible!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

				// if (player.SiegeWeapon != null && player.SiegeWeapon.Owner == player)
				// {
				// 	player.SiegeWeapon.Move();
				// 	return 0;
				// }
				if (player.Steed != null && player.Steed.MAX_PASSENGERS >= 1 && player.Steed.OwnerID == player.InternalID)
				{
					if (player.Steed is GameTaxiBoat) return 0;
					if (player.Steed is GameBoat)
						// Ichi - && player.GroundTarget.Z > player.CurrentZone.ZoneRegion.WaterLevel) return;
					{
						if (player.Steed.OwnerID == player.InternalID)
						{
							player.Out.SendMessage("You usher your boat forward.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
							player.Steed.WalkTo(player.GroundTarget, player.Steed.MaxSpeed);
							return 0;
						}
					}

					if (player.Steed.MAX_PASSENGERS > 8 && player.Steed.CurrentRiders.Length < player.Steed.REQUIRED_PASSENGERS)
					{
						player.Out.SendMessage("The " + player.Steed.Name + " does not yet have enough passengers to move!",
						                       eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return 0;
					}
					player.Steed.WalkTo(player.GroundTarget, player.Steed.MaxSpeed);
					return 0;
				}

				return 0;
			}
		}
	}
}