using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

public class AvartackTheChampionBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public AvartackTheChampionBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
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
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (Body.TargetObject != null)
            {
                float angle = Body.TargetObject.GetAngle(Body);
                if (angle >= 160 && angle <= 200)
                {
                    Body.styleComponent.NextCombatBackupStyle = AvartackTheChampion.Taunt;
                    Body.styleComponent.NextCombatStyle = AvartackTheChampion.BackStyle;
                }
                else
                {
                    Body.styleComponent.NextCombatStyle = AvartackTheChampion.Taunt;
                }
            }
            Body.CastSpell(AvartackDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.Think();
    }
    private Spell m_AvartackDD;
    private Spell AvartackDD
    {
        get
        {
            if (m_AvartackDD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = Util.Random(15,25);
                spell.ClientEffect = 5435;
                spell.Icon = 5435;
                spell.TooltipId = 5435;
                spell.Damage = 300;
                spell.Range = 1500;
                spell.Name = "Avartack's Force";
                spell.SpellID = 11786;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Body;
                m_AvartackDD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AvartackDD);
            }
            return m_AvartackDD;
        }
    }
}