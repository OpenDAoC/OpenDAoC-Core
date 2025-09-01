using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.BeFriend)]
	public class BeFriendSpellHandler : SpellHandler 
	{
		/// <summary>
		/// Dictionary to Keep track of Friend Brains Attached to NPC
		/// </summary>
		private readonly ConcurrentDictionary<GameNPC, FriendBrain> m_NPCFriendBrain = new();
		
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
		/// Select only uncontrolled GameNPC Targets
		/// </summary>
		/// <param name="castTarget"></param>
		/// <returns></returns>
		public override IList<GameLiving> SelectTargets(GameObject castTarget)
		{
			return base.SelectTargets(castTarget).Where(t => t is GameNPC).ToList();
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
				// Resisted
				SendSpellResistAnimation(target);
				MessageToCaster($"{target.GetName(0, true)} is too strong for you to charm!", eChatType.CT_SpellResisted);
				return;
			}
			
			if (npcTarget.Brain is IControlledBrain)
			{
				SendSpellResistAnimation(target);
				MessageToCaster($"{target.GetName(0, true)} is already under control.", eChatType.CT_SpellResisted);
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
			
			var currentBrain = npcTarget.Brain as IOldAggressiveBrain;
			var friendBrain = new FriendBrain(this);
			m_NPCFriendBrain[npcTarget] = friendBrain;
			
			npcTarget.AddBrain(friendBrain);
			friendBrain.Think();
			
			// Prevent Aggro on Effect Expires.
			if (currentBrain != null)
				currentBrain.ClearAggroList();
			
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

			FriendBrain fearBrain;
			if (m_NPCFriendBrain.TryRemove(npcTarget, out fearBrain))
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
		public BeFriendSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
