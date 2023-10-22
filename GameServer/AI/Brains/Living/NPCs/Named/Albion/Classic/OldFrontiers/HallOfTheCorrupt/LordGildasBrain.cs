using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.AI.Brains;

public class LordGildasBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LordGildasBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 500;
    }
    public static bool CanWalk = false;
    public static bool Stage2 = false;
    public static bool Reset_Gildas = false;
    public int ResetGildas(EcsGameTimer timer)
    {
        Reset_Gildas = false;
        return 0;
    }
    public override void Think()
    {       
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            CanWalk = false;                              
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            if (Reset_Gildas == false)
            {
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
                GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
                template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
                template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
                template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
                template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
                template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
                template.AddNPCEquipment(EInventorySlot.RightHandWeapon, 4, 0, 0);
                template.AddNPCEquipment(EInventorySlot.LeftHandWeapon, 1077, 0, 0);
                template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 7, 0, 0);
                Body.Inventory = template.CloseTemplate();
                Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                Body.VisibleActiveWeaponSlots = 16;
                if (!Body.Styles.Contains(LordGildas.slam))
                    Body.Styles.Add(LordGildas.slam);
                if (!Body.Styles.Contains(LordGildas.taunt))
                    Body.Styles.Add(LordGildas.taunt);
                if (!Body.Styles.Contains(LordGildas.BackStyle))
                    Body.Styles.Add(LordGildas.BackStyle);
                if (!Body.Styles.Contains(LordGildas.Taunt2h))
                    Body.Styles.Remove(LordGildas.Taunt2h);
                Body.Strength = npcTemplate.Strength;
                Body.ParryChance = npcTemplate.ParryChance;
                Body.BlockChance = npcTemplate.BlockChance;
                Stage2 = false;
                Body.styleComponent.NextCombatStyle = LordGildas.taunt;
                new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGildas), 7000);
                Reset_Gildas = true;
            }
        }
        if (HasAggro && Body.TargetObject != null)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            if (Body.TargetObject != null)
            {
                if (Stage2 == false)
                {
                    INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(7719);
                    float angle = Body.TargetObject.GetAngle(Body);
                    if (angle >= 160 && angle <= 200)
                    {
                        Body.Strength = 400;
                        Body.ParryChance = 60;
                        Body.BlockChance = 0;
                        Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                        Body.VisibleActiveWeaponSlots = 34;
                        Body.styleComponent.NextCombatBackupStyle = LordGildas.Taunt2h;
                        Body.styleComponent.NextCombatStyle = LordGildas.BackStyle;//do backstyle when angle allow it
                    }
                    else
                    {
                        Body.Strength = npcTemplate.Strength;
                        Body.ParryChance = 25;
                        Body.BlockChance = 75;
                        Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Body.VisibleActiveWeaponSlots = 16;
                        Body.styleComponent.NextCombatStyle = LordGildas.taunt;//if not backstyle for angle then do taunt
                    }
                    if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun) && !target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        Body.Strength = npcTemplate.Strength;
                        Body.SwitchWeapon(EActiveWeaponSlot.Standard);
                        Body.VisibleActiveWeaponSlots = 16;
                        Body.ParryChance = 25;
                        Body.BlockChance = 80;
                        Body.styleComponent.NextCombatStyle = LordGildas.slam;//check if target has stun or immunity if not slam
                    }
                    if (target.effectListComponent.ContainsEffectForEffectType(EEffect.Stun))
                    {
                        if (CanWalk == false)
                        {
                            new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(WalkBack), 500);//if target got stun then start timer to run behind it
                            CanWalk = true;
                        }
                    }
                    if (!target.effectListComponent.ContainsEffectForEffectType(EEffect.StunImmunity))
                    {
                        CanWalk = false;//reset flag so can slam again
                    }
                }
            }
            if(Body.HealthPercent < 50 && Stage2==false)//boss change to polearm armsman
            {
                GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
                template.AddNPCEquipment(EInventorySlot.TorsoArmor, 46, 0, 0, 6);//modelID,color,effect,extension
                template.AddNPCEquipment(EInventorySlot.ArmsArmor, 48, 0);
                template.AddNPCEquipment(EInventorySlot.LegsArmor, 47, 0);
                template.AddNPCEquipment(EInventorySlot.HandsArmor, 49, 0, 0, 4);
                template.AddNPCEquipment(EInventorySlot.FeetArmor, 50, 0, 0, 5);
                template.AddNPCEquipment(EInventorySlot.Cloak, 91, 0, 0, 0);
                template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 70, 0, 0);
                Body.Inventory = template.CloseTemplate();
                if(Body.Styles.Contains(LordGildas.slam))
                    Body.Styles.Remove(LordGildas.slam);
                if (Body.Styles.Contains(LordGildas.taunt))
                    Body.Styles.Remove(LordGildas.taunt);
                if (Body.Styles.Contains(LordGildas.BackStyle))
                    Body.Styles.Remove(LordGildas.BackStyle);
                if (Body.Styles.Contains(LordGildas.Taunt2h))
                    Body.Styles.Remove(LordGildas.Taunt2h);
                Stage2 = true;
            }
            if(Stage2 == true)
            {
                Body.styleComponent.NextCombatBackupStyle = LordGildas.PoleAnytimer;
                Body.styleComponent.NextCombatStyle = LordGildas.AfterStyle;
                Body.Strength = 340;
                Body.SwitchWeapon(EActiveWeaponSlot.TwoHanded);
                Body.MeleeDamageType = EDamageType.Crush;
                Body.VisibleActiveWeaponSlots = 34;
                Body.ParryChance = 60;
                Body.BlockChance = 0;
            }
        }
        base.Think();
    }
    public int WalkBack(EcsGameTimer timer)
    {
        if (Body.InCombat && HasAggro && Body.TargetObject != null && Stage2==false)
        {
            if (Body.TargetObject is GameLiving)
            {
                GameLiving living = Body.TargetObject as GameLiving;
                float angle = living.GetAngle(Body);
                Point2D positionalPoint;
                positionalPoint = living.GetPointFromHeading((ushort)(living.Heading + (180 * (4096.0 / 360.0))), 65);
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