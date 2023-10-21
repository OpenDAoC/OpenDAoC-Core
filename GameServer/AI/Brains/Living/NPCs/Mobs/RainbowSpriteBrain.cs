using Core.Database;
using Core.Database.Tables;
using Core.GS;

namespace Core.AI.Brain;

#region Blue pixie
public class RainbowSpriteBlueBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public RainbowSpriteBlueBrain() : base()
	{
		ThinkInterval = 1500;
	}
	private bool CallforHelp = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
			CallforHelp = false;

		if (HasAggro && Body.TargetObject != null)
		{
			if (!CallforHelp)
			{
				if (Body.HealthPercent <= 20)
				{
					foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteBlueBrain brain && npc != Body)
						{
							if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
								brain.AddToAggroList(target, 10);
						}
					}
					CallforHelp = true;
				}
			}
		}
		base.Think();
	}
}
#endregion Blue pixie

#region Green pixie
public class RainbowSpriteGreenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public RainbowSpriteGreenBrain() : base()
	{
		ThinkInterval = 1500;
	}
	private bool CallforHelp = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
			CallforHelp = false;

		if(Body.HealthPercent <= 50 && !Body.IsCasting && Util.Chance(100))
			Body.CastSpell(GreenSpriteHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

		if (HasAggro && Body.TargetObject != null)
		{
			if (!CallforHelp)
			{
				if (Body.HealthPercent <= 20)
				{
					foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteGreenBrain brain && npc != Body)
						{
							if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
								brain.AddToAggroList(target, 10);
						}
					}
					CallforHelp = true;
				}
			}
		}
		base.Think();
	}
	private Spell m_GreenSpriteHeal;
	private Spell GreenSpriteHeal
	{
		get
		{
			if (m_GreenSpriteHeal == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 3;
				spell.RecastDelay = 8;
				spell.ClientEffect = 1340;
				spell.Icon = 1340;
				spell.TooltipId = 1340;
				spell.Value = 180;
				spell.Name = "GreenSprite's Heal";
				spell.Range = 1500;
				spell.SpellID = 11988;
				spell.Target = "Self";
				spell.Type = ESpellType.Heal.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_GreenSpriteHeal = new Spell(spell, 30);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_GreenSpriteHeal);
			}
			return m_GreenSpriteHeal;
		}
	}
}
#endregion Green pixie

#region Tan pixie
public class RainbowSpriteTanBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public RainbowSpriteTanBrain() : base()
    {
        ThinkInterval = 1500;
    }
    private bool CallforHelp = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
            CallforHelp = false;

        if (HasAggro && Body.TargetObject != null)
        {
            if (!CallforHelp)
            {
                if (Body.HealthPercent <= 20)
                {
                    foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteTanBrain brain && npc != Body)
                        {
                            if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
                                brain.AddToAggroList(target, 10);
                        }
                    }
                    CallforHelp = true;
                }
            }
        }
        base.Think();
    }
}
#endregion

#region White pixie
public class RainbowSpriteWhiteBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public RainbowSpriteWhiteBrain() : base()
	{
		ThinkInterval = 1500;
	}
	private bool CallforHelp = false;
	public override void Think()
	{
		if (!CheckProximityAggro())
			CallforHelp = false;

		if (HasAggro && Body.TargetObject != null)
		{
			if (!CallforHelp)
			{
				if (Body.HealthPercent <= 20)
				{
					foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (npc != null && npc.IsAlive && npc.Name.ToLower() == "rainbow sprite" && npc.Brain is not ControlledNpcBrain && npc.Brain is RainbowSpriteWhiteBrain brain && npc != Body)
						{
							if (target != null && target.IsAlive && brain != null && !brain.HasAggro)
								brain.AddToAggroList(target, 10);
						}
					}
					CallforHelp = true;
				}
			}
		}
		base.Think();
	}
}
#endregion White pixie