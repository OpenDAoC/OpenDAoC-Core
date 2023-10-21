using Core.Database;
using Core.Database.Tables;
using Core.GS;

namespace Core.AI.Brain;

public class RotoddjurBrain : StandardMobBrain
{
	public RotoddjurBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 500;
	}
	public static bool IsPulled = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if(!CheckProximityAggro())
        {
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is RotoddjurAddBrain)
						{
							npc.RemoveFromWorld();
						}
					}
				}
				RemoveAdds = true;
			}
		}
		if(HasAggro && Body.TargetObject != null)
        {
			RemoveAdds = false;
			GameLiving target = Body.TargetObject as GameLiving;
			if (Util.Chance(25) && target != null)
			{
				if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				{
					Body.CastSpell(RotodddjurDot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			if(IsPulled==false)
            {
				SpawnMushrooms();
				IsPulled = true;
            }
		}
		base.Think();
	}
	public void SpawnMushrooms()
	{
		for (int i = 0; i < Util.Random(4, 5); i++)
		{
			RotoddjurAdd add = new RotoddjurAdd();
			add.X = Body.X + Util.Random(-100, 100);
			add.Y = Body.Y + Util.Random(-100, 100);
			add.Z = Body.Z;
			add.CurrentRegion = Body.CurrentRegion;
			add.Heading = Body.Heading;
			add.RespawnInterval = -1;
			add.AddToWorld();
		}
	}
	private Spell m_RotodddjurDot;
	private Spell RotodddjurDot
	{
		get
		{
			if (m_RotodddjurDot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 3475;
				spell.Icon = 3475;
				spell.TooltipId = 3475;
				spell.Name = "Orm Poison";
				spell.Description = "Inflicts 70 damage to the target every 3 sec for 30 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.Damage = 80;
				spell.Duration = 30;
				spell.Frequency = 30;
				spell.Range = 500;
				spell.SpellID = 11855;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.DamageType = (int)EDamageType.Body;
				spell.Uninterruptible = true;
				m_RotodddjurDot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RotodddjurDot);
			}
			return m_RotodddjurDot;
		}
	}
}

#region Rotoddjur adds
public class RotoddjurAddBrain : StandardMobBrain
{
	public RotoddjurAddBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Rotoddjur adds