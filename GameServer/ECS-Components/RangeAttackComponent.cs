using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class RangeAttackComponent
    {
        private GameLiving m_owner;

        public RangeAttackComponent(GameLiving owner)
        {
            m_owner = owner;
        }

        internal const string RANGED_ATTACK_START = "RangedAttackStart";
        public const int RANGE_ATTACK_ENDURANCE = 5;
        public const int CRITICAL_SHOT_ENDURANCE = 10;
        protected eRangedAttackState m_rangedAttackState;
        protected eRangedAttackType m_rangedAttackType;
        protected eActiveQuiverSlot m_activeQuiverSlot;
        protected WeakReference m_rangeAttackAmmo = new WeakRef(null);
        protected WeakReference m_rangeAttackTarget = new WeakRef(null);
        protected InventoryItem m_ammo;
        protected bool m_isAmmoCompatible;

        public eRangedAttackState RangedAttackState
        {
            get => m_rangedAttackState;
            set => m_rangedAttackState = value;
        }

        public eRangedAttackType RangedAttackType
        {
            get => m_rangedAttackType;
            set => m_rangedAttackType = value;
        }

        public eActiveQuiverSlot ActiveQuiverSlot
        {
            get => m_activeQuiverSlot;
            set => m_activeQuiverSlot = value;
        }

        public InventoryItem Ammo => m_ammo;
        public bool IsAmmoCompatible => m_isAmmoCompatible;

        private InventoryItem GetAmmoFromInventory(eObjectType ammoType)
        {
            switch (ActiveQuiverSlot)
            {
                case eActiveQuiverSlot.First:
                    return m_owner.Inventory.GetItem(eInventorySlot.FirstQuiver);
                case eActiveQuiverSlot.Second:
                    return m_owner.Inventory.GetItem(eInventorySlot.SecondQuiver);
                case eActiveQuiverSlot.Third:
                    return m_owner.Inventory.GetItem(eInventorySlot.ThirdQuiver);
                case eActiveQuiverSlot.Fourth:
                    return m_owner.Inventory.GetItem(eInventorySlot.FourthQuiver);
                case eActiveQuiverSlot.None:
                    return m_owner.Inventory.GetFirstItemByObjectType((int)ammoType, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            }

            return null;
        }

        public InventoryItem UpdateAmmo(InventoryItem weapon)
        {
            m_ammo = null;
            m_isAmmoCompatible = true;

            if (m_owner is not GamePlayer || weapon == null)
                return null;

            switch (weapon.Object_Type)
            {
                case (int)eObjectType.Thrown:
                    m_ammo = m_owner.Inventory.GetItem(eInventorySlot.DistanceWeapon);
                    break;
                case (int)eObjectType.Crossbow:
                    m_ammo = GetAmmoFromInventory(eObjectType.Bolt);
                    m_isAmmoCompatible = m_ammo.Object_Type == (int)eObjectType.Bolt;
                    break;
                case (int)eObjectType.Longbow:
                case (int)eObjectType.CompositeBow:
                case (int)eObjectType.RecurvedBow:
                case (int)eObjectType.Fired:
                    m_ammo = GetAmmoFromInventory(eObjectType.Arrow);
                    m_isAmmoCompatible = m_ammo.Object_Type == (int)eObjectType.Arrow;
                    break;
            }

            return m_ammo;
        }

        public GameObject Target
        {
            get
            {
                if (m_owner is GamePlayer)
                {
                    GameObject target = (GameObject)m_rangeAttackTarget.Target;

                    if (target == null || target.ObjectState != eObjectState.Active)
                        target = m_owner.TargetObject;

                    return target;
                }
                else
                    return m_owner.TargetObject;
            }
            set => m_rangeAttackTarget.Target = value;
        }

        /// <summary>
        /// Check the range attack state and decides what to do. Called inside the AttackTimerCallback.
        /// </summary>
        public eCheckRangeAttackStateResult CheckRangeAttackState(GameObject target)
        {
            if (m_owner is GamePlayer playerOwner)
            {
                long attackStart = m_owner.TempProperties.getProperty<long>(RANGED_ATTACK_START);

                // Failsafe, but it should never happen.
                if (attackStart == 0)
                {
                    attackStart = GameLoop.GameLoopTime;
                    playerOwner.TempProperties.setProperty(RANGED_ATTACK_START, attackStart);
                }

                if ((GameLoop.GameLoopTime - attackStart) > 15000 && playerOwner.ActiveWeapon.Object_Type != (int)eObjectType.Crossbow)
                {
                    playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.TooTired"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return eCheckRangeAttackStateResult.Stop;
                }

                // This state is set when the player wants to fire.
                if (RangedAttackState is eRangedAttackState.Fire or eRangedAttackState.AimFire or eRangedAttackState.AimFireReload)
                {
                    // Clean the RangeAttackTarget at the first shot try even if failed.
                    Target = null;

                    if (target is null or not GameLiving)
                    {
                        // Volley check to avoid spam.
                        ECSGameEffect volley = EffectListService.GetEffectOnTarget(playerOwner, eEffect.Volley);

                        if (volley == null)
                            playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "System.MustSelectTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (!playerOwner.IsWithinRadius(target, playerOwner.attackComponent.AttackRange))
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.TooFarAway", target.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else if (!playerOwner.TargetInView)  // TODO: Wrong, must be checked with the target parameter and not with the targetObject.
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantSeeTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else if (!playerOwner.IsObjectInFront(target, 90))
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.NotInView", target.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    else if (UpdateAmmo(playerOwner.ActiveWeapon) == null)
                        // Another check for ammo just before firing.
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.MustSelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else if (!m_isAmmoCompatible)
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else if (GameServer.ServerRules.IsAllowedToAttack(playerOwner, (GameLiving)target, false))
                    {
                        if (target is GameLiving living &&
                            RangedAttackType == eRangedAttackType.Critical &&
                            (living.CurrentSpeed > 90 || // Walk speed == 85, hope that's what they mean.
                            (living.attackComponent.AttackState && living.InCombat) || // Maybe not 100% correct.
                            EffectListService.GetEffectOnTarget(living, eEffect.Mez) != null))
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
                            playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantCritical"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            RangedAttackType = eRangedAttackType.Normal;
                        }

                        return eCheckRangeAttackStateResult.Fire;
                    }

                    RangedAttackState = eRangedAttackState.ReadyToFire;
                    return eCheckRangeAttackStateResult.Hold;
                }

                if (RangedAttackState == eRangedAttackState.Aim)
                {
                    ECSGameEffect volley = EffectListService.GetEffectOnTarget(playerOwner, eEffect.Volley);//volley check to avoid spam
                    if (volley == null)
                    {
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.ReadyToFire"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        RangedAttackState = eRangedAttackState.ReadyToFire;
                        return eCheckRangeAttackStateResult.Hold;
                    }
                }
                else if (RangedAttackState == eRangedAttackState.ReadyToFire)
                    return eCheckRangeAttackStateResult.Hold;

                return eCheckRangeAttackStateResult.Fire;
            }
            else
            {
                if (!m_owner.IsWithinRadius(target, m_owner.attackComponent.AttackRange))
                    return eCheckRangeAttackStateResult.Stop;

                return eCheckRangeAttackStateResult.Fire;
            }
        }

        public void RemoveEnduranceAndAmmoOnShot()
        {
            int arrowRecoveryChance = m_owner.GetModified(eProperty.ArrowRecovery);

            if (arrowRecoveryChance == 0 || Util.Chance(100 - arrowRecoveryChance))
                m_owner.Inventory.RemoveCountFromStack(m_ammo, 1);

            if (RangedAttackType == eRangedAttackType.Critical)
                m_owner.Endurance -= CRITICAL_SHOT_ENDURANCE;
            else if (RangedAttackType == eRangedAttackType.RapidFire && m_owner.GetAbilityLevel(Abilities.RapidFire) == 2)
                m_owner.Endurance -= (int)Math.Ceiling(RANGE_ATTACK_ENDURANCE / 2.0);
            else
                m_owner.Endurance -= RANGE_ATTACK_ENDURANCE;
        }
    }
}
