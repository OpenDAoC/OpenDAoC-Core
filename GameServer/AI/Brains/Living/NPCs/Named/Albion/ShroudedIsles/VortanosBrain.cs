using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI.Brains;

#region Vortanos
public class VortanosBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public VortanosBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;		
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	private bool CanSpawnAdds = false;
	private bool NotInCombat = false;
	private bool InCombat1 = false;
	private bool SpamMess1 = false;
	private bool RemoveAdds = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_SystemWindow);
		}
	}
    public override void OnAttackedByEnemy(AttackData ad)
    {
		if(ad != null && ad.Damage > 0 && !SpamMess1 && Util.Chance(25))
        {
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpamMessage), 1000);
			SpamMess1=true;
        }
        base.OnAttackedByEnemy(ad);
    }
	private int SpamMessage(EcsGameTimer timer)
    {
		if(HasAggro)
			BroadcastMessage(Body.Name + " says, \"The living can never conquer the eternal darkness of death incarnate!\"");

		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetSpamMessage), Util.Random(25000,45000));
		return 0;
    }
	private int ResetSpamMessage(EcsGameTimer timer)
	{
		SpamMess1 = false;
		return 0;
	}
	public override void Think()
	{
		if (Body.CurrentRegion.IsNightTime)
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
				changed = true;
			}
		}
		if (Body.CurrentRegion.IsNightTime == false)
		{
			if (changed)
			{
				Body.Flags = oldFlags;
				Body.Model = oldModel;
				changed = false;
			}
		}
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnAdds = false;
			SpamMess1 = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain)
					{
						npc.RemoveFromWorld();
						if (!NotInCombat)
						{
							BroadcastMessage(Body.Name + " says, \"Sleep my unwilling prisoners, you are no longer needed here\"");
							NotInCombat = true;
						}
					}
				}
				RemoveAdds = true;
			}
		}
		if(HasAggro && Body.TargetObject != null)
        {
			RemoveAdds = false;
			NotInCombat = false;
			if(!InCombat1)
            {
				BroadcastMessage(Body.Name + " says, \"Your flesh will be mine, one piece at a time!\"");
				InCombat1 = true;
            }
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is VortanosAddBrain brain)
				{
					if (!brain.HasAggro && target.IsAlive && target != null)
						brain.AddToAggroList(target, 10);
				}
			}
			if (Util.Chance(35) && !Body.IsCasting)
				Body.CastSpell(Vortanos_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			if (Util.Chance(35) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.DamageOverTime))
				Body.CastSpell(Vortanos_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			if(!CanSpawnAdds)
            {
				SpawnAdds();
				CanSpawnAdds = true;
            }					
		}
		base.Think();
	}
	private void SpawnAdds()
    {
		for(int i= 0; i < 2; i++)
        {
			VortanosAdd add = new VortanosAdd();
			add.Model = 1676;
			add.Name = "fell geist";
			add.Level = (byte)Util.Random(50, 56);
			add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
			add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
			add.Z = Body.SpawnPoint.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.AddToWorld();
		}
		for (int i = 0; i < 4; i++)
		{
			VortanosAdd add = new VortanosAdd();
			add.Model = 921;
			add.Name = "ancient zombie";
			add.Level = (byte)Util.Random(54, 61);
			add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
			add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
			add.Z = Body.SpawnPoint.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.AddToWorld();
		}
		for (int i = 0; i < 4; i++)
		{
			VortanosAdd add = new VortanosAdd();
			add.Model = 921;
			add.Name = "ghoul desecrator ";
			add.Level = (byte)Util.Random(49, 53);
			add.X = Body.SpawnPoint.X + Util.Random(-400, 400);
			add.Y = Body.SpawnPoint.Y + Util.Random(-400, 400);
			add.Z = Body.SpawnPoint.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.AddToWorld();
		}
	}
	#region Spells
	private Spell m_Vortanos_DD;
	private Spell Vortanos_DD
	{
		get
		{
			if (m_Vortanos_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3.5;
				spell.RecastDelay = 10;
				spell.ClientEffect = 360;
				spell.Icon = 360;
				spell.TooltipId = 1609;
				spell.Damage = 450;
				spell.Name = "Fire Blast";
				spell.Range = 1500;
				spell.Radius = 350;
				spell.SpellID = 11915;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_Vortanos_DD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vortanos_DD);
			}
			return m_Vortanos_DD;
		}
	}
	private Spell m_Vortanos_Dot;
	private Spell Vortanos_Dot
	{
		get
		{
			if (m_Vortanos_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 0;
				spell.ClientEffect = 562;
				spell.Icon = 562;
				spell.Name = "Disintegrating Force";
				spell.Description = "Target takes 89 matter damage every 2.0 sec.";
				spell.Message1 = "An acidic cloud surrounds you!";
				spell.Message2 = "{0} is surrounded by an acidic cloud!";
				spell.Message3 = "The acidic mist around you dissipates.";
				spell.Message4 = "The acidic mist around {0} dissipates.";
				spell.TooltipId = 562;
				spell.Range = 1500;
				spell.Damage = 89;
				spell.Duration = 12;
				spell.Frequency = 20;
				spell.SpellID = 11916;
				spell.Target = "Enemy";
				spell.SpellGroup = 1800;
				spell.EffectGroup = 1500;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Vortanos_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Vortanos_Dot);
			}
			return m_Vortanos_Dot;
		}
	}		
	#endregion
}
#endregion Vortanos

#region Vortanos adds
public class VortanosAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public VortanosAddBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		base.Think();
	}
}
#endregion Vortanos adds