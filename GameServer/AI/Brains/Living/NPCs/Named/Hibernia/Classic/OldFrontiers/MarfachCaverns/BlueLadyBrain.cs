using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Blue Lady
public class BlueLadyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BlueLadyBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    private bool CanSpawnAdds = false;
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            Body.Health = Body.MaxHealth;
            CanSpawnAdds = false;
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc.Brain is BlueLadyAddBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            RemoveAdds = false;
            Body.CastSpell(BlueLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            if ((BlueLadySwordAdd.SwordCount < 10 || BlueLadyAxeAdd.AxeCount < 10) && CanSpawnAdds == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdds), Util.Random(25000, 45000));
                CanSpawnAdds = true;
            }
        }
        base.Think();
    }
    private int SpawnAdds(EcsGameTimer timer)
    {
        for (int i = 0; i < 10; i++)
        {
            if (BlueLadySwordAdd.SwordCount < 10)
            {
                BlueLadySwordAdd add = new BlueLadySwordAdd();
                add.X = Body.X + Util.Random(-100, 100);
                add.Y = Body.Y + Util.Random(-100, 100);
                add.Z = Body.Z;
                add.CurrentRegion = Body.CurrentRegion;
                add.Heading = Body.Heading;
                add.AddToWorld();
            }
        }
        for (int i = 0; i < 10; i++)
        {
            if (BlueLadyAxeAdd.AxeCount < 10)
            {
                BlueLadyAxeAdd add2 = new BlueLadyAxeAdd();
                add2.X = Body.X + Util.Random(-100, 100);
                add2.Y = Body.Y + Util.Random(-100, 100);
                add2.Z = Body.Z;
                add2.CurrentRegion = Body.CurrentRegion;
                add2.Heading = Body.Heading;
                add2.AddToWorld();
            }
        }
        CanSpawnAdds = false;
        return 0;
    }
    public Spell m_BlueLady_DD;
    public Spell BlueLady_DD
    {
        get
        {
            if (m_BlueLady_DD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 5;
                spell.RecastDelay = Util.Random(25, 35);
                spell.ClientEffect = 4369;
                spell.Icon = 4369;
                spell.TooltipId = 4369;
                spell.Damage = 800;
                spell.Name = "Mana Bomb";
                spell.Radius = 550;
                spell.Range = 0;
                spell.SpellID = 11788;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Cold;
                m_BlueLady_DD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BlueLady_DD);
            }

            return m_BlueLady_DD;
        }
    }
}
#endregion Blue Lady

public class BlueLadyAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BlueLadyAddBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
}