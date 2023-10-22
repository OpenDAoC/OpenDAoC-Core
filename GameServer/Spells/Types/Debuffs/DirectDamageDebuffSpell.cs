using System;
using System.Collections.Generic;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Packets.Server;
using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
/// Damages the target and lowers their resistance to the spell's type.
/// </summary>
[SpellHandler("DirectDamageWithDebuff")]
public class DirectDamageDebuffSpell : AResistDebuff
{
	public override void CreateECSEffect(EcsGameEffectInitParams initParams)
	{
		new StatDebuffEcsSpellEffect(initParams);
	}
	
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public override EProperty Property1 { get { return Caster.GetResistTypeForDamage(Spell.DamageType); } }
	public override string DebuffTypeName { get { return GlobalConstants.DamageTypeToName(Spell.DamageType); } }

	#region LOS on Keeps

	public override void OnDirectEffect(GameLiving target)
	{
		if (target == null)
			return;

		if (Spell.Target == ESpellTarget.CONE || (Spell.Target == ESpellTarget.ENEMY && Spell.IsPBAoE))
		{
			GamePlayer player = null;
			if (target is GamePlayer)
			{
				player = target as GamePlayer;
			}
			else
			{
				if (Caster is GamePlayer)
					player = Caster as GamePlayer;
				else if (Caster is GameNpc && (Caster as GameNpc).Brain is IControlledBrain)
				{
					IControlledBrain brain = (Caster as GameNpc).Brain as IControlledBrain;
					//Ryan: edit for BD
					if (brain.Owner is GamePlayer)
						player = (GamePlayer)brain.Owner;
					else
						player = (GamePlayer)((IControlledBrain)((GameNpc)brain.Owner).Brain).Owner;
				}
			}
			if (player != null)
				player.Out.SendCheckLOS(Caster, target, new CheckLOSResponse(DealDamageCheckLOS));
			else
				DealDamage(target);
		}

		else DealDamage(target);
	}

	private void DealDamageCheckLOS(GamePlayer player, ushort response, ushort targetOID)
	{
		if (player == null || targetOID == 0)
			return;

		if ((response & 0x100) == 0x100)
		{
			try
			{
				GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;
				if (target != null)
				{
					DealDamage(target);

					// Due to LOS check delay the actual cast happens after FinishSpellCast does a notify, so we notify again
					GameEventMgr.Notify(GameLivingEvent.CastFinished, m_caster, new CastingEventArgs(this, target, m_lastAttackData));
				}
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
			}
		}
	}

	public override void ApplyEffectOnTarget(GameLiving target)
	{
		// do not apply debuff to keep components or doors
		if ((target is Keeps.GameKeepComponent) == false && (target is Keeps.GameKeepDoor) == false)
		{
			base.ApplyEffectOnTarget(target);
		}

		if ((Spell.Duration > 0 && Spell.Target != ESpellTarget.AREA) || Spell.Concentration > 0)
		{
			OnDirectEffect(target);
		}
	}

	private void DealDamage(GameLiving target)
	{
		if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

		if (target is Keeps.GameKeepDoor || target is Keeps.GameKeepComponent)
		{
			MessageToCaster("Your spell has no effect on the keep component!", EChatType.CT_SpellResisted);
			return;
		}
		// calc damage
		AttackData ad = CalculateDamageToTarget(target);
		SendDamageMessages(ad);
		DamageTarget(ad, true);
		target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
		/*
		if (target.IsAlive)
			base.ApplyEffectOnTarget(target, effectiveness);*/
	}
	/*
	 * We need to send resist spell los check packets because spell resist is calculated first, and
	 * so you could be inside keep and resist the spell and be interupted when not in view
	 */
	protected override void OnSpellResisted(GameLiving target)
	{
		if (target is GamePlayer && Caster.TempProperties.GetProperty("player_in_keep_property", false))
		{
			GamePlayer player = target as GamePlayer;
			player.Out.SendCheckLOS(Caster, player, new CheckLOSResponse(ResistSpellCheckLOS));
		}
		else SpellResisted(target);
	}

	private void ResistSpellCheckLOS(GamePlayer player, ushort response, ushort targetOID)
	{
		if ((response & 0x100) == 0x100)
		{
			try
			{
				GameLiving target = Caster.CurrentRegion.GetObject(targetOID) as GameLiving;
				if (target != null)
					SpellResisted(target);
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error(string.Format("targetOID:{0} caster:{1} exception:{2}", targetOID, Caster, e));
			}
		}
	}

	private void SpellResisted(GameLiving target)
	{
		base.OnSpellResisted(target);
	}
	#endregion

	/// <summary>
	/// Delve Info
	/// </summary>
	public override IList<string> DelveInfo
	{
		get
		{
			/*
			<Begin Info: Lesser Raven Bolt>
			Function: dmg w/resist decrease

			Damages the target, and lowers the target's resistance to that spell type.

			Damage: 32
			Resist decrease (Cold): 10%
			Target: Targetted
			Range: 1500
			Duration: 1:0 min
			Power cost: 5
			Casting time:      3.0 sec
			Damage: Cold

			<End Info>
			*/

			var list = new List<string>();

            list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DirectDamageDebuffSpellHandler.DelveInfo.Function"));
			list.Add(" "); //empty line
			list.Add(Spell.Description);
			list.Add(" "); //empty line
            if (Spell.Damage != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
            if (Spell.Value != 0)
                list.Add(String.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DirectDamageDebuffSpellHandler.DelveInfo.Decrease", DebuffTypeName, Spell.Value)));
            list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Target", Spell.Target));
            if (Spell.Range != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Range", Spell.Range));
            if (Spell.Duration >= ushort.MaxValue * 1000)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");
            else if (Spell.Duration > 60000)
                list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
            else if (Spell.Duration != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
            if (Spell.Frequency != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
            if (Spell.Power != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
            list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
			if(Spell.RecastDelay > 60000)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay/60000).ToString() + ":" + (Spell.RecastDelay%60000/1000).ToString("00") + " min");
			else if(Spell.RecastDelay > 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.RecastTime") + (Spell.RecastDelay/1000).ToString() + " sec");
   			if(Spell.Concentration != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.ConcentrationCost", Spell.Concentration));
			if(Spell.Radius != 0)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Radius", Spell.Radius));
			if(Spell.DamageType != EDamageType.Natural)
                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));

			return list;
		}
	}

	// constructor
	public DirectDamageDebuffSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}