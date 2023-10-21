using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.GameUtils;
using Core.GS.Movement;

namespace Core.GS.Keeps
{
	public class KeepGuardPatrol
	{
		public const int PATROL_SPEED = 250;

		public KeepGuardPatrol(GameKeepComponent component)
		{
			m_component = component;
		}

		private DbKeepPosition m_spawnPosition = null;
		/// <summary>
		/// The Position object the guards are assigned to
		/// </summary>
		public DbKeepPosition SpawnPosition
		{
			get { return m_spawnPosition; }
			set { m_spawnPosition = value; }
		}

		private GameKeepComponent m_component = null;
		/// <summary>
		/// The Component object the guards are assigned to
		/// </summary>
		public GameKeepComponent Component
		{
			get { return m_component; }
			set { m_component = value; }
		}

		/// <summary>
		/// What type of keep should this patrol spawn at?
		/// </summary>
		AGameKeep.eKeepType m_keepType = AGameKeep.eKeepType.Any;
		public AGameKeep.eKeepType KeepType
		{
			get { return m_keepType; }
			set { m_keepType = value; }
		}


		/// <summary>
		/// The Patrol ID, consider this a template ID
		/// </summary>
		public string PatrolID = IdGenerator.GenerateID();
		/// <summary>
		/// The Guard Types that make up the Patrol
		/// </summary>
		public static Type[] GuardTypes = new Type[] { typeof(GuardFighter), typeof(GuardArcher), typeof(GuardHealer), typeof(GuardFighter), typeof(GuardArcher), typeof(GuardFighter) };

		/// <summary>
		/// A list of all the Guards
		/// </summary>
		public List<GameKeepGuard> PatrolGuards = new List<GameKeepGuard>();
		/// <summary>
		/// The Patrol Path
		/// </summary>
		public PathPoint PatrolPath = null;

		/// <summary>
		/// Method to Initialise the Guards
		/// 
		/// Here we create the instances of the guard
		/// We add to the local array of guards
		/// We assign a Guard ID which is the Patrol ID
		/// We assign the guards component
		/// </summary>
		public void InitialiseGuards()
		{
			Component.Keep.Patrols.Add(PatrolID, this);

			//need this here becuase it's checked in add to world
			PatrolPath = GuardPositionMgr.LoadPatrolPath(PatrolID, Component);

			int guardsOnPatrol = 1;

			if (Component != null && Component.Keep != null && Component.Keep is GameKeep)
			{
				guardsOnPatrol++;

				if (Component.Keep.Level > 4)
					guardsOnPatrol++;
			}

			if (PatrolGuards.Count < guardsOnPatrol)
			{
				for (int i = 0; i < guardsOnPatrol; i++)
				{
					CreatePatrolGuard(i);
				}
			}

			// tolakram - this might be redundant
			foreach (GameKeepGuard guard in PatrolGuards)
			{
				GuardPositionMgr.LoadGuardPosition(SpawnPosition, guard);
			}

			ChangePatrolLevel();
		}

		private void CreatePatrolGuard(int type)
		{
			Assembly asm = Assembly.GetAssembly(typeof(GameServer));

			if (type < 0) type = 0;
			if (type > GuardTypes.Length - 1) type = GuardTypes.Length - 1;

			GameKeepGuard guard = (GameKeepGuard)asm.CreateInstance(GuardTypes[type].FullName, true);
			guard.TemplateID = PatrolID;
			guard.Component = Component;
			guard.PatrolGroup = this;
			GuardPositionMgr.LoadGuardPosition(SpawnPosition, guard);
			guard.RefreshTemplate();
			PatrolGuards.Add(guard);
			Component.Keep.Guards.Add(IdGenerator.GenerateID(), guard);
			guard.AddToWorld();

			if (ServerProperties.Properties.ENABLE_DEBUG)
			{
				guard.Name += " PatrolID " + PatrolID;
			}
		}

		public void DeletePatrol()
		{
			if (Component != null && Component.Keep != null)
				Component.Keep.Patrols.Remove(PatrolID); // .Remove(this) - InitialiseGuards() adds patrols using PatrolID as the key

			foreach (GameKeepGuard guard in PatrolGuards)
				guard.DeleteObject();

			PatrolGuards.Clear();
			Component = null;
			SpawnPosition = null;
		}

		/// <summary>
		/// Method to Change a Patrol's Level
		/// 
		/// This method handles the add and removing of guards
		/// </summary>
		public void ChangePatrolLevel()
		{
			int guardsToPatrol = 1;

			if (Component != null && Component.Keep != null && Component.Keep is GameKeep)
			{
				guardsToPatrol++;

				if (Component.Keep.Level > 4)
					guardsToPatrol++;
			}

			PatrolPath = GuardPositionMgr.LoadPatrolPath(PatrolID, Component);

			// Console.WriteLine(PatrolID + " guardstopatrol = " + guardsToPatrol + ", count = " + PatrolGuards.Count);

			while (guardsToPatrol > PatrolGuards.Count)
			{
				CreatePatrolGuard(PatrolGuards.Count);
			}

			int x = 0;
			int y = 0;

			List<GameKeepGuard> guardsToKeep = new List<GameKeepGuard>();

			for (int i = 0; i < PatrolGuards.Count; i++)
			{
				GameKeepGuard guard = PatrolGuards[i];

				// Console.WriteLine(PatrolID + " loading guard " + guard.Name);

				if (i < guardsToPatrol)
				{
					// we need to reposition the patrol at their spawn point plus variation
					if (x == 0)
					{
						x = guard.SpawnPoint.X;
						y = guard.SpawnPoint.Y;
					}
					else
					{
						x += Util.Random(250, 350);
						y += Util.Random(250, 350);
					}

					if (guard.IsAlive)
					{
						guard.StopMovingOnPath();
						guard.MoveTo(guard.CurrentRegionID, x, y, guard.SpawnPoint.Z, guard.SpawnHeading);
					}

					guardsToKeep.Add(guard);
				}
				else
				{
					guard.Delete();
				}
			}

			PatrolGuards = guardsToKeep;

			StartPatrol();
		}

		/// <summary>
		/// Method to start a Patrol patroling
		/// It sets a patrol leader
		/// And starts moving on Patrol
		/// </summary>
		public void StartPatrol()
		{
			if (PatrolPath == null)
				PatrolPath = GuardPositionMgr.LoadPatrolPath(PatrolID, Component);

			foreach (GameKeepGuard guard in PatrolGuards)
			{
				if (guard.CurrentWaypoint == null)
					guard.CurrentWaypoint = PatrolPath;

				guard.MoveOnPath(PATROL_SPEED);
			}
		}
		

		public void GetMovementOffset(GameKeepGuard guard, out int x, out int y)
		{
			int modifier = 50;
			x = 0; y = 0;
			int index = PatrolGuards.IndexOf(guard);
			switch (index)
			{
				case 0: x = -modifier; y = modifier; break;
				case 1: x = 0; y = modifier; break;
				case 2: x = -modifier; y = 0; break;
				case 3: x = 0; y = 0; break;
				case 4: x = -modifier; y = -modifier; break;
				case 5: x = 0; y = -modifier; break;
			}
		}
	}
}
