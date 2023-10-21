using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Cailean
public class CaileanBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CaileanBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	private bool CanSpawnTree = false;
	private bool RemoveTrees = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnTree = false;
			if (!RemoveTrees)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is WalkingTreeBrain)
						npc.RemoveFromWorld();
				}
				foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is WalkingTree2Brain)
						npc.RemoveFromWorld();
				}
				RemoveTrees = true;
			}
		}
		if(HasAggro && Body.TargetObject != null)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			RemoveTrees = false;
			if (!CanSpawnTree)
            {
				SpawnTree();
				SpawnTree2();
				CanSpawnTree = true;
            }
			if(Body.TargetObject != null)
            {					
				if(Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff))
					Body.CastSpell(TreeRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MezImmunity) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Mez))
					Body.CastSpell(BossMezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
			{
				if (npc != null && npc.IsAlive)
				{
					if (npc.Brain is WalkingTreeBrain brain)
					{
						if (brain != null && target != null && !brain.HasAggro && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
					if (npc.Brain is WalkingTree2Brain brain2)
					{
						if (brain2 != null && target != null && !brain2.HasAggro && target.IsAlive)
							brain2.AddToAggroList(target, 10);
					}
				}
			}
		}
		if (Body.HealthPercent <= 30)
			Body.CastSpell(CaileanHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

		base.Think();
	}
    #region Spawn Trees
    private void SpawnTree()
    {
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is WalkingTreeBrain)
				return;
		}
		for (int i = 0; i < Util.Random(4, 5); i++)
		{
			WalkingTree tree = new WalkingTree();
			tree.X = Body.X + Util.Random(-500, 500);
			tree.Y = Body.Y + Util.Random(-500, 500);
			tree.Z = Body.Z;
			tree.Heading = Body.Heading;
			tree.CurrentRegion = Body.CurrentRegion;
			tree.AddToWorld();
		}
	}
	private void SpawnTree2()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is WalkingTree2Brain)
				return;
		}
		Point3D point1 = new Point3D(479778, 508293, 4534);
		Point3D point2 = new Point3D(478647, 508450, 4639);
		Point3D point3 = new Point3D(479444, 508548, 4532);
		for (int i = 0; i < Util.Random(8, 10); i++)
		{
			WalkingTree2 tree = new WalkingTree2();
			switch(Util.Random(1,3))
            {
				case 1:
					tree.X = point1.X + Util.Random(-200, 200);
					tree.Y = point1.Y + Util.Random(-200, 200);
					tree.Z = point1.Z;
					break;
				case 2:
					tree.X = point2.X + Util.Random(-200, 200);
					tree.Y = point2.Y + Util.Random(-200, 200);
					tree.Z = point2.Z;
					break;
				case 3:
					tree.X = point3.X + Util.Random(-200, 200);
					tree.Y = point3.Y + Util.Random(-200, 200);
					tree.Z = point3.Z;
					break;
			}
			tree.Heading = Body.Heading;
			tree.CurrentRegion = Body.CurrentRegion;
			tree.AddToWorld();
		}
	}
    #endregion
    #region Spells
    private Spell m_CaileanHeal;
	private Spell CaileanHeal
	{
		get
		{
			if (m_CaileanHeal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 6;
				spell.ClientEffect = 1340;
				spell.Icon = 1340;
				spell.TooltipId = 1340;
				spell.Value = 400;
				spell.Name = "Cailean's Heal";
				spell.Range = 1500;
				spell.SpellID = 11902;
				spell.Target = "Self";
				spell.Type = ESpellType.Heal.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CaileanHeal = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CaileanHeal);
			}
			return m_CaileanHeal;
		}
    }
	private Spell m_TreeRoot;
	private Spell TreeRoot
	{
		get
		{
			if (m_TreeRoot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5208;
				spell.Icon = 5208;
				spell.TooltipId = 5208;
				spell.Value = 99;
				spell.Duration = 70;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Root";
				spell.Range = 1500;
				spell.SpellID = 11979;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpeedDecrease.ToString();
				m_TreeRoot = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
			}
			return m_TreeRoot;
		}
	}
	private Spell m_BossmezSpell;
	private Spell BossMezz
	{
		get
		{
			if (m_BossmezSpell == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5318;
				spell.Icon = 5318;
				spell.TooltipId = 5318;
				spell.Name = "Mesmerized";
				spell.Range = 1500;
				spell.SpellID = 11980;
				spell.Duration = 60;
				spell.Target = "Enemy";
				spell.Type = "Mesmerize";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Spirit; //Spirit DMG Type
				m_BossmezSpell = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BossmezSpell);
			}
			return m_BossmezSpell;
		}
	}
	#endregion
}
#endregion Cailean

