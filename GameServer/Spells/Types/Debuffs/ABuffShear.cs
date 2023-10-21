using System.Collections.Generic;
using Core.AI.Brain;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.PacketHandler;

namespace Core.GS.Spells;

/// <summary>
/// Base class for all buff shearing spells
/// </summary>
public abstract class ABuffShear : SpellHandler
{
	/// <summary>
	/// The spell type to shear
	/// </summary>
	public abstract string ShearSpellType { get; }

	/// <summary>
	/// The spell type shown in delve info
	/// </summary>
	public abstract string DelveSpellType { get; }

	/// <summary>
	/// called after normal spell cast is completed and effect has to be started
	/// </summary>
	public override void FinishSpellCast(GameLiving target)
	{
		m_caster.Mana -= PowerCost(target);
		base.FinishSpellCast(target);
	}

	public override void OnDirectEffect(GameLiving target)
	{
		base.OnDirectEffect(target);
		if (target == null) return;
		if (!target.IsAlive || target.ObjectState!=GameLiving.eObjectState.Active) return;

		target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
		GameSpellEffect mez = SpellHandler.FindEffectOnTarget(target, "Mesmerize");
        if (mez != null)
        {
            mez.Cancel(false);
            return;
        }
		if (target is GameNpc)
		{
			GameNpc npc = (GameNpc)target;
			IOldAggressiveBrain aggroBrain = npc.Brain as IOldAggressiveBrain;
			if (aggroBrain != null)
				aggroBrain.AddToAggroList(Caster, 1);
		}

		//check for spell.
		foreach (GameSpellEffect effect in target.EffectList.GetAllOfType<GameSpellEffect>())
		{
			if (effect.Spell.SpellType.ToString() == ShearSpellType)
			{
				if ((effect.Owner != effect.SpellHandler.Caster || effect.Spell.IsShearable) && effect.Spell.Value <= Spell.Value)
				{
					SendEffectAnimation(target, 0, false, 1);
					effect.Cancel(false);
					MessageToCaster("Your spell rips away some of your target's enhancing magic.", EChatType.CT_Spell);
					MessageToLiving(target, "Some of your enhancing magic has been ripped away by a spell!", EChatType.CT_Spell);
				}
				else
				{
					SendEffectAnimation(target, 0, false, 0);
					MessageToCaster("The target's connection to their enhancement is too strong for you to remove.", EChatType.CT_SpellResisted);
				}

				return;
			}
		}

		SendEffectAnimation(target, 0, false, 0);
		MessageToCaster("No enhancement of that type found on the target.", EChatType.CT_SpellResisted);

		/*
		if (!noMessages) 
		{
			MessageToLiving(effect.Owner, effect.Spell.Message3, eChatType.CT_SpellExpires);
			Message.SystemToArea(effect.Owner, Util.MakeSentence(effect.Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
		}
		*/
	}

	/// <summary>
	/// When spell was resisted
	/// </summary>
	/// <param name="target">the target that resisted the spell</param>
	protected override void OnSpellResisted(GameLiving target)
	{
		base.OnSpellResisted(target);
		if (Spell.Damage == 0 && Spell.CastTime == 0)
		{
			target.StartInterruptTimer(target.SpellInterruptDuration, EAttackType.Spell, Caster);
		}
	}

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo 
	{
		get 
		{
			/*
			<Begin Info: Potency Whack>
			Function: buff shear

			Destroys a positive enhancement on the target.

			Type: Str/Con
			Maximum strength of buffs removed: 150
			Target: Enemy realm players and controlled pets only
			Range: 1500
			Power cost: 12
			Casting time:      2.0 sec
			Damage: Body

			<End Info>
			*/

			var list = new List<string>();

			list.Add("Function: " + (Spell.SpellType.ToString() == "" ? "(not implemented)" : Spell.SpellType.ToString()));
			list.Add(" "); //empty line
			list.Add(Spell.Description);
			list.Add(" "); //empty line
			list.Add("Type: " + DelveSpellType);
			list.Add("Maximum strength of buffs removed: " + Spell.Value);
			if(Spell.Range != 0) list.Add("Range: " + Spell.Range);
			if(Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
			list.Add("Casting time: " + (Spell.CastTime*0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
			if(Spell.Radius != 0) list.Add("Radius: " + Spell.Radius);
			if(Spell.DamageType != EDamageType.Natural) list.Add("Damage: " + GlobalConstants.DamageTypeToName(Spell.DamageType));

			return list;
		}
	}

	// constructor
	public ABuffShear(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}