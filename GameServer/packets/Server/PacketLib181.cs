using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.GS.PlayerTitles;

namespace DOL.GS.PacketHandler
{
	[PacketLib(181, GameClient.eClientVersion.Version181)]
	public class PacketLib181 : PacketLib180
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.81 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib181(GameClient client):base(client)
		{
		}

		public override void SendNonHybridSpellLines()
		{
			base.SendNonHybridSpellLines();

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VariousUpdate))))
			{
				pak.WriteByte(0x02); //subcode
				pak.WriteByte(0x00);
				pak.WriteByte(99); //subtype (new subtype 99 in 1.80e)
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}

		public override void SendCustomTextWindow(string caption, IList<string> text)
		{
			if (text == null)
				return;

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.DetailWindow))))
			{
				pak.WriteByte(0); // new in 1.75
				pak.WriteByte(0); // new in 1.81
				if (caption == null)
					caption = string.Empty;
				if (caption.Length > byte.MaxValue)
					caption = caption.Substring(0, byte.MaxValue);
				pak.WritePascalString(caption); //window caption

				WriteCustomTextWindowData(pak, text);

				//Trailing Zero!
				pak.WriteByte(0);
				SendTCP(pak);
			}
		}

		public override void SendPlayerTitles()
		{
			var titles = m_gameClient.Player.Titles;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.DetailWindow))))
			{
				pak.WriteByte(1); // new in 1.75
				pak.WriteByte(0); // new in 1.81
				pak.WritePascalString("Player Statistics"); //window caption

				byte line = 1;
				foreach (string str in m_gameClient.Player.FormatStatistics())
				{
					pak.WriteByte(line++);
					pak.WritePascalString(str);
				}

				pak.WriteByte(200);
				long titlesCountPos = pak.Position;
				pak.WriteByte(0); // length of all titles part
				pak.WriteByte((byte)titles.Count);
				line = 0;
				foreach (IPlayerTitle title in titles)
				{
					pak.WriteByte(line++);
					pak.WritePascalString(title.GetDescription(m_gameClient.Player));
				}
				long titlesLen = (pak.Position - titlesCountPos - 1); // include titles count
				if (titlesLen > byte.MaxValue)
					log.WarnFormat("Titles block is too long! {0} (player: {1})", titlesLen, m_gameClient.Player);
				//Trailing Zero!
				pak.WriteByte(0);
				//Set titles length
				pak.Position = titlesCountPos;
				pak.WriteByte((byte)titlesLen); // length of all titles part
				SendTCP(pak);
			}
		}

		public override void SendPetWindow(GameLiving pet, ePetWindowAction windowAction, eAggressionState aggroState, eWalkState walkState)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.PetWindow))))
			{
				pak.WriteShort((ushort)(pet == null ? 0 : pet.ObjectID));
				pak.WriteByte(0x00); //unused
				pak.WriteByte(0x00); //unused
				switch (windowAction) //0-released, 1-normal, 2-just charmed? | Roach: 0-close window, 1-update window, 2-create window
				{
					case ePetWindowAction.Open:
						pak.WriteByte(2);
						break;
					case ePetWindowAction.Update:
						pak.WriteByte(1);
						break;
					default: pak.WriteByte(0);
						break;
				}
				switch (aggroState) //1-aggressive, 2-defensive, 3-passive
				{
					case eAggressionState.Aggressive:
						pak.WriteByte(1);
						break;
					case eAggressionState.Defensive:
						pak.WriteByte(2);
						break;
					case eAggressionState.Passive:
						pak.WriteByte(3);
						break;
					default: pak.WriteByte(0); break;
				}
				switch (walkState) //1-follow, 2-stay, 3-goto, 4-here
				{
					case eWalkState.Follow:
						pak.WriteByte(1);
						break;
					case eWalkState.Stay:
						pak.WriteByte(2);
						break;
					case eWalkState.GoTarget:
						pak.WriteByte(3);
						break;
					case eWalkState.ComeHere:
						pak.WriteByte(4);
						break;
					default: pak.WriteByte(0);
						break;
				}
				pak.WriteByte(0x00); //unused

				if (pet != null)
				{
					ArrayList icons = new ArrayList();
					foreach (var effect in pet.effectListComponent.GetEffects())
					{
						if (icons.Count >= 8)
							break;
						if (effect.Icon == 0)
							continue;
						icons.Add(effect.Icon);
					}
					pak.WriteByte((byte)icons.Count); // effect count
					// 0x08 - null terminated - (byte) list of shorts - spell icons on pet
					foreach (ushort icon in icons)
					{
						pak.WriteShort(icon);
					}
				}
				else
					pak.WriteByte((byte)0); // effect count

				SendTCP(pak);
			}
		}
	}
}
