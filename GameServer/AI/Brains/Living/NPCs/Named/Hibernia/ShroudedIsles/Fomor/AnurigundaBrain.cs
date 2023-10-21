using Core.Database.Tables;

namespace Core.GS.AI.Brains;

#region Anurigunda
public class AnurigundaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public AnurigundaBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	public static bool IsPulled = false;
	private bool RemoveAdds = false;
	public void BlockEntrance()
    {
		Point3D entrance = new Point3D(31234, 33215, 15842);
		foreach(GamePlayer player in Body.GetPlayersInRadius(Body.CurrentRegionID))
        {
			if (player == null) continue;
			if(player.IsAlive && player.Client.Account.PrivLevel == (uint)EPrivLevel.Player)
            {
				if(player.IsWithinRadius(entrance,400))
                {
					player.MoveTo(180, 30652, 33089, 16124, 3169);
                }
            }
        }
    }
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			IsPulled = false;
			Adds1 = false;
			Adds2 = false;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is AnurigundaAddsBrain)
						{
							npc.RemoveFromWorld();
						}
					}
				}
				RemoveAdds = true;
			}
		}
		if(Body.IsAlive)
        {
			BlockEntrance();
        }
		if (Body.IsAlive && HasAggro && Body.TargetObject != null)
		{
			RemoveAdds = false;
			if (IsPulled == false)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "AnurigundaBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
				IsPulled = true;
			}
			if(Body.TargetObject != null)
            {
				Body.CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			SpawnFomorians();
		}
		base.Think();
	}
	public static bool Adds1 = false;
	public static bool Adds2 = false;
	public void SpawnFomorians()
    {
		if (Body.HealthPercent <= 40 && Adds1 == false)
		{
			for (int i = 0; i < 4; i++)
			{
				AnurigundaAdd Add1 = new AnurigundaAdd();
				Add1.X = Body.X + Util.Random(-100, 100);
				Add1.Y = Body.Y + Util.Random(-100, 100);
				Add1.Z = Body.Z;
				Add1.CurrentRegion = Body.CurrentRegion;
				Add1.Heading = Body.Heading;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
			}
			Adds1 = true;
		}
		if (Body.HealthPercent <= 20 && Adds2 == false)
		{
			for (int i = 0; i < 5; i++)
			{
				AnurigundaAdd Add1 = new AnurigundaAdd();
				Add1.X = Body.X + Util.Random(-100, 100);
				Add1.Y = Body.Y + Util.Random(-100, 100);
				Add1.Z = Body.Z;
				Add1.CurrentRegion = Body.CurrentRegion;
				Add1.Heading = Body.Heading;
				Add1.RespawnInterval = -1;
				Add1.AddToWorld();
			}
			Adds2 = true;
		}
	}
	private Spell m_FireGroundDD;
	private Spell FireGroundDD
	{
		get
		{
			if (m_FireGroundDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = Util.Random(10,15);
				spell.ClientEffect = 77;
				spell.Icon = 77;
				spell.TooltipId = 77;
				spell.Damage = 45;
				spell.Duration = 60;
				spell.Frequency = 20;
				spell.Name = "Touch of Flames";
				spell.Description = "Inflicts 45 damage to the target every 2 sec for 60 seconds.";
				spell.Message1 = "You are covered in lava!";
				spell.Message2 = "{0} is covered in lava!";
				spell.Message3 = "The lava hardens and falls away.";
				spell.Message4 = "The lava falls from {0}'s skin.";
				spell.Range = 0;
				spell.Radius = 350;
				spell.SpellID = 11839;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageOverTime.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				spell.DamageType = (int)EDamageType.Heat;
				m_FireGroundDD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireGroundDD);
			}
			return m_FireGroundDD;
		}
	}
}
#endregion Anurigunda

#region Anurigunda adds
public class AnurigundaAddsBrain : StandardMobBrain
{
	public AnurigundaAddsBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
		}
		base.Think();
	}
}
#endregion Anurigunda adds