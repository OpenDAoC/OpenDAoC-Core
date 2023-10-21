using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

#region Deben se Gecynde
public class DebenSeGecyndeBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DebenSeGecyndeBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool spawnadds = false;
	private bool IsPulled = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			spawnadds = false;
			IsPulled = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && (npc.Brain is DebenFighterBrain || npc.Brain is DebenMageBrain))
							npc.RemoveFromWorld();
					}
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if(IsPulled==false)
            {
				foreach(GameNpc npc in Body.GetNPCsInRadius(2500))
                {
					if(npc != null)
                    {
						if(npc.IsAlive && npc.PackageID == "DebenBaf")
                        {
							AddAggroListTo(npc.Brain as StandardMobBrain);
							IsPulled = true;
                        }
                    }
                }
            }
			if (spawnadds == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdds), Util.Random(45000, 70000));
				spawnadds = true;
			}
		}
		base.Think();
	}
	public int SpawnAdds(EcsGameTimer timer)
    {
		if (HasAggro)
		{
			for (int i = 0; i < Util.Random(1, 3); i++)
			{
				DebenFighter npc = new DebenFighter();
				npc.X = Body.X + Util.Random(-100, 100);
				npc.Y = Body.Y + Util.Random(-100, 100);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.RespawnInterval = -1;
				npc.AddToWorld();
			}
			for (int i = 0; i < Util.Random(1, 3); i++)
			{
				DebenMage npc = new DebenMage();
				npc.X = Body.X + Util.Random(-100, 100);
				npc.Y = Body.Y + Util.Random(-100, 100);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.RespawnInterval = -1;
				npc.AddToWorld();
			}
		}
		spawnadds = false;
		return 0;
    }
}
#endregion Deben se Gecynde

#region Deben Soldiers
public class DebenFighterBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DebenFighterBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Deben Soldiers

#region Deben Mages
public class DebenMageBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DebenMageBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
        {
			if (!Body.IsCasting && !Body.IsBeingInterrupted)
			{
				if (Body.attackComponent.AttackState)
					Body.attackComponent.StopAttack();
				if (Body.IsMoving)
					Body.StopFollowing();
				Body.TurnTo(Body.TargetObject);
				switch (Util.Random(1, 2))
				{
					case 1: Body.CastSpell(Mage_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
					case 2: Body.CastSpell(Mage_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
				}
			}
        }
		base.Think();
	}
    #region Spells
    private Spell m_Mage_DD;
	private Spell Mage_DD
	{
		get
		{
			if (m_Mage_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.TooltipId = 360;
				spell.Damage = 300;
				spell.Name = "Major Conflagration";
				spell.Range = 1500;
				spell.SpellID = 11883;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Heat;
				m_Mage_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mage_DD);
			}
			return m_Mage_DD;
		}
	}
	private Spell m_Mage_DD2;
	private Spell Mage_DD2
	{
		get
		{
			if (m_Mage_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 161;
				spell.Icon = 161;
				spell.TooltipId = 161;
				spell.Damage = 300;
				spell.Name = "Major Ice Blast";
				spell.Range = 1500;
				spell.SpellID = 11884;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Cold;
				m_Mage_DD2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mage_DD2);
			}
			return m_Mage_DD2;
		}
	}
    #endregion
}
#endregion Deben Mages