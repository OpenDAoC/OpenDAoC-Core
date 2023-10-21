using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.ECS;

namespace Core.GS.AI.Brains;

public class DrihtenElredenBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DrihtenElredenBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
	private bool canbringhelp = false;
	List<GameNpc> CallHelp = new List<GameNpc>();
	List<GameNpc> PulledMobs = new List<GameNpc>();
	public void BringHelp()
    {
		if(HasAggro)
        {
			foreach(GameNpc npc in Body.GetNPCsInRadius(2500))
            {
				if (npc == null) continue;
				if(npc.IsAlive && npc.PackageID == "DrihtenBaf" && !CallHelp.Contains(npc) && !PulledMobs.Contains(npc))
					CallHelp.Add(npc);
            }
        }
		if(CallHelp.Count > 0)
        {
			GameNpc friend = CallHelp[Util.Random(0, CallHelp.Count - 1)];
			GameLiving target = Body.TargetObject as GameLiving;
			if(target != null && target.IsAlive && friend != null && friend.Brain is StandardMobBrain brain)
            {
				if (!brain.HasAggro)
				{
					brain.AddToAggroList(target, 100);
					if(CallHelp.Contains(friend))
						CallHelp.Remove(friend);
					if(!PulledMobs.Contains(friend))
						PulledMobs.Add(friend);
				}
            }
		}
    }
	public int PickRandomMob(EcsGameTimer timer)
    {
		if(HasAggro)
			BringHelp();
		canbringhelp = false;
		return 0;
    }
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			canbringhelp=false;
			if (CallHelp.Count > 0)
				CallHelp.Clear();
			if(PulledMobs.Count > 0)
				PulledMobs.Clear();
		}
		if (HasAggro && Body.TargetObject != null)
		{
			if (!Body.effectListComponent.ContainsEffectForEffectType(EEffect.MeleeHasteBuff))
				Body.CastSpell(Boss_Haste_Buff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			if (canbringhelp==false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PickRandomMob), Util.Random(15000, 30000));
				canbringhelp=true;
            }
		}
		base.Think();
	}
	private Spell m_Boss_Haste_Buff;
	private Spell Boss_Haste_Buff
	{
		get
		{
			if (m_Boss_Haste_Buff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 50;
				spell.Duration = 25;
				spell.ClientEffect = 1727;
				spell.Icon = 1727;
				spell.Name = "Alacrity of the Heavenly Host";
				spell.Message2 = "{0} begins attacking faster!";
				spell.Message4 = "{0}'s attacks return to normal.";
				spell.TooltipId = 1727;
				spell.Range = 500;
				spell.Value = 38;
				spell.SpellID = 11888;
				spell.Target = "Self";
				spell.Type = ESpellType.CombatSpeedBuff.ToString();
				spell.Uninterruptible = true;
				spell.MoveCast = true;
				m_Boss_Haste_Buff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_Haste_Buff);
			}
			return m_Boss_Haste_Buff;
		}
	}
}