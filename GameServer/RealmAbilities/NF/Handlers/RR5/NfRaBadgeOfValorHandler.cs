using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
    public class NfRaBadgeOfValorHandler : Rr5RealmAbility
    {
		public NfRaBadgeOfValorHandler(DbAbilities dba, int level) : base(dba, level) { }

        int m_reuseTimer = 900;

        public override void Execute(GameLiving living)
        {
            #region preCheck
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

			if (living.EffectList.CountOfType<NfRaBadgeOfValorEffect>() > 0)
            {
				if (living is GamePlayer)
					(living as GamePlayer).Out.SendMessage("You already an effect of that type!", EChatType.CT_SpellResisted, EChatLoc.CL_SystemWindow);
                return;
            }

            #endregion


            //send spelleffect
			foreach (GamePlayer visPlayer in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				visPlayer.Out.SendSpellEffectAnimation(living, living, 7057, 0, false, 0x01);

            new NfRaBadgeOfValorEffect().Start(living);
            living.DisableSkill(this, m_reuseTimer * 1000);
        }
    }
}