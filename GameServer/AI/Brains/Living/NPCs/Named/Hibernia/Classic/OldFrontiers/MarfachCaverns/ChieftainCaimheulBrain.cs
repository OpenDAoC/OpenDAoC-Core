using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class ChieftainCaimheulBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ChieftainCaimheulBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 1500;
    }
    public static bool Phase2 = false;
    public static bool CanWalk = false;
    public static bool IsPulled = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8821);
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Phase2 = false;
            CanWalk = false;
            IsPulled = false;
            Body.Strength = npcTemplate.Strength;
            Body.SwitchWeapon(EActiveWeaponSlot.Standard);
            Body.MeleeDamageType = EDamageType.Slash;
            Body.VisibleActiveWeaponSlots = 16;
            if (!Body.Styles.Contains(ChieftainCaimheul.Taunt))
                Body.Styles.Add(ChieftainCaimheul.Taunt);
            if (!Body.Styles.Contains(ChieftainCaimheul.slam))
                Body.Styles.Add(ChieftainCaimheul.slam); 
        }
        if (Body.InCombat && HasAggro && Body.TargetObject != null)
        {
            if (IsPulled == false)
            {
                foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "CaimheulBaf")
                        {
                            AddAggroListTo(npc.Brain as StandardMobBrain);
                        }
                    }
                }
                IsPulled = true;
            }
            if (Body.TargetObject != null)
            {
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(8821);
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = Body.TargetObject.GetAngle(Body);
                if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                {
                    CanWalk = false;//reset flag 
                }
                if (Phase2 == false)
                {
                    if (!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        Body.Strength = npcTemplate.Strength;
                        Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Body.VisibleActiveWeaponSlots = 16;
                        Body.styleComponent.NextCombatStyle = ChieftainCaimheul.slam;//check if target has stun or immunity if not slam
                        Body.BlockChance = 50;
                        Body.ParryChance = 0;
                        Body.MeleeDamageType = EDamageType.Crush;
                    }
                    if (living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                    {
                        if (CanWalk == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkSide), 500);
                            CanWalk = true;
                        }
                    }
                    if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                    {
                        Body.Strength = 400;
                        Body.BlockChance = 0;
                        Body.ParryChance = 50;
                        Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Body.MeleeDamageType = EDamageType.Thrust;
                        Body.VisibleActiveWeaponSlots = (byte)EActiveWeaponSlot.TwoHanded;
                        Body.styleComponent.NextCombatBackupStyle = ChieftainCaimheul.SideStyle;
                        Body.styleComponent.NextCombatStyle = ChieftainCaimheul.SideFollowUp;
                    }
                    else if(!living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && living.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        Body.Strength = npcTemplate.Strength;
                        Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Body.VisibleActiveWeaponSlots = 16;
                        Body.styleComponent.NextCombatStyle = ChieftainCaimheul.Taunt;
                        Body.MeleeDamageType = EDamageType.Slash;
                        Body.BlockChance = 50;
                        Body.ParryChance = 0;
                    }
                }
                if(Body.HealthPercent <= 50 && Phase2==false)
                {
                    Phase2 = true;
                }
                if(Phase2)
                {
                    Body.Strength = 400;
                    Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                    Body.VisibleActiveWeaponSlots = (byte)EActiveWeaponSlot.TwoHanded;
                    Body.BlockChance = 0;
                    Body.ParryChance = 50;
                    if(Body.Styles.Contains(ChieftainCaimheul.slam))
                        Body.Styles.Remove(ChieftainCaimheul.slam);
                    if(Body.Styles.Contains(ChieftainCaimheul.Taunt))
                        Body.Styles.Remove(ChieftainCaimheul.Taunt);
                    Body.MeleeDamageType = EDamageType.Thrust;

                    if (living.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                    {
                        if (CanWalk == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkSide), 500);
                            CanWalk = true;
                        }
                    }
                    if ((angle >= 45 && angle < 150) || (angle >= 210 && angle < 315))//side
                    {
                        Body.styleComponent.NextCombatBackupStyle = ChieftainCaimheul.SideStyle;
                        Body.styleComponent.NextCombatStyle = ChieftainCaimheul.SideFollowUp;
                    }
                    else
                    {
                        Body.styleComponent.NextCombatBackupStyle = ChieftainCaimheul.taunt2h;
                        Body.styleComponent.NextCombatStyle = ChieftainCaimheul.AfterParry;
                    }
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