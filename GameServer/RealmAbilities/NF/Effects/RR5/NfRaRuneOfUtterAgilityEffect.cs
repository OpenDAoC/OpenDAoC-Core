using System.Collections.Generic;

namespace Core.GS.Effects
{
	public class NfRaRuneOfUtterAgilityEffect : TimedEffect
	{
		private GameLiving owner;

		public NfRaRuneOfUtterAgilityEffect()
			: base(15000)
		{
		}

		public override void Start(GameLiving target)
		{
			base.Start(target);
			owner = target;
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
				}
				player.BuffBonusCategory4[(int)EProperty.EvadeChance] += 90;
			}
		}

		public override void Stop()
		{
			GamePlayer player = owner as GamePlayer;
			if (player != null)
				player.BuffBonusCategory4[(int)EProperty.EvadeChance] -= 90;
			base.Stop();
		}

		public override string Name { get { return "Rune of Utter Agility"; } }

		public override ushort Icon { get { return 3073; } }

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add("Increases your evade chance up to 90% for 30 seconds.");
				return list;
			}
		}
	}
}