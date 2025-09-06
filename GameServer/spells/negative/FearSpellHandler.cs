using System.Collections.Concurrent;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.Fear)]
	public class FearSpellHandler : SpellHandler 
	{
		/// <summary>
		/// Dictionary to Keep Track of Fear Brains attached to NPCs
		/// </summary>
		private readonly ConcurrentDictionary<GameNPC, FearBrain> m_NPCFearBrains = new();
		
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
		public override List<GameLiving> SelectTargets(GameObject castTarget)
		{
			List<GameLiving> targets = base.SelectTargets(castTarget);
			targets.RemoveAll(t => t is not GameNPC);
			return targets;
		}

		/// <summary>
		/// called when spell effect has to be started and applied to targets
		/// </summary>
		public override void ApplyEffectOnTarget(GameLiving target)
		{
			var npcTarget = target as GameNPC;
			if (npcTarget == null) return;
			
			if (npcTarget.Level > Spell.Value)
			{
				OnSpellNegated(target, SpellNegatedReason.Immune);
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
			var npcTarget = effect.Owner as GameNPC;
			
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
			var npcTarget = effect.Owner as GameNPC;

			FearBrain fearBrain;
			if (m_NPCFearBrains.TryRemove(npcTarget, out fearBrain))
			{
				npcTarget.RemoveBrain(fearBrain);
			}

			if(npcTarget.Brain == null)
				npcTarget.AddBrain(new StandardMobBrain());

			return base.OnEffectExpires(effect, noMessages);
		}

		protected override void OnSpellNegated(GameLiving target, SpellNegatedReason reason)
		{
			if (reason is SpellNegatedReason.Resisted)
			{
				SendSpellResistAnimation(target);
				SendSpellResistMessages(target);
			}

			StartSpellNegatedLastAttackTimer(target);
		}

		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="caster"></param>
		/// <param name="spell"></param>
		/// <param name="line"></param>
		public FearSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
