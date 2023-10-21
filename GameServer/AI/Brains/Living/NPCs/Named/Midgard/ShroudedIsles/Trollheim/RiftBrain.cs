using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

public class RiftBrain : StandardMobBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public RiftBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}

	public static bool IsPulled = false;
	public static bool IsValkyn = false;
	public static bool IsRift = false;

	public void ChangeAppearance()
	{
		if (!HasAggro && Body.IsAlive && IsValkyn == false)
		{
			Body.Name = "Trollkarl Bonchar";
			Body.Model = 874;
			Body.Level = 65;
			Body.Size = 81;
			Body.MaxSpeedBase = 250;
			Body.Strength = 320;
			Body.Dexterity = 150;
			Body.Constitution = 100;
			Body.Quickness = 100;
			Body.Piety = 150;
			Body.Intelligence = 150;
			Body.Empathy = 300;
			Body.MeleeDamageType = EDamageType.Crush;
			Body.Flags = 0;
			IsValkyn = true;
		}

		if (HasAggro && Body.IsAlive && IsRift == false)
		{
			Body.Name = "The Rift";
			Body.Model = 2049;
			Body.Level = 70;
			Body.Size = 80;
			Body.MaxSpeedBase = 280;
			Body.Strength = 320;
			Body.Dexterity = 150;
			Body.Constitution = 100;
			Body.Quickness = 100;
			Body.Piety = 150;
			Body.Intelligence = 150;
			Body.Empathy = 300;
			Body.MeleeDamageType = EDamageType.Energy;
			Body.Flags = ENpcFlags.DONTSHOWNAME;
			IsRift = true;
		}
	}

	private bool RemoveAdds = false;

	public override void Think()
	{
		if (Body.IsAlive)
			ChangeAppearance();

		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
			IsRift = false;
			SpawnMoreAdds = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is MorkenhetBrain)
						{
							npc.RemoveFromWorld();
						}
					}
				}

				RemoveAdds = true;
			}
		}

		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			IsValkyn = false;
			if (IsRift)
			{
				Body.CastSpell(RiftDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (SpawnMoreAdds == false)
				{
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdds),
						Util.Random(10000, 20000)); //10-20s spawn add
					SpawnMoreAdds = true;
				}
			}
		}

		base.Think();
	}

	public static bool SpawnMoreAdds = false;

	public int SpawnAdds(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro && IsRift)
		{
			Morkenhet Add1 = new Morkenhet();
			Add1.X = Body.X + Util.Random(-100, 100);
			Add1.Y = Body.Y + Util.Random(-100, 100);
			Add1.Z = Body.Z;
			Add1.CurrentRegion = Body.CurrentRegion;
			Add1.Heading = Body.Heading;
			Add1.RespawnInterval = -1;
			Add1.AddToWorld();
		}

		SpawnMoreAdds = false;
		return 0;
	}

	private Spell m_RiftDD;

	private Spell RiftDD
	{
		get
		{
			if (m_RiftDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(6, 10);
				spell.ClientEffect = 3510;
				spell.Icon = 3510;
				spell.TooltipId = 3510;
				spell.Name = "Rift Strike";
				spell.Damage = 300;
				spell.Range = 1500;
				spell.SpellID = 11852;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.DamageType = (int)EDamageType.Energy;
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_RiftDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RiftDD);
			}

			return m_RiftDD;
		}
	}
}

#region Rift adds
public class MorkenhetBrain : StandardMobBrain
{
	public MorkenhetBrain()
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
#endregion Rift adds