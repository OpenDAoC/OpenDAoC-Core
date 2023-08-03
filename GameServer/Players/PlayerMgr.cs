﻿using System;
using DOL.GS.Friends;

namespace DOL.GS
{
	/// <summary>
	/// GameServer Manager to Handle Player Data and restriction for this GameServer.
	/// </summary>
	public sealed class PlayerMgr
	{
		/// <summary>
		/// Reference to the Instanced GameServer
		/// </summary>
		private GameServer GameServerInstance { get; set; }
		
		/// <summary>
		/// Reference to the Invalid Names Manager
		/// </summary>
		public InvalidNamesMgr InvalidNames { get; private set; }
		
		/// <summary>
		/// Reference to the Friends List Manager
		/// </summary>
		public FriendMgr Friends { get; private set; }

		/// <summary>
		/// Create a new Instance of <see cref="PlayerMgr"/>
		/// </summary>
		public PlayerMgr(GameServer GameServerInstance)
		{
			if (GameServerInstance == null)
				throw new ArgumentNullException("GameServerInstance");
			
			this.GameServerInstance = GameServerInstance;
			
			InvalidNames = new InvalidNamesMgr(this.GameServerInstance.Configuration.InvalidNamesFile);
			Friends = new FriendMgr(GameServerInstance.IDatabase);
		}
	}
}