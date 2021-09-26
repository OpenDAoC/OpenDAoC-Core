using DOL.Database;
using DOL.Events;
using DOL.GS.Spells;
using DOL.GS.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class WeaponAction
    {
        protected readonly GameLiving owner;
        /// <summary>
        /// The target of the attack
        /// </summary>
        protected readonly GameObject m_target;

        /// <summary>
        /// The weapon of the attack
        /// </summary>
        protected readonly InventoryItem m_attackWeapon;

        /// <summary>
        /// The weapon in the left hand of the attacker
        /// </summary>
        protected readonly InventoryItem m_leftWeapon;

        /// <summary>
        /// The effectiveness of the attack
        /// </summary>
        protected readonly double m_effectiveness;

        /// <summary>
        /// The interrupt duration of the attack
        /// </summary>
        protected readonly int m_interruptDuration;

        /// <summary>
        /// The combat style of the attack
        /// </summary>
        protected readonly Style m_combatStyle;

        private long startTime;
        public bool AttackFinished { get; private set; }
        /// <summary>
        /// Constructs a new attack action
        /// </summary>
        /// <param name="owner">The action source</param>
        /// <param name="attackWeapon">the weapon used to attack</param>
        /// <param name="combatStyle">the style used</param>
        /// <param name="effectiveness">the effectiveness</param>
        /// <param name="interruptDuration">the interrupt duration</param>
        /// <param name="leftHandSwingCount">the left hand swing count</param>
        /// <param name="leftWeapon">the left hand weapon used to attack</param>
        /// <param name="target">the target of the attack</param>
        public WeaponAction(GameLiving owner, GameObject target, InventoryItem attackWeapon, InventoryItem leftWeapon, double effectiveness, int interruptDuration, Style combatStyle, long time)
        {
            this.owner = owner;
            m_target = target;
            m_attackWeapon = attackWeapon;
            m_leftWeapon = leftWeapon;
            m_effectiveness = effectiveness;
            m_interruptDuration = interruptDuration;
            m_combatStyle = combatStyle;
            startTime = time + GameLoop.GameLoopTime;
            //Tick(time);
        }

        public void Tick(long time)
        {
            if (time > startTime)
            {
                AttackFinished = true;
                //styleHandler?.Tick(time);
                //GameLiving.WeaponOnTargetAction()

                //GameLiving owner = (GameLiving)m_actionSource;
                Style style = m_combatStyle;
                int leftHandSwingCount = 0;
                AttackData mainHandAD = null;
                AttackData leftHandAD = null;
                InventoryItem mainWeapon = m_attackWeapon;
                InventoryItem leftWeapon = m_leftWeapon;
                double leftHandEffectiveness = m_effectiveness;
                double mainHandEffectiveness = m_effectiveness;

                mainHandEffectiveness *= owner.attackComponent.CalculateMainHandEffectiveness(mainWeapon, leftWeapon);
                leftHandEffectiveness *= owner.attackComponent.CalculateLeftHandEffectiveness(mainWeapon, leftWeapon);

                // GameNPC can Dual Swing even with no weapon
                if (owner is GameNPC && owner.attackComponent.CanUseLefthandedWeapon)
                {
                    leftHandSwingCount = owner.attackComponent.CalculateLeftHandSwingCount();
                }
                else if (owner.attackComponent.CanUseLefthandedWeapon && leftWeapon != null && leftWeapon.Object_Type != (int)eObjectType.Shield
                    && mainWeapon != null && (mainWeapon.Item_Type == Slot.RIGHTHAND || mainWeapon.Item_Type == Slot.LEFTHAND))
                {
                    leftHandSwingCount = owner.attackComponent.CalculateLeftHandSwingCount();
                }

                // CMH
                // 1.89
                //- Pets will no longer continue to attack a character after the character has stealthed.
                // 1.88
                //- Monsters, pets and Non-Player Characters (NPCs) will now halt their pursuit when the character being chased stealths.
                if (owner is GameNPC
                    && m_target is GamePlayer
                    && ((GamePlayer)m_target).IsStealthed)
                {
                    // note due to the 2 lines above all npcs stop attacking
                    GameNPC npc = (GameNPC)owner;
                    npc.attackComponent.NPCStopAttack();
                    npc.TargetObject = null;
                    //Stop(); // stop the full tick timer? looks like other code is doing this
                    CleanupAttack();

                    // target death caused this below, so I'm replicating it
                    if (npc.ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                        npc.Inventory != null &&
                        npc.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
                        npc.SwitchWeapon(eActiveWeaponSlot.Distance);
                    return;
                }

                if (leftHandSwingCount > 0)
                {
                    // both hands are used for attack
                    mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, true);
                    if (style == null)
                    {
                        mainHandAD.AnimationId = -2; // virtual code for both weapons swing animation
                    }
                }
                else if (mainWeapon != null)
                {
                    // no left hand used, all is simple here
                    mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, false);
                    leftHandSwingCount = 0;
                }
                else
                {
                    // one of two hands is used for attack if no style, treated as a main hand attack
                    if (style == null && Util.Chance(50))
                    {
                        mainWeapon = leftWeapon;
                        mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, true);
                        mainHandAD.AnimationId = -1; // virtual code for left weapons swing animation
                    }
                    else
                    {
                        mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, true);
                    }
                }
                
                owner.TempProperties.setProperty(LAST_ATTACK_DATA, mainHandAD);

                //Notify the target of our attack (sends damage messages, should be before damage)
                // ...but certainly not if the attack never took place, like when the living
                // is out of range!
                if (mainHandAD.Target != null && mainHandAD.AttackResult != eAttackResult.OutOfRange)
                {
                    mainHandAD.Target.attackComponent.AddAttacker(owner);
                    mainHandAD.Target.OnAttackedByEnemy(mainHandAD);
                }

                // deal damage and start effect
                if (mainHandAD.AttackResult == eAttackResult.HitUnstyled || mainHandAD.AttackResult == eAttackResult.HitStyle)
                {
                    owner.DealDamage(mainHandAD);
                    if (mainHandAD.IsMeleeAttack)
                    {
                        owner.CheckWeaponMagicalEffect(mainHandAD, mainWeapon); // proc, poison
                        HandleDamageAdd(owner, mainHandAD);


                        if (mainHandAD.Target is GameLiving)
                        {
                            GameLiving living = mainHandAD.Target as GameLiving;
                            RealmAbilities.L3RAPropertyEnhancer ra = living.GetAbility<RealmAbilities.ReflexAttackAbility>();
                            if (ra != null && Util.Chance(ra.Amount))
                            {
                                AttackData ReflexAttackAD = living.attackComponent.LivingMakeAttack(owner, living.attackComponent.AttackWeapon, null, 1, m_interruptDuration, false, true);
                                living.DealDamage(ReflexAttackAD);
                                living.SendAttackingCombatMessages(ReflexAttackAD);
                            }
                        }
                    }
                }

                //CMH
                // 1.89:
                // - Characters who are attacked by stealthed archers will now target the attacking archer if the attacked player does not already have a target.
                if (mainHandAD.Attacker.IsStealthed
                   && mainHandAD.AttackType == AttackData.eAttackType.Ranged
                   && (mainHandAD.AttackResult == eAttackResult.HitUnstyled || mainHandAD.AttackResult == eAttackResult.HitStyle))
                {
                    if (mainHandAD.Target.TargetObject == null)
                    {
                        if (mainHandAD.Target is GamePlayer)
                        {
                            GameClient targetClient = WorldMgr.GetClientByPlayerID(mainHandAD.Target.InternalID, false, false);
                            if (targetClient != null)
                            {
                                targetClient.Out.SendChangeTarget(mainHandAD.Attacker);
                            }
                        }
                    }
                }

                //Send the proper attacking messages to ourself
                owner.SendAttackingCombatMessages(mainHandAD);

                //Notify ourself about the attack
                //owner.Notify(GameLivingEvent.AttackFinished, owner, new AttackFinishedEventArgs(mainHandAD));
                if (mainHandAD.AttackType == AttackData.eAttackType.Ranged)
                    owner.rangeAttackComponent.RangeAttackHandler(new AttackFinishedEventArgs(mainHandAD));

                // remove the left-hand AttackData from the previous attack
                owner.TempProperties.removeProperty(LAST_ATTACK_DATA_LH);

                //now left hand damage
                if (leftHandSwingCount > 0)
                {
                    switch (mainHandAD.AttackResult)
                    {
                        case eAttackResult.HitStyle:
                        case eAttackResult.HitUnstyled:
                        case eAttackResult.Missed:
                        case eAttackResult.Blocked:
                        case eAttackResult.Evaded:
                        case eAttackResult.Parried:
                            for (int i = 0; i < leftHandSwingCount; i++)
                            {
                                if (m_target is GameLiving && (((GameLiving)m_target).IsAlive == false || ((GameLiving)m_target).ObjectState != eObjectState.Active))
                                    break;

                                // Savage swings - main,left,main,left.
                                if (i % 2 == 0)
                                    leftHandAD = owner.attackComponent.MakeAttack(m_target, leftWeapon, null, leftHandEffectiveness, m_interruptDuration, true);
                                else
                                    leftHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, null, leftHandEffectiveness, m_interruptDuration, true);

                                //Notify the target of our attack (sends damage messages, should be before damage)
                                if (leftHandAD.Target != null)
                                    leftHandAD.Target.OnAttackedByEnemy(leftHandAD);

                                // deal damage and start the effect if any
                                if (leftHandAD.AttackResult == eAttackResult.HitUnstyled || leftHandAD.AttackResult == eAttackResult.HitStyle)
                                {
                                    owner.DealDamage(leftHandAD);
                                    if (leftHandAD.IsMeleeAttack)
                                    {

                                        owner.CheckWeaponMagicalEffect(leftHandAD, leftWeapon);
                                    }
                                }

                                owner.TempProperties.setProperty(LAST_ATTACK_DATA_LH, leftHandAD);

                                //Send messages about our left hand attack now
                                owner.SendAttackingCombatMessages(leftHandAD);

                                //Notify ourself about the attack
                                //owner.Notify(GameLivingEvent.AttackFinished, owner, new AttackFinishedEventArgs(leftHandAD));
                            }
                            break;
                    }
                }

                if (mainHandAD.AttackType == AttackData.eAttackType.Ranged)
                {
                    owner.RangedAttackFinished();
                    AttackFinished = true;
                }

                switch (mainHandAD.AttackResult)
                {
                    case eAttackResult.NoTarget:
                    case eAttackResult.TargetDead:
                        {
                            CleanupAttack();
                            //Stop();
                            owner.OnTargetDeadOrNoTarget();
                            return;
                        }
                    case eAttackResult.NotAllowed_ServerRules:
                    case eAttackResult.NoValidTarget:
                        {
                            owner.attackComponent.LivingStopAttack();
                            CleanupAttack();
                            //Stop();
                            return;
                        }
                    case eAttackResult.OutOfRange:
                        break;
                }

                // unstealth before attack animation
                if (owner is GamePlayer)
                    ((GamePlayer)owner).Stealth(false);

                //Show the animation
                if (mainHandAD.AttackResult != eAttackResult.HitUnstyled && mainHandAD.AttackResult != eAttackResult.HitStyle && leftHandAD != null)
                    owner.attackComponent.ShowAttackAnimation(leftHandAD, leftWeapon);
                else
                    owner.attackComponent.ShowAttackAnimation(mainHandAD, mainWeapon);

                // (procs) start style effect after any damage
                if (mainHandAD.StyleEffects.Count > 0 && mainHandAD.AttackResult == eAttackResult.HitStyle)
                {
                    foreach (ISpellHandler proc in mainHandAD.StyleEffects)
                    {
                        proc.StartSpell(mainHandAD.Target);
                    }
                }

                if (leftHandAD != null && leftHandAD.StyleEffects.Count > 0 && leftHandAD.AttackResult == eAttackResult.HitStyle)
                {
                    foreach (ISpellHandler proc in leftHandAD.StyleEffects)
                    {
                        proc.StartSpell(leftHandAD.Target);
                    }
                }
                
                //mobs dont update the heading after they start attacking
                //so here they update it after they swing
                //update internal heading, do not send update to client
                if (owner is GameNPC)
                    (owner as GameNPC).TurnTo(mainHandAD.Target, false);

                //Stop();
                //mainHandAD = null;
                //leftHandAD = null;
                //CleanupAttack();
                
                return;
            }
        }

        private static void HandleDamageAdd(GameLiving owner, AttackData ad)
        {
            // DamageAdd
            if (owner.effectListComponent.Effects.TryGetValue(eEffect.DamageAdd, out List<ECSGameEffect> dAEffects))
            {
                dAEffects = dAEffects.OrderByDescending(e => e.SpellHandler.Spell.Damage).ToList();
                int numDmgAddsAffectedByStackingApplied = 0;
                for (int i = 0; i < dAEffects.Count; i++)
                {
                    if (dAEffects[i].IsBuffActive)
                    {
                        double effectiveness = 1;

                        // Check if we should halve the effectiveness due to stacking.
                        if (numDmgAddsAffectedByStackingApplied > 0)
                        {
                            // EffectGroup 99999 means it can stack fully with other DmgAdds. Used for RA-based DmgAdd.
                            if (dAEffects[i].SpellHandler == null || dAEffects[i].SpellHandler.Spell == null || dAEffects[i].SpellHandler?.Spell?.EffectGroup != 99999)
                            {
                                effectiveness *= .5;
                                numDmgAddsAffectedByStackingApplied++;
                            }
                        }
                        ((DamageAddSpellHandler)dAEffects[i].SpellHandler).EventHandler(null, owner, new AttackFinishedEventArgs(ad), effectiveness);
                    }
                }
            }
        }

        public void CleanupAttack()
        {
            if (owner is GamePlayer p)
            {
                
                p.attackComponent.weaponAction = null;
            }
        }} 
}
