
using DOL.GS.Effects;

namespace DOL.GS.PacketHandler.Client.v168
{
	/// <summary>
	/// Handles effect cancel requests
	/// </summary>
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerCancelsEffect, "Handle Player Effect Cancel Request.", eClientStatus.PlayerInGame)]
	public class PlayerCancelsEffectHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			int effectID = packet.ReadShort();
			if (client.Version <= GameClient.eClientVersion.Version1109)
				new CancelEffectHandler(client.Player, effectID).Start(1);
			else
				new CancelEffectHandler1110(client.Player, effectID).Start(1);
		}

		/// <summary>
		/// Handles players cancel effect actions
		/// </summary>
		protected class CancelEffectHandler : RegionAction
		{
			/// <summary>
			/// The effect Id
			/// </summary>
			protected readonly int m_effectId;

			/// <summary>
			/// Constructs a new CancelEffectHandler
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="effectId">The effect Id</param>
			public CancelEffectHandler(GamePlayer actionSource, int effectId) : base(actionSource)
			{
				m_effectId = effectId;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) m_actionSource;

				IGameEffect found = null;
				lock (player.EffectList)
				{
					foreach (IGameEffect effect in player.EffectList)
					{
						if (effect.InternalID == m_effectId)
						{
							found = effect;
							break;
						}
					}
				}
				if (found != null)
					found.Cancel(true);
				return 0;
			}
		}

		/// <summary>
		/// Handles players cancel effect actions
		/// </summary>
		protected class CancelEffectHandler1110 : RegionAction
		{
			/// <summary>
			/// The effect Id
			/// </summary>
			protected readonly int m_effectId;

			/// <summary>
			/// Constructs a new CancelEffectHandler
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="effectId">The effect Id</param>
			public CancelEffectHandler1110(GamePlayer actionSource, int effectId) : base(actionSource)
			{
				m_effectId = effectId;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) m_actionSource;
				EffectListComponent effectListComponent = player.effectListComponent;
				ECSGameEffect effect = effectListComponent.TryGetEffectFromEffectId(m_effectId);

				if (effect != null)
					EffectService.RequestImmediateCancelEffect(effect, true);

				return 0;
			}
		}
	}
}
