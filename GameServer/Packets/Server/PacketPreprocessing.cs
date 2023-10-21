﻿using System;
using System.Collections.Generic;
using log4net;

namespace Core.GS.PacketHandler
{
	/// <summary>
	/// Handles preprocessing for incoming game packets.
	/// </summary>
	/// <remarks>
	/// <para>Preprocessing includes things like checking if a certain precondition exists or if a packet meets a 
	/// certain criteria before we actually handle it.
	/// </para>
	/// <para>
	/// Any time that a packet comes thru with a preprocessor ID of 0, it means there is no preprocessor associated 
	/// with it, and thus we pass it thru. (return true)
	/// </para>
	/// </remarks>
	public class PacketPreprocessing
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		private readonly Dictionary<int, int> _packetIdToPreprocessMap;
		private readonly Dictionary<int, Func<GameClient, GsPacketIn, bool>> _preprocessors;

		public PacketPreprocessing()
		{
			_packetIdToPreprocessMap = new Dictionary<int, int>();
			_preprocessors = new Dictionary<int, Func<GameClient, GsPacketIn, bool>>();

			RegisterPreprocessors((int)EClientStatus.LoggedIn, (client, packet) => client.Account != null);		// player must be logged into an account
			RegisterPreprocessors((int)EClientStatus.PlayerInGame, (client, player) => client.Player != null);	// player must be logged into a character
		}

		/// <summary>
		/// Registers a packet definition with a preprocessor.
		/// </summary>
		/// <param name="packetId">the ID of the packet in question</param>
		/// <param name="preprocessorId">the ID of the preprocessor for the given packet ID</param>
		public void RegisterPacketDefinition(int packetId, int preprocessorId)
		{
			// if they key doesn't exist, add it, and if it does, replace it
			if (!_packetIdToPreprocessMap.ContainsKey(packetId))
			{
				_packetIdToPreprocessMap.Add(packetId, preprocessorId);
			}
			else
			{
				log.InfoFormat("Replacing Packet Processor for packet ID {0} with preprocessorId {1}", packetId, preprocessorId);
				_packetIdToPreprocessMap[packetId] = preprocessorId;
			}
	}

		/// <summary>
		/// Registers a preprocessor.
		/// </summary>
		/// <param name="preprocessorId">the ID for the preprocessor</param>
		/// <param name="preprocessorFunc">the preprocessor delegate to use</param>
		public void RegisterPreprocessors(int preprocessorId, Func<GameClient, GsPacketIn, bool> preprocessorFunc)
		{
			_preprocessors.Add(preprocessorId, preprocessorFunc);
		}

		/// <summary>
		/// Checks if a packet can be processed by the server.
		/// </summary>
		/// <param name="client">the client that sent the packet</param>
		/// <param name="packet">the packet in question</param>
		/// <returns>true if the packet passes all preprocessor checks; false otherwise</returns>
		public bool CanProcessPacket(GameClient client, GsPacketIn packet)
		{
			int preprocessorId;
			if(!_packetIdToPreprocessMap.TryGetValue(packet.ID, out preprocessorId))
				return false;

			if(preprocessorId == 0)
			{
				// no processing, pass thru.
				return true;
			}

			Func<GameClient, GsPacketIn, bool> preprocessor;
			if(!_preprocessors.TryGetValue(preprocessorId, out preprocessor))
				return false;

			return preprocessor(client, packet);
		}
	}
}
