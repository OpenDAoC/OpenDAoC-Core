using System;
using System.Collections.Generic;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Dummy effect for testing purposes (identifying buff icons and such).
    /// </summary>
    public class DummyEffect : TimedEffect, IGameEffect
    {
        /// <summary>
        /// Create a new DummyEffect for the given effect ID.
        /// </summary>
        /// <param name="effectId"></param>
        public DummyEffect(ushort effectId) : base(60000)
        {
            EffectId = effectId;
        }

        /// <summary>
        /// The ID associated with this effect.
        /// </summary>
        public ushort EffectId { get; protected set; }

		/// <summary>
		/// The effect owner.
		/// </summary>
		GamePlayer m_player;

		/// <summary>
		/// Start the dummy effect on a player.
		/// </summary>
		/// <param name="living">The effect target</param>
		public override void Start(GameLiving living)
		{
			GamePlayer player = living as GamePlayer;

			if (player != null)
			{
				m_player = player;

				player.EffectList.Add(this);
                player.Out.SendUpdatePlayer();       
			}
		}

		/// <summary>
		/// Stop the effect.
		/// </summary>
		public override void Stop()
		{
            if (m_player != null)
            {
                m_player.EffectList.Remove(this);
                m_player.Out.SendUpdatePlayer();
            }
		}

		/// <summary>
		/// Name of the effect.
		/// </summary>
		public override string Name 
        {
            get { return String.Format("Effect #{0}", EffectId); } 
        }	

		/// <summary>
		/// Icon to show on players.
		/// </summary>
		public override ushort Icon 
        { 
            get { return EffectId; } 
        }
		
		/// <summary>
		/// Delve information.
		/// </summary>
		public override IList<string> DelveInfo 
        {
            get 
			{
				string[] delve = new string[1];
				delve[0] = "Dummy Effect Icon #" + Icon;
				return delve;
			} 
        }
    }
}
