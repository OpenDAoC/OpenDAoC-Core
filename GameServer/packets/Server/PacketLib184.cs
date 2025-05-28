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
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.QuestEntry))))
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
					string name = string.Format("{0} (Level {1})", quest.Name, quest.Level);
					string desc = string.Format("[Step #{0}]: {1}", quest.Step,	quest.Description);
					if (name.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": name is too long for 1.68+ clients (" + name.Length + ") '" + name + "'");
						name = name.Substring(0, byte.MaxValue);
					}
					if (desc.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": description is too long for 1.68+ clients (" + desc.Length + ") '" + desc + "'");
						desc = desc.Substring(0, byte.MaxValue);
					}
					pak.WriteByte((byte)name.Length);
					pak.WriteShortLowEndian((ushort)desc.Length);
					pak.WriteByte(0); // Quest Zone ID ?
					pak.WriteByte(0);
					pak.WriteStringBytes(name); //Write Quest Name without trailing 0
					pak.WriteStringBytes(desc); //Write Quest Description without trailing 0
				}

				SendTCP(pak);
			}
		}

		protected override void SendTaskInfo()
		{
			string name = BuildTaskString();

			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.QuestEntry))))
			{
				pak.WriteByte(0); //index
				pak.WriteShortLowEndian((ushort)name.Length);
				pak.WriteByte((byte)0);
				pak.WriteByte((byte)0);
				pak.WriteByte((byte)0);
				pak.WriteStringBytes(name); //Write Quest Name without trailing 0
				pak.WriteStringBytes(""); //Write Quest Description without trailing 0
				SendTCP(pak);
			}
		}
	}
}
