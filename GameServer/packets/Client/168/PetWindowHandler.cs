using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PetWindow, "Handle Pet Window Command", eClientStatus.PlayerInGame)]
	public class PetWindowHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			var aggroState = (byte) packet.ReadByte(); // 1-Aggressive, 2-Deffensive, 3-Passive
			var walkState = (byte) packet.ReadByte(); // 1-Follow, 2-Stay, 3-GoTarg, 4-Here
			var command = (byte) packet.ReadByte(); // 1-Attack, 2-Release
			new HandlePetCommandAction(client.Player, aggroState, walkState, command).Start(1);
		}

		/// <summary>
		/// Handles pet command actions
		/// </summary>
		protected class HandlePetCommandAction : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The pet aggro state
			/// </summary>
			protected readonly int _aggroState;

			/// <summary>
			/// The pet command
			/// </summary>
			protected readonly int _command;

			/// <summary>
			/// The pet walk state
			/// </summary>
			protected readonly int _walkState;

			/// <summary>
			/// Constructs a new HandlePetCommandAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="aggroState">The pet aggro state</param>
			/// <param name="walkState">The pet walk state</param>
			/// <param name="command">The pet command</param>
			public HandlePetCommandAction(GamePlayer actionSource, int aggroState, int walkState, int command)
				: base(actionSource)
			{
				_aggroState = aggroState;
				_walkState = walkState;
				_command = command;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;

				switch (_aggroState)
				{
					case 0:
						break; // ignore
					case 1:
						player.CommandNpcAgressive();
						break;
					case 2:
						player.CommandNpcDefensive();
						break;
					case 3:
						player.CommandNpcPassive();
						break;
					default:
						Log.Warn($"unknown aggro state {_aggroState}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
						break;
				}
				switch (_walkState)
				{
					case 0:
						break; // ignore
					case 1:
						player.CommandNpcFollow();
						break;
					case 2:
						player.CommandNpcStay();
						break;
					case 3:
						player.CommandNpcGoTarget();
						break;
					case 4:
						player.CommandNpcComeHere();
						break;
					default:
						Log.Warn($"unknown walk state {_walkState}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
						break;
				}
				switch (_command)
				{
					case 0:
						break; // ignore
					case 1:
						player.CommandNpcAttack();
						break;
					case 2:
						player.CommandNpcRelease();
						break;
					default:
						Log.Warn($"unknown command state {_command}, player={player.Name}  version={player.Client.Version}  client type={player.Client.ClientType}");
						break;
				}

				return 0;
			}
		}
	}
}
