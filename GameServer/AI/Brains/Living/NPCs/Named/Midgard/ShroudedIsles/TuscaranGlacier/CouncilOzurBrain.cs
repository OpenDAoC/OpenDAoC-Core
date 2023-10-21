using System;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

public class CouncilOzurBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private static int _GettingFirstPlayerStage = 50;
    private static int _GettingSecondPlayerStage = 100;
    private static int _GettingNonZerkedStage = _GettingFirstPlayerStage - 1;
    private const int m_value = 20;
    private const int min_value = 0;

    public CouncilOzurBrain()
        : base()
    {
        AggroLevel = 200;
        AggroRange = 800;
    }

    public void BroadcastMessage(String message)
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
        {
            player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
        }
    }

    private void Resists(bool isNotZerked)
    {
        if (isNotZerked)
        {
            Body.AbilityBonus[(int) EProperty.Resist_Body] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Heat] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Cold] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Matter] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Energy] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Spirit] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Slash] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Crush] = m_value;
            Body.AbilityBonus[(int) EProperty.Resist_Thrust] = m_value;
        }
        else
        {
            Body.AbilityBonus[(int) EProperty.Resist_Body] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Heat] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Cold] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Matter] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Energy] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Spirit] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Slash] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Crush] = min_value;
            Body.AbilityBonus[(int) EProperty.Resist_Thrust] = min_value;
        }
    }

    private void Weak(bool weak)
    {
        if (weak)
        {
            Body.AbilityBonus[(int) EProperty.Resist_Body] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Heat] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Cold] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Matter] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Energy] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Spirit] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Slash] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Crush] = min_value - 20;
            Body.AbilityBonus[(int) EProperty.Resist_Thrust] = min_value - 20;
        }
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (Body.TargetObject != null && Body.InCombat && Body.Health != Body.MaxHealth && HasAggro)
        {
            int countPlayer = 0;
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                countPlayer++;
            }

            if (countPlayer <= _GettingNonZerkedStage)
            {
                Resists(true);
            }

            if (countPlayer >= _GettingFirstPlayerStage && countPlayer < _GettingSecondPlayerStage)
            {
                Body.ScalingFactor += 10;
                Body.Strength = 200;
                Resists(false);
            }

            if (countPlayer >= _GettingSecondPlayerStage)
            {
                Body.ScalingFactor += 25;
                Body.Strength = 350;
                Weak(true);
            }
        }
        if(HasAggro && Body.TargetObject != null)
        {
            Body.CastSpell(OzurDisease, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
        }
        base.Think();
    }
    private Spell m_OzurDisease;
    private Spell OzurDisease
    {
        get
        {
            if (m_OzurDisease == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 60;
                spell.ClientEffect = 4375;
                spell.Icon = 4375;
                spell.Name = "Ozur's Disease";
                spell.Message1 = "You are diseased!";
                spell.Message2 = "{0} is diseased!";
                spell.Message3 = "You look healthy.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4375;
                spell.Range = 1500;
                spell.Radius = 400;
                spell.Duration = 120;
                spell.SpellID = 11926;
                spell.Target = "Enemy";
                spell.Type = "Disease";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Energy; //Energy DMG Type
                m_OzurDisease = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OzurDisease);
            }
            return m_OzurDisease;
        }
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        base.Notify(e, sender, args);
    }
}