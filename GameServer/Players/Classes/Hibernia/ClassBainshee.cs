using System;
using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Bainshee, "Bainshee", "Magician")]
public class ClassBainshee : ClassMagician
{
	public ClassBainshee() : base()
	{
		m_profession = "PlayerClass.Profession.PathofAffinity";
		m_specializationMultiplier = 10;
		m_primaryStat = EStat.INT;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.INT;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}
	
	#region Wraith Form
	protected const int WRAITH_FORM_RESET_DELAY = 30000;
	
	/// <summary>
	/// Timer Action for Reseting Wraith Form
	/// </summary>
	protected EcsGameTimer m_wraithTimerAction;
	
	/// <summary>
	/// Event Trigger When Player Zoning Out to Force Reset Form
	/// </summary>
	protected CoreEventHandler m_wraithTriggerEvent;
	
	/// <summary>
	/// Bainshee Transform While Casting.
	/// </summary>
	/// <param name="player"></param>
	public override void Init(GamePlayer player)
	{
		base.Init(player);

		m_wraithTimerAction = new EcsGameTimer(player, new EcsGameTimer.EcsTimerCallback(_ =>
		{
			if (player.PlayerClass is ClassBainshee bainshee)
				bainshee.TurnOutOfWraith();

			return 0;
		}));

		m_wraithTriggerEvent = new CoreEventHandler(TriggerUnWraithForm);
		GameEventMgr.AddHandler(Player, GameLivingEvent.CastFinished, new CoreEventHandler(TriggerWraithForm));
	}

	/// <summary>
	/// Check if this Spell Cast Trigger Wraith Form
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="arguments"></param>
	protected virtual void TriggerWraithForm(CoreEvent e, object sender, EventArgs arguments)
	{
		var player = sender as GamePlayer;
		
		if (player != Player)
			return;
		
		var args = arguments as CastingEventArgs;
		
		if (args == null || args.SpellHandler == null)
			return;

		if (!args.SpellHandler.HasPositiveEffect)
			TurnInWraith();
	}
	
	/// <summary>
	/// Check if we should remove Wraith Form
	/// </summary>
	/// <param name="e"></param>
	/// <param name="sender"></param>
	/// <param name="arguments"></param>
	protected virtual void TriggerUnWraithForm(CoreEvent e, object sender, EventArgs arguments)
	{
		GamePlayer player = sender as GamePlayer;
		
		if (player != Player)
			return;
		
		TurnOutOfWraith(true);
	}
	
	/// <summary>
	/// Turn in Wraith Change Model and Start Timer for Reverting.
	/// If Already in Wraith Form Restart Timer Only.
	/// </summary>
	public virtual void TurnInWraith()
	{
		if (Player == null)
			return;
		
		if (!m_wraithTimerAction.IsAlive)
		{
			Player.Model = Player.Race switch
			{
				11 => 1885,//Elf
				12 => 1884,//Lurikeen
				_ => 1883,//Celt
			};

			GameEventMgr.AddHandler(Player, GameObjectEvent.RemoveFromWorld, m_wraithTriggerEvent);
		}
		
		m_wraithTimerAction.Start(WRAITH_FORM_RESET_DELAY);
	}

	/// <summary>
	/// Turn out of Wraith.
	/// Stop Timer and Remove Event Handlers.
	/// </summary>
	public void TurnOutOfWraith()
	{
		TurnOutOfWraith(false);
	}
	
	/// <summary>
	/// Turn out of Wraith.
	/// Stop Timer and Remove Event Handlers.
	/// </summary>
	public virtual void TurnOutOfWraith(bool forced)
	{
		if (Player == null)
			return;

		// Keep Wraith Form if Pulsing Offensive Spell Running
		//if (!forced && Player.ConcentrationEffects.OfType<PulsingSpellEffect>().Any(pfx => pfx.SpellHandler != null && !pfx.SpellHandler.HasPositiveEffect))
		//{
		//	TurnInWraith();
		//	return;
		//}
		
		m_wraithTimerAction.Stop();
		GameEventMgr.RemoveHandler(Player, GameObjectEvent.RemoveFromWorld, m_wraithTriggerEvent);
		Player.Model = (ushort)Player.Client.Account.Characters[Player.Client.ActiveCharIndex].CreationModel;
	}

	public override List<PlayerRace> EligibleRaces => new()
	{
		// PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen,
	};
}
#endregion