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

        internal const string RANGE_ATTACK_HOLD_START = " RangeAttackHoldStart";
        public const int RANGE_ATTACK_ENDURANCE = 5;
        public const int CRITICAL_SHOT_ENDURANCE = 10;
        protected eRangedAttackState m_rangedAttackState;
        protected eRangedAttackType m_rangedAttackType;
        protected eActiveQuiverSlot m_activeQuiverSlot;
        protected WeakReference m_rangeAttackAmmo = new WeakRef(null);
        protected WeakReference m_rangeAttackTarget = new WeakRef(null);

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

        public virtual eActiveQuiverSlot ActiveQuiverSlot
        {
            get => m_activeQuiverSlot;
            set => m_activeQuiverSlot = value;
        }

        public virtual bool IsRangedAmmoCompatibleWithActiveWeapon()
        {
            GamePlayer playerOwner = m_owner as GamePlayer;
            InventoryItem weapon = playerOwner.attackComponent.AttackWeapon;

            if (weapon != null)
            {
                switch ((eObjectType)weapon.Object_Type)
                {
                    case eObjectType.Crossbow:
                    case eObjectType.Longbow:
                    case eObjectType.CompositeBow:
                    case eObjectType.RecurvedBow:
                    case eObjectType.Fired:
                        {
                            if (ActiveQuiverSlot != eActiveQuiverSlot.None)
                            {
                                InventoryItem ammo = null;
                                switch (ActiveQuiverSlot)
                                {
                                    case eActiveQuiverSlot.Fourth: ammo = playerOwner.Inventory.GetItem(eInventorySlot.FourthQuiver); break;
                                    case eActiveQuiverSlot.Third: ammo = playerOwner.Inventory.GetItem(eInventorySlot.ThirdQuiver); break;
                                    case eActiveQuiverSlot.Second: ammo = playerOwner.Inventory.GetItem(eInventorySlot.SecondQuiver); break;
                                    case eActiveQuiverSlot.First: ammo = playerOwner.Inventory.GetItem(eInventorySlot.FirstQuiver); break;
                                }

                                if (ammo == null)
                                    return false;

                                return weapon.Object_Type == (int)eObjectType.Crossbow ? ammo.Object_Type == (int)eObjectType.Bolt : ammo.Object_Type == (int)eObjectType.Arrow;
                            }
                        }

                        break;
                }
            }

            return true;
        }

        public InventoryItem Ammo
        {
            get
            {
                if (m_owner is GamePlayer)
                {
                    // TODO: Ammo should be saved on start of every range attack and used here.
                    InventoryItem ammo = null;
                    InventoryItem weapon = m_owner.attackComponent.AttackWeapon;

                    if (weapon != null)
                    {
                        switch (weapon.Object_Type)
                        {
                            case (int)eObjectType.Thrown: ammo = m_owner.Inventory.GetItem(eInventorySlot.DistanceWeapon); break;
                            case (int)eObjectType.Crossbow:
                            case (int)eObjectType.Longbow:
                            case (int)eObjectType.CompositeBow:
                            case (int)eObjectType.RecurvedBow:
                            case (int)eObjectType.Fired:
                                {
                                    switch (ActiveQuiverSlot)
                                    {
                                        case eActiveQuiverSlot.First: ammo = m_owner.Inventory.GetItem(eInventorySlot.FirstQuiver); break;
                                        case eActiveQuiverSlot.Second: ammo = m_owner.Inventory.GetItem(eInventorySlot.SecondQuiver); break;
                                        case eActiveQuiverSlot.Third: ammo = m_owner.Inventory.GetItem(eInventorySlot.ThirdQuiver); break;
                                        case eActiveQuiverSlot.Fourth: ammo = m_owner.Inventory.GetItem(eInventorySlot.FourthQuiver); break;
                                        case eActiveQuiverSlot.None:
                                            eObjectType findType = eObjectType.Arrow;

                                            if (weapon.Object_Type == (int)eObjectType.Crossbow)
                                                findType = eObjectType.Bolt;

                                            ammo = m_owner.Inventory.GetFirstItemByObjectType((int)findType, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

                                            break;
                                    }
                                }

                                break;
                        }
                    }

                    return ammo;
                }
                else
                    return null;
            }
            set
            {
                if (m_owner is GamePlayer)
                    m_rangeAttackAmmo.Target = value;
            }
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
                long holdStart = m_owner.TempProperties.getProperty<long>(RANGE_ATTACK_HOLD_START);

                if (holdStart == 0)
                {
                    holdStart = GameLoop.GameLoopTime;
                    playerOwner.TempProperties.setProperty(RANGE_ATTACK_HOLD_START, holdStart);
                }

                if ((GameLoop.GameLoopTime - holdStart) > 15000 && playerOwner.attackComponent.AttackWeapon.Object_Type != (int)eObjectType.Crossbow)
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
                    else if (Ammo == null)
                        // Another check for ammo just before firing.
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.MustSelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else if (!IsRangedAmmoCompatibleWithActiveWeapon())
                        playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else if (GameServer.ServerRules.IsAllowedToAttack(playerOwner, (GameLiving)target, false))
                    {
                        if (target is GameLiving living &&
                            RangedAttackType == eRangedAttackType.Critical &&
                            (living.CurrentSpeed > 90 || // >alk speed == 85, hope that's what they mean.
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
                m_owner.Inventory.RemoveCountFromStack(Ammo, 1);

            if (RangedAttackType == eRangedAttackType.Critical)
                m_owner.Endurance -= CRITICAL_SHOT_ENDURANCE;
            else if (RangedAttackType == eRangedAttackType.RapidFire && m_owner.GetAbilityLevel(Abilities.RapidFire) == 2)
                m_owner.Endurance -= (int)Math.Ceiling(RANGE_ATTACK_ENDURANCE / 2.0);
            else
                m_owner.Endurance -= RANGE_ATTACK_ENDURANCE;
        }
    }
}
