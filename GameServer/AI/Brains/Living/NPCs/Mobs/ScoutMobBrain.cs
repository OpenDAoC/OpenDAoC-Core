using System;
using System.Collections;
using System.Reflection;
using DOL.Events;
using DOL.GS;
using DOL.GS.Effects;
using log4net;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Brain for scout mobs. Scout mobs are NPCs that will not aggro
	/// on a player of their own accord, instead, they'll go searching
	/// for adds around the area and make those aggro on a player.
	/// </summary>
	class ScoutMobBrain : StandardMobBrain
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Mob brain main loop.
		/// </summary>
		public override void Think()
		{
			if (IsGettingHelp && Body.IsWithinRadius(m_helperNPC, 200))
			{
				// We arrived at our target mob, let's have a look around
				// and see if we can get multiple adds.
				foreach (GameNpc npc in Body.GetNPCsInRadius(600))
				{
					if (npc.IsFriend(Body) && npc.IsAggressive && npc.IsAvailable)
						ReportTargets(npc);
				}

				// Once that's done, aggro on targets ourselves and run back.
				ReportTargets(Body);
				m_targetList.Clear();
				m_helperNPC = null;
				IsGettingHelp = false;
				AttackMostWanted();
			}
			else
				base.Think();
		}

		private bool m_scouting = true;

		/// <summary>
		/// Whether this mob is scouting or not; if a mob is scouting it
		/// means the mob is still looking for players.
		/// </summary>
		public virtual bool IsScouting
		{
			get { return m_scouting; }
			set { m_scouting = value; }
		}

		private ArrayList m_targetList = new ArrayList();

		/// <summary>
		/// Check if there are any players around.
		/// </summary>
		protected override void CheckPlayerAggro()
		{
			// If mob is not scouting anymore, it is either still on its way to
			// get help or it has finished doing that, in which case it will
			// behave like an ordinary mob and aggro players.

			if (!IsScouting)
			{
				if (!IsGettingHelp)
				{
					base.CheckPlayerAggro();
					m_targetList.Clear();
					foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
						if (!m_targetList.Contains(player))
						IsScouting=true;//if there is no player in AggroRange Start Scouting
				}	
				return;
			}

			// Add all players in range to this scout's target list. The scout
			// will report all these players to any potential adds.

			m_targetList.Clear();

			foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!CanAggroTarget(player))
					continue;

				if (player.IsStealthed || player.Steed != null)
					continue;

				if (player.EffectList.GetOfType<NecromancerShadeEffect>() != null)
					continue;

				m_targetList.Add(player);
			}

			// Once we got at least one player we stop scouting and run for help.

			if (m_targetList.Count > 0)
			{
				IsScouting = false;
				GetHelp();
			}
		}

		private ushort m_scoutRange = 3000;

		/// <summary>
		/// The range the scout will look for adds in.
		/// </summary>
		public ushort ScoutRange
		{
			get { return m_scoutRange; }
			set { m_scoutRange = value; }
		}

		private bool m_gettingHelp = false;

		/// <summary>
		/// Whether or not this mob is on its way to get help.
		/// </summary>
		public bool IsGettingHelp
		{
			get { return m_gettingHelp; }
			set { m_gettingHelp = value; }
		}

		/// <summary>
		/// The NPC this scout has picked to help.
		/// </summary>
		private GameNpc m_helperNPC = null;

		/// <summary>
		/// Look for potential adds in the area and be on your way.
		/// </summary>
		/// <returns></returns>
		protected void GetHelp()
		{
			// Nothing to get help for.

			if (m_targetList.Count == 0) return;

			// Find all mobs in scout range.

			ArrayList addList = new ArrayList();
			foreach (GameNpc npc in Body.GetNPCsInRadius(ScoutRange))
			{
				if (npc.IsFriend(Body) && npc.IsAggressive && npc.IsAvailable)
					addList.Add(npc);
			}

			// If there is no help available, fall back on standard mob
			// behaviour.

			if (addList.Count == 0)
			{
				ReportTargets(Body);
				m_targetList.Clear();
				IsGettingHelp = false;
				return;
			}

			// Pick a random NPC from the list and go for it.

			IsGettingHelp = true;
			m_helperNPC = (GameNpc) addList[Util.Random(1, addList.Count)-1];
			Body.Follow(m_helperNPC, 90, int.MaxValue);
		}

		/// <summary>
		/// Add targets to an NPC's aggro table.
		/// </summary>
		/// <param name="npc">The NPC to aggro on the targets.</param>
		private void ReportTargets(GameNpc npc)
		{
			if (npc == null) return;

			// Assign a random amount of aggro for each target, that way 
			// different NPCs will attack different targets first.

			StandardMobBrain brain = npc.Brain as StandardMobBrain;
			foreach (GameLiving target in m_targetList)
				brain.AddToAggroList(target, Util.Random(1, m_targetList.Count));
		}

		/// <summary>
		/// Called whenever the NPC's body sends something to its brain.
		/// </summary>
		/// <param name="e">The event that occured.</param>
		/// <param name="sender">The source of the event.</param>
		/// <param name="args">The event details.</param>
		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			base.Notify(e, sender, args);

			if (e == GameObjectEvent.TakeDamage)
			{
				// If we are attacked at any point we'll stop scouting or
				// running for help.

				IsScouting = false;
				IsGettingHelp = false;
				m_targetList.Clear();
			}
		}
	}
}
