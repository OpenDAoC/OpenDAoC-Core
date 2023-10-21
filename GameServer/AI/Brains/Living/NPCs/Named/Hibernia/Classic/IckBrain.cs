using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.AI.Brains;

public class IckBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public IckBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 1500;
	}
	private bool InitlifeLeechForm = false;
	private bool lifeLeechForm = false;
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
		}
	}
	public override void Think()
	{
		if(HasAggro && Body.TargetObject != null)
        {
			if(!InitlifeLeechForm)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(LifeLeech), 20000);
				InitlifeLeechForm = true;
            }
			if(lifeLeechForm && !Body.IsCasting)
            {
				Body.CastSpell(IckDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
        }
		base.Think();
	}
	private int LifeLeech(EcsGameTimer timer)
    {
		if (HasAggro && Body.TargetObject != null)
		{
			BroadcastMessage(String.Format("{0} grows in size as he steals {1}'s life energy!",Body.Name,Body.TargetObject.Name));
			lifeLeechForm = true;
			Body.Size = 50;				
		}
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(EndLifeLeech), 20000);
		return 0;
    }
	private int EndLifeLeech(EcsGameTimer timer)
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162371);
		BroadcastMessage(String.Format("{0}'s stolen life energy fades and he returns to normal.",Body.Name));
		if (HasAggro && Body.TargetObject != null)
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(LifeLeech), 20000);
		Body.Size = 20;
		lifeLeechForm = false;
		return 0;
	}
	private Spell m_IckDD;
	private Spell IckDD
	{
		get
		{
			if (m_IckDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = Util.Random(5, 8);
				spell.ClientEffect = 581;
				spell.Icon = 581;
				spell.Damage = 80;
				spell.DamageType = (int)EDamageType.Body;
				spell.Name = "LifeDrain";
				spell.Range = 1500;
				spell.SpellID = 11945;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_IckDD = new Spell(spell, 20);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_IckDD);
			}
			return m_IckDD;
		}
	}
}

public class IckAddBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public IckAddBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}