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
    public class AttackAction
    {
        // Check interval (upper bound) in ms of entities around this NPC when its main target is out of range. Used to attack other entities on its path.
        private const int NPC_VICINITY_CHECK_DELAY = 1000;
        // Next tick interval for when the current tick doesn't result in an attack.
        private const int TICK_INTERVAL_FOR_NON_ATTACK = 100;

        private GameLiving m_owner;
        private int m_interval;
        private long m_startTime;
        private long m_rangeInterruptTime;
        // Next check for NPCs in the attack range to hit while on the way to main target.
        private long m_NPCNextNPCVicinityCheck = 0;

        // Set to current time when a round doesn't result in an attack. Used to prevent combat log spam and kept until reset in AttackComponent.SendAttackingCombatMessages().
        public long RoundWithNoAttackTime { get; set; }

        public long StartTime
        {
            get => m_startTime;
            set => m_startTime = value + GameLoop.GameLoopTime;
        }

        public long RangeInterruptTime
        {
            get => m_rangeInterruptTime;
            set => m_rangeInterruptTime = value + GameLoop.GameLoopTime;
        }

        public long TimeUntilStart => StartTime - GameLoop.GameLoopTime;

        public AttackAction(GameLiving owner)
        {
            m_owner = owner;
        }

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

                // Store all datas which must not change during the attack.
                double effectiveness = m_owner.Effectiveness;
                int ticksToTarget = 0;
                int interruptDuration = 0;
                Style combatStyle = null;
                InventoryItem attackWeapon = attackComponent.AttackWeapon;
                InventoryItem leftWeapon = m_owner.Inventory?.GetItem(eInventorySlot.LeftHandWeapon);
                GameObject attackTarget = null;
                StyleComponent styleComponent = m_owner.styleComponent;

                if (m_owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    attackTarget = m_owner.rangeAttackComponent.Target; // Must be done here because RangeAttackTarget is changed in CheckRangeAttackState.
                    eCheckRangeAttackStateResult rangeCheckresult = m_owner.rangeAttackComponent.CheckRangeAttackState(attackTarget);

                    if (rangeCheckresult == eCheckRangeAttackStateResult.Hold)
                    {
                        m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                        return; // Hold the shot another second.
                    }
                    else if (rangeCheckresult == eCheckRangeAttackStateResult.Stop || attackTarget == null)
                    {
                        attackComponent.StopAttack();
                        attackComponent.attackAction?.CleanupAttackAction();
                        return;
                    }

                    int model = attackWeapon == null ? 0 : attackWeapon.Model;
                    ticksToTarget = m_owner.GetDistanceTo(attackTarget) * 1000 / 1800; // 1800 units per second. Live value is unknown, but DoL had 1500.
                    byte flightDuration = (byte)(ticksToTarget > 350 ? 1 + (ticksToTarget - 350) / 75 : 1);
                    bool cancelPrepareAnimation = m_owner.attackComponent.AttackWeapon.Object_Type == (int)eObjectType.Thrown;

                    Parallel.ForEach(m_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
                    {
                        if (player == null)
                            return;

                        // Special case for thrown weapons (bows and crossbows don't need this).
                        // For some obscure reason, their 'BowShoot' animation doesn't cancel their 'BowPrepare', and 'BowPrepare' resumes after 'BowShoot'.
                        if (cancelPrepareAnimation)
                            player.Out.SendInterruptAnimation(m_owner);

                        // The 'stance' parameter appears to be used to indicate the time it should take for the arrow's model to reach its target.
                        // 0 doesn't display any arrow.
                        // 1 means roughly 350ms (the lowest time possible), then each increment adds about 75ms (needs testing).
                        // Using ticksToTarget, we can make the arrow take more time to reach its target the farther it is.
                        player.Out.SendCombatAnimation(m_owner, attackTarget, (ushort)model, 0x00, player.Out.BowShoot, flightDuration, 0x00, ((GameLiving)attackTarget).HealthPercent);
                    });

                    interruptDuration = attackComponent.AttackSpeed(attackWeapon);

                    switch (m_owner.rangeAttackComponent.RangedAttackType)
                    {
                        case eRangedAttackType.Critical:
                        {
                            double tmpEffectiveness = 2 - 0.3 * m_owner.GetConLevel(attackTarget);

                            if (tmpEffectiveness > 2)
                                effectiveness *= 2;
                            else if (tmpEffectiveness < 1.1)
                                effectiveness *= 1.1;
                            else
                                effectiveness *= tmpEffectiveness;

                            break;
                        }

                        case eRangedAttackType.SureShot:
                        {
                            effectiveness *= 0.5;
                            break;
                        }

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
                                effectiveness *= 0.25 + elapsedTime * 0.5 / rapidFireMaxDuration;
                                interruptDuration = (int)(interruptDuration * effectiveness);
                            }

                            break;
                        }
                    }

                    // Calculate Penetrating Arrow damage reduction.
                    if (attackTarget is GameLiving livingTarget)
                    {
                        int PALevel = m_owner.GetAbilityLevel(Abilities.PenetratingArrow);

                        if ((PALevel > 0) && (m_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long))
                        {
                            ECSGameSpellEffect bladeturn = livingTarget.effectListComponent.GetSpellEffects(eEffect.Bladeturn)?.FirstOrDefault();

                            if (bladeturn != null && attackTarget != bladeturn.SpellHandler.Caster)
                                effectiveness *= 0.25 + PALevel * 0.25;
                        }
                    }
                }
                else
                {
                    attackTarget = m_owner.TargetObject;

                    // NPCs try to switch to their ranged weapon whenever possible.
                    if (m_owner is GameNPC &&
                        !m_owner.IsBeingInterrupted &&
                        m_owner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                        !m_owner.IsWithinRadius(attackTarget, 500))
                    {
                        ((GameNPC)m_owner).SwitchToRanged(attackTarget);
                        return;
                    }

                    if (attackData != null && attackData.AttackResult is eAttackResult.Fumbled)
                    {
                        // Don't start the attack if the last one fumbled.
                        styleComponent.NextCombatStyle = null;
                        styleComponent.NextCombatBackupStyle = null;
                        attackData.AttackResult = eAttackResult.Missed;
                        m_interval = attackComponent.AttackSpeed(attackWeapon) * 2;
                        StartTime = m_interval;
                        return;
                    }

                    // Figure out which combat style may be (GamePlayer) or is being (GameNPC) used.
                    combatStyle = m_owner is GamePlayer ? styleComponent.GetStyleToUse() : styleComponent.NPCGetStyleToUse();

                    if (combatStyle != null && combatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                        attackWeapon = leftWeapon;

                    int mainHandAttackSpeed = attackComponent.AttackSpeed(attackWeapon);

                    if (GameLoop.GameLoopTime > styleComponent.NextCombatStyleTime + mainHandAttackSpeed)
                    {
                        // Cancel the styles if they were registered too long ago.
                        // Nature's Shield stays active forever and falls back to a non-backup style.
                        if (styleComponent.NextCombatBackupStyle?.ID == 394)
                            styleComponent.NextCombatStyle = styleComponent.NextCombatBackupStyle;
                        else if (styleComponent.NextCombatStyle?.ID != 394)
                            styleComponent.NextCombatStyle = null;

                        styleComponent.NextCombatBackupStyle = null;
                    }

                    interruptDuration = mainHandAttackSpeed;

                    // Damage is doubled on sitting players, but only with melee weapons; arrows and magic does normal damage.
                    if (attackTarget is GamePlayer playerTarget && playerTarget.IsSitting)
                        effectiveness *= 2;
                }

                int addRange = combatStyle?.Procs?.FirstOrDefault()?.Item1.SpellType == (byte)eSpellType.StyleRange ? (int)combatStyle?.Procs?.FirstOrDefault()?.Item1.Value - attackComponent.AttackRange : 0;

                // This is a NPC and the target isn't in melee range yet. Check if another target is in range to attack on the way to the main target.
                if (attackTarget != null &&
                    m_owner is GameNPC npcOwner &&
                    m_owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                    npcOwner.Brain is not IControlledBrain &&
                    npcOwner.Brain is StandardMobBrain npcBrain &&
                    npcBrain.AggroTable.Count > 0 &&
                    !m_owner.IsWithinRadius(attackTarget, attackComponent.AttackRange + addRange))
                {
                    GameLiving possibleTarget = null;
                    long maxaggro = 0;
                    long aggro = 0;

                    foreach (GamePlayer playerInRadius in m_owner.GetPlayersInRadius((ushort)attackComponent.AttackRange))
                    {
                        if (npcBrain.AggroTable.ContainsKey(playerInRadius))
                        {
                            aggro = npcBrain.GetAggroAmountForLiving(playerInRadius);

                            if (aggro <= 0)
                                continue;

                            if (aggro > maxaggro)
                            {
                                possibleTarget = playerInRadius;
                                maxaggro = aggro;
                            }
                        }
                    }

                    // Check for NPCs in attack range. Only check if the NPCNextNPCVicinityCheck is less than the current GameLoop Time.
                    if (m_NPCNextNPCVicinityCheck < GameLoop.GameLoopTime)
                    {
                        // Set the next check for NPCs. Will be in a range from 100ms -> NPC_VICINITY_CHECK_DELAY.
                        m_NPCNextNPCVicinityCheck = GameLoop.GameLoopTime + Util.Random(100,NPC_VICINITY_CHECK_DELAY);

                        foreach (GameNPC npcInRadius in m_owner.GetNPCsInRadius((ushort)attackComponent.AttackRange))
                        {
                            if (npcBrain.AggroTable.ContainsKey(npcInRadius))
                            {
                                aggro = npcBrain.GetAggroAmountForLiving(npcInRadius);

                                if (aggro <= 0)
                                    continue;

                                if (aggro > maxaggro)
                                {
                                    possibleTarget = npcInRadius;
                                    maxaggro = aggro;
                                }
                            }
                        }
                    }

                    if (possibleTarget == null)
                    {
                        m_interval = TICK_INTERVAL_FOR_NON_ATTACK;
                        return;
                    }
                    else
                        attackTarget = possibleTarget;
                }

                attackComponent.weaponAction = new WeaponAction(m_owner, attackTarget, attackWeapon, leftWeapon, effectiveness, interruptDuration, combatStyle);

                // A positive ticksToTarget means the effects of our attack will be delayed. Typically used for ranged attacks.
                if (ticksToTarget > 0)
                {
                    new ECSGameTimer(m_owner, new ECSGameTimer.ECSTimerCallback(attackComponent.weaponAction.Execute), ticksToTarget);

                    // This is done in weaponAction.Execute(), but we probably shouldn't wait for the attack to reach our target since AttackComponent.Tick() relies on it.
                    attackComponent.weaponAction.AttackFinished = true;
                    
                    // This is also done in weaponAction.Execute(), but we must unstealth immediately.
                    if (m_owner is GamePlayer playerOwner)
                        playerOwner.Stealth(false);

                    m_owner.rangeAttackComponent.RemoveEnduranceAndAmmoOnShot();

                    // TODO: Are there any other actions that should be performed here?
                }
                else
                    attackComponent.weaponAction.Execute();
                
                if (m_owner.ObjectState != eObjectState.Active)
                {
                    attackComponent.attackAction?.CleanupAttackAction();
                    return;
                }

                // Switch to melee if range to target is less than 200.
                if (m_owner is GameNPC &&
                    m_owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance &&
                    m_owner.TargetObject != null &&
                    m_owner.IsWithinRadius(m_owner.TargetObject, 200))
                    m_owner.SwitchWeapon(eActiveWeaponSlot.Standard);

                // Retrieve the newest data after from the last WeaponAction.
                attackData = m_owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                m_interval = attackComponent.AttackSpeed(attackWeapon, leftWeapon);

                // Non-ranged weapons tick every TICK_INTERVAL_FOR_NON_ATTACK if they didn't attack.
                if (m_owner.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    if (attackData == null ||
                        attackData.AttackResult is not eAttackResult.Missed
                        and not eAttackResult.HitUnstyled
                        and not eAttackResult.HitStyle
                        and not eAttackResult.Evaded
                        and not eAttackResult.Blocked
                        and not eAttackResult.Parried)
                    {
                        m_interval = TICK_INTERVAL_FOR_NON_ATTACK;

                        if (RoundWithNoAttackTime == 0)
                            RoundWithNoAttackTime = GameLoop.GameLoopTime;
                    }
                    else
                    {
                        // Clear styles for the next round.
                        styleComponent.NextCombatStyle = null;
                        styleComponent.NextCombatBackupStyle = null;

                        if (m_owner is GamePlayer weaponskiller && weaponskiller.UseDetailedCombatLog)
                            weaponskiller.Out.SendMessage($"Attack Speed: {m_interval / 1000.0}s", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }
                }
                else
                {
                    // Mobs always shot and reload.
                    if (m_owner is GameNPC)
                        m_owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.AimFireReload;

                    if (m_owner.rangeAttackComponent.RangedAttackState != eRangedAttackState.AimFireReload)
                    {
                        attackComponent.StopAttack();
                        attackComponent.attackAction?.CleanupAttackAction();
                        return;
                    }
                    else
                    {
                        if (m_owner is not GamePlayer || m_owner.rangeAttackComponent.RangedAttackType != eRangedAttackType.Long)
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
                            m_owner.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, GameLoop.GameLoopTime);

                        if (m_owner is not GamePlayer || !m_owner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
                        {
                            Parallel.ForEach(m_owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).OfType<GamePlayer>(), player =>
                            {
                                if (player == null)
                                    return;

                                // The 'stance' parameter appears to be used to tell whether or not the animation should be held, and doesn't seem to be related to the weapon speed.
                                player.Out.SendCombatAnimation(m_owner, null, (ushort)(attackWeapon != null ? attackWeapon.Model : 0), 0x00, player.Out.BowPrepare, 0x1A, 0x00, 0x00);
                            });
                        }

                        int speed = attackComponent.AttackSpeed(attackWeapon);

                        if (m_owner.rangeAttackComponent.RangedAttackType == eRangedAttackType.RapidFire)
                        {
                            // Can start fire at the middle of the normal time.
                            speed /= 2;
                            speed = Math.Max(1500, speed);
                        }

                        m_interval = speed;
                    }
                }

                StartTime = m_interval;
            }

            if (RangeInterruptTime > time)
            {
                if (m_owner.rangeAttackComponent?.RangedAttackState is eRangedAttackState.Aim or eRangedAttackState.AimFire or eRangedAttackState.AimFireReload)
                {
                    if (m_owner is GamePlayer playerOwner && playerOwner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        if (playerOwner.InterruptTime > GameLoop.GameLoopTime && playerOwner.attackComponent.Attackers.Count > 0)
                        {
                            GameObject attacker = playerOwner.attackComponent.Attackers.Last();
                            double chance = 90;

                            if (attacker is GamePlayer)
                                chance = 100;
                            else
                            {
                                double mod = playerOwner.GetConLevel(attacker);
                                chance += mod * 10;
                                chance = Math.Max(1, chance);
                                chance = Math.Min(99, chance);
                            }

                            if (!Util.Chance((int) chance))
                                return;

                            string attackTypeMsg = LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Shot");

                            if (playerOwner.attackComponent.AttackWeapon != null && playerOwner.attackComponent.AttackWeapon.Object_Type == (int) eObjectType.Thrown)
                                attackTypeMsg = LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.Type.Throw");
                            if (attacker is GameNPC)
                                playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true, playerOwner.Client.Account.Language, attacker as GameNPC), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            else
                                playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.Interrupted", attacker.GetName(0, true), attackTypeMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                            playerOwner.attackComponent.StopAttack();
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
    }
}
