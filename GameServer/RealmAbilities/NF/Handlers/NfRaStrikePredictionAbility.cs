using System.Collections;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.RealmAbilities
{
    public class NfRaStrikePredictionAbility : TimedRealmAbility
    {
        public NfRaStrikePredictionAbility(DbAbility dba, int level) : base(dba, level) { }
        int m_range = 2000;
        int m_duration = 30;
        int m_value = 0;
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            GamePlayer player = living as GamePlayer;
			if (player.EffectList.CountOfType<NfRaStrikePredictionEffect>() > 0)
            {
                player.Out.SendMessage("You already have an effect of that type!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
            }
			
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
	            switch (Level)
	            {
	                case 1: m_value = 5; break;
	                case 2: m_value = 7; break;
	                case 3: m_value = 10; break;
	                case 4: m_value = 15; break;
	                case 5: m_value = 20; break;
	                default: return;
	            }				
			}
			else
			{
	            switch (Level)
	            {
	                case 1: m_value = 5; break;
	                case 2: m_value = 10; break;
	                case 3: m_value = 20; break;
	                default: return;
	            }
			}
			
            DisableSkill(living);
            ArrayList targets = new ArrayList();
            if (player.Group == null)
                targets.Add(player);
            else
                foreach (GamePlayer grpMate in player.Group.GetPlayersInTheGroup())
                    if (grpMate.IsWithinRadius(player, m_range) && grpMate.IsAlive)
                        targets.Add(grpMate);
            bool success;
            foreach (GamePlayer target in targets)
            {
				success = (target.EffectList.CountOfType<NfRaStrikePredictionEffect>() == 0);
                foreach (GamePlayer visPlayer in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    visPlayer.Out.SendSpellEffectAnimation(player, target, 7037, 0, false, CastSuccess(success));
                if (success)
                    if (target != null)
                    {
                        new NfRaStrikePredictionEffect().Start(target, m_duration, m_value);
                    }
            }

        }
        private byte CastSuccess(bool suc)
        {
            if (suc)
                return 1;
            else
                return 0;
        }
        public override int GetReUseDelay(int level)
        {
            return 600;
        }
    }
}
