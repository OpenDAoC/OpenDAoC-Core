using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI.Brains;

#region Kontar
public class KontarCorruptBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KontarCorruptBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    private bool spawnAdds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            spawnAdds = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
                {
                    if (npc != null && npc.IsAlive && npc.Brain is CorruptorBodyguardBrain)
                        npc.RemoveFromWorld();
                }
                RemoveAdds = true;
            }
        }
        if(HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(!spawnAdds)
            {
                SpawnAdds();
                spawnAdds = true;
            }
        }
        base.Think();
    }
    private void SpawnAdds()
    {
        for (int i = 0; i < Util.Random(8, 10); i++)
        {
            CorruptorBodyguard add = new CorruptorBodyguard();
            add.X = Body.X + Util.Random(-500, 500);
            add.Y = Body.Y + Util.Random(-500, 500);
            add.Z = Body.Z;
            add.Heading = Body.Heading;
            add.CurrentRegion = Body.CurrentRegion;
            add.AddToWorld();
        }
    }
}
#endregion Kontar

#region Kontar adds
public class CorruptorBodyguardBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public CorruptorBodyguardBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (HasAggro && Body.TargetObject != null)
		{
			foreach(GameNpc npc in Body.GetNPCsInRadius(1500))
            {
				if(npc != null && npc.IsAlive && npc.Brain is KontarCorruptBrain && npc.HealthPercent < 100)
                {					
					if (!Body.IsCasting)
					{
						Body.TargetObject = npc;
						Body.TurnTo(npc);
						Body.CastSpell(CorruptorBodyguardHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
					}
				}
            }
		}
		base.Think();
	}
	#region Spells
	private Spell m_CorruptorBodyguardHeal;
	private Spell CorruptorBodyguardHeal
	{
		get
		{
			if (m_CorruptorBodyguardHeal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 5;
				spell.ClientEffect = 1340;
				spell.Icon = 1340;
				spell.TooltipId = 1340;
				spell.Value = 200;
				spell.Name = "Corruptor Bodyguard's Heal";
				spell.Range = 1500;
				spell.SpellID = 11906;
				spell.Target = "Realm";
				spell.Type = ESpellType.Heal.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_CorruptorBodyguardHeal = new Spell(spell, 50);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CorruptorBodyguardHeal);
			}
			return m_CorruptorBodyguardHeal;
		}
	}		
	#endregion
}
#endregion Kontar adds