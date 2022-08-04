using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.GS.PacketHandler;
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
        public WeaponAction(GameLiving owner, GameObject target, InventoryItem attackWeapon, InventoryItem leftWeapon, double effectiveness, int interruptDuration, Style combatStyle)
        {
            this.owner = owner;
            m_target = target;
            m_attackWeapon = attackWeapon;
            m_leftWeapon = leftWeapon;
            m_effectiveness = effectiveness;
            m_interruptDuration = interruptDuration;
            m_combatStyle = combatStyle;
            Execute();
        }

        public void Execute()
        {
            AttackFinished = true;
            //styleHandler?.Tick(time);
            //GameLiving.WeaponOnTargetAction()

            // Crash fix since its apparently possible to get here with a null target.
            if (m_target == null)
            {
                return;
            }

            //GameLiving owner = (GameLiving)m_actionSource;
            Style style = m_combatStyle;
            int leftHandSwingCount = 0;
            AttackData mainHandAD = null;
            AttackData leftHandAD = null;
            InventoryItem mainWeapon = m_attackWeapon;
            InventoryItem leftWeapon = m_leftWeapon;
            double leftHandEffectiveness = m_effectiveness;
            double mainHandEffectiveness = m_effectiveness;

            //mainHandEffectiveness *= owner.attackComponent.CalculateMainHandEffectiveness(mainWeapon, leftWeapon);
            //leftHandEffectiveness *= owner.attackComponent.CalculateLeftHandEffectiveness(mainWeapon, leftWeapon);

            // GameNPC can Dual Swing even with no weapon
            if (owner is GameNPC && owner.attackComponent.CanUseLefthandedWeapon)
            {
                leftHandSwingCount = owner.attackComponent.CalculateLeftHandSwingCount();
            }
            else if (owner.attackComponent.CanUseLefthandedWeapon && leftWeapon != null && leftWeapon.Object_Type != (int)eObjectType.Shield
                && mainWeapon != null && mainWeapon.Hand != 1)
            {
                leftHandSwingCount = owner.attackComponent.CalculateLeftHandSwingCount();
            }

            // CMH
            // 1.89
            //- Pets will no longer continue to attack a character after the character has stealthed.
            // 1.88
            //- Monsters, pets and Non-Player Characters (NPCs) will now halt their pursuit when the character being chased stealths.
            /*
            if (owner is GameNPC
                && m_target is GamePlayer
                && ((GamePlayer)m_target).IsStealthed 
                && !(owner is GamePet))
            {
                // note due to the 2 lines above all npcs stop attacking
                GameNPC npc = (GameNPC)owner;
                npc.attackComponent.NPCStopAttack();
                npc.TargetObject = null;
                //Stop(); // stop the full tick timer? looks like other code is doing this

                // target death caused this below, so I'm replicating it
                if (npc.ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                    npc.Inventory != null &&
                    npc.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
                    npc.SwitchWeapon(eActiveWeaponSlot.Distance);
                return;
            }*/

            bool usingOH = false;
            owner.attackComponent.LastAttackWasDualWield = false;
            if (leftHandSwingCount > 0)
            {
                if (mainWeapon.Object_Type == (int)eObjectType.HandToHand || 
                    leftWeapon?.Object_Type == (int)eObjectType.HandToHand || 
                    mainWeapon.Object_Type == (int)eObjectType.TwoHandedWeapon || 
                    mainWeapon.Object_Type == (int)eObjectType.Thrown ||
                    mainWeapon.SlotPosition == (int)Slot.RANGED)
                    usingOH = false;
                else
                    usingOH = true;

                if (owner is GameNPC)
                    usingOH = false;

                if (usingOH)
                    owner.attackComponent.LastAttackWasDualWield = true;
                
                // both hands are used for attack
                mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, usingOH);
                if (style == null)
                {
                    mainHandAD.AnimationId = -2; // virtual code for both weapons swing animation
                }
            }
            else if (mainWeapon != null)
            {
                if (leftWeapon != null && leftWeapon.Object_Type != (int)eObjectType.Shield)
                {
                    usingOH = true;
                }

                if (owner is GameNPC)
                    usingOH = false;

                if (mainWeapon.Item_Type == (int)Slot.TWOHAND || mainWeapon.SlotPosition == (int)Slot.RANGED)
                    usingOH = false;

                // no left hand used, all is simple here
                mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, usingOH);
                leftHandSwingCount = 0;
            }
            else
            {
                // one of two hands is used for attack if no style, treated as a main hand attack
                if (style == null && Util.Chance(50))
                {
                    mainWeapon = leftWeapon;
                    mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, false);
                    mainHandAD.AnimationId = -1; // virtual code for left weapons swing animation
                }
                else
                {
                    mainHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, style, mainHandEffectiveness, m_interruptDuration, false);
                }
            }

            owner.TempProperties.setProperty(LAST_ATTACK_DATA, mainHandAD);

            //Notify the target of our attack (sends damage messages, should be before damage)
            // ...but certainly not if the attack never took place, like when the living
            // is out of range!
            if (mainHandAD.Target != null && 
                mainHandAD.AttackResult != eAttackResult.OutOfRange && 
                mainHandAD.AttackResult != eAttackResult.TargetNotVisible &&
                mainHandAD.AttackResult != eAttackResult.NotAllowed_ServerRules &&
                mainHandAD.AttackResult != eAttackResult.TargetDead)
            {
                mainHandAD.Target.attackComponent.AddAttacker(owner);
                mainHandAD.Target.OnAttackedByEnemy(mainHandAD);
            }

            // Check if Reflex Attack RA should apply. This is checked once here and cached since it is used multiple times below (every swing triggers Reflex Attack).
            bool targetHasReflexAttackRA = false;
            GamePlayer targetPlayer = mainHandAD.Target as GamePlayer;
            if (targetPlayer != null && targetPlayer.effectListComponent != null && targetPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.ReflexAttack))
            {
                targetHasReflexAttackRA = true;
            }

            // Reflex Attack - Mainhand
            if (targetHasReflexAttackRA)
            {
                HandleReflexAttack(owner, mainHandAD.Target, mainHandAD.AttackResult, m_interruptDuration);
            }

            // deal damage and start effect
            if (mainHandAD.AttackResult == eAttackResult.HitUnstyled || mainHandAD.AttackResult == eAttackResult.HitStyle)
            {
                owner.DealDamage(mainHandAD);
                if (mainHandAD.IsMeleeAttack)
                {
                    owner.CheckWeaponMagicalEffect(mainHandAD, mainWeapon); // proc, poison
                    HandleDamageAdd(owner, mainHandAD);

                    /// [Atlas - Takii] Reflex Attack NF Implementation commented out.
                    //                         if (mainHandAD.Target is GameLiving)
                    //                         {
                    //                             GameLiving living = mainHandAD.Target as GameLiving;
                    // 
                    //                             RealmAbilities.L3RAPropertyEnhancer ra = living.GetAbility<RealmAbilities.ReflexAttackAbility>();
                    //                             if (ra != null && Util.Chance(ra.Amount))
                    //                             {
                    //                                 AttackData ReflexAttackAD = living.attackComponent.LivingMakeAttack(owner, living.attackComponent.AttackWeapon, null, 1, m_interruptDuration, false, true);
                    //                                 living.DealDamage(ReflexAttackAD);
                    //                                 living.SendAttackingCombatMessages(ReflexAttackAD);
                    //                             }
                    //                         }
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
            if(mainHandAD == null || mainHandAD.Target == null) { return; }
            mainHandAD.Target.HandleDamageShields(mainHandAD);

            //Notify ourself about the attack
            //owner.Notify(GameLivingEvent.AttackFinished, owner, new AttackFinishedEventArgs(mainHandAD));
            if (mainHandAD.AttackType == AttackData.eAttackType.Ranged)
                owner.rangeAttackComponent.RangeAttackHandler(new AttackFinishedEventArgs(mainHandAD));

            // remove the left-hand AttackData from the previous attack
            owner.TempProperties.removeProperty(LAST_ATTACK_DATA_LH);

            //now left hand damage
            if (leftHandSwingCount > 0 && mainWeapon.SlotPosition != Slot.RANGED)
            {
                switch (mainHandAD.AttackResult)
                {
                    case eAttackResult.HitStyle:
                    case eAttackResult.HitUnstyled:
                    case eAttackResult.Missed:
                    case eAttackResult.Blocked:
                    case eAttackResult.Evaded:
                    case eAttackResult.Fumbled: // Takii - Fumble should not prevent Offhand attack. https://www.atlasfreeshard.com/tickets/fumble-mainhand-lets-offhand-not-swing.1012/
                    case eAttackResult.Parried:
                        for (int i = 0; i < leftHandSwingCount; i++)
                        {
                            if (m_target is GameLiving && (((GameLiving)m_target).IsAlive == false || ((GameLiving)m_target).ObjectState != eObjectState.Active))
                                break;

                            // Savage swings - main,left,main,left.
                            if (i % 2 == 0)
                                leftHandAD = owner.attackComponent.MakeAttack(m_target, leftWeapon, null, leftHandEffectiveness, m_interruptDuration, usingOH);
                            else
                                leftHandAD = owner.attackComponent.MakeAttack(m_target, mainWeapon, null, leftHandEffectiveness, m_interruptDuration, usingOH);

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
                                    HandleDamageAdd(owner, leftHandAD);
                                }
                            }

                            owner.TempProperties.setProperty(LAST_ATTACK_DATA_LH, leftHandAD);

                            //Send messages about our left hand attack now
                            owner.SendAttackingCombatMessages(leftHandAD);
                            leftHandAD.Target.HandleDamageShields(leftHandAD);
                            //Notify ourself about the attack
                            //owner.Notify(GameLivingEvent.AttackFinished, owner, new AttackFinishedEventArgs(leftHandAD));

                            // Reflex Attack - Offhand
                            if (targetHasReflexAttackRA)
                            {
                                HandleReflexAttack(owner, leftHandAD.Target, leftHandAD.AttackResult, m_interruptDuration);
                            }
                        }
                        break;
                }
            }

            if (mainHandAD.AttackType == AttackData.eAttackType.Ranged)
            {
                owner.CheckWeaponMagicalEffect(mainHandAD, mainWeapon); // proc, poison
                HandleDamageAdd(owner, mainHandAD);
                owner.RangedAttackFinished();
            }

            switch (mainHandAD.AttackResult)
            {
                case eAttackResult.NoTarget:
                case eAttackResult.TargetDead:
                    {
                        //Stop();
                        owner.OnTargetDeadOrNoTarget();
                        return;
                    }
                case eAttackResult.NotAllowed_ServerRules:
                case eAttackResult.NoValidTarget:
                    {
                        owner.attackComponent.LivingStopAttack();
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

            return;
        }

        private static void HandleDamageAdd(GameLiving owner, AttackData ad)
        {
            var dAEffects = owner.effectListComponent.GetSpellEffects(eEffect.DamageAdd);

            /// [Atlas - Takii] This could probably be optimized a bit by doing the split below between "affected/unaffected by stacking"
            /// when the effect is applied in the EffectListComponent instead of every time we swing our weapon?
            if (dAEffects != null)
            {
                List<ECSGameSpellEffect> dmgAddsUnaffectedByStacking = new List<ECSGameSpellEffect>();

                // 1 - Apply the DmgAdds that are unaffected by stacking (usually RA-based DmgAdds, EffectGroup 99999) first regardless of their damage.
                foreach (var effect in dAEffects)
                {
                    if (effect.SpellHandler.Spell.EffectGroup == 99999)
                    {
                        dmgAddsUnaffectedByStacking.Add(effect);
                        ((DamageAddSpellHandler)effect.SpellHandler).EventHandler(null, owner, new AttackFinishedEventArgs(ad), /* effectiveness = */ 1);
                    }
                }

                // 2 - Apply regular damage adds. We only start reducing to 50% effectiveness if there is more than one regular damage add being applied.
                // "Unaffected by stacking" dmg adds also dont reduce subsequence damage adds; they are effectively outside of the stacking mechanism.
                int numRegularDmgAddsApplied = 0;
                foreach (var effect in dAEffects.Except(dmgAddsUnaffectedByStacking).OrderByDescending(e => e.SpellHandler.Spell.Damage))
                {
                    var effectiveness = 1 + effect.SpellHandler.Caster.GetModified(eProperty.BuffEffectiveness) * 0.01;
                    if (effect.IsBuffActive)
                    {
                        ((DamageAddSpellHandler)effect.SpellHandler).EventHandler(null, owner, new AttackFinishedEventArgs(ad), numRegularDmgAddsApplied > 0 ? effectiveness * 0.5 : effectiveness);
                        numRegularDmgAddsApplied++;
                    }
                }
            }
        }

        private static void HandleReflexAttack(GameLiving attacker, GameLiving target, eAttackResult attackResult, int interruptDuration)
        {
            // Create an attack where the target hits the attacker back.
            // Triggers if we actually took a swing at the target, regardless of whether or not we hit.
            switch (attackResult)
            {
                case eAttackResult.HitStyle:
                case eAttackResult.HitUnstyled:
                case eAttackResult.Missed:
                case eAttackResult.Blocked:
                case eAttackResult.Evaded:
                case eAttackResult.Parried:
                    AttackData ReflexAttackAD = target.attackComponent.LivingMakeAttack(attacker, target.attackComponent.AttackWeapon, null, 1, interruptDuration, false, true);
                    target.DealDamage(ReflexAttackAD);

                    // If we get hit by Reflex Attack (it can miss), send a "you were hit" message to the attacker manually
                    // since it will not be done automatically as this attack is not processed by regular attacking code.
                    GamePlayer playerAttacker = attacker as GamePlayer;
                    if (ReflexAttackAD.AttackResult == eAttackResult.HitUnstyled)
                    {
                        playerAttacker?.Out.SendMessage(target.Name + " counter-attacks you for " + ReflexAttackAD.Damage + " damage.", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    }
                    break;
                case eAttackResult.NotAllowed_ServerRules:
                case eAttackResult.NoTarget:
                case eAttackResult.TargetDead:
                case eAttackResult.OutOfRange:
                case eAttackResult.NoValidTarget:
                case eAttackResult.TargetNotVisible:
                case eAttackResult.Fumbled:
                case eAttackResult.Bodyguarded:
                case eAttackResult.Phaseshift:
                case eAttackResult.Grappled:
                default:
                    break;
            }
        }
    }
}
