using Core.GS.Effects;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("ScarabProc")]
    public class ScarabProcSpell : UnbreakableSpeedDecreaseSpell
    {
 		public override int CalculateSpellResistChance(GameLiving target)
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
        public ScarabProcSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
