using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Scarab proc spell handler
    /// Snare and morph target. Cecity is a subspell.
    /// </summary>
    [SpellHandler(eSpellType.ScarabProc)]
    public class ScarabProc : UnbreakableSpeedDecreaseSpellHandler
    {
 		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

        public override void OnEffectStart(GameSpellEffect effect)
        {        	           
            if(effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
            	player.Model = (ushort)Spell.LifeDrainReturn; // 1200 is official id
            }     
            base.OnEffectStart(effect);
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if(effect.Owner is GamePlayer)
            {
            	GamePlayer player = effect.Owner as GamePlayer;
 				player.Model = player.CreationModel;     
            }                       
            return base.OnEffectExpires(effect, noMessages);
        }
        public ScarabProc(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
