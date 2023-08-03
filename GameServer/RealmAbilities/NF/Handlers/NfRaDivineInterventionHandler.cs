using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
    public class NfRaDivineInterventionHandler : TimedRealmAbility
	{
        public NfRaDivineInterventionHandler(DbAbilities dba, int level) : base(dba, level) { }
		public const int poolDuration = 1200000; // 20 minutes

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            GamePlayer player = living as GamePlayer;

			Group playerGroup = player.Group;

			if (playerGroup == null)
			{
				player.Out.SendMessage("You must be in a group to use this ability!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
				return;
			}

			int poolValue = 0;
			
			if(ServerProperties.ServerProperties.USE_NEW_ACTIVES_RAS_SCALING)
			{
	            switch (Level)
	            {
	                case 1: poolValue = 1000; break;
	                case 2: poolValue = 1500; break;
	                case 3: poolValue = 2000; break;
	                case 4: poolValue = 2500; break;
	                case 5: poolValue = 3000; break;
	            }				
			}
			else
			{
	            switch (Level)
	            {
	                case 1: poolValue = 1000; break;
	                case 2: poolValue = 2000; break;
	                case 3: poolValue = 3000; break;
	            }				
			}


			foreach (GamePlayer groupMember in playerGroup.GetPlayersInTheGroup())
			{
				NfRaDivineInterventionEffect DIEffect = groupMember.EffectList.GetOfType<NfRaDivineInterventionEffect>();
				if (DIEffect != null)
				{
					player.Out.SendMessage("You are already protected by a pool of healing", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
					return;
				}
			}
            DisableSkill(living);
            if (player != null)
            {
                new NfRaDivineInterventionEffect(poolValue).Start(player);
            }
		}
        public override int GetReUseDelay(int level)
        {
            return 600;
        }
	}
}