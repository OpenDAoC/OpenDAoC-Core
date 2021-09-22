using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class RangeAttackComponent
    {
        GameLiving owner;

        public RangeAttackComponent(GameLiving owner)
        {
            this.owner = owner;
        }

        /// <summary>
		/// The time someone can hold a ranged attack before tiring
		/// </summary>
		internal const string RANGE_ATTACK_HOLD_START = " RangeAttackHoldStart";
        /// <summary>
        /// Endurance used for normal range attack
        /// </summary>
        public const int RANGE_ATTACK_ENDURANCE = 5;
        /// <summary>
        /// Endurance used for critical shot
        /// </summary>
        public const int CRITICAL_SHOT_ENDURANCE = 10;

        /// <summary>
		/// The state of the ranged attack
		/// </summary>
		protected eRangedAttackState m_rangedAttackState;
        /// <summary>
        /// The gtype of the ranged attack
        /// </summary>
        protected eRangedAttackType m_rangedAttackType;

        /// <summary>
        /// Gets or Sets the state of a ranged attack
        /// </summary>
        public eRangedAttackState RangedAttackState
        {
            get { return m_rangedAttackState; }
            set { m_rangedAttackState = value; }
        }

        /// <summary>
        /// Gets or Sets the type of a ranged attack
        /// </summary>
        public eRangedAttackType RangedAttackType
        {
            get { return m_rangedAttackType; }
            set { m_rangedAttackType = value; }
        }

        /// <summary>
        /// Holds the quiverslot to be used
        /// </summary>
        protected eActiveQuiverSlot m_activeQuiverSlot;

        /// <summary>
        /// Gets/Sets the current active quiver slot of this living
        /// </summary>
        public virtual eActiveQuiverSlot ActiveQuiverSlot
        {
            get { return m_activeQuiverSlot; }
            set { m_activeQuiverSlot = value; }
        }
        
        

        
        
  //      /// <summary>
		///// Check the range attack state and decides what to do
		///// Called inside the AttackTimerCallback
		///// </summary>
		///// <returns></returns>
		//public virtual eCheckRangeAttackStateResult CheckRangeAttackState(GameObject target)
  //      {
  //          //Standard livings ALWAYS shot and reload automatically!
  //          return eCheckRangeAttackStateResult.Fire;
  //      }

        ///// <summary>
        ///// Gets/Sets the item that is used for ranged attack
        ///// </summary>
        ///// <returns>Item that will be used for range/accuracy/damage modifications</returns>
        //public virtual InventoryItem RangeAttackAmmo
        //{
        //    get { return null; }
        //    set { }
        //}

        ///// <summary>
        ///// Gets/Sets the target for current ranged attack
        ///// </summary>
        ///// <returns></returns>
        //public virtual GameObject RangeAttackTarget
        //{
        //    get { return owner.TargetObject; }
        //    set { }
        //}

        /// <summary>
		/// Check the selected range ammo and decides if it's compatible with select weapon
		/// </summary>
		/// <returns>True if compatible, false if not</returns>
		public virtual bool CheckRangedAmmoCompatibilityWithActiveWeapon()
        {
            var p = owner as GamePlayer;

            InventoryItem weapon = p.attackComponent.AttackWeapon;
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
                                    case eActiveQuiverSlot.Fourth: ammo = p.Inventory.GetItem(eInventorySlot.FourthQuiver); break;
                                    case eActiveQuiverSlot.Third: ammo = p.Inventory.GetItem(eInventorySlot.ThirdQuiver); break;
                                    case eActiveQuiverSlot.Second: ammo = p.Inventory.GetItem(eInventorySlot.SecondQuiver); break;
                                    case eActiveQuiverSlot.First: ammo = p.Inventory.GetItem(eInventorySlot.FirstQuiver); break;
                                }

                                if (ammo == null) return false;

                                if (weapon.Object_Type == (int)eObjectType.Crossbow)
                                    return ammo.Object_Type == (int)eObjectType.Bolt;
                                return ammo.Object_Type == (int)eObjectType.Arrow;
                            }
                        }
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Holds the arrows for next range attack
        /// </summary>
        protected WeakReference m_rangeAttackAmmo = new WeakRef(null);

        /// <summary>
        /// Gets/Sets the item that is used for ranged attack
        /// </summary>
        /// <returns>Item that will be used for range/accuracy/damage modifications</returns>
        public InventoryItem RangeAttackAmmo
        {
            get
            {
                if (owner is GamePlayer)
                {
                    //TODO: ammo should be saved on start of every range attack and used here
                    InventoryItem ammo = null;//(InventoryItem)m_rangeAttackArrows.Target;

                    InventoryItem weapon = owner.attackComponent.AttackWeapon;
                    if (weapon != null)
                    {
                        switch (weapon.Object_Type)
                        {
                            case (int)eObjectType.Thrown: ammo = owner.Inventory.GetItem(eInventorySlot.DistanceWeapon); break;
                            case (int)eObjectType.Crossbow:
                            case (int)eObjectType.Longbow:
                            case (int)eObjectType.CompositeBow:
                            case (int)eObjectType.RecurvedBow:
                            case (int)eObjectType.Fired:
                                {
                                    switch (ActiveQuiverSlot)
                                    {
                                        case eActiveQuiverSlot.First: ammo = owner.Inventory.GetItem(eInventorySlot.FirstQuiver); break;
                                        case eActiveQuiverSlot.Second: ammo = owner.Inventory.GetItem(eInventorySlot.SecondQuiver); break;
                                        case eActiveQuiverSlot.Third: ammo = owner.Inventory.GetItem(eInventorySlot.ThirdQuiver); break;
                                        case eActiveQuiverSlot.Fourth: ammo = owner.Inventory.GetItem(eInventorySlot.FourthQuiver); break;
                                        case eActiveQuiverSlot.None:
                                            eObjectType findType = eObjectType.Arrow;
                                            if (weapon.Object_Type == (int)eObjectType.Crossbow)
                                                findType = eObjectType.Bolt;

                                            ammo = owner.Inventory.GetFirstItemByObjectType((int)findType, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

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
            set {
                if (owner is GamePlayer)
                    m_rangeAttackAmmo.Target = value;
                else { };
            }
        }

        /// <summary>
        /// Holds the target for next range attack
        /// </summary>
        protected WeakReference m_rangeAttackTarget = new WeakRef(null);

        /// <summary>
        /// Gets/Sets the target for current ranged attack
        /// </summary>
        /// <returns></returns>
        public GameObject RangeAttackTarget
        {
            get
            {
                if (owner is GamePlayer)
                {
                    GameObject target = (GameObject)m_rangeAttackTarget.Target;
                    if (target == null || target.ObjectState != eObjectState.Active)
                        target = owner.TargetObject;
                    return target;
                }
                else
                    return owner.TargetObject;
            }
            set { m_rangeAttackTarget.Target = value; }
        }

        /// <summary>
        /// Check the range attack state and decides what to do
        /// Called inside the AttackTimerCallback
        /// </summary>
        /// <returns></returns>
        public eCheckRangeAttackStateResult CheckRangeAttackState(GameObject target)
        {
            if (owner is GamePlayer)
            {
                var p = owner as GamePlayer;

                long holdStart = p.TempProperties.getProperty<long>(RANGE_ATTACK_HOLD_START);
                if (holdStart == 0)
                {
                    holdStart = GameLoop.GameLoopTime;
                    p.TempProperties.setProperty(RANGE_ATTACK_HOLD_START, holdStart);
                }
                //DOLConsole.WriteLine("Holding.... ("+holdStart+") "+(Environment.TickCount - holdStart));
                if ((GameLoop.GameLoopTime - holdStart) > 15000 && p.attackComponent.AttackWeapon.Object_Type != (int)eObjectType.Crossbow)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.TooTired"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return eCheckRangeAttackStateResult.Stop; //Stop the attack
                }

                //This state is set when the player wants to fire!
                if (RangedAttackState == eRangedAttackState.Fire
                    || RangedAttackState == eRangedAttackState.AimFire
                    || RangedAttackState == eRangedAttackState.AimFireReload)
                {
                    RangeAttackTarget = null; // clean the RangeAttackTarget at the first shot try even if failed

                    if (target == null || !(target is GameLiving))
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "System.MustSelectTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (!p.IsWithinRadius(target, p.attackComponent.AttackRange))
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.TooFarAway", target.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (!p.TargetInView)  // TODO : wrong, must be checked with the target parameter and not with the targetObject
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.CantSeeTarget"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (!p.IsObjectInFront(target, 90))
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.NotInView", target.GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                    else if (RangeAttackAmmo == null)
                    {
                        //another check for ammo just before firing
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.MustSelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else if (!CheckRangedAmmoCompatibilityWithActiveWeapon())
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else if (GameServer.ServerRules.IsAllowedToAttack(p, (GameLiving)target, false))
                    {
                        GameLiving living = target as GameLiving;
                        if (RangedAttackType == eRangedAttackType.Critical && living != null
                            && (living.CurrentSpeed > 90 //walk speed == 85, hope that's what they mean
                                || (living.attackComponent.AttackState && living.InCombat) //maybe not 100% correct
                                || EffectListService.GetEffectOnTarget(living, eEffect.Mez) /*SpellHandler.FindEffectOnTarget(living, "Mesmerize")*/ != null
                               ))
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

                            // TODO: more checks?
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.CantCritical"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            RangedAttackType = eRangedAttackType.Normal;
                        }
                        return eCheckRangeAttackStateResult.Fire;
                    }

                    RangedAttackState = eRangedAttackState.ReadyToFire;
                    return eCheckRangeAttackStateResult.Hold;
                }

                //Player is aiming
                if (RangedAttackState == eRangedAttackState.Aim)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.ReadyToFire"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    RangedAttackState = eRangedAttackState.ReadyToFire;
                    return eCheckRangeAttackStateResult.Hold;
                }
                else if (RangedAttackState == eRangedAttackState.ReadyToFire)
                {
                    return eCheckRangeAttackStateResult.Hold; //Hold the shot
                }
                return eCheckRangeAttackStateResult.Fire;
            }
            else
                return eCheckRangeAttackStateResult.Fire;
        }

        /// <summary>
        /// Removes ammo and endurance on range attack
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        public void RangeAttackHandler(EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if (args == null) return;

            switch (args.AttackData.AttackResult)
            {
                case eAttackResult.HitUnstyled:
                case eAttackResult.Missed:
                case eAttackResult.Blocked:
                case eAttackResult.Parried:
                case eAttackResult.Evaded:
                case eAttackResult.HitStyle:
                case eAttackResult.Fumbled:
                    // remove an arrow and endurance
                    InventoryItem ammo = RangeAttackAmmo;
                    owner.Inventory.RemoveCountFromStack(ammo, 1);

                    if (RangedAttackType == eRangedAttackType.Critical)
                        owner.Endurance -= CRITICAL_SHOT_ENDURANCE;
                    else if (RangedAttackType == eRangedAttackType.RapidFire && owner.GetAbilityLevel(Abilities.RapidFire) == 1)
                        owner.Endurance -= 2 * RANGE_ATTACK_ENDURANCE;
                    else owner.Endurance -= RANGE_ATTACK_ENDURANCE;
                    break;
            }
        }

    }
}
