using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.GS.Effects;

namespace DOL.GS.PacketHandler
{
	[PacketLib(191, GameClient.eClientVersion.Version191)]
	public class PacketLib191 : PacketLib190
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		protected override void WriteGroupMemberUpdate(GSTCPPacketOut pak, bool updateIcons, GameLiving living)
		{
			pak.WriteByte((byte)(living.GroupIndex + 1)); // From 1 to 8
			bool sameRegion = living.CurrentRegion == m_gameClient.Player.CurrentRegion;
            GamePlayer player;

			if (sameRegion)
			{
                player = living as GamePlayer;

                if (player != null)
                    pak.WriteByte(player.CharacterClass.HealthPercentGroupWindow);
                else
                    pak.WriteByte(living.HealthPercent);

				pak.WriteByte(living.ManaPercent);
				pak.WriteByte(living.EndurancePercent); // new in 1.69

				byte playerStatus = 0;
				if (!living.IsAlive)
					playerStatus |= 0x01;
				if (living.IsMezzed)
					playerStatus |= 0x02;
				if (living.IsDiseased)
					playerStatus |= 0x04;
				if (living.IsPoisoned)
					playerStatus |= 0x08;
				if (player?.Client.ClientState == GameClient.eClientState.Linkdead)
					playerStatus |= 0x10;
				if (!sameRegion)
					playerStatus |= 0x20;
				if (living.DebuffCategory[eProperty.SpellRange] != 0 || living.DebuffCategory[eProperty.ArcheryRange] != 0)
					playerStatus |= 0x40;

				pak.WriteByte(playerStatus);
				// 0x00 = Normal , 0x01 = Dead , 0x02 = Mezzed , 0x04 = Diseased ,
				// 0x08 = Poisoned , 0x10 = Link Dead , 0x20 = In Another Region, 0x40 - NS

				if (updateIcons)
				{
					pak.WriteByte((byte)(0x80 | living.GroupIndex));
					lock (living.EffectList)
					{
						byte i = 0;
						foreach (IGameEffect effect in living.EffectList)
							if (effect is GameSpellEffect)
								i++;
						pak.WriteByte(i);
						foreach (IGameEffect effect in living.EffectList)
							if (effect is GameSpellEffect)
							{
								pak.WriteByte(0);
								pak.WriteShort(effect.Icon);
							}
					}
				}
				WriteGroupMemberMapUpdate(pak, living);
			}
			else
			{
				pak.WriteInt(0x20);
				if (updateIcons)
				{
					pak.WriteByte((byte)(0x80 | living.GroupIndex));
					pak.WriteByte(0);
				}
			}
		}

		public override void SendConcentrationList()
		{
			if (m_gameClient.Player == null)
				return;

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.ConcentrationList))))
			{
				List<ECSGameSpellEffect> concentrationEffects = m_gameClient.Player.effectListComponent.GetConcentrationEffects();
				pak.WriteByte((byte) concentrationEffects.Count);
				pak.WriteByte(0); // unknown
				pak.WriteByte(0); // unknown
				pak.WriteByte(0); // unknown

				for (int i = 0; i < concentrationEffects.Count; i++)
				{
					IConcentrationEffect effect = concentrationEffects[i];
					pak.WriteByte((byte) i);
					pak.WriteByte(0); // unknown
					pak.WriteByte(effect.Concentration);
					pak.WriteShort(effect.Icon);

					if (effect.Name.Length > 14)
						pak.WritePascalString(string.Concat(effect.Name.AsSpan(0, 12), ".."));
					else
						pak.WritePascalString(effect.Name);
					if (effect.OwnerName.Length > 14)
						pak.WritePascalString(string.Concat(effect.OwnerName.AsSpan(0, 12), ".."));
					else
						pak.WritePascalString(effect.OwnerName);
				}

				SendTCP(pak);
			}

			SendStatusUpdate();
		}

		/// <summary>
		/// Constructs a new PacketLib for Version 1.91 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib191(GameClient client)
			: base(client)
		{
		}
	}
}