#region Cailean's Trees 4-5 yellows
public class WalkingTreeBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public WalkingTreeBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if(HasAggro && Body.TargetObject != null)
        {
			GameLiving target = Body.TargetObject as GameLiving;
			if(Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff))
				Body.CastSpell(TreeRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			if (Util.Chance(20) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				Body.CastSpell(CaileanTree_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		}
		base.Think();
	}
    #region Spells
    private Spell m_TreeRoot;
	private Spell TreeRoot
	{
		get
		{
			if (m_TreeRoot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5208;
				spell.Icon = 5208;
				spell.TooltipId = 5208;
				spell.Value = 99;
				spell.Duration = 70;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Root";
				spell.Range = 1500;
				spell.SpellID = 11901;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpeedDecrease.ToString();
				m_TreeRoot = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
			}
			return m_TreeRoot;
		}
	}
	private Spell m_CaileanTree_Dot;
	private Spell CaileanTree_Dot
	{
		get
		{
			if (m_CaileanTree_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 3411;
				spell.Icon = 3411;
				spell.Name = "Poison";
				spell.Description = "Inflicts 60 damage to the target every 4 sec for 20 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 3411;
				spell.Range = 1500;
				spell.Damage = 60;
				spell.Duration = 20;
				spell.Frequency = 40;
				spell.SpellID = 11978;
				spell.Target = "Enemy";
				spell.SpellGroup = 1800;
				spell.EffectGroup = 1500;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_CaileanTree_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CaileanTree_Dot);
			}
			return m_CaileanTree_Dot;
		}
	}
    #endregion
}
#endregion Cailean's Trees 4-5 yellows

#region Cailean's Trees 8-10 blue
public class WalkingTree2Brain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public WalkingTree2Brain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			if (Util.Chance(20))
			{
				if(target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity) && target != null && target.IsAlive)
                {
					var effect = EffectListService.GetEffectOnTarget(target, EEffect.SnareImmunity);
					if(effect != null)
						EffectService.RequestImmediateCancelEffect(effect);//remove snare immunity here
				}
				if(!target.effectListComponent.ContainsEffectForEffectType(EEffect.SnareImmunity) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MovementSpeedDebuff) && target != null && target.IsAlive)
					Body.CastSpell(TreeRoot2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
		}
		base.Think();
	}
	#region Spells
	private Spell m_TreeRoot;
	private Spell TreeRoot
	{
		get
		{
			if (m_TreeRoot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 30;
				spell.ClientEffect = 5208;
				spell.Icon = 5208;
				spell.TooltipId = 5208;
				spell.Value = 99;
				spell.Duration = 70;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Root";
				spell.Range = 4500;
				spell.SpellID = 11981;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpeedDecrease.ToString();
				m_TreeRoot = new Spell(spell, 40);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot);
			}
			return m_TreeRoot;
		}
	}
	private Spell m_TreeRoot2;
	private Spell TreeRoot2
	{
		get
		{
			if (m_TreeRoot2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 10;
				spell.ClientEffect = 5208;
				spell.Icon = 5208;
				spell.TooltipId = 5208;
				spell.Value = 40;
				spell.Duration = 20;
				spell.DamageType = (int)EDamageType.Matter;
				spell.Name = "Snare";
				spell.Range = 4500;
				spell.SpellID = 11982;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpeedDecrease.ToString();
				m_TreeRoot2 = new Spell(spell, 40);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_TreeRoot2);
			}
			return m_TreeRoot2;
		}
	}
	#endregion
}
#endregion Cailean's Trees 8-10 blue