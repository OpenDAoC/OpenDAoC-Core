using System;
using System.Linq;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.Language;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    /// <summary>
    /// The attack action of this living
    /// </summary>
    public class AttackAction
    {
        // Check delay in ms for when to check for NPCs in area to attack when not in range of main target. Used as upper bound of checks 
        private const int NPC_VICINITY_CHECK_DELAY = 1000;
        // Next tick interval for when the current tick doesn't result in an attack
        private const int TICK_INTERVAL_FOR_NON_ATTACK = 100;

        private GameLiving m_owner;
        private int m_interval;
        private long m_startTime;
        private long m_rangeInterruptTime;
        private long m_NPCNextNPCVicinityCheck = 0; // Next check for NPCs in the attack range to hit while on the way to main target
        private long m_roundWithNoAttackTime; // Set to current time when a round doesn't result in an attack. Kept until reset in ShouldRoundShowMessage()

        public long StartTime { get { return m_startTime; } set { m_startTime = value + GameLoop.GameLoopTime; } }
        public long RangeInterruptTime { get { return m_rangeInterruptTime; } set { m_rangeInterruptTime = value + GameLoop.GameLoopTime; } }
        public long TimeUntilStart { get { return StartTime - GameLoop.GameLoopTime; } }

        /// <summary>
        /// Constructs a new attack action
        /// </summary>
        /// <param name="owner">The action source</param>
        public AttackAction(GameLiving owner)
        {
            m_owner = owner;
        }

        /// <summary>
        /// Called on every timer tick
        /// </summary>
        public void Tick(long time)
        {
            if (time > StartTime)
            {
                if (m_owner.IsMezzed || m_owner.IsStunned)
                {
                    m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return;
                }

                if (m_owner.IsCasting && !m_owner.CurrentSpellHandler.Spell.Uninterruptible)
                {
                    m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return;
                }

                if (m_owner.IsEngaging || m_owner.TargetObject == null)
                {
                    m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    return;
                }

                AttackComponent attackComponent = m_owner.attackComponent;
                AttackData attackData = m_owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;

                if (!attackComponent.AttackState)
                {
                    m_owner.TempProperties.removeProperty(LAST_ATTACK_DATA);
                    if (attackData != null && attackData.Target != null)
                        attackData.Target.attackComponent.RemoveAttacker(m_owner);
                    attackComponent.attackAction?.CleanupAttackAction();
                    return;
                }

                // Store all datas which must not change during the attack
                double effectiveness = m_owner.Effectiveness;
                int ticksToTarget = 1;
                int interruptDuration = 0;
                Style combatStyle = null;
                InventoryItem attackWeapon = attackComponent.AttackWeapon;
                InventoryItem leftWeapon = m_owner.Inventory?.GetItem(eInventorySlot.LeftHandWeapon);
                GameObject attackTarget = null;
                StyleComponent styleComponent = m_owner.styleComponent;

                if (m_owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    attackTarget = m_owner.rangeAttackComponent.RangeAttackTarget; // must be do here because RangeAttackTarget is changed in CheckRangeAttackState
                    eCheckRangeAttackStateResult rangeCheckresult = m_owner.rangeAttackComponent.CheckRangeAttackState(attackTarget);
                    if (rangeCheckresult == eCheckRangeAttackStateResult.Hold)
                    {
                        m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                        return; //Hold the shot another second
                    }
                    else if (rangeCheckresult == eCheckRangeAttackStateResult.Stop || attackTarget == null)
                    {
                        attackComponent.LivingStopAttack(); //Stop the attack
                                            //Stop();
                        attackComponent.attackAction?.CleanupAttackAction();
                        return;
                    }

                    int model = (attackWeapon == null ? 0 : attackWeapon.Model);
                    Parallel.ForEach((m_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)).OfType<GamePlayer>(), player =>
                    {
                        if (player == null)
                            return;
                        player.Out.SendCombatAnimation(m_owner, attackTarget, (ushort)model, 0x00, player.Out.BowShoot, 0x01, 0, ((GameLiving)attackTarget).HealthPercent);
                    });

                    interruptDuration = attackComponent.AttackSpeed(attackWeapon);

                    switch (m_owner.rangeAttackComponent.RangedAttackType)
                    {
                        case eRangedAttackType.Critical:
                            {
                                var tmpEffectiveness = 2 - 0.3 * m_owner.GetConLevel(attackTarget);
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

                                long rapidFireMaxDuration = attackComponent.AttackSpeed(attackWeapon);
                                long elapsedTime = GameLoop.GameLoopTime - m_owner.TempProperties.getProperty<long>(RangeAttackComponent.RANGE_ATTACK_HOLD_START); // elapsed time before ready to fire
                                if (elapsedTime < rapidFireMaxDuration)
                                {
                                    effectiveness *= 0.25 + (double)elapsedTime * 0.5 / (double)rapidFireMaxDuration;
                                    interruptDuration = (int)(interruptDuration * effectiveness);
                                }
                            }
                            break;
                    }

                    // calculate Penetrating Arrow damage reduction
                    if (attackTarget is GameLiving)
                    {
                        int PALevel = m_owner.GetAbilityLevel(Abilities.PenetratingArrow);
                        if ((PALevel > 0) && (m_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
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

                    ticksToTarget = 1 + m_owner.GetDistanceTo(attackTarget) * 100 / 150; // 150 units per 1/10s
                }
                else
                {
                    attackTarget = m_owner.TargetObject;

                    if (attackData != null && attackData.AttackResult is eAttackResult.Fumbled)
                    {
                        // Don't start the attack if the last one fumbled
                        styleComponent.NextCombatStyle = null;
                        styleComponent.NextCombatBackupStyle = null;
                        attackData.AttackResult = eAttackResult.Missed;
                        m_interval = attackComponent.AttackSpeed(attackWeapon) * 2;
                        StartTime = m_interval;
                        return;
                    }

                    // Figure out which combat style may be (GamePlayer) or is being (GameNPC) used
                    if (m_owner is GamePlayer)
                    {
                        combatStyle = styleComponent.GetStyleToUse();
                    }
                    else
                    {
                        combatStyle = styleComponent.NPCGetStyleToUse();
                    }
                    
                    if (combatStyle != null && combatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                    {
                        attackWeapon = leftWeapon;
                    }

                    int mainHandAttackSpeed = attackComponent.AttackSpeed(attackWeapon);

                    if (GameLoop.GameLoopTime > styleComponent.NextCombatStyleTime + mainHandAttackSpeed)
                    {
                        // Cancel the styles if they were registered too long ago
                        // Nature's Shield stays active forever and falls back to a non-backup style
                        if (styleComponent.NextCombatBackupStyle?.ID == 394)
                            styleComponent.NextCombatStyle = styleComponent.NextCombatBackupStyle;
                        else if (styleComponent.NextCombatStyle?.ID != 394)
                            styleComponent.NextCombatStyle = null;
                        styleComponent.NextCombatBackupStyle = null;
                    }

                    interruptDuration = mainHandAttackSpeed;

                    // Damage is doubled on sitting players
                    // but only with melee weapons; arrows and magic does normal damage.
                    if (attackTarget is GamePlayer && ((GamePlayer)attackTarget).IsSitting)
                    {
                        effectiveness *= 2;
                    }

                    ticksToTarget = 1;
                }

                int addRange = combatStyle?.Procs?.FirstOrDefault()?.Item1.SpellType == (byte)eSpellType.StyleRange ? (int)combatStyle?.Procs?.FirstOrDefault()?.Item1.Value - attackComponent.AttackRange : 0;

                // Target not in range yet
                if (attackTarget != null && !m_owner.IsWithinRadius(attackTarget, attackComponent.AttackRange + addRange) && m_owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    // This is a NPC and target not in range. Check if another target is in range to attack on the way to main target
                    if (m_owner is GameNPC && (m_owner as GameNPC).Brain is StandardMobBrain && ((m_owner as GameNPC).Brain as StandardMobBrain).AggroTable.Count > 0 && (m_owner as GameNPC).Brain is IControlledBrain == false)
                    {
                        GameNPC npc = m_owner as GameNPC;
                        StandardMobBrain npc_brain = npc.Brain as StandardMobBrain;
                        GameLiving Possibly_target = null;
                        long maxaggro = 0, aggro = 0;

                        foreach (GamePlayer player_test in m_owner.GetPlayersInRadius((ushort)attackComponent.AttackRange))
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

                        // Check for NPCs in attack range. Only check if the NPCNextNPCVicinityCheck is less than the current GameLoop Time
                        if (m_NPCNextNPCVicinityCheck < GameLoop.GameLoopTime)
                        {
                            // Set the next check for NPCs. Will be in a range from 100ms -> NPC_VICINITY_CHECK_DELAY
                            m_NPCNextNPCVicinityCheck = GameLoop.GameLoopTime + Util.Random(100,NPC_VICINITY_CHECK_DELAY);

                            foreach (GameNPC target_possibility in m_owner.GetNPCsInRadius((ushort)attackComponent.AttackRange))
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
                        }

                        if (Possibly_target == null)
                        {
                            m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                            return;
                        }
                        else
                        {
                            attackTarget = Possibly_target;
                        }
                    }
                }

                // This makes the attack
                attackComponent.weaponAction = new WeaponAction(m_owner, attackTarget, attackWeapon, leftWeapon, effectiveness, interruptDuration, combatStyle);
                
                // Are we inactive?
                if (m_owner.ObjectState != eObjectState.Active)
                {
                    attackComponent.attackAction?.CleanupAttackAction();
                    return;
                }

                // Switch to melee if range to target is less than 200
                if (m_owner is GameNPC && m_owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance && m_owner.TargetObject != null && m_owner.IsWithinRadius(m_owner.TargetObject, 200))
                {
                    m_owner.SwitchWeapon(eActiveWeaponSlot.Standard);
                }

                // Retrieve the newest data after from the last WeaponAction
                attackData = m_owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                m_interval = attackComponent.AttackSpeed(attackWeapon, leftWeapon);

                if (attackData == null || attackData.AttackResult 
                    is not eAttackResult.Missed
                    and not eAttackResult.HitUnstyled
                    and not eAttackResult.HitStyle
                    and not eAttackResult.Evaded
                    and not eAttackResult.Blocked
                    and not eAttackResult.Parried)
                {
                    m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                    if (m_roundWithNoAttackTime == 0)
                        m_roundWithNoAttackTime = GameLoop.GameLoopTime;
                }
                else
                {
                    // Clear styles for the next round
                    styleComponent.NextCombatStyle = null;
                    styleComponent.NextCombatBackupStyle = null;

                    if (m_owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        //Mobs always shot and reload
                        if (m_owner is GameNPC)
                        {
                            m_owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFireReload;
                        }

                        if (m_owner.rangeAttackComponent.RangedAttackState != eRangedAttackState.AimFireReload)
                        {
                            attackComponent.LivingStopAttack();
                            //Stop();
                            attackComponent.attackAction?.CleanupAttackAction();
                            return;
                        }
                        else
                        {
                            if (!(m_owner is GamePlayer) || (m_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
                            {
                                m_owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;

                                if (EffectListService.GetAbilityEffectOnTarget(m_owner, eEffect.SureShot) != null)
                                    m_owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                                if (EffectListService.GetAbilityEffectOnTarget(m_owner, eEffect.RapidFire) != null)
                                    m_owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                                if (EffectListService.GetAbilityEffectOnTarget(m_owner, eEffect.TrueShot) != null)
                                    m_owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;                            
                            }

                            m_owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;

                            if (m_owner is GamePlayer)
                            {
                                m_owner.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, GameLoop.GameLoopTime);
                            }

                            int speed = attackComponent.AttackSpeed(attackWeapon);
                            byte attackSpeed = (byte)(speed / 100);
                            int model = (attackWeapon == null ? 0 : attackWeapon.Model);
                            if (m_owner is GamePlayer && m_owner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))//volley check
                            { }
                            else
                            {
                                Parallel.ForEach((m_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE)).OfType<GamePlayer>(), player =>
                                {
                                    if (player == null)
                                        return;
                                    player.Out.SendCombatAnimation(m_owner, null, (ushort)model, 0x00, player.Out.BowPrepare, attackSpeed, 0x00, 0x00);
                                });
                            }
                            if (m_owner.rangeAttackComponent.RangedAttackType == eRangedAttackType.RapidFire)
                            {
                                speed /= 2; // can start fire at the middle of the normal time
                                speed = Math.Max(1500, speed);
                            }

                            m_interval = speed;
                        }
                    }
                    else
                    {
                        if (m_owner is GamePlayer weaponskiller && weaponskiller.UseDetailedCombatLog)
                        {
                            weaponskiller.Out.SendMessage(
                                    $"Attack Speed: {m_interval / 1000.0}s",
                                    eChatType.CT_DamageAdd,eChatLoc.CL_SystemWindow);
                        }
                    }
                }

                StartTime = m_interval;
            }

            if (RangeInterruptTime > time)
            {
                if (m_owner.rangeAttackComponent?.RangedAttackState is eRangedAttackState.Aim or eRangedAttackState.AimFire or eRangedAttackState.AimFireReload)
                {
                    var p = m_owner as GamePlayer;
                    if (p != null && p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        if (p.InterruptTime > GameLoop.GameLoopTime && p.attackComponent.Attackers.Count > 0 )
                        {
                            var attacker = p.attackComponent.Attackers.Last();
                            double chance = 90;

                            if (attacker is GamePlayer)
                                chance = 100;
                            else
                            {
                                double mod = p.GetConLevel(attacker);
                                chance += mod * 10;
                                chance = Math.Max(1, chance);
                                chance = Math.Min(99, chance);
                            }

                            if (!Util.Chance((int) chance))
                                return;
                            
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
            m_owner.attackComponent.attackAction = null;
        }

        public bool ShouldRoundShowMessage(eAttackResult attackResult)
        {
            bool shouldRoundShowMessage = true;
            if (attackResult
                is not eAttackResult.Missed
                and not eAttackResult.HitUnstyled
                and not eAttackResult.HitStyle
                and not eAttackResult.Evaded
                and not eAttackResult.Blocked
                and not eAttackResult.Parried)
            {
                shouldRoundShowMessage = GameLoop.GameLoopTime - m_roundWithNoAttackTime > 1500;
            }
            if (shouldRoundShowMessage)
                m_roundWithNoAttackTime = 0;
            return shouldRoundShowMessage;
        }
    }
}
