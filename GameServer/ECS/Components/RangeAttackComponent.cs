using System;
using Core.Database;
using Core.GS.PacketHandler;
using Core.Language;

namespace Core.GS
{
    public class RangeAttackComponent
    {
        private GameLiving m_owner;

        public RangeAttackComponent(GameLiving owner)
        {
            m_owner = owner;
        }

        internal const string RANGED_ATTACK_START = "RangedAttackStart";
        public const int DEFAULT_ENDURANCE_COST = 5;
        public const int CRITICAL_SHOT_ENDURANCE_COST = 10;
        public const int VOLLEY_ENDURANCE_COST = 15;
        public const int PROJECTILE_FLIGHT_SPEED = 1800; // 1800 units per second. Live value is unknown, but DoL had 1500. Also affects throwing weapons.
        public const int MAX_DRAW_DURATION = 15000;
        public GameObject AutoFireTarget { get; set; } // Used to shoot at a different target than the one currently selected. Always null for NPCs.
        public ERangedAttackState RangedAttackState { get; set; }
        public ERangedAttackType RangedAttackType { get; set; }
        public EActiveQuiverSlot ActiveQuiverSlot { get; set; }
        public DbInventoryItem Ammo { get; private set; }
        public bool IsAmmoCompatible { get; private set; }

        private DbInventoryItem GetAmmoFromInventory(EObjectType ammoType)
        {
            switch (ActiveQuiverSlot)
            {
                case EActiveQuiverSlot.First:
                    return m_owner.Inventory.GetItem(EInventorySlot.FirstQuiver);
                case EActiveQuiverSlot.Second:
                    return m_owner.Inventory.GetItem(EInventorySlot.SecondQuiver);
                case EActiveQuiverSlot.Third:
                    return m_owner.Inventory.GetItem(EInventorySlot.ThirdQuiver);
                case EActiveQuiverSlot.Fourth:
                    return m_owner.Inventory.GetItem(EInventorySlot.FourthQuiver);
                case EActiveQuiverSlot.None:
                    return m_owner.Inventory.GetFirstItemByObjectType((int)ammoType, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack);
            }

            return null;
        }

        public DbInventoryItem UpdateAmmo(DbInventoryItem weapon)
        {
            Ammo = null;
            IsAmmoCompatible = true;

            if (m_owner is not GamePlayer || weapon == null)
                return null;

            switch (weapon.Object_Type)
            {
                case (int)EObjectType.Thrown:
                    Ammo = m_owner.Inventory.GetItem(EInventorySlot.DistanceWeapon);
                    break;
                case (int)EObjectType.Crossbow:
                    Ammo = GetAmmoFromInventory(EObjectType.Bolt);
                    IsAmmoCompatible = Ammo?.Object_Type == (int)EObjectType.Bolt;
                    break;
                case (int)EObjectType.Longbow:
                case (int)EObjectType.CompositeBow:
                case (int)EObjectType.RecurvedBow:
                case (int)EObjectType.Fired:
                    Ammo = GetAmmoFromInventory(EObjectType.Arrow);
                    IsAmmoCompatible = Ammo?.Object_Type == (int)EObjectType.Arrow;
                    break;
            }

            return Ammo;
        }

