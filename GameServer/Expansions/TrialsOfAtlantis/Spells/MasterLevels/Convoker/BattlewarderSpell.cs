using System;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

//shared timer 1

[SpellHandler("Battlewarder")]
public class BattlewarderSpell : SpellHandler
{
	private GameNpc warder;
	private GameSpellEffect m_effect;

	/// <summary>
	/// Execute battle warder summon spell
	/// </summary>
	/// <param name="target"></param>
	public override void FinishSpellCast(GameLiving target)
	{
		m_caster.Mana -= PowerCost(target);
		base.FinishSpellCast(target);
	}

	public override bool IsOverwritable(EcsGameSpellEffect compare)
	{
		return false;
	}

	public override void OnEffectStart(GameSpellEffect effect)
	{
		base.OnEffectStart(effect);
		m_effect = effect;
		if (effect.Owner == null || !effect.Owner.IsAlive)
			return;

		if ((effect.Owner is GamePlayer))
		{
			GamePlayer casterPlayer = effect.Owner as GamePlayer;
			if (casterPlayer.GroundTarget != null && casterPlayer.GroundTargetInView)
			{
				GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.Moving, new CoreEventHandler(PlayerMoves));
				GameEventMgr.AddHandler(warder, GameLivingEvent.Dying, new CoreEventHandler(BattleWarderDie));
				GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.CastStarting,
					new CoreEventHandler(PlayerMoves));
				GameEventMgr.AddHandler(casterPlayer, GamePlayerEvent.AttackFinished,
					new CoreEventHandler(PlayerMoves));
				warder.X = casterPlayer.GroundTarget.X;
				warder.Y = casterPlayer.GroundTarget.Y;
				warder.Z = casterPlayer.GroundTarget.Z;
				warder.AddBrain(new MlBrain());
				warder.AddToWorld();
			}
			else
			{
				MessageToCaster("Your area target is out of range.  Set a closer ground position.",
					EChatType.CT_SpellResisted);
				effect.Cancel(false);
			}
		}
	}

	public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
	{
		if (warder != null)
		{
			GameEventMgr.RemoveHandler(warder, GameLivingEvent.Dying, new CoreEventHandler(BattleWarderDie));
			warder.RemoveBrain(warder.Brain);
			warder.Health = 0;
			warder.Delete();
		}

		if ((effect.Owner is GamePlayer))
		{
			GamePlayer casterPlayer = effect.Owner as GamePlayer;
			GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.Moving, new CoreEventHandler(PlayerMoves));
			GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.CastStarting,
				new CoreEventHandler(PlayerMoves));
			GameEventMgr.RemoveHandler(casterPlayer, GamePlayerEvent.AttackFinished,
				new CoreEventHandler(PlayerMoves));
		}

		effect.Owner.EffectList.Remove(effect);
		return base.OnEffectExpires(effect, noMessages);
	}

	// Event : player moves, lose focus
	public void PlayerMoves(CoreEvent e, object sender, EventArgs args)
	{
		GameLiving player = sender as GameLiving;
		if (player == null) return;
		if (e == GamePlayerEvent.Moving)
		{
			MessageToCaster("Your concentration fades", EChatType.CT_SpellExpires);
			OnEffectExpires(m_effect, true);
			return;
		}
	}

	// Event : Battle warder has died
	private void BattleWarderDie(CoreEvent e, object sender, EventArgs args)
	{
		GameNpc kWarder = sender as GameNpc;
		if (kWarder == null) return;
		if (e == GameLivingEvent.Dying)
		{
			MessageToCaster("Your Battle Warder has fallen!", EChatType.CT_SpellExpires);
			OnEffectExpires(m_effect, true);
			return;
		}
	}

	public override bool CheckBeginCast(GameLiving selectedTarget)
	{
		if (!base.CheckBeginCast(selectedTarget)) return false;
		if (!(m_caster.GroundTarget != null && m_caster.GroundTargetInView))
		{
			MessageToCaster("Your area target is out of range.  Set a closer ground position.",
				EChatType.CT_SpellResisted);
			return false;
		}

		return true;

	}

	public BattlewarderSpell(GameLiving caster, Spell spell, SpellLine line)
		: base(caster, spell, line)
	{
		warder = new GameNpc();
		//Fill the object variables
		warder.CurrentRegion = caster.CurrentRegion;
		warder.Heading = (ushort)((caster.Heading + 2048) % 4096);
		warder.Level = 70;
		warder.Realm = caster.Realm;
		warder.Name = "Battle Warder";
		warder.Model = 993;
		warder.CurrentSpeed = 0;
		warder.MaxSpeedBase = 0;
		warder.GuildName = "";
		warder.Size = 50;
	}
}