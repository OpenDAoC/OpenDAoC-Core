using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.PacketHandler;

namespace Core.AI.Brain;

#region Melancholic Fairy Queen
public class MelancholicFairyQueenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MelancholicFairyQueenBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
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
					if (adds != null && adds.IsAlive && adds.Brain is FairyQueenGuardBrain)
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
				BroadcastMessage("You hear the sound of trumpets in the distance.");
				CreateFairyGuards();
				changed = false;
			}
		}
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			Body.MaxSpeedBase = 250;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is FairyQueenGuardBrain brain)
				{
					GameLiving target = Body.TargetObject as GameLiving;
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}
			if (Util.Chance(50) && !Body.IsCasting)
				Body.CastSpell(MFQDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		}
		base.Think();
	}
	private void CreateFairyGuards()
	{
		for (int i = 0; i < 5; i++)
		{
			FairyQueenGuards guards = new FairyQueenGuards();
			guards.X = Body.X + Util.Random(-500, 500);
			guards.Y = Body.Y + Util.Random(-500, 500);
			guards.Z = Body.Z;
			guards.Heading = Body.Heading;
			guards.CurrentRegion = Body.CurrentRegion;
			guards.AddToWorld();
		}
	}
	#region Spell
	private Spell m_MFQDD;
	public Spell MFQDD
	{
		get
		{
			if (m_MFQDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 8;
				spell.Power = 0;
				spell.ClientEffect = 4111;
				spell.Icon = 4111;
				spell.Damage = 400;
				spell.DamageType = (int)EDamageType.Heat;
				spell.Name = "Heat Beam";
				spell.Range = 1500;
				spell.SpellID = 11896;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_MFQDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_MFQDD);
			}
			return m_MFQDD;
		}
	}
    #endregion
}
#endregion Melancholic Fairy Queen

#region Melancholic Fairy Queen Guards
public class FairyQueenGuardBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public FairyQueenGuardBrain() : base()
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
#endregion Melancholic Fairy Queen Guards