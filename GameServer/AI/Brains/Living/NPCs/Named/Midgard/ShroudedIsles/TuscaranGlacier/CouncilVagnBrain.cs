using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class CouncilVagnBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CouncilVagnBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 2000;
    }
    public static bool IsPulled = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if (IsPulled == false)
        {
            foreach (GameNpc Nokkvi in Body.GetNPCsInRadius(1800))
            {
                if (Nokkvi != null && Nokkvi.IsAlive && Nokkvi.Brain is CouncilNokkviBrain)
                    AddAggroListTo(Nokkvi.Brain as CouncilNokkviBrain);
            }
            IsPulled = true;
        }
        base.OnAttackedByEnemy(ad);
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            Body.CastSpell(VagnDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
        }
        base.Think();
    }
    private Spell m_VagnDD;
    public Spell VagnDD
    {
        get
        {
            if (m_VagnDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.Power = 0;
                spell.RecastDelay = Util.Random(10,15);
                spell.ClientEffect = 4075;
                spell.Icon = 4075;
                spell.Damage = 600;
                spell.DamageType = (int)EDamageType.Cold;
                spell.Name = "Frost Shock";
                spell.Range = 1500;
                spell.SpellID = 11927;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                m_VagnDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VagnDD);
            }
            return m_VagnDD;
        }
    }
}