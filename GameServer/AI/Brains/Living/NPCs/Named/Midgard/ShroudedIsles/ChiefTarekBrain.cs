using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.AI;

public class ChiefTarekBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ChiefTarekBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }

        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "ChiefTarekBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }

        if (Body.HealthPercent <= 50 && !Body.IsCasting)
        {
            Body.CastSpell(ChiefTarekHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }

        base.Think();
    }

    private Spell m_ChiefTarekHeal;

    private Spell ChiefTarekHeal
    {
        get
        {
            if (m_ChiefTarekHeal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 10;
                spell.ClientEffect = 1340;
                spell.Icon = 1340;
                spell.TooltipId = 1340;
                spell.Value = 400;
                spell.Name = "Chief Tarek's Heal";
                spell.Range = 1500;
                spell.SpellID = 11889;
                spell.Target = "Self";
                spell.Type = ESpellType.Heal.ToString();
                spell.Uninterruptible = true;
                m_ChiefTarekHeal = new Spell(spell, 60);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_ChiefTarekHeal);
            }

            return m_ChiefTarekHeal;
        }
    }
}