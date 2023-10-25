using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

public class BoligarBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BoligarBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			TeleportTarget = null;
			IsTargetTeleported = false;
			if (Port_Enemys.Count > 0)
				Port_Enemys.Clear();
		}
		if (HasAggro && Body.TargetObject != null)
		{
			if (IsTargetTeleported == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickTeleportPlayer), Util.Random(25000, 45000));
				IsTargetTeleported = true;
			}
			GameLiving target = Body.TargetObject as GameLiving;
			if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Mez))
				Body.CastSpell(Boligar_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		base.Think();
	}
	public static bool IsTargetTeleported = false;
	#region Pick player to port
	public static GamePlayer teleporttarget = null;
	public static GamePlayer TeleportTarget
	{
		get { return teleporttarget; }
		set { teleporttarget = value; }
	}
	List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	public int PickTeleportPlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(1500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Port_Enemys.Contains(player))
						{
							if (player != Body.TargetObject)
								Port_Enemys.Add(player);
						}
					}
				}
			}
			if (Port_Enemys.Count == 0)
			{
				TeleportTarget = null;//reset random target to null
				IsTargetTeleported = false;
			}
			else
			{
				if (Port_Enemys.Count > 0)
				{
					GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					TeleportTarget = Target;
					if (TeleportTarget.IsAlive && TeleportTarget != null)
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), 3000);
				}
			}
		}
		return 0;
	}
	public int TeleportPlayer(EcsGameTimer timer)
	{
		if (TeleportTarget.IsAlive && TeleportTarget != null && HasAggro)
		{
			switch (Util.Random(1, 4))
			{
				case 1: TeleportTarget.MoveTo(151, 409989, 378960, 7752, 3040); break;
				case 2: TeleportTarget.MoveTo(151, 411003, 377902, 7704, 3070); break;
				case 3: TeleportTarget.MoveTo(151, 409983, 376894, 7376, 3097); break;
				case 4: TeleportTarget.MoveTo(151, 408968, 377920, 7440, 3047); break;
			}
			Port_Enemys.Remove(TeleportTarget);
			TeleportTarget = null;//reset random target to null
			IsTargetTeleported = false;
		}
		return 0;
	}
	#endregion

	private Spell m_Boligar_Mezz;
	private Spell Boligar_Mezz
	{
		get
		{
			if (m_Boligar_Mezz == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 60;
				spell.Duration = 72;
				spell.ClientEffect = 2619;
				spell.Icon = 2619;
				spell.Name = "Mesmerize";
				spell.TooltipId = 2619;
				spell.Radius = 450;
				spell.Range = 450;
				spell.SpellID = 11884;
				spell.Target = "Enemy";
				spell.Type = "Mesmerize";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Boligar_Mezz = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boligar_Mezz);
			}
			return m_Boligar_Mezz;
		}
	}
}