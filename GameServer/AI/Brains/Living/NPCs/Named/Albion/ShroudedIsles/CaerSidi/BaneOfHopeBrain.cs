using Core.Database.Tables;
using Core.GS.ECS;

namespace Core.GS.AI.Brains;

public class BaneOfHopeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BaneOfHopeBrain()
        :base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public static GameLiving TeleportTarget = null;
    public static bool CanPoison = false;
    public override void OnAttackedByEnemy(AttackData ad)
    {
        if(ad != null && ad.Attacker != null && Body.TargetObject != ad.Attacker && CanPoison==false)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (Util.Chance(25))
            {
                GameObject oldTarget = Body.TargetObject;
                Body.TurnTo(ad.Attacker);
                Body.TargetObject = ad.Attacker;
                TeleportTarget = ad.Attacker;
                if (ad.Attacker.IsAlive)
                {
                    Body.CastSpell(BaneOfHope_Aoe_Dot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(TeleportEnemy), 4500);
                    CanPoison = true;
                }
                if (oldTarget != null) Body.TargetObject = oldTarget;
            }
        }
        base.OnAttackedByEnemy(ad);
    }
    public int TeleportEnemy(EcsGameTimer timer)
    {
        if (TeleportTarget != null && HasAggro)
        {
            switch (Util.Random(1, 3))
            {
                case 1: TeleportTarget.MoveTo(Body.CurrentRegionID, 34496, 30879, 14551, 1045); break;
                case 2: TeleportTarget.MoveTo(Body.CurrentRegionID, 37377, 30154, 13973, 978); break;
                case 3: TeleportTarget.MoveTo(Body.CurrentRegionID, 38292, 31794, 13940, 986); break;
            }
        }
        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetTeleport), Util.Random(12000,18000));
        return 0;
    }
    private int ResetTeleport(EcsGameTimer timer)
    {
        CanPoison = false;
        TeleportTarget = null;
        return 0;
    }
    public override void Think()
    {
        if(!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            CanPoison = false;
            TeleportTarget = null;
        }
        base.Think();
    }
    private Spell m_BaneOfHope_Aoe_Dot;
    public Spell BaneOfHope_Aoe_Dot
    {
        get
        {
            if (m_BaneOfHope_Aoe_Dot == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 4;
                spell.RecastDelay = 0;
                spell.ClientEffect = 4445;
                spell.Icon = 4445;
                spell.Damage = 150;
                spell.Name = "Essence of Souls";
                spell.Description = "Inflicts powerful magic damage to the target, then target dies in painful agony.";
                spell.Message1 = "You are wracked with pain!";
                spell.Message2 = "{0} is wracked with pain!";
                spell.Message3 = "You look healthy again.";
                spell.Message4 = "{0} looks healthy again.";
                spell.TooltipId = 4445;
                spell.Range = 1500;
                spell.Radius = 600;
                spell.Duration = 45;
                spell.Frequency = 40; //dot tick every 4s
                spell.SpellID = 11783;
                spell.Target = "Enemy";
                spell.Type = "DamageOverTime";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int)EDamageType.Spirit; //Spirit DMG Type
                m_BaneOfHope_Aoe_Dot = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BaneOfHope_Aoe_Dot);
            }

            return m_BaneOfHope_Aoe_Dot;
        }
    }
}