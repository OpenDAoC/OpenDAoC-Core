using System.Reflection;
using DOL.GS.Quests;

namespace DOL.GS.PacketHandler
{
	[PacketLib(183, GameClient.eClientVersion.Version183)]
	public class PacketLib183 : PacketLib182
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.83 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib183(GameClient client):base(client)
		{
		}

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.QuestEntry)))
			{
				pak.WriteByte(index);
				if (quest.Step <= 0)
				{
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
					pak.WriteByte(0);
				}
				else
				{
					string name = quest.Name;
					string desc = quest.Description;
					if (name.Length > byte.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": name is too long for 1.71 clients ("+name.Length+") '"+name+"'");
						name = name.Substring(0, byte.MaxValue);
					}
					if (desc.Length > ushort.MaxValue)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": description is too long for 1.71 clients ("+desc.Length+") '"+desc+"'");
						desc = desc.Substring(0, ushort.MaxValue);
					}
					if (name.Length + desc.Length > 2048-10)
					{
						if (log.IsWarnEnabled) log.Warn(quest.GetType().ToString() + ": name + description length is too long and would have crashed the client.\nName ("+name.Length+"): '"+name+"'\nDesc ("+desc.Length+"): '"+desc+"'");
						name = name.Substring(0, 32);
						desc = desc.Substring(0, 2048-10 - name.Length); // all that's left
					}
					pak.WriteByte((byte)name.Length);
					pak.WriteShortLowEndian((ushort)desc.Length);
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

			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.QuestEntry)))
			{
				pak.WriteByte(0); //index
				pak.WriteShortLowEndian((ushort)name.Length);
				pak.WriteByte((byte)0);
				pak.WriteByte((byte)0);
				pak.WriteStringBytes(name); //Write Quest Name without trailing 0
				pak.WriteStringBytes(""); //Write Quest Description without trailing 0
				SendTCP(pak);
			}
		}
	}
}
