using System;
using System.Reflection;
using DOL.GS.Quests;

namespace DOL.GS.PacketHandler
{
	[PacketLib(184, GameClient.eClientVersion.Version184)]
	public class PacketLib184 : PacketLib183
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.83 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib184(GameClient client):base(client)
		{
		}

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.QuestEntry)))
			{
				pak.WriteByte(index);
				if (quest == null)
				{
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
				}
				else
				{
					ReadOnlySpan<char> nameSpan = $"{quest.Name} (Level {quest.Level})";
					ReadOnlySpan<char> descSpan = $"[Step #{quest.Step}]: {quest.Description}";

					if (nameSpan.Length > byte.MaxValue)
						nameSpan = nameSpan[..byte.MaxValue];

					if (descSpan.Length > byte.MaxValue)
						descSpan = descSpan[..byte.MaxValue];
					
					pak.WriteByte((byte) nameSpan.Length);
					pak.WriteShortLowEndian((ushort) descSpan.Length);
					pak.WriteByte(0); // Quest Zone ID ?
					pak.WriteByte(0);
					pak.WriteNonNullTerminatedString(nameSpan); //Write Quest Name without trailing 0
					pak.WriteNonNullTerminatedString(descSpan); //Write Quest Description without trailing 0
				}

				SendTCP(pak);
			}
		}

		protected override void SendTaskInfo()
		{
			string name = BuildTaskString();

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.QuestEntry)))
			{
				pak.WriteByte(0); //index
				pak.WriteShortLowEndian((ushort)name.Length);
				pak.WriteByte((byte)0);
				pak.WriteByte((byte)0);
				pak.WriteByte((byte)0);
				pak.WriteNonNullTerminatedString(name); //Write Quest Name without trailing 0
				pak.WriteNonNullTerminatedString(""); //Write Quest Description without trailing 0
				SendTCP(pak);
			}
		}
	}
}
