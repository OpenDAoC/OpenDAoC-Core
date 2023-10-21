using System.Collections;
using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;

namespace Core.GS.AI.Brains;

public class IarnvidiurBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public IarnvidiurBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
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
		if (Body.IsAlive)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
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
					{							
						Body.TargetObject = TeleportTarget; //set target to randomly picked
						Body.TurnTo(TeleportTarget,4000);
						if(!Body.IsCasting && Body.GetSkillDisabledDuration(Iarnvidiur_Bolt) == 0)
							Body.CastSpell(Iarnvidiur_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportPlayer), 3000);
					}
				}
			}
		}
		return 0;
	}
	public int TeleportPlayer(EcsGameTimer timer)
    {
		GamePlayer oldTarget = (GamePlayer)Body.TargetObject; //old target
		if (TeleportTarget.IsAlive && TeleportTarget != null)
		{
			switch (Util.Random(1, 4))
			{
				case 1: TeleportTarget.MoveTo(161, 12563, 13450, 18537, 2997); break;
				case 2: TeleportTarget.MoveTo(161, 15212, 13331, 18537, 920); break;
				case 3: TeleportTarget.MoveTo(161, 16958, 13269, 18537, 988); break;
				case 4: TeleportTarget.MoveTo(161, 11829, 13530, 18537, 2985); break;
			}
			Port_Enemys.Remove(TeleportTarget);
			if (oldTarget != null) Body.TargetObject = oldTarget; //return to old target
			Body.StartAttack(oldTarget); //start attack old target
			TeleportTarget = null;//reset random target to null
			IsTargetTeleported = false;
		}
		return 0;
    }
	#endregion
	#region DD or Dot random player
	public static bool IsTargetPicked = false;
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	public int PickPlayerToDD(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro)
		{
			IList enemies = new ArrayList(AggroTable.Keys);
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!AggroTable.ContainsKey(player))
							AggroTable.Add(player, 1);
					}
				}
			}

			if (enemies.Count == 0)
			{
				/*do nothing*/
			}
			else
			{
				List<GameLiving> damage_enemies = new List<GameLiving>();
				for (int i = 0; i < enemies.Count; i++)
				{
					if (enemies[i] == null)
						continue;
					if (!(enemies[i] is GameLiving))
						continue;
					if (!(enemies[i] as GameLiving).IsAlive)
						continue;
					GameLiving living = null;
					living = enemies[i] as GameLiving;
					if (living.IsVisibleTo(Body) && Body.TargetInView && living is GamePlayer)
					{
						damage_enemies.Add(enemies[i] as GameLiving);
					}
				}

				if (damage_enemies.Count > 0)
				{
					GamePlayer Target = (GamePlayer)damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
					RandomTarget = Target; //randomly picked target is now RandomTarget
					if (RandomTarget != null && RandomTarget.IsAlive)
					{
						GameLiving oldTarget = Body.TargetObject as GameLiving; //old target
						Body.TargetObject = RandomTarget; //set target to randomly picked
						Body.TurnTo(RandomTarget);
						switch (Util.Random(1, 2)) //pick one of 2 spells to cast
						{
							case 1: Body.CastSpell(Iarnvidiur_Dot,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break; //dot
							case 2: Body.CastSpell(IarnvidiurDD,SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break; //dd
						}							
						if (oldTarget != null) Body.TargetObject = oldTarget; //return to old target
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDD), 3000);
					}
				}
			}
		}
		return 0;
	}
	public int ResetDD(EcsGameTimer timer)
    {
		RandomTarget = null; //reset random target to null
		IsTargetPicked = false;
		return 0;
    }
    #endregion
    public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsTargetPicked = false;
			RandomTarget = null;
		}
		if (Body.InCombat && HasAggro)
		{
			if(Body.TargetObject != null)
            {
				if (Util.Chance(15))
				{
					GameLiving target = Body.TargetObject as GameLiving;
					if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastDisease), 1000);
					}
				}
				if (Util.Chance(15))
				{
					if (IsTargetPicked==false)
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickPlayerToDD), Util.Random(15000, 25000));
						IsTargetPicked = true;
					}
				}
			}
			if(Body.HealthPercent < 50)
            {
				if (IsTargetTeleported == false)
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickTeleportPlayer), Util.Random(25000, 45000));
					IsTargetTeleported = true;
				}
			}
		}
		base.Think();
	}
	public int CastDisease(EcsGameTimer timer)
	{
		if (Body.TargetObject != null && HasAggro && Body.IsAlive)
		{
			Body.CastSpell(IarnvidiurDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	#region Spells
	private Spell m_Iarnvidiur_Dot;
	private Spell Iarnvidiur_Dot
	{
		get
		{
			if (m_Iarnvidiur_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.Duration = 40;
				spell.Frequency = 40;
				spell.ClientEffect = 3475;
				spell.Icon = 3475;
				spell.TooltipId = 3475;
				spell.Name = "Iarnvidiur's Plague";
				spell.Message1 = "Your body is covered with painful sores!";
				spell.Message2 = "{0}'s skin erupts in open wounds!";
				spell.Message3 = "The destructive energy wounding you fades.";
				spell.Message4 = "The destructive energy around {0} fades.";
				spell.Damage = 100;
				spell.Range = 1500;
				spell.Radius = 700;
				spell.SpellID = 11828;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				m_Iarnvidiur_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Iarnvidiur_Dot);
			}
			return m_Iarnvidiur_Dot;
		}
	}
	private Spell m_IarnvidiurDisease;
	private Spell IarnvidiurDisease
	{
		get
		{
			if (m_IarnvidiurDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 120;
				spell.ClientEffect = 4375;
				spell.Icon = 4375;
				spell.Name = "Black Plague";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 4375;
				spell.Radius = 850;
				spell.Range = 0;
				spell.Duration = 600;
				spell.SpellID = 11829;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_IarnvidiurDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IarnvidiurDisease);
			}
			return m_IarnvidiurDisease;
		}
	}
	private Spell m_Iarnvidiur_Bolt;

	private Spell Iarnvidiur_Bolt
	{
		get
		{
			if (m_Iarnvidiur_Bolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 2;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4559;
				spell.Icon = 4559;
				spell.TooltipId = 4559;
				spell.Damage = 250;
				spell.Name = "Plague Bolt";
				spell.Range = 2500;
				spell.SpellID = 11830;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.Bolt.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Cold;
				m_Iarnvidiur_Bolt = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Iarnvidiur_Bolt);
			}

			return m_Iarnvidiur_Bolt;
		}
	}
	private Spell m_IarnvidiurDD;
	private Spell IarnvidiurDD
	{
		get
		{
			if (m_IarnvidiurDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 479;
				spell.Icon = 479;
				spell.Name = "Iarnvidiur's Strike";
				spell.TooltipId = 479;
				spell.Damage = 400;
				spell.Range = 1500;
				spell.SpellID = 11831;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_IarnvidiurDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IarnvidiurDD);
			}
			return m_IarnvidiurDD;
		}
	}
    #endregion
}