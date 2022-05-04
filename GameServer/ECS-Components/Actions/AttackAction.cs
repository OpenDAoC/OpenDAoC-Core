using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
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
    /// <summary>
    /// The attack action of this living
    /// </summary>
    public class AttackAction
    {
        private GameLiving owner;
        private int Interval;
        private long startTime;
        private long rangeInterruptTime;
        public long StartTime { get { return startTime; } set { startTime = value + GameLoop.GameLoopTime; } }
        public long RangeInterruptTime { get { return rangeInterruptTime; } set { rangeInterruptTime = value + GameLoop.GameLoopTime; } }
        public long TimeUntilStart { get { return StartTime - GameLoop.GameLoopTime; } }

        /// <summary>
        /// Constructs a new attack action
        /// </summary>
        /// <param name="owner">The action source</param>
        public AttackAction(GameLiving owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// Called on every timer tick
        /// </summary>
        public void Tick(long time)
        {

            if (time > StartTime)
            {
                //GameLiving owner = (GameLiving)m_actionSource;

                if (owner.IsMezzed || owner.IsStunned)
                {
                    Interval = 100;
                    return;
                }

                if (owner.IsCasting && !owner.CurrentSpellHandler.Spell.Uninterruptible)
                {
                    Interval = 100;
                    return;
                }

                if (!owner.attackComponent.AttackState)
                {
                    AttackData ad = owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                    owner.TempProperties.removeProperty(LAST_ATTACK_DATA);
                    if (ad != null && ad.Target != null)
                        ad.Target.attackComponent.RemoveAttacker(owner);
                    //Stop();
                    owner.attackComponent.attackAction?.CleanupAttackAction();
                    return;
                }

                // Don't attack if gameliving is engaging
                if (owner.IsEngaging)
                {
                    Interval = owner.attackComponent.AttackSpeed(owner.attackComponent.AttackWeapon); // while gameliving is engageing it doesn't attack.
                    return;
                }

                // Store all datas which must not change during the attack
                // double effectiveness = 1.0;
                double effectiveness = owner.Effectiveness;
                int ticksToTarget = 1;
                int interruptDuration = 0;
                int leftHandSwingCount = 0;
                Style combatStyle = null;
                InventoryItem attackWeapon = owner.attackComponent.AttackWeapon;
                InventoryItem leftWeapon = (owner.Inventory == null) ? null : owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                GameObject attackTarget = null;

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    attackTarget = owner.rangeAttackComponent.RangeAttackTarget; // must be do here because RangeAttackTarget is changed in CheckRangeAttackState
                    eCheckRangeAttackStateResult rangeCheckresult = owner.rangeAttackComponent.CheckRangeAttackState(attackTarget);
                    if (rangeCheckresult == eCheckRangeAttackStateResult.Hold)
                    {
                        Interval = 100;
                        return; //Hold the shot another second
                    }
                    else if (rangeCheckresult == eCheckRangeAttackStateResult.Stop || attackTarget == null)
                    {
                        owner.attackComponent.LivingStopAttack(); //Stop the attack
                                            //Stop();
                        owner.attackComponent.attackAction?.CleanupAttackAction();
                        return;
                    }

                    int model = (attackWeapon == null ? 0 : attackWeapon.Model);
                    foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        if (player == null) continue;
                        player.Out.SendCombatAnimation(owner, attackTarget, (ushort)model, 0x00, player.Out.BowShoot, 0x01, 0, ((GameLiving)attackTarget).HealthPercent);
                    }

                    interruptDuration = owner.attackComponent.AttackSpeed(attackWeapon);

                    switch (owner.rangeAttackComponent.RangedAttackType)
                    {
                        case eRangedAttackType.Critical:
                            {
                                var tmpEffectiveness = 2 - 0.3 * owner.GetConLevel(attackTarget);
                                if (tmpEffectiveness > 2)
                                    effectiveness *= 2;
                                else if (tmpEffectiveness < 1.1)
                                    effectiveness *= 1.1;
                                else
                                    effectiveness *= tmpEffectiveness;
                            }
                            break;

                        case eRangedAttackType.SureShot:
                            {
                                effectiveness *= 0.5;
                            }
                            break;

                        case eRangedAttackType.RapidFire:
                            {
                                // Source : http://www.camelotherald.com/more/888.shtml
                                // - (About Rapid Fire) If you release the shot 75% through the normal timer, the shot (if it hits) does 75% of its normal damage. If you
                                // release 50% through the timer, you do 50% of the damage, and so forth - The faster the shot, the less damage it does.

                                // Source : http://www.camelotherald.com/more/901.shtml
                                // Related note about Rapid Fire interrupts are determined by the speed of the bow is fired, meaning that the time of interruptions for each shot will be scaled
                                // down proportionally to bow speed. If that made your eyes bleed, here's an example from someone who would know: "I fire a 5.0 spd bow. Because I am buffed and have
                                // stat bonuses, I fire that bow at 3.0 seconds. The resulting interrupt on the caster will last 3.0 seconds. If I rapid fire that same bow, I will fire at 1.5 seconds,
                                // and the resulting interrupt will last 1.5 seconds."

                                long rapidFireMaxDuration = owner.attackComponent.AttackSpeed(attackWeapon) / 2; // half of the total time
                                long elapsedTime = GameLoop.GameLoopTime - owner.TempProperties.getProperty<long>(RangeAttackComponent.RANGE_ATTACK_HOLD_START); // elapsed time before ready to fire
                                if (elapsedTime < rapidFireMaxDuration)
                                {
                                    effectiveness *= 0.5 + (double)elapsedTime * 0.5 / (double)rapidFireMaxDuration;
                                    interruptDuration = (int)(interruptDuration * effectiveness);
                                }
                            }
                            break;
                    }

                    // calculate Penetrating Arrow damage reduction
                    if (attackTarget is GameLiving)
                    {
                        int PALevel = owner.GetAbilityLevel(Abilities.PenetratingArrow);
                        if ((PALevel > 0) && (owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
                        {
                            ECSGameSpellEffect bladeturn = ((GameLiving)attackTarget).effectListComponent.GetSpellEffects(eEffect.Bladeturn)?.FirstOrDefault();
                            //GameSpellEffect bladeturn = null;
                            //lock (((GameLiving)attackTarget).EffectList)
                            //{
                            //    foreach (IGameEffect effect in ((GameLiving)attackTarget).EffectList)
                            //    {
                            //        if (effect is GameSpellEffect && ((GameSpellEffect)effect).Spell.SpellType == (byte)eSpellType.Bladeturn)
                            //        {
                            //            bladeturn = (GameSpellEffect)effect;
                            //            break;
                            //        }
                            //    }
                            //}

                            if (bladeturn != null && attackTarget != bladeturn.SpellHandler.Caster)
                            {
                                // Penetrating Arrow work
                                effectiveness *= 0.25 + PALevel * 0.25;
                            }
                        }
                    }

                    ticksToTarget = 1 + owner.GetDistanceTo(attackTarget) * 100 / 150; // 150 units per 1/10s
                }
                else
                {
                    attackTarget = owner.TargetObject;

                    // wait until target is selected
                    if (attackTarget == null || attackTarget == owner)
                    {
                        Interval = 100;
                        //return;
                    }

                    AttackData ad = owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                    if (ad != null && ad.AttackResult == eAttackResult.Fumbled)
                    {
                        Interval = owner.attackComponent.AttackSpeed(attackWeapon);
                        ad.AttackResult = eAttackResult.Missed;
                        StartTime = Interval;
                        return; //Don't start the attack if the last one fumbled
                    }

                    combatStyle = owner.styleComponent.GetStyleToUse();
                    if (combatStyle != null && combatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                    {
                        attackWeapon = leftWeapon;
                    }
                    interruptDuration = owner.attackComponent.AttackSpeed(attackWeapon);

                    // Damage is doubled on sitting players
                    // but only with melee weapons; arrows and magic does normal damage.
                    if (attackTarget is GamePlayer && ((GamePlayer)attackTarget).IsSitting)
                    {
                        effectiveness *= 2;
                    }

                    ticksToTarget = 1;
                }

                int addRange = combatStyle?.Procs?.FirstOrDefault()?.Item1.SpellType == (byte)eSpellType.StyleRange ? (int)combatStyle?.Procs?.FirstOrDefault()?.Item1.Value - owner.attackComponent.AttackRange : 0;

                if (attackTarget != null && !owner.IsWithinRadius(attackTarget, owner.attackComponent.AttackRange + addRange) && owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    if (owner is GameNPC && (owner as GameNPC).Brain is StandardMobBrain && ((owner as GameNPC).Brain as StandardMobBrain).AggroTable.Count > 0 && (owner as GameNPC).Brain is IControlledBrain == false)
                    {
                        #region Attack another target in range

                        GameNPC npc = owner as GameNPC;
                        StandardMobBrain npc_brain = npc.Brain as StandardMobBrain;
                        GameLiving Possibly_target = null;
                        long maxaggro = 0, aggro = 0;

                        foreach (GamePlayer player_test in owner.GetPlayersInRadius((ushort)owner.attackComponent.AttackRange))
                        {
                            if (npc_brain.AggroTable.ContainsKey(player_test))
                            {
                                aggro = npc_brain.GetAggroAmountForLiving(player_test);
                                if (aggro <= 0) continue;
                                if (aggro > maxaggro)
                                {
                                    Possibly_target = player_test;
                                    maxaggro = aggro;
                                }
                            }
                        }
                        foreach (GameNPC target_possibility in owner.GetNPCsInRadius((ushort)owner.attackComponent.AttackRange))
                        {
                            if (npc_brain.AggroTable.ContainsKey(target_possibility))
                            {
                                aggro = npc_brain.GetAggroAmountForLiving(target_possibility);
                                if (aggro <= 0) continue;
                                if (aggro > maxaggro)
                                {
                                    Possibly_target = target_possibility;
                                    maxaggro = aggro;
                                }
                            }
                        }

                        if (Possibly_target == null)
                        {
                            Interval = 100;
                            return;
                        }
                        else
                        {
                            attackTarget = Possibly_target;
                        }

                        #endregion

                    }
                    else
                    {
                        owner.TempProperties.removeProperty(LAST_ATTACK_DATA);
                        Interval = 100;
                        //return;
                    }
                }

                //new WeaponOnTargetAction(owner, attackTarget, attackWeapon, leftWeapon, effectiveness, interruptDuration, combatStyle).Start(ticksToTarget);  // really start the attack
                //if (GameServer.ServerRules.IsAllowedToAttack(owner, attackTarget as GameLiving, false))

                owner.attackComponent.weaponAction = new WeaponAction(owner, attackTarget, attackWeapon, leftWeapon, effectiveness, interruptDuration, combatStyle);
                //Are we inactive?
                if (owner.ObjectState != eObjectState.Active)
                {
                    //Stop();
                    owner.attackComponent.attackAction?.CleanupAttackAction();
                    return;
                }

                //switch to melee if range to target is less than 200
                if (owner is GameNPC && owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance && owner.TargetObject != null && owner.IsWithinRadius(owner.TargetObject, 200))
                {
                    owner.SwitchWeapon(eActiveWeaponSlot.Standard);
                }

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    //Mobs always shot and reload
                    if (owner is GameNPC)
                    {
                        owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFireReload;
                    }

                    if (owner.rangeAttackComponent.RangedAttackState != eRangedAttackState.AimFireReload)
                    {
                        owner.attackComponent.LivingStopAttack();
                        //Stop();
                        owner.attackComponent.attackAction?.CleanupAttackAction();
                        return;
                    }
                    else
                    {
                        if (!(owner is GamePlayer) || (owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
                        {
                            owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

                            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.SureShot) != null)
                                owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.RapidFire) != null)
                                owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.TrueShot) != null)
                                owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;                            
                        }

                        owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;

                        if (owner is GamePlayer)
                        {
                            owner.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, 0L);
                        }

                        int speed = owner.attackComponent.AttackSpeed(attackWeapon);
                        byte attackSpeed = (byte)(speed / 100);
                        int model = (attackWeapon == null ? 0 : attackWeapon.Model);
                        if (owner is GamePlayer && owner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))//volley check
                        { }
                        else
                        {
                            foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            {
                                player.Out.SendCombatAnimation(owner, null, (ushort)model, 0x00, player.Out.BowPrepare, attackSpeed, 0x00, 0x00);
                            }
                        }
                        if (owner.rangeAttackComponent.RangedAttackType == eRangedAttackType.RapidFire)
                        {
                            speed /= 2; // can start fire at the middle of the normal time
                            speed = Math.Max(1500, speed);
                        }

                        Interval = speed;
                    }
                }
                else
                {
                    if (attackWeapon != null && leftWeapon != null && leftWeapon.Object_Type != (int)eObjectType.Shield/*  leftHandSwingCount > 0*/)
                    {
                        Interval = owner.attackComponent.AttackSpeed(attackWeapon, leftWeapon);
                    }
                    else
                    {
                        Interval = owner.attackComponent.AttackSpeed(attackWeapon);
                    }
                }
                StartTime = Interval;// owner.AttackSpeed(attackWeapon);
                //owner.attackComponent.attackAction.CleanupAttackAction();
            }


            if (RangeInterruptTime > time)
            {
                if (owner.rangeAttackComponent?.RangedAttackState == eRangedAttackState.Aim)
                {
                    var p = owner as GamePlayer;
                    if (p != null && p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        if (p != null && p.InterruptTime > GameLoop.GameLoopTime && p.attackComponent.Attackers.Count > 0)
                        {
                            var attacker = p.attackComponent.Attackers.Last();
                            string attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Type.Shot");
                            if (p.attackComponent.AttackWeapon != null && p.attackComponent.AttackWeapon.Object_Type == (int)eObjectType.Thrown)
                                attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Type.Throw");
                            if (attacker is GameNPC)
                                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, p.Client.Account.Language, (attacker as GameNPC)), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            else
                                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            p.attackComponent.StopAttack(true);
                            return;
                        }
                    }
                }
            }
        }

        public void CleanupAttackAction()
        {
            owner.attackComponent.attackAction = null;
        }
    }
}
