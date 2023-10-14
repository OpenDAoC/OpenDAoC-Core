using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler("WaterBreathing")]
	public class WaterBreathingSpell : SpellHandler
	{
		public WaterBreathingSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = Spell.Duration;
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
			return (int)duration;
		}
		
		public override void OnEffectStart(GameSpellEffect effect)
		{

			GamePlayer player = effect.Owner as GamePlayer;
            
			if (player != null)
			{
                player.CanBreathUnderWater = true;
				player.BaseBuffBonusCategory[(int)EProperty.WaterSpeed] += (int)Spell.Value;
				player.Out.SendUpdateMaxSpeed();
			}

			EChatType toLiving = (Spell.Pulse == 0) ? EChatType.CT_Spell : EChatType.CT_SpellPulse;
			EChatType toOther = (Spell.Pulse == 0) ? EChatType.CT_System : EChatType.CT_SpellPulse;
			if (Spell.Message2 != "")
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
			MessageToLiving(effect.Owner, Spell.Message1 == "" ? "You find yourself able to move freely and breathe water like air!" : Spell.Message1, toLiving);
			base.OnEffectStart(effect);
		}

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			GamePlayer player = effect.Owner as GamePlayer;
			
            if (player != null)
			{
                //Check for Mythirian of Ektaktos on effect expiration to prevent unneccessary removal of Water Breathing Effect
                DbInventoryItem item = player.Inventory.GetItem((EInventorySlot)37);
                if (item == null || !item.Name.ToLower().Contains("ektaktos"))
                {
                    player.CanBreathUnderWater = false;
                }
				player.BaseBuffBonusCategory[(int)EProperty.WaterSpeed] -= (int)Spell.Value;
				player.Out.SendUpdateMaxSpeed();
				if (player.IsDiving & player.CanBreathUnderWater == false)
					MessageToLiving(effect.Owner, "With a gulp and a gasp you realize that you are unable to breathe underwater any longer!", EChatType.CT_SpellExpires);
			}
			return base.OnEffectExpires(effect, noMessages);
		}
	}
}
