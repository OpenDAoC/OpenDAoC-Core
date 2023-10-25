using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Core.GS.AI;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("BeFriend")]
public class BefriendSpell : SpellHandler 
{
	/// <summary>
	/// Dictionary to Keep track of Friend Brains Attached to NPC
	/// </summary>
	private readonly ConcurrentDictionary<GameNpc, FriendBrain> m_NPCFriendBrain = new();
	
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
			SendSpellResistAnimation(target);
			this.MessageToCaster(EChatType.CT_SpellResisted, "{0} is too strong for you to charm!", target.GetName(0, true));
			return;
		}
		
		if (npcTarget.Brain is IControlledBrain)
		{
			SendSpellResistAnimation(target);
			this.MessageToCaster(EChatType.CT_SpellResisted, "{0} is already under control.",  target.GetName(0, true));
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
		var npcTarget = effect.Owner as GameNpc;

		FriendBrain fearBrain;
		if (m_NPCFriendBrain.TryRemove(npcTarget, out fearBrain))
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
	public BefriendSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}