using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class SpiritOfLordEmthoroBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SpiritOfLordEmthoroBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	
	private bool CanSpawnAdd = false;
	private bool RemoveAdds = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			CanSpawnAdd = false;
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
			Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
						npc.Die(Body);
				}
				RemoveAdds = true;
			}
		}
		if (HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(3000))
			{
				if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
						AddAggroListTo(npc.Brain as StandardMobBrain);
			}
			Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);				
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
			if (target != null)
			{
				if (!target.IsWithinRadius(spawn, 420))
					Body.MaxSpeedBase = 0;
				else
					Body.MaxSpeedBase = npcTemplate.MaxSpeed;
			}
			if(CanSpawnAdd == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdd), Util.Random(25000, 40000));
				CanSpawnAdd = true;
            }
			Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
			Body.CastSpell(LifedrianPulse, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
		}
		base.Think();
	}
	private int SpawnAdd(EcsGameTimer timer)
    {
		if (HasAggro && Body.IsAlive)
		{
			GameNpc add = new GameNpc();
			add.Name = Body.Name + "'s servant";
			switch(Util.Random(1,2))
            {
				case 1: add.Model = 814; break;//orc
				case 2: add.Model = 921; break;//zombie
			}				
			add.Size = (byte)Util.Random(55, 65);
			add.Level = (byte)Util.Random(55, 59);
			add.Strength = 150;
			add.Quickness = 80;
			add.MeleeDamageType = EDamageType.Crush;
			add.MaxSpeedBase = 225;
			add.PackageID = "EmthoroAdd";
			add.RespawnInterval = -1;
			add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
			add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
			add.Z = Body.SpawnPoint.Z;
			add.CurrentRegion = Body.CurrentRegion;
			add.Heading = Body.Heading;
			add.Faction = FactionMgr.GetFactionByID(64);
			add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
			StandardMobBrain brain = new StandardMobBrain();
			add.SetOwnBrain(brain);
			brain.AggroRange = 800;
			brain.AggroLevel = 100;
			add.AddToWorld();
		}
		CanSpawnAdd = false;
		return 0;
    }
	private Spell m_LifedrianPulse;
	private Spell LifedrianPulse
	{
		get
		{
			if (m_LifedrianPulse == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 10;
				spell.ClientEffect = 14352;
				spell.Icon = 14352;
				spell.TooltipId = 14352;
				spell.Damage = 80;
				spell.Name = "Lifedrain Pulse";
				spell.Range = 1500;
				spell.Radius = 440;
				spell.SpellID = 11898;
				spell.Target = ESpellTarget.AREA.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_LifedrianPulse = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LifedrianPulse);
			}
			return m_LifedrianPulse;
		}
	}
}