using DOL.Database;
using DOL.GS;

namespace DOL.AI.Brain;

#region Badb
public class BadbBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BadbBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	public override void Think()
	{
		if (Body.CurrentRegion.IsNightTime == false)
		{
			if (changed == false)
			{
				oldFlags = Body.Flags;
				Body.Flags ^= ENpcFlags.CANTTARGET;
				Body.Flags ^= ENpcFlags.DONTSHOWNAME;
				Body.Flags ^= ENpcFlags.PEACE;

				if (oldModel == 0)
					oldModel = Body.Model;

				Body.Model = 1;
				foreach (GameNpc adds in Body.GetNPCsInRadius(8000))
				{
					if (adds != null && adds.IsAlive && adds.Brain is BadbWraithBrain)
						adds.RemoveFromWorld();
				}
				changed = true;
			}
		}
		if (Body.CurrentRegion.IsNightTime)
		{
			if (changed)
			{
				Body.Flags = oldFlags;
				Body.Model = oldModel;
				CreateWraiths();
				changed = false;
			}
		}
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if(HasAggro && Body.TargetObject != null)
        {
			foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is BadbWraithBrain brain)
				{
					GameLiving target = Body.TargetObject as GameLiving;
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}
			if (Util.Chance(50) && !Body.IsCasting)
				Body.CastSpell(BadbDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
		}
		base.Think();
	}
	#region Create Wraiths
	private void CreateWraiths()
	{
		for (int i = 0; i < 3; i++)
		{
			BadbWraith wraith = new BadbWraith();
			wraith.X = 384497 + Util.Random(-100, 100);
			wraith.Y = 745145 + Util.Random(-100, 100);
			wraith.Z = 4888;
			wraith.Heading = 3895;
			wraith.CurrentRegion = Body.CurrentRegion;
			wraith.AddToWorld();
		}

		for (int i = 0; i < 3; i++)
		{
			BadbWraith wraith = new BadbWraith();
			wraith.X = 383922 + Util.Random(-100, 100);
			wraith.Y = 745175 + Util.Random(-100, 100);
			wraith.Z = 4888;
			wraith.Heading = 669;
			wraith.CurrentRegion = Body.CurrentRegion;
			wraith.AddToWorld();
		}

		for (int i = 0; i < 3; i++)
		{
			BadbWraith wraith = new BadbWraith();
			wraith.X = 384146 + Util.Random(-100, 100);
			wraith.Y = 744564 + Util.Random(-100, 100);
			wraith.Z = 4888;
			wraith.Heading = 1859;
			wraith.CurrentRegion = Body.CurrentRegion;
			wraith.AddToWorld();
		}

		for (int i = 0; i < 3; i++)
		{
			BadbWraith wraith = new BadbWraith();
			wraith.X = 384677 + Util.Random(-100, 100);
			wraith.Y = 744788 + Util.Random(-100, 100);
			wraith.Z = 4888;
			wraith.Heading = 2621;
			wraith.CurrentRegion = Body.CurrentRegion;
			wraith.AddToWorld();
		}
	}
	#endregion
	#region Spells
	private Spell m_BadbDD;
	public Spell BadbDD
	{
		get
		{
			if (m_BadbDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.Power = 0;
				spell.RecastDelay = 10;
				spell.ClientEffect = 13533;
				spell.Icon = 13533;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Cold;
				spell.Name = "Voices of Pain";
				spell.Range = 1500;
				spell.SpellID = 11874;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_BadbDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BadbDD);
			}
			return m_BadbDD;
		}
	}
	#endregion
}
#endregion Badb

public class BadbWraithBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BadbWraithBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}