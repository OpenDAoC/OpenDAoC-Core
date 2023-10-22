using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI.Brains;

#region Valnir Mordeth
public class ValnirMordethBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public ValnirMordethBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool CanSpawnMoreGhouls = false;
	private bool spawnAddsAfterCombat = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnMoreGhouls = false;
			if(spawnAddsAfterCombat == false)
            {
				SpawnAddsAfterCombat();
				spawnAddsAfterCombat = true;
			}
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ValnirMordethAddBrain && npc.PackageID == "MordethAdds")
						npc.Die(Body);
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			spawnAddsAfterCombat = false;
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.PackageID == "MordethBaf")
					AddAggroListTo(npc.Brain as StandardMobBrain);
			}
			GameLiving target = Body.TargetObject as GameLiving;
			if (target != null)
			{
				if (Body.IsCasting)
				{
					if (Body.attackComponent.AttackState)
						Body.attackComponent.StopAttack();
					if (Body.IsMoving)
						Body.StopFollowing();
				}
				Body.TurnTo(Body.TargetObject);
				if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
					Body.CastSpell(Valnir_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
					Body.CastSpell(ValnirDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			if(CanSpawnMoreGhouls == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdds), Util.Random(25000, 35000));
				CanSpawnMoreGhouls = true;
            }
		}
		base.Think();
	}
	private int SpawnAdds(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro && ValnirMordethAdd.EssenceGhoulCount == 0)
		{
			ValnirMordethAdd add = new ValnirMordethAdd();
			add.X = Body.X + Util.Random(-100, 100);
			add.Y = Body.Y + Util.Random(-100, 100);
			add.Z = Body.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.PackageID = "MordethAdds";
			add.AddToWorld();
		}
		CanSpawnMoreGhouls = false;
		return 0;
	}
	private void SpawnAddsAfterCombat()
    {
		if (!HasAggro && ValnirMordethAdd.EssenceGhoulCount == 0)
		{
			for (int i = 0; i < Util.Random(2, 3); i++)
			{
				ValnirMordethAdd add = new ValnirMordethAdd();
				add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
				add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
				add.Z = Body.SpawnPoint.Z;
				add.Heading = Body.Heading;
				add.CurrentRegion = Body.CurrentRegion;
				add.PackageID = "MordethBaf";
				add.AddToWorld();
			}
		}
	}
	#region Spells
	private Spell m_Valnir_Dot;
	private Spell Valnir_Dot
	{
		get
		{
			if (m_Valnir_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 562;
				spell.Icon = 562;
				spell.Name = "Valnir Modreth's Breath";
				spell.Description = "Inflicts 80 damage to the target every 4 sec for 40 seconds";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 562;
				spell.Range = 1500;
				spell.Damage = 80;
				spell.Duration = 40;
				spell.Frequency = 40;
				spell.SpellID = 11903;
				spell.Target = "Enemy";
				spell.SpellGroup = 1800;
				spell.EffectGroup = 1500;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Valnir_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Valnir_Dot);
			}
			return m_Valnir_Dot;
		}
	}
	private Spell m_ValnirDisease;
	private Spell ValnirDisease
	{
		get
		{
			if (m_ValnirDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 40;
				spell.ClientEffect = 731;
				spell.Icon = 731;
				spell.Name = "Valnir Mordeth's Plague";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 731;
				spell.Range = 1500;
				spell.Duration = 120;
				spell.SpellID = 11904;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_ValnirDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirDisease);
			}
			return m_ValnirDisease;
		}
	}
    #endregion
}
#endregion Valnir Mordeth

#region Mordeth adds
public class ValnirMordethAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public ValnirMordethAddBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
		{
			if (Body.IsCasting)
			{
				if (Body.attackComponent.AttackState)
					Body.attackComponent.StopAttack();
				if (Body.IsMoving)
					Body.StopFollowing();
			}
			Body.TurnTo(Body.TargetObject);
			Body.CastSpell(ValnirAddLifeDrain, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			
		}
		base.Think();
	}
	private Spell m_ValnirAddLifeDrain;
	private Spell ValnirAddLifeDrain
	{
		get
		{
			if (m_ValnirAddLifeDrain == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = 0;
				spell.ClientEffect = 14352;
				spell.Icon = 14352;
				spell.TooltipId = 14352;
				spell.Damage = 350;
				spell.Name = "Lifedrain";
				spell.Range = 1800;
				spell.SpellID = 11902;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Body;
				m_ValnirAddLifeDrain = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ValnirAddLifeDrain);
			}
			return m_ValnirAddLifeDrain;
		}
	}
}
#endregion Mordeth adds