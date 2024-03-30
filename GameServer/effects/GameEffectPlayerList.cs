using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DOL.GS.Effects
{
	/// <summary>
	/// Sends updates only for changed effects
	/// when iterating over this effect list lock the list!
	/// </summary>
	public class GameEffectPlayerList : GameEffectList
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Lock object for Change Update
		/// </summary>
		private readonly object m_changedLock = new object();		
		/// <summary>
		/// Holds the list of changed effects
		/// </summary>
		protected readonly HashSet<IGameEffect> m_changedEffects = new HashSet<IGameEffect>();
		/// <summary>
		/// The count of effects on last update
		/// </summary>
		protected int m_lastUpdateEffectsCount;

		/// <summary>
		/// Constructs a new GameEffectPlayerList
		/// </summary>
		/// <param name="owner">The owner of effect list</param>
		public GameEffectPlayerList(GamePlayer owner) : base(owner)
		{
		}
		
		/// <summary>
		/// Add Effect to Player and Update Player Group if Any
		/// </summary>
		/// <param name="effect"></param>
		/// <returns></returns>
		public override bool Add(IGameEffect effect)
		{
			if(base.Add(effect))
			{
				var player = m_owner as GamePlayer;
				if (player != null)
				{
					if (player.Group != null)
					{
						player.Group.UpdateMember(player, true, false);
					}
				}
				
				return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Remove Effect from Player and Update Player Group if Any
		/// </summary>
		/// <param name="effect"></param>
		/// <returns></returns>
		public override bool Remove(IGameEffect effect)
		{
			if(base.Remove(effect))
			{
				var player = m_owner as GamePlayer;
				if (player != null)
				{
					if (player.Group != null)
					{
						player.Group.UpdateMember(player, true, false);
					}
				}
				
				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Called when an effect changed
		/// </summary>
		public override void OnEffectsChanged(IGameEffect changedEffect)
		{
			if (changedEffect.Icon == 0)
				return;
			
			lock (m_changedLock)
			{
				if (!m_changedEffects.Contains(changedEffect))
					m_changedEffects.Add(changedEffect);
			}
			
			base.OnEffectsChanged(changedEffect);
		}


		/// <summary>
		/// Updates changed effects to the owner.
		/// </summary>
		protected override void UpdateChangedEffects()
		{
			var player = m_owner as GamePlayer;
			if (player != null)
			{
				// Send Modified Effects and Clear
				player.Out.SendUpdateIcons(m_changedEffects.ToList(), ref m_lastUpdateEffectsCount);
				m_changedEffects.Clear();
			}
		}

	}
}
