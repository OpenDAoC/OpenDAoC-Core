/*
Gudlaugr.
<author>Kelt</author>
 */
using System;
using System.Collections.Generic;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using System.Reflection;
using System.Collections;
using DOL.AI.Brain;
using DOL.GS.Scripts.DOL.AI.Brain;


namespace DOL.GS.Scripts
{

	public class Gudlaugr : GameNPC
	{
		/// <summary>
		/// Add Gudlaugr to World
		/// </summary>
		public override bool AddToWorld()
		{
			Realm = eRealm.None;
			Model = 650;
			Size = 62;
			Level = 72;
			Strength = 255;
			Dexterity = 120;
			Constitution = 1200;
			Intelligence = 220;
			Health = MaxHealth;
			Piety = 130;
			Empathy = 130;
			Charisma = 130;
			MaxDistance = 4000;
			TetherRange = 3500;
			Faction = FactionMgr.GetFactionByID(779);
			Name = "Gudlaugr";
			BodyType = 1;

			ScalingFactor = 40;
			base.SetOwnBrain(new GudlaugrBrain());
			base.AddToWorld();
			
			return true;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Gudlaugr NPC Initializing...");
		}
	}

	namespace DOL.AI.Brain
	{
		public class GudlaugrBrain : StandardMobBrain
		{
			public GudlaugrBrain() : base()
			{
			}

			public static bool startRage = true;

			public override void Think()
			{
				if (Body.InCombat == true && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
					}
				}
			}
		}
	}
}