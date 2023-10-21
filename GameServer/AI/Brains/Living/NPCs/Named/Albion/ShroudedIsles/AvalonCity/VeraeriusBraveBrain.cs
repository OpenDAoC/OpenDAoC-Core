using Core.Database.Tables;

namespace Core.GS.AI.Brains;

public class VeraeriusBraveBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public VeraeriusBraveBrain() : base()
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
        if (Body.InCombat && Body.IsAlive && HasAggro)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "VeraeriusBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
                        }
                    }
                }
                IsPulled = true;
            }
            if(Body.HealthPercent <= 70)
            {
                Body.CastSpell(VeraeriusHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
        }
        base.Think();
    }
    private Spell m_VeraeriusHeal;
    private Spell VeraeriusHeal
    {
        get
        {
            if (m_VeraeriusHeal == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = 8;
                spell.ClientEffect = 1340;
                spell.Icon = 1340;
                spell.TooltipId = 1340;
                spell.Value = 400;
                spell.Name = "Vera'erius Heal";
                spell.Range = 1500;
                spell.SpellID = 11796;
                spell.Target = "Self";
                spell.Type = ESpellType.Heal.ToString();
                spell.Uninterruptible = true;
                m_VeraeriusHeal = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VeraeriusHeal);
            }
            return m_VeraeriusHeal;
        }
    }
}