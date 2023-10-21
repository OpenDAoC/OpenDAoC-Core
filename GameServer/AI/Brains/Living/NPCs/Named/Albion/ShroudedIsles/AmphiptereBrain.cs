using System;
using Core.Database.Tables;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Amphiptere
public class AmphiptereBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public AmphiptereBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	private bool CanSpawnAdds = false;
	private bool RemoveAdds = false;
	public override void OnAttackedByEnemy(AttackData ad)
	{
		if(ad != null && ad.Damage > 0 && ad.Attacker != null && CanSpawnAdds == false && Util.Chance(20))
        {
			BroadcastMessage(String.Format("A blow knocks one of " + Body.Name + "'s tooths to the ground."));
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdd), 5000);
			CanSpawnAdds = true;
		}
		base.OnAttackedByEnemy(ad);
	}
	private int SpawnAdd(EcsGameTimer timer)
    {
		if (HasAggro && Body.IsAlive)
		{
			AmphiptereAdds add = new AmphiptereAdds();
			add.X = Body.X + Util.Random(-500, 500);
			add.Y = Body.Y + Util.Random(-500, 500);
			add.Z = Body.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			add.AddToWorld();
		}
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetSpawnAdd), Util.Random(25000, 35000));
		return 0;
    }
	private int ResetSpawnAdd(EcsGameTimer timer)
    {
		CanSpawnAdds = false;
		return 0;
    }
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnAdds = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is AmphiptereAddsBrain)
						npc.Die(Body);
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is AmphiptereAddsBrain brain)
					if (target != null && !brain.HasAggro)
						brain.AddToAggroList(target, 100);
			}				
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
					Body.CastSpell(Amphiptere_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.Disease))
					Body.CastSpell(AmphiptereDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
				if (Util.Chance(25) && !Body.IsCasting && !Body.effectListComponent.ContainsEffectForEffectType(EEffect.Bladeturn))
					Body.CastSpell(Bubble, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			}
		}
		base.Think();
	}
    #region Spells
    private Spell m_Amphiptere_Dot;
	private Spell Amphiptere_Dot
	{
		get
		{
			if (m_Amphiptere_Dot == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 20;
				spell.ClientEffect = 562;
				spell.Icon = 562;
				spell.Name = "Amphiptere's Venom";
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
				spell.SpellID = 11908;
				spell.Target = "Enemy";
				spell.SpellGroup = 1800;
				spell.EffectGroup = 1500;
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Matter;
				m_Amphiptere_Dot = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Amphiptere_Dot);
			}
			return m_Amphiptere_Dot;
		}
	}
	private Spell m_AmphiptereDisease;
	private Spell AmphiptereDisease
	{
		get
		{
			if (m_AmphiptereDisease == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 40;
				spell.ClientEffect = 731;
				spell.Icon = 731;
				spell.Name = "Amphiptere's Infection";
				spell.Message1 = "You are diseased!";
				spell.Message2 = "{0} is diseased!";
				spell.Message3 = "You look healthy.";
				spell.Message4 = "{0} looks healthy again.";
				spell.TooltipId = 731;
				spell.Range = 1500;
				spell.Duration = 120;
				spell.SpellID = 11907;
				spell.Target = "Enemy";
				spell.Type = "Disease";
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
				m_AmphiptereDisease = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AmphiptereDisease);
			}
			return m_AmphiptereDisease;
		}
	}
	private Spell m_Bubble;
	private Spell Bubble
	{
		get
		{
			if (m_Bubble == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 10;
				spell.Duration = 10;
				spell.ClientEffect = 5126;
				spell.Icon = 5126;
				spell.TooltipId = 5126;
				spell.Name = "Shield of Feathers";
				spell.Range = 0;
				spell.SpellID = 11909;
				spell.Target = "Self";
				spell.Type = ESpellType.Bladeturn.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Bubble = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Bubble);
			}
			return m_Bubble;
		}
	}
    #endregion
}
#endregion Amphiptere

#region Amphiptere adds
public class AmphiptereAddsBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public AmphiptereAddsBrain() : base()
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
#endregion Amphiptere adds