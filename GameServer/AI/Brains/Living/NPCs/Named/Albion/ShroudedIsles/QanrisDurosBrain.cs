using Core.Database.Tables;

namespace Core.GS.AI.Brains;

public class QanrisDurosBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public QanrisDurosBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(3500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "DurosBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
            if (!Body.IsCasting && Util.Chance(30))
            {
                Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
                Body.CastSpell(Boss_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            }
        }
        base.Think();
    }
    private Spell m_Boss_PBAOE;
    private Spell Boss_PBAOE
    {
        get
        {
            if (m_Boss_PBAOE == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = Util.Random(8, 18);
                spell.ClientEffect = 1695;
                spell.Icon = 1695;
                spell.TooltipId = 1695;
                spell.Name = "Thunder Stomp";
                spell.Damage = 400;
                spell.Range = 500;
                spell.Radius = 1000;
                spell.SpellID = 11905;
                spell.Target = ESpellTarget.AREA.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.DamageType = (int)EDamageType.Energy;
                spell.Uninterruptible = true;
                m_Boss_PBAOE = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Boss_PBAOE);
            }
            return m_Boss_PBAOE;
        }
    }
}