using Core.Database;
using Core.GS;

namespace Core.AI.Brain;

public class KoalinthElderBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public KoalinthElderBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 300;
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "KoalinthElderBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
            GameLiving target = Body.TargetObject as GameLiving;
            if (Util.Chance(25) && !Body.IsCasting && !target.effectListComponent.ContainsEffectForEffectType(EEffect.MeleeHasteDebuff))
                Body.CastSpell(KoalinthElder_HasteDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.Think();
    }
    #region Spells		
    private Spell m_KoalinthElder_HasteDebuff;
    private Spell KoalinthElder_HasteDebuff
    {
        get
        {
            if (m_KoalinthElder_HasteDebuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.RecastDelay = 7;
                spell.Power = 6;
                spell.Duration = 45;
                spell.ClientEffect = 723;
                spell.Icon = 723;
                spell.Name = "Inflict Suffering";
                spell.Description = "Target's attack speed reduced by 17%.";
                spell.Range = 1500;
                spell.Value = 17;
                spell.SpellID = 11971;
                spell.Target = "Enemy";
                spell.Type = ESpellType.CombatSpeedDebuff.ToString();
                spell.DamageType = (int)EDamageType.Body;
                m_KoalinthElder_HasteDebuff = new Spell(spell, 13);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_KoalinthElder_HasteDebuff);
            }
            return m_KoalinthElder_HasteDebuff;
        }
    }
    #endregion
}