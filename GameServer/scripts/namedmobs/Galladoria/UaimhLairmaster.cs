/*
 * author: Kelt
 * Name: Uaimh Lairmaster
 * Server: Atlas Freeshard
 */
using System;
using System.Collections;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;

namespace DOL.GS.Scripts
{
    public class UaimhLairmaster : GameNPC
    {
		public UaimhLairmaster() : base() { }

		public override bool AddToWorld()
		{
			Model = 844;
			Name = "Uaimh Lairmaster";
			Size = 60;
			Level = 81;
			Gender = eGender.Neutral;

			BodyType = 6; // Humanoid
			RoamingRange = 0;
			base.AddToWorld();
			base.SetOwnBrain(new UaimhLairmasterBrain());
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class UaimhLairmasterBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public UaimhLairmasterBrain() : base() { }

		public static bool IsFleeing = true;
		public static bool IsAggroEnemies = true;

		public override void Think()
		{
			if (Body.InCombat == true && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					
				}
			}
			base.Think();
		}
	}
}