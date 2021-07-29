using DOL.AI.Brain;
using DOL.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
	/// <summary>
	/// Alluvian is a mob that spawns Alluvian Globules mobs. With a maximum of 12
	/// Remove the existing Alluvian in the water by Caifelle
	/// </summary>
	public class AlluvianMob : GameNPC
	{
		public AlluvianMob() : base()
		{
			SetOwnBrain(new AlluBrain());
		}

		private static int m_globuleCount;

		public static int GlobuleNumber
		{
			get { return m_globuleCount; }
			set { m_globuleCount = value; }
		}

		public int SpawnGlobule()
		{
			AlluvianMob globulespawn = new AlluvianMob();
			globulespawn.Model = 928;
			globulespawn.Size = 40;
			globulespawn.Level = (byte)Util.Random(3, 4);
			globulespawn.Name = "alluvian globule";
			globulespawn.CurrentRegionID = 51;
			globulespawn.Heading = Heading;
			globulespawn.Realm = 0;
			globulespawn.CurrentSpeed = 0;
			globulespawn.MaxSpeedBase = 191;
			globulespawn.GuildName = "";
			globulespawn.X = X;
			globulespawn.Y = Y;
			globulespawn.Z = 3083;
			globulespawn.RespawnInterval = -1;
			globulespawn.BodyType = 4;
			globulespawn.Flags ^= eFlags.FLYING;

			GlobuleBrain brain = new GlobuleBrain();
			brain.AggroLevel = 70;
			brain.AggroRange = 500;
			globulespawn.SetOwnBrain(brain);
			globulespawn.AutoSetStats();
			globulespawn.AddToWorld();
			GlobuleNumber++;
			brain.WalkFromSpawn();
			GameEventMgr.AddHandler(globulespawn, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));
			return 0;
		}

		public static void GlobuleHasDied(DOLEvent e, object sender, EventArgs args)
		{
			GlobuleNumber--;
			GameEventMgr.RemoveHandler(sender, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));
			return;
		}
	}

	/// <summary>
	/// Class to override default respawn behaviour
	/// </summary>
	public class Alluvian : GameNPC
	{
		public Alluvian() : base()
		{
			SetOwnBrain(new AlluBrain());
		}

		private static int m_globuleCount;

		//Holds the current number of globules, be sure to GlobuleNumber--; when a globule dies.
		public static int GlobuleNumber
		{
			get { return m_globuleCount; }
			set { m_globuleCount = value; }
		}

		public int SpawnGlobule()
		{
			AlluvianMob globulespawn = new AlluvianMob();
			globulespawn.Model = 928;
			globulespawn.Size = 40;
			globulespawn.Level = (byte)Util.Random(3, 4);
			globulespawn.Name = "alluvian globule";
			globulespawn.CurrentRegionID = 51;
			globulespawn.Heading = Heading;
			globulespawn.Realm = 0;
			globulespawn.CurrentSpeed = 0;
			globulespawn.MaxSpeedBase = 191;
			globulespawn.GuildName = "";
			globulespawn.X = X;
			globulespawn.Y = Y;
			globulespawn.Z = 3083;
			globulespawn.RespawnInterval = -1;
			globulespawn.BodyType = 4;
			globulespawn.Flags ^= eFlags.FLYING;

			GlobuleBrain brain = new GlobuleBrain();
			brain.AggroLevel = 70;
			brain.AggroRange = 500;
			globulespawn.SetOwnBrain(brain);
			globulespawn.AutoSetStats();
			globulespawn.AddToWorld();
			GlobuleNumber++;
			brain.WalkFromSpawn();
			//Tell me when you die so I can GlobuleNumber--;
			GameEventMgr.AddHandler(globulespawn, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));

			return 0;
		}

		public static void GlobuleHasDied(DOLEvent e, object sender, EventArgs args)
		{
			GlobuleNumber--;
			//Remove the handler so they don't pile up.
			GameEventMgr.RemoveHandler(sender, GameNPCEvent.Dying, new DOLEventHandler(GlobuleHasDied));
			return;
		}
	}
}
