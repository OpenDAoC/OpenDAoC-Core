using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Ameliorating Melodies
	/// </summary>
	public class AtlasOF_AmelioratingMelodiesEffect : TimedEffect
	{
		/// <summary>
		/// The countdown value. If this value is 0, the effect vanishes
		/// </summary>
		int m_countdown;

		/// <summary>
		/// The number of hit points healed each tick
		/// </summary>
		int m_heal;

		/// <summary>
		/// Max healing range
		/// </summary>
		int m_range;

        /// <summary>
        /// Tick Rate in milliseconds
        /// </summary>
        int m_tickrate;

        /// <summary>
        /// The rgion timer
        /// </summary>
        RegionTimer m_countDownTimer = null;

		/// <summary>
		/// Ameliorating Melodies
		/// </summary>
		/// <param name="heal">Delve value hit points per tick"</param>
		public AtlasOF_AmelioratingMelodiesEffect(int heal)
			: base(30000)
		{
			m_heal = heal;
			m_range = 1500;
			m_countdown = 20; // This means the number of ticks. At 1.5s tick rate we want 20 to get a 30s duration.
			m_tickrate = 1500;
		}

		/// <summary>
		/// Starts the effect
		/// </summary>
		/// <param name="target">The player of this effect</param>
		public override void Start(GameLiving target)
		{
			base.Start(target);
			GamePlayer player = target as GamePlayer;
			if (player == null) return;
			player.EffectList.Add(this);
            m_countDownTimer = new RegionTimer(player, new RegionTimerCallback(CountDown));
			m_countDownTimer.Start(1);
		}

		/// <summary>
		/// Stops the effect
		/// </summary>
		public override void Stop()
		{
			base.Stop();
			Owner.EffectList.Remove(this);
			if (m_countDownTimer != null)
			{
				m_countDownTimer.Stop();
				m_countDownTimer = null;
			}
		}

		/// <summary>
		/// Timer callback
		/// </summary>
		/// <param name="timer">The region timer</param>
		public int CountDown(RegionTimer timer)
		{
			if (m_countdown > 0)
			{
				m_countdown--;
				GamePlayer player = Owner as GamePlayer;
				if (player == null) return 0;

                ICollection<GamePlayer> playersToHeal = null;

				// OF AM does not have the "does not heal the Bard/caster" restriction that NF AM has.
                if (player.Group == null)
                {
					playersToHeal.Add(player);
				}
                else
                {
					playersToHeal = player.Group.GetPlayersInTheGroup();
                }

                foreach (GamePlayer p in playersToHeal)
				{
					if ((p.Health < p.MaxHealth) && player.IsWithinRadius(p, m_range) && (p.IsAlive))
					{
						int heal = m_heal;
						if (p.Health + heal > p.MaxHealth) heal = p.MaxHealth - p.Health;
						p.ChangeHealth(player, eHealthChangeType.Regenerate, heal);
						player.Out.SendMessage("Your Ameliorating Melodies heal " + p.Name + " for " + heal.ToString() + " hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
						p.Out.SendMessage(player.Name + "'s Ameliorating Melodies heals you for " + heal.ToString() + " hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
					}
				}
				return m_tickrate;
			}
			return 0;
		}

		/// <summary>
		/// Name of the effect
		/// </summary>
		public override string Name { get { return "Ameliorating Melodies"; } }

		/// <summary>
		/// Icon of the effect
		/// </summary>
		public override ushort Icon { get { return 3021; } }

		/// <summary>
		/// Delve information
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>(8);
				list.Add("Ameliorating Melodies");
				list.Add(" ");
				list.Add("Value: " + m_heal.ToString() + " / tick");
				list.Add("Target: Group");
				list.Add("Range: " + m_range.ToString());
				list.Add("Duration: 30 seconds");

				return list;
			}
		}
	}
}