        /// <summary>
        /// Check the range attack state and decides what to do. Called inside the AttackTimerCallback.
        /// </summary>
        public ECheckRangeAttackStateResult CheckRangeAttackState(GameObject target)
        {
            if (m_owner is GamePlayer playerOwner)
            {
                long attackStart = m_owner.TempProperties.GetProperty<long>(RANGED_ATTACK_START);

                // Failsafe, but it should never happen.
                if (attackStart == 0)
                {
                    attackStart = GameLoop.GameLoopTime;
                    playerOwner.TempProperties.SetProperty(RANGED_ATTACK_START, attackStart);
                }

                if ((GameLoop.GameLoopTime - attackStart) > MAX_DRAW_DURATION && playerOwner.ActiveWeapon.Object_Type != (int)EObjectType.Crossbow)
                {
                    playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.TooTired"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    return ECheckRangeAttackStateResult.Stop;
                }

                // This state is set when the player wants to fire.
                if (RangedAttackState is ERangedAttackState.Fire or ERangedAttackState.AimFire or ERangedAttackState.AimFireReload)
                {
                    // Clean the RangeAttackTarget at the first shot try even if failed.
                    AutoFireTarget = null;

                    if (target is null or not GameLiving)
                    {
                        // Volley check to avoid spam.
                        EcsGameEffect volley = EffectListService.GetEffectOnTarget(playerOwner, EEffect.Volley);

                        if (volley == null)
                            playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "System.MustSelectTarget"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    }
                    else if (!playerOwner.IsWithinRadius(target, playerOwner.attackComponent.AttackRange))
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.TooFarAway", target.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    else if (!playerOwner.TargetInView)  // TODO: Wrong, must be checked with the target parameter and not with the targetObject.
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantSeeTarget"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    else if (!playerOwner.IsObjectInFront(target, 90))
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.NotInView", target.GetName(0, true)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                    else if (UpdateAmmo(playerOwner.ActiveWeapon) == null)
                        // Another check for ammo just before firing.
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.MustSelectQuiver"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                    else if (!IsAmmoCompatible)
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantUseQuiver"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                    else if (GameServer.ServerRules.IsAllowedToAttack(playerOwner, (GameLiving)target, false))
                    {
                        if (target is GameLiving living &&
                            RangedAttackType == ERangedAttackType.Critical &&
                            (living.CurrentSpeed > 90 || // Walk speed == 85, hope that's what they mean.
                            (living.attackComponent.AttackState && living.InCombat) || // Maybe not 100% correct.
                            EffectListService.GetEffectOnTarget(living, EEffect.Mez) != null))
                        {
                            /*
                             * http://rothwellhome.org/guides/archery.htm
                             * Please note that critical shot will work against targets that are:
                             * sitting, standing still (which includes standing in combat mode but
                             * not actively swinging at something), walking, moving backwards,
                             * strafing, or casting a spell. Critical shot will not work against
                             * targets that are: running, in active combat (swinging at something),
                             * or mezzed. Stunned targets may be critical shot once any timers from
                             * active combat have expired if they are not yet free to act; i.e.:
                             * they may not be critical shot until their weapon delay timer has run
                             * out after their last attack, they may be critical shot during the
                             * period between the weapon delay running out and the stun wearing off,
                             * and they may not be critical shot once they have begun swinging again.
                             * If the target was in melee with an archer, the critical shot may not
                             * be drawn against them until after their weapon delay has run out or it
                             * will be interrupted.  This means that the scout's shield stun is much
                             * less effective against large weapon wielders (who have longer weapon
                             * delays) than against fast piercing/thrusting weapon wielders.
                             */

                            // TODO: More checks?
                            playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantCritical"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            RangedAttackType = ERangedAttackType.Normal;
                        }

                        return ECheckRangeAttackStateResult.Fire;
                    }

                    RangedAttackState = ERangedAttackState.ReadyToFire;
                    return ECheckRangeAttackStateResult.Hold;
                }

                if (RangedAttackState == ERangedAttackState.Aim)
                {
                    EcsGameEffect volley = EffectListService.GetEffectOnTarget(playerOwner, EEffect.Volley);//volley check to avoid spam
                    if (volley == null)
                    {
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.ReadyToFire"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
                        RangedAttackState = ERangedAttackState.ReadyToFire;
                        return ECheckRangeAttackStateResult.Hold;
                    }
                }
                else if (RangedAttackState == ERangedAttackState.ReadyToFire)
                    return ECheckRangeAttackStateResult.Hold;

                return ECheckRangeAttackStateResult.Fire;
            }
            else
            {
                if (!m_owner.IsWithinRadius(target, m_owner.attackComponent.AttackRange))
                    return ECheckRangeAttackStateResult.Stop;

                return ECheckRangeAttackStateResult.Fire;
            }
        }

        public void RemoveEnduranceAndAmmoOnShot()
        {
            int arrowRecoveryChance = m_owner.GetModified(EProperty.ArrowRecovery);

            if (arrowRecoveryChance == 0 || Util.Chance(100 - arrowRecoveryChance))
                m_owner.Inventory.RemoveCountFromStack(Ammo, 1);

            if (RangedAttackType == ERangedAttackType.Critical)
                m_owner.Endurance -= CRITICAL_SHOT_ENDURANCE_COST;
            else if (RangedAttackType == ERangedAttackType.RapidFire && m_owner.GetAbilityLevel(Abilities.RapidFire) == 2)
                m_owner.Endurance -= (int)Math.Ceiling(DEFAULT_ENDURANCE_COST / 2.0);
            else if (RangedAttackType == ERangedAttackType.Volley)
                m_owner.Endurance -= VOLLEY_ENDURANCE_COST;
            else
                m_owner.Endurance -= DEFAULT_ENDURANCE_COST;
        }
    }
}
