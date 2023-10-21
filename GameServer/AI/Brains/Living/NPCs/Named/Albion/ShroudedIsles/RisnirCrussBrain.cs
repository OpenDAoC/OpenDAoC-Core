using Core.Database;
using Core.Database.Tables;
using Core.GS;

namespace Core.AI.Brain;

public class RisnirCrussBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public RisnirCrussBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool NotInCombat = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			if(Body.Flags != ENpcFlags.FLYING)
				Body.Flags = ENpcFlags.FLYING;
			if (NotInCombat == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
				NotInCombat = true;
			}
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "RisnirCrussAdd")
						npc.Die(Body);
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			NotInCombat = false;
			if (Body.Flags != 0)
				Body.Flags = 0;
			if (!Body.IsCasting && Util.Chance(30))
			{
				Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
				Body.CastSpell(Boss_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			if (!Body.IsCasting && Util.Chance(20))
			{
				Body.CastSpell(Boss_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
		}
		base.Think();
	}

	protected int Show_Effect(EcsGameTimer timer)
	{
		if (Body.IsAlive && !HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				player.Out.SendSpellEffectAnimation(Body, Body, 6085, 0, false, 0x01);

			return 3000;
		}

		return 0;
	}

	private Spell m_Boss_PBAOE;
	private Spell Boss_PBAOE
	{
		get
		{
			if (m_Boss_PBAOE == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(20, 25);
				spell.ClientEffect = 1695;
				spell.Icon = 1695;
				spell.TooltipId = 1695;
				spell.Name = "Thunder Stomp";
				spell.Damage = 650;
				spell.Range = 500;
				spell.Radius = 1000;
				spell.SpellID = 11898;
				spell.Target = ESpellTarget.AREA.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Energy;
				spell.Uninterruptible = true;
				m_Boss_PBAOE = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_PBAOE);
			}
			return m_Boss_PBAOE;
		}
	}
	private Spell m_Boss_Mezz;
	private Spell Boss_Mezz
	{
		get
		{
			if (m_Boss_Mezz == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 40;
				spell.Duration = 60;
				spell.ClientEffect = 975;
				spell.Icon = 975;
				spell.Name = "Mesmerize";
				spell.Description = "Target is mesmerized and cannot move or take any other action for the duration of the spell. If the target suffers any damage or other negative effect the spell will break.";
				spell.TooltipId = 2619;
				spell.Radius = 450;
				spell.Range = 1500;
				spell.SpellID = 18914;
				spell.Target = "Enemy";
				spell.Type = "Mesmerize";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Boss_Mezz = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Mezz);
			}
			return m_Boss_Mezz;
		}
	}
}