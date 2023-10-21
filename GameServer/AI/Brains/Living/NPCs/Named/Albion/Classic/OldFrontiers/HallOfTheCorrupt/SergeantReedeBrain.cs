using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class SergeantReedeBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SergeantReedeBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public static bool CanWalk = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            this.Body.Health = this.Body.MaxHealth;
            CanWalk = false;
        }
        if (Body.InCombat && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = Body.TargetObject.GetAngle(Body);
                if (living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                {
                    if(CanWalk==false)
                    {
                        new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkSide), 500);
                        CanWalk = true;
                    }
                }
                if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                {
                    Body.styleComponent.NextCombatBackupStyle = SergeantReede.Side;
                    Body.styleComponent.NextCombatStyle = SergeantReede.SideFollowUp;
                }
                else
                {
                    Body.styleComponent.NextCombatBackupStyle = SergeantReede.Taunt;
                    Body.styleComponent.NextCombatStyle = SergeantReede.AfterEvade;
                }
                if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                {
                    CanWalk = false;//reset flag 
                }
            }
        }
        base.Think();
    }
    public int WalkSide(EcsGameTimer timer)
    {
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (Body.TargetObject is GameLiving)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = living.GetAngle(Body);
                Point2D positionalPoint;
                positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (90 * (4096.0 / 360.0))), 65);
                //Body.WalkTo(positionalPoint.X, positionalPoint.Y, living.Z, 280);
                Body.X = positionalPoint.X;
                Body.Y = positionalPoint.Y;
                Body.Z = living.Z;
                Body.Heading = 1250;
            }
        }
        return 0;
    }
}