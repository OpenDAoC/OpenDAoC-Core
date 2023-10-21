using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Lady Darra
public class LadyDarraBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public LadyDarraBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 1500;
    }
    public static bool reset_darra = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;              
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            if (reset_darra == false)
            {
                if (SpectralPaladin.paladins_count <= 3)
                {
                    LadyDarra.spawn_palas = false;
                    foreach (GameNpc pala in Body.GetNPCsInRadius(2000))
                    {
                        if (pala != null)
                        {
                            if (pala.IsAlive && pala.Brain is SpectralPaladinBrain)
                            {
                                pala.Die(Body);
                            }
                        }
                    }
                    LadyDarra darra = new LadyDarra();
                    darra.SpawnPaladins();
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetDarra), 7000);
                    reset_darra = true;
                }
            }
        }
        if (Body.InCombat && HasAggro)
        {
        }
        base.Think();
    }
    public int ResetDarra(EcsGameTimer timer)
    {
        reset_darra = false;
        return 0;
    }
}
#endregion Lady Darra

#region Spectral Paladin
public class SpectralPaladinBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SpectralPaladinBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public override void Think()
    {
        if (Body.IsAlive)
        {
            foreach(GameNpc Darra in Body.GetNPCsInRadius(2000))
            {
                if(Darra != null)
                {
                    if(Darra.IsAlive && Darra.Brain is LadyDarraBrain)
                    {
                        if (Darra.HealthPercent < 100)
                        {
                            if (!Body.IsCasting && Body.GetSkillDisabledDuration(Paladin_Heal) == 0)
                            {
                                Body.TargetObject = Darra;
                                Body.TurnTo(Darra);
                                Body.CastSpell(Paladin_Heal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                            }
                        }
                    }
                }
            }                
        }
        base.Think();
    }
    private Spell m_Paladin_Heal;

    private Spell Paladin_Heal
    {
        get
        {
            if (m_Paladin_Heal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 5;
                spell.ClientEffect = 1358;
                spell.Icon = 1358;
                spell.TooltipId = 360;
                spell.Name = "Spectral Heal";
                spell.Value = 350;
                spell.Range = 2000;
                spell.SpellID = 11776;
                spell.Target = ESpellTarget.REALM.ToString();
                spell.Type = ESpellType.Heal.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                m_Paladin_Heal = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Paladin_Heal);
            }

            return m_Paladin_Heal;
        }
    }
}
#endregion Spectral Paladin