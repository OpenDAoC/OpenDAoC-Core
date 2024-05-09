using System.Collections.Generic;
using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
	public class SelflessDevotionEffect : TimedEffect
	{
		public SelflessDevotionEffect() : base(15000)
		{
			m_healpulse = 5;
		}

		private GamePlayer owner;
		private ECSGameTimer m_timer = null;
		private int m_healpulse;
		private Dictionary<eProperty, int> m_debuffs;

		public override void Start(GameLiving target)
		{
			base.Start(target);

			owner = target as GamePlayer;
			if (owner == null) return;

			foreach (GamePlayer p in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				p.Out.SendSpellEffectAnimation(owner, owner, Icon, 0, false, 1);

			m_debuffs = new Dictionary<eProperty, int>(1+eProperty.Stat_Last-eProperty.Stat_First);
			
			for (eProperty property = eProperty.Stat_First; property <= eProperty.Stat_Last; property++)
			{
				m_debuffs.Add(property, (int)(owner.GetModified(property) * 0.25));
				owner.DebuffCategory[(int)property] += m_debuffs[property];
			}

			owner.Out.SendCharStatsUpdate();
			
			m_timer = new ECSGameTimer(owner, new ECSGameTimer.ECSTimerCallback(HealPulse));
			m_timer.Start(1);
		}
		
		public int HealPulse(ECSGameTimer timer)
		{
			if (m_healpulse > 0)
			{
				m_healpulse--;
				
				GamePlayer player = Owner as GamePlayer;
				if (player == null) return 0;
				if (player.Group == null) return 3000;
				
				foreach (GamePlayer p in player.Group.GetPlayersInTheGroup())
				{
					if (p.Health < p.MaxHealth && player.IsWithinRadius(p, 750) && p.IsAlive)
					{
						player.Stealth(false);
						int heal = 300;
						
						if (p.Health + heal > p.MaxHealth)
							heal = p.MaxHealth - p.Health;
							
						p.ChangeHealth(player, eHealthChangeType.Regenerate, heal);
						
						player.Out.SendMessage("You heal " + p.Name + " for " + heal.ToString() + " hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
						p.Out.SendMessage(player.Name + " heals you for " + heal.ToString() + " hit points.", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
					}
				}
				return 3000;
			}
			return 0;
		}

		public override void Stop()
		{
			base.Stop();
			
			if (owner != null)
			{
				for (eProperty property = eProperty.Stat_First; property <= eProperty.Stat_Last; property++)
				{
					if (m_debuffs.TryGetValue(property, out int value))
						owner.DebuffCategory[(int)property] -= value;
				}

				owner.Out.SendCharStatsUpdate();
			}
			
			if (m_timer != null)
			{
				m_timer.Stop();
				m_timer = null;
			}
		}

		public override string Name { get { return "Selfless Devotion"; } }

		public override ushort Icon { get { return 3038; } }

		public int SpellEffectiveness { get { return 100; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Decrease Paladin stats by 25%, and pulse a 300 points group heal with a 750 units range every 3 seconds for 15 seconds total.");
				return list;
			}
		}
	}
}
