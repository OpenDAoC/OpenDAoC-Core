using System;
using Core.Database.Tables;
using Core.Events;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class CouncilHordBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    protected String m_HealAnnounce;
    
    public CouncilHordBrain()
        : base()
    {
        //m_HealAnnounce = "{0} heals his wounds.";
        AggroLevel = 200;
        AggroRange = 1500; //so players cant just pass him without aggroing
    }
    
    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }    
    public override void Think()
    {
        /*if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
        {
            if (Util.Chance(1))
            {
                new RegionTimer(Body, new RegionTimerCallback(CastHeal), 1000);
            }
        }*/
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);

        if (e == GameObjectEvent.TakeDamage)
        {
            if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
            {
                if (Util.Chance(3))
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastHeal), 1000);
            }
        }
    }

    /// <summary>
    /// Cast Heal on itself
    /// </summary>
    /// <param name="timer">The timer that started this cast.</param>
    /// <returns></returns>
    private int CastHeal(EcsGameTimer timer)
    {
        //BroadcastMessage(String.Format(m_HealAnnounce, Body.Name));
        Body.CastSpell(Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        return 0;
    }
    
    protected Spell m_healSpell;
    /// <summary>
    /// The Heal spell.
    /// </summary>
    protected Spell Heal
    {
        get
        {
            if (m_healSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 0;
                spell.Power = 0;
                spell.ClientEffect = 3011;
                spell.Icon = 3011;
                spell.TooltipId = 3011;
                spell.Damage = 0;
                spell.Name = "Minor Emendation";
                spell.Range = 0;
                spell.SpellID = 3011;
                spell.Duration = 0;
                spell.Value = 250;
                spell.SpellGroup = 130;
                spell.Target = "Self";
                spell.Type = "Heal";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_healSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_healSpell);
            }
            return m_healSpell;
        }
    }
}