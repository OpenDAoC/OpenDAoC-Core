using Core.GS.Keeps;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("Port")]
    public class GatewaySpell : MasterLevelSpellHandling
    {
        // constructor
        public GatewaySpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            GamePlayer player = Caster as GamePlayer;

            if (player != null)
            {
                if (!player.InCombat && !GameRelic.IsPlayerCarryingRelic(player))
                {
                    SendEffectAnimation(player, 0, false, 1);
                    player.MoveToBind();
                }
            }
        }
    }
}