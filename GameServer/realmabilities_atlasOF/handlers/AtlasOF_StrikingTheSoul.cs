using DOL.Database;
using DOL.GS.Effects;

namespace DOL.GS.RealmAbilities
{

    public class AtlasOF_StrikingTheSoul : TimedRealmAbility
    {
		public AtlasOF_StrikingTheSoul(DBAbility dba, int level) : base(dba, level) { }
		
		int m_duration = 60000; // 60s

		private int m_value = 25; // 25% bonus to hit (aka -25% spell resist)
		public override int MaxLevel { get { return 1; } }
		public override int GetReUseDelay(int level) { return 1800; } // 30 mins
		public override int CostForUpgrade(int level)
		{
			return 10;
		}

		public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			var player = living as GamePlayer;

			if (player is not {IsAlive: true}) return;
			new StrikingTheSoulECSEffect(new ECSGameEffectInitParams(player, m_duration, m_value));
			foreach (GamePlayer visPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				visPlayer.Out.SendSpellEffectAnimation(player, player, 7163, 0, false, 1);
			DisableSkill(living);
		} 
		
		public override bool CheckRequirement(GamePlayer player)
		{
			return player.Level >= 40;
		}
    }
}