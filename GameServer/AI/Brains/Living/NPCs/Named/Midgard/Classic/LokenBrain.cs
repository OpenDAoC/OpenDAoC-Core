using Core.Database;
using Core.Database.Tables;
using Core.GS;

namespace Core.AI.Brain;

public class LokenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public LokenBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
		ThinkInterval = 1500;
	}
	private bool SpawnWolf = false;
	public override void Think()
	{
		if(Body.IsAlive)
        {
			if (!Body.Spells.Contains(LokenDD))
				Body.Spells.Add(LokenDD);
			if (!Body.Spells.Contains(LokenBolt))
				Body.Spells.Add(LokenBolt);
		}
		if(!CheckProximityAggro())
        {
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163372);
			Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			if (LokenWolf.WolfsCount < 2 && !SpawnWolf)
            {
				SpawnWolfs();
				SpawnWolf = true;
            }
        }
		if(HasAggro && Body.TargetObject != null)
        {
			SpawnWolf = false;
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is LokenWolfBrain brian)
				{
					if (!brian.HasAggro && brian != null && target != null && target.IsAlive)
						brian.AddToAggroList(target, 10);
				}
			}
			if(Util.Chance(100) && !Body.IsCasting)
				Body.CastSpell(LokenDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		if (Body.IsOutOfTetherRange && Body.TargetObject != null)
		{
			Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
			GameLiving target = Body.TargetObject as GameLiving;
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163372);
			if (target != null)
			{
				if (!target.IsWithinRadius(spawn, Body.TetherRange))
				{
					Body.MaxSpeedBase = 0;
				}
				else
					Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			}
		}
		base.Think();
	}
	private void SpawnWolfs()
	{
		Point3D spawn = new Point3D(636780, 762427, 4597);
		for (int i = 0; i < 2; i++)
		{
			if (LokenWolf.WolfsCount < 2)
			{
				LokenWolf npc = new LokenWolf();
				npc.X = spawn.X + Util.Random(-100, 100);
				npc.Y = spawn.Y + Util.Random(-100, 100);
				npc.Z = spawn.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
	#region Spells
	private Spell m_LokenDD;
	private Spell LokenDD
	{
		get
		{
			if (m_LokenDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.Damage = 280;
				spell.Duration = 30;
				spell.Value = 20;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Description = "Damages the target and lowers their resistance to Heat by 20%";
				spell.Name = "Searing Blast";
				spell.Range = 1500;
				spell.SpellID = 12001;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageWithDebuff.ToString();
				m_LokenDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenDD);
			}
			return m_LokenDD;
		}
	}
	private Spell m_LokenDD2;
	private Spell LokenDD2
	{
		get
		{
			if (m_LokenDD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = Util.Random(5,10);
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.Damage = 280;
				spell.Duration = 30;
				spell.Value = 20;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Description = "Damages the target and lowers their resistance to Heat by 20%";
				spell.Name = "Searing Blast";
				spell.Range = 1500;
				spell.SpellID = 12002;
				spell.Target = "Enemy";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.Type = ESpellType.DirectDamageWithDebuff.ToString();
				m_LokenDD2 = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenDD2);
			}
			return m_LokenDD2;
		}
	}
	private Spell m_LokenBolt;
	private Spell LokenBolt
	{
		get
		{
			if (m_LokenBolt == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = Util.Random(15,20);
				spell.ClientEffect = 378;
				spell.Icon = 378;
				spell.Damage = 200;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Flame Spear";
				spell.Range = 1800;
				spell.SpellID = 12003;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Bolt.ToString();
				m_LokenBolt = new Spell(spell, 60);
				spell.Uninterruptible = true;
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LokenBolt);
			}
			return m_LokenBolt;
		}
	}
	#endregion
}

#region Loken wolves
public class LokenWolfBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public LokenWolfBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Loken wolves