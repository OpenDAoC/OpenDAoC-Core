using System.Collections.Generic;
using Core.Language;

namespace Core.GS.Effects
{
	public class ShadeEffect : StaticEffect, IGameEffect
	{
		/// <summary>
		/// The effect owner
		/// </summary>
		GamePlayer m_player;

		/// <summary>
		/// Start the shade effect on a player.
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
		/// Cancel the effect.
		/// </summary>
		public override void Cancel(bool playerCancel) 
		{
            if (m_player != null)
			    m_player.Shade(false);
		}

		/// <summary>
		/// Name of the effect.
		/// </summary>
		public override string Name 
        { 
            get { return LanguageMgr.GetTranslation(m_player.Client, "Effects.ShadeEffect.Name"); } 
        }	

		/// <summary>
		/// Icon to show on players, can be id
		/// </summary>
		public override ushort Icon 
        { 
            get { return 0x193; } 
        }
		
		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo 
        { 
            get { return new string[0]; } 
        }
	}
}
