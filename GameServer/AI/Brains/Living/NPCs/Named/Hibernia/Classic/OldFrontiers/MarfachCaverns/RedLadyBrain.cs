using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Red Lady
class RedLadyBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public RedLadyBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
    }
    private bool CanSpawnAdds = false;
    private int SpawnAdd(EcsGameTimer timer)
    {
        for (int i = 0; i < 8; i++)
        {
            if (SpecialInnocent.InnocentCount < 9)
            {
                SpecialInnocent add = new SpecialInnocent();
                add.X = Body.X + Util.Random(-100, 100);
                add.Y = Body.Y + Util.Random(-100, 100);
                add.Z = Body.Z;
                add.CurrentRegionID = 276;
                add.RespawnInterval = -1;
                add.Heading = Body.Heading;
                add.AddToWorld();
            }
        }
        CanSpawnAdds = false;
        return 0;
    }
    private bool RemoveAdds = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            SpecialInnocent.InnocentCount = 0;
            CanSpawnAdds = false;
            if (!RemoveAdds)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc.Brain is SpecialInnocentBrain)
                    {
                        npc.RemoveFromWorld();
                    }
                }
                RemoveAdds = true;
            }
        }
        if (HasAggro && Body.InCombat && Body.TargetObject != null)
        {
            RemoveAdds = false;
            if(SpecialInnocent.InnocentCount<9 && CanSpawnAdds == false)
            {
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnAdd), Util.Random(20000, 30000));
                CanSpawnAdds=true;
            }
            foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
            {
                if (npc.Brain is SpecialInnocentBrain)
                {
                    AddAggroListTo(npc.Brain as SpecialInnocentBrain);
                }
            }
            Body.CastSpell(RedLady_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.Think();
    }
    public Spell m_RedLady_DD;
    public Spell RedLady_DD
    {
        get
        {
            if (m_RedLady_DD == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.RecastDelay = Util.Random(25, 35);
                spell.ClientEffect = 4445;
                spell.Icon = 4445;
                spell.TooltipId = 4445;
                spell.Damage = 100;
                spell.Duration = 30;
                spell.Frequency = 30;
                spell.Name = "Soul Drain";
                spell.Description = "Inflicts 100 damage to the target every 3 sec for 30 seconds";
                spell.Message1 = "You are wracked with pain!";
                spell.Message2 = "{0} is wracked with pain!";
                spell.Message3 = "You look healthy again.";
                spell.Message4 = "{0} looks healthy again.";
                spell.Radius = 350;
                spell.Range = 1500;
                spell.SpellID = 11790;
                spell.Target = ESpellTarget.ENEMY.ToString();
                spell.Type = ESpellType.DamageOverTime.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Matter;
                m_RedLady_DD = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_RedLady_DD);
            }
            return m_RedLady_DD;
        }
    }
}
#endregion Red Lady

public class SpecialInnocentBrain : StandardMobBrain
{
    public SpecialInnocentBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 700;
    }
    public override void Think()
    {
        base.Think();
    }
}