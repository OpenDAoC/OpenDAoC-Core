using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;

namespace Core.GS
{
	public class Alluvian : GameNpc
	{
		public Alluvian() : base()
		{
			SetOwnBrain(new AlluvianBrain());
		}

		/// <summary>
		/// Keep track of globules
		/// </summary>
		private static int m_globuleCount;

		/// <summary>
		/// Keep track of globules, decrease on death, increase on spawn method
		/// </summary>
		public static int GlobuleNumber
		{
			get { return m_globuleCount; }
			set { m_globuleCount = value; }
		}


		/// <summary>
		/// Spawns globules into the world, total of 12. this can be tweaked.
		/// </summary>
		/// <returns></returns>
		public int SpawnGlobule()
		{
			AlluvianGlobule globulespawn = new AlluvianGlobule();
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
			globulespawn.Flags ^= ENpcFlags.FLYING;
			AlluvianGlobuleBrain brain = new AlluvianGlobuleBrain();
			brain.AggroLevel = 70;
			brain.AggroRange = 500;
			globulespawn.SetOwnBrain(brain);
			globulespawn.AutoSetStats();
			globulespawn.AddToWorld();
			GlobuleNumber++;
			brain.WalkFromSpawn();
			GameEventMgr.AddHandler(globulespawn, GameLivingEvent.Dying, new CoreEventHandler(GlobuleHasDied));
			return 0;
		}

		/// <summary>
		/// Notify that a globule has died so we can reduce the count.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void GlobuleHasDied(CoreEvent e, object sender, EventArgs args)
		{
			GlobuleNumber--;
			GameEventMgr.RemoveHandler(sender, GameLivingEvent.Dying, new CoreEventHandler(GlobuleHasDied));
			return;
		}
	}
}
