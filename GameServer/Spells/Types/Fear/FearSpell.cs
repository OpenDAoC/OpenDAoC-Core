using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Effects;

namespace Core.GS.Spells
{
	[SpellHandler("Fear")]
	public class FearSpell : SpellHandler 
	{
		/// <summary>
		/// Dictionary to Keep Track of Fear Brains attached to NPCs
		/// </summary>
		private readonly ConcurrentDictionary<GameNpc, FearBrain> m_NPCFearBrains = new();
		
		/// <summary>
		/// Consume Power on Spell Start
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast (target);
		}

		/// <summary>
		/// Select only GameNPC Targets
		/// </summary>
		/// <param name="castTarget"></param>
		/// <returns></returns>
		public override IList<GameLiving> SelectTargets(GameObject castTarget)
		{
			return base.SelectTargets(castTarget).Where(t => t is GameNpc).ToList();
		}

		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			var npcTarget = target as GameNpc;
			if (npcTarget == null) return;
			
			if (npcTarget.Level > Spell.Value)
			{
				// Resisted
				OnSpellResisted(target);
				return;
			}
			
			base.ApplyEffectOnTarget(target);
		}

		/// <summary>
		/// On Effect Start Replace Brain with Fear Brain.
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			var npcTarget = effect.Owner as GameNpc;
			
			var fearBrain = new FearBrain();
			m_NPCFearBrains[npcTarget] = fearBrain;
			
			npcTarget.AddBrain(fearBrain);
			fearBrain.Think();
			
			base.OnEffectStart(effect);
		}
		
		/// <summary>
		/// Called when Effect Expires
		/// </summary>
		/// <param name="effect"></param>
		/// <param name="noMessages"></param>
		/// <returns></returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			var npcTarget = effect.Owner as GameNpc;

			FearBrain fearBrain;
			if (m_NPCFearBrains.TryRemove(npcTarget, out fearBrain))
			{
				npcTarget.RemoveBrain(fearBrain);
			}

			if(npcTarget.Brain == null)
				npcTarget.AddBrain(new StandardMobBrain());

			return base.OnEffectExpires(effect, noMessages);
		}
		
		/// <summary>
		/// Spell Resists don't trigger notification or interrupt
		/// </summary>
		/// <param name="target"></param>
		protected override void OnSpellResisted(GameLiving target)
		{
			SendSpellResistAnimation(target);
			SendSpellResistMessages(target);
			StartSpellResistLastAttackTimer(target);
		}

		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public FearSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
