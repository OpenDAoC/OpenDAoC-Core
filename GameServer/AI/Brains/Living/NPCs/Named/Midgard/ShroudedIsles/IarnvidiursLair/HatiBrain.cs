using DOL.Database;
using DOL.GS;

namespace DOL.AI.Brain;

public class HatiBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HatiBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public static bool IsPulled = false;

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            IsPulled = false;
        }

        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "HatiBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);
                        }
                    }
                }

                IsPulled = true;
            }

            if (Body.TargetObject != null)
            {
                Body.CastSpell(Hati_Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }

        base.Think();
    }

    private Spell m_Hati_Mezz;

    private Spell Hati_Mezz
    {
        get
        {
            if (m_Hati_Mezz == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 30;
                spell.Duration = 29;
                spell.ClientEffect = 2619;
                spell.Icon = 2619;
                spell.Name = "Hati's Mezz";
                spell.TooltipId = 2619;
                spell.Radius = 450;
                spell.Range = 0;
                spell.SpellID = 11813;
                spell.Target = "Enemy";
                spell.Type = "Mesmerize";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Spirit;
                m_Hati_Mezz = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Hati_Mezz);
            }

            return m_Hati_Mezz;
        }
    }
}