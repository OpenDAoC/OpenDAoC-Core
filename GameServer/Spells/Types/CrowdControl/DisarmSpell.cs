using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Simulates disarming a target by stopping their attack
	/// </summary>
	[SpellHandler("Disarm")]
	public class DisarmSpell : SpellHandler
	{
		/// <summary>
		/// called after normal spell cast is completed and effect has to be started
		/// </summary>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}		
		/// <summary>
		/// When an applied effect starts
		/// duration spells only
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if (effect.Owner.Realm == 0 || Caster.Realm == 0)
			{
				effect.Owner.LastAttackedByEnemyTickPvE = effect.Owner.CurrentRegion.Time;
				Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
			}
			else
			{
				effect.Owner.LastAttackedByEnemyTickPvP = effect.Owner.CurrentRegion.Time;
				Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
			}
            effect.Owner.DisarmedTime = effect.Owner.CurrentRegion.Time + CalculateEffectDuration(effect.Owner, Caster.Effectiveness);
			effect.Owner.attackComponent.StopAttack();
			MessageToLiving(effect.Owner, Spell.Message1, EChatType.CT_Spell);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), EChatType.CT_Spell, effect.Owner);
			effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
			if (effect.Owner is GameNpc)
			{
				IOldAggressiveBrain aggroBrain = ((GameNpc)effect.Owner).Brain as IOldAggressiveBrain;
				if (aggroBrain != null)
					aggroBrain.AddToAggroList(Caster, 1);
			}
		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			//effect.Owner.IsDisarmed = false;
			if (!noMessages) {
				MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
				MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
			}
			return base.OnEffectExpires(effect,noMessages);
		}

		/// <summary>
		/// Delve Info
		/// </summary>
		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();

				list.Add("Function: " + (Spell.SpellType.ToString() == "" ? "(not implemented)" : Spell.SpellType.ToString()));
				list.Add(" "); //empty line
				list.Add(Spell.Description);
				list.Add(" "); //empty line
				if(Spell.Duration != 0) list.Add(string.Format("Duration: {0}sec", (int)Spell.Duration/1000));
				list.Add("Target: " + Spell.Target);
				if(Spell.Range != 0) list.Add("Range: " + Spell.Range);
				if(Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
				list.Add("Casting time: " + (Spell.CastTime*0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
				if(Spell.RecastDelay > 60000) list.Add("Recast time: " + (Spell.RecastDelay/60000).ToString() + ":" + (Spell.RecastDelay%60000/1000).ToString("00") + " min");
				else if(Spell.RecastDelay > 0) list.Add("Recast time: " + (Spell.RecastDelay/1000).ToString() + " sec");
				return list;
			}
		}

		// constructor
		public DisarmSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}
