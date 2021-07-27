using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
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
    public class AttackComponent
    {
        public GameLiving owner;
        public WeaponAction weaponAction;
        public AttackAction attackAction;

        /// <summary>
		/// Holds the Style that this living should use next
		/// </summary>
		protected Style m_nextCombatStyle;
        /// <summary>
        /// Holds the backup style for the style that the living should use next
        /// </summary>
        protected Style m_nextCombatBackupStyle;

        /// <summary>
        /// Gets or Sets the next combat style to use
        /// </summary>
        public Style NextCombatStyle
        {
            get { return m_nextCombatStyle; }
            set { m_nextCombatStyle = value; }
        }
        /// <summary>
        /// Gets or Sets the next combat backup style to use
        /// </summary>
        public Style NextCombatBackupStyle
        {
            get { return m_nextCombatBackupStyle; }
            set { m_nextCombatBackupStyle = value; }
        }

        public AttackComponent(GameLiving owner)
        {
            this.owner = owner;
        }

        public void Tick(long time)
        {
            if (weaponAction != null)
            {
                if (weaponAction.AttackFinished)
                    weaponAction = null;
                else
                    weaponAction.Tick(time);
            }
            if (attackAction != null)
            {
                attackAction.Tick(time);
            }
        }

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(owner, style);
        }

        /// <summary>
        /// Decides which style living will use in this moment
        /// </summary>
        /// <returns>Style to use or null if none</returns>
        public Style GetStyleToUse()
        {
            InventoryItem weapon;
            if (NextCombatStyle == null) return null;
            if (NextCombatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                weapon = owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
            else weapon = owner.AttackWeapon;

            if (StyleProcessor.CanUseStyle(owner, NextCombatStyle, weapon))
                return NextCombatStyle;

            if (NextCombatBackupStyle == null) return NextCombatStyle;

            return NextCombatBackupStyle;
        }

        /// <summary>
		/// Picks a style, prioritizing reactives an	d chains over positionals and anytimes
		/// </summary>
		/// <returns>Selected style</returns>
		public Style NPCGetStyleToUse()
        {
            var p = owner as GameNPC;
            if (p.Styles == null || p.Styles.Count < 1 || p.TargetObject == null)
                return null;

            // Chain and defensive styles skip the GAMENPC_CHANCES_TO_STYLE,
            //	or they almost never happen e.g. NPC blocks 10% of the time,
            //	default 20% style chance means the defensive style only happens
            //	2% of the time, and a chain from it only happens 0.4% of the time.
            if (p.StylesChain != null && p.StylesChain.Count > 0)
                foreach (Style s in p.StylesChain)
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;

            if (p.StylesDefensive != null && p.StylesDefensive.Count > 0)
                foreach (Style s in p.StylesDefensive)
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon)
                        && p.CheckStyleStun(s)) // Make sure we don't spam stun styles like Brutalize
                        return s;

            if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
            {
                // Check positional styles
                // Picking random styles allows mobs to use multiple styles from the same position
                //	e.g. a mob with both Pincer and Ice Storm side styles will use both of them.
                if (p.StylesBack != null && p.StylesBack.Count > 0)
                {
                    Style s = p.StylesBack[Util.Random(0, p.StylesBack.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                if (p.StylesSide != null && p.StylesSide.Count > 0)
                {
                    Style s = p.StylesSide[Util.Random(0, p.StylesSide.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                if (p.StylesFront != null && p.StylesFront.Count > 0)
                {
                    Style s = p.StylesFront[Util.Random(0, p.StylesFront.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                // Pick a random anytime style
                if (p.StylesAnytime != null && p.StylesAnytime.Count > 0)
                    return p.StylesAnytime[Util.Random(0, p.StylesAnytime.Count - 1)];
            }

            return null;
        }

        /// <summary>
		/// Starts a melee attack with this player
		/// </summary>
		/// <param name="attackTarget">the target to attack</param>
		public void StartAttack(GameObject attackTarget)
        {
            var p = owner as GamePlayer;

            if (p != null)
            {
                if (p.CharacterClass.StartAttack(attackTarget) == false)
                {
                    return;
                }

                if (!p.IsAlive)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouCantCombat"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                // Necromancer with summoned pet cannot attack
                if (p.ControlledBrain != null)
                    if (p.ControlledBrain.Body != null)
                        if (p.ControlledBrain.Body is NecromancerPet)
                        {
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantInShadeMode"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            return;
                        }

                if (p.IsStunned)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantAttackStunned"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (p.IsMezzed)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantAttackmesmerized"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                long vanishTimeout = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                if (vanishTimeout > 0 && vanishTimeout > GameLoop.GameLoopTime)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWaitAgain", (vanishTimeout - GameLoop.GameLoopTime + 1000) / 1000), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                long VanishTick = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                long changeTime = GameLoop.GameLoopTime - VanishTick;
                if (changeTime < 30000 && VanishTick > 0)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWait", ((30000 - changeTime) / 1000).ToString()), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsOnHorse)
                    p.IsOnHorse = false;

                if (p.IsDisarmed)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantDisarmed"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsSitting)
                {
                    p.Sit(false);
                }
                if (p.AttackWeapon == null)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CannotWithoutWeapon"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (p.AttackWeapon.Object_Type == (int)eObjectType.Instrument)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CannotMelee"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
                    {
                        if ((eCharacterClass)p.CharacterClass.ID == eCharacterClass.Scout || (eCharacterClass)p.CharacterClass.ID == eCharacterClass.Hunter || (eCharacterClass)p.CharacterClass.ID == eCharacterClass.Ranger)
                        {
                            // There is no feedback on live when attempting to fire a bow with arrows
                            return;
                        }
                    }

                    // Check arrows for ranged attack
                    if (p.RangeAttackAmmo == null)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.SelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    // Check if selected ammo is compatible for ranged attack
                    if (!p.CheckRangedAmmoCompatibilityWithActiveWeapon())
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    lock (p.EffectList)
                    {
                        foreach (IGameEffect effect in p.EffectList) // switch to the correct range attack type
                        {
                            if (effect is SureShotEffect)
                            {
                                p.RangedAttackType = eRangedAttackType.SureShot;
                                break;
                            }

                            if (effect is RapidFireEffect)
                            {
                                p.RangedAttackType = eRangedAttackType.RapidFire;
                                break;
                            }

                            if (effect is TrueshotEffect)
                            {
                                p.RangedAttackType = eRangedAttackType.Long;
                                break;
                            }
                        }
                    }

                    if (p.RangedAttackType == eRangedAttackType.Critical && p.Endurance < GamePlayer.CRITICAL_SHOT_ENDURANCE)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredShot"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.Endurance < GamePlayer.RANGE_ATTACK_ENDURANCE)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", p.AttackWeapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.IsStealthed)
                    {
                        // -Chance to unstealth while nocking an arrow = stealth spec / level
                        // -Chance to unstealth nocking a crit = stealth / level  0.20
                        int stealthSpec = p.GetModifiedSpecLevel(Specs.Stealth);
                        int stayStealthed = stealthSpec * 100 / p.Level;
                        if (p.RangedAttackType == eRangedAttackType.Critical)
                            stayStealthed -= 20;

                        if (!Util.Chance(stayStealthed))
                            p.Stealth(false);
                    }
                }
                else
                {
                    if (attackTarget == null)
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatNoTarget"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else
                        if (attackTarget is GameNPC)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget",
                            attackTarget.GetName(0, false, p.Client.Account.Language, (attackTarget as GameNPC))), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget", attackTarget.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                }

                if (p.CharacterClass is PlayerClass.ClassVampiir)
                {
                    GameSpellEffect removeEffect = SpellHandler.FindEffectOnTarget(p, "VampiirSpeedEnhancement");
                    if (removeEffect != null)
                        removeEffect.Cancel(false);
                }
                else
                {
                    // Bard RR5 ability must drop when the player starts a melee attack
                    IGameEffect DreamweaverRR5 = p.EffectList.GetOfType<DreamweaverEffect>();
                    if (DreamweaverRR5 != null)
                        DreamweaverRR5.Cancel(false);
                }
                LivingStartAttack(attackTarget);

                if (p.IsCasting && !p.castingComponent.spellHandler.Spell.Uninterruptible)
                {
                    p.StopCurrentSpellcast();
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.SpellCancelled"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                }

                //Clear styles
                NextCombatStyle = null;
                NextCombatBackupStyle = null;

                if (p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    p.Out.SendAttackMode(p.AttackState);
                }
                else
                {
                    p.TempProperties.setProperty(GamePlayer.RANGE_ATTACK_HOLD_START, 0L);

                    string typeMsg = "shot";
                    if (p.AttackWeapon.Object_Type == (int)eObjectType.Thrown)
                        typeMsg = "throw";

                    string targetMsg = "";
                    if (attackTarget != null)
                    {
                        if (p.IsWithinRadius(attackTarget, p.AttackRange))
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TargetInRange");
                        else
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TargetOutOfRange");
                    }

                    int speed = p.AttackSpeed(p.AttackWeapon) / 100;
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouPrepare", typeMsg, speed / 10, speed % 10, targetMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
            }
            else if (owner is GameNPC)
            {
                NPCStartAttack(attackTarget);
            }
            else
            {
                LivingStartAttack(attackTarget);
            }
        }

        /// <summary>
		/// Starts a melee or ranged attack on a given target.
		/// </summary>
		/// <param name="attackTarget">The object to attack.</param>
		private void LivingStartAttack(GameObject attackTarget)
        {
            // Aredhel: Let the brain handle this, no need to call StartAttack
            // if the body can't do anything anyway.
            if (owner.IsIncapacitated)
                return;

            if (owner.IsEngaging)
                owner.CancelEngageEffect();

            owner.AttackState = true;

            int speed = owner.AttackSpeed(owner.AttackWeapon);

            if (speed > 0)
            {
                //m_attackAction = CreateAttackAction();
                attackAction = new AttackAction(owner);

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // only start another attack action if we aren't already aiming to shoot
                    if (owner.RangedAttackState != eRangedAttackState.Aim)
                    {
                        owner.RangedAttackState = eRangedAttackState.Aim;

                        foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendCombatAnimation(owner, null, (ushort)(owner.AttackWeapon == null ? 0 : owner.AttackWeapon.Model),
                                                           0x00, player.Out.BowPrepare, (byte)(speed / 100), 0x00, 0x00);

                        //m_attackAction.Start((RangedAttackType == eRangedAttackType.RapidFire) ? speed / 2 : speed);
                        attackAction.StartTime = (owner.RangedAttackType == eRangedAttackType.RapidFire) ? speed / 2 : speed;

                    }
                }
                else
                {
                    //if (m_attackAction.TimeUntilElapsed < 500)
                    //	m_attackAction.Start(500);
                    if (attackAction.TimeUntilStart < 500)
                        attackAction.StartTime = 500;
                }
            }
        }

        /// <summary>
		/// Starts a melee attack on a target
		/// </summary>
		/// <param name="target">The object to attack</param>
		private void NPCStartAttack(GameObject target)
        {
            if (target == null)
                return;

            var p = owner as GameNPC;

            p.TargetObject = target;

            long lastTick = p.TempProperties.getProperty<long>(GameNPC.LAST_LOS_TICK_PROPERTY);

            if (ServerProperties.Properties.ALWAYS_CHECK_PET_LOS &&
                p.Brain != null &&
                p.Brain is IControlledBrain &&
                (target is GamePlayer || (target is GameNPC && (target as GameNPC).Brain != null && (target as GameNPC).Brain is IControlledBrain)))
            {
                GameObject lastTarget = (GameObject)p.TempProperties.getProperty<object>(GameNPC.LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == target)
                {
                    if (lastTick != 0 && GameLoop.GameLoopTime - lastTick < ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        return;
                }

                GamePlayer losChecker = null;
                if (target is GamePlayer)
                {
                    losChecker = target as GamePlayer;
                }
                else if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
                {
                    losChecker = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
                }
                else
                {
                    // try to find another player to use for checking line of site
                    foreach (GamePlayer player in p.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        losChecker = player;
                        break;
                    }
                }

                if (losChecker == null)
                {
                    return;
                }

                lock (p.LOS_LOCK)
                {
                    int count = p.TempProperties.getProperty<int>(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);

                    if (count > 10)
                    {
                        GameNPC.log.DebugFormat("{0} LOS count check exceeds 10, aborting LOS check!", p.Name);

                        // Now do a safety check.  If it's been a while since we sent any check we should clear count
                        if (lastTick == 0 || GameLoop.GameLoopTime - lastTick > ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        {
                            GameNPC.log.Debug("LOS count reset!");
                            p.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);
                        }

                        return;
                    }

                    count++;
                    p.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, count);

                    p.TempProperties.setProperty(GameNPC.LAST_LOS_TARGET_PROPERTY, target);
                    p.TempProperties.setProperty(GameNPC.LAST_LOS_TICK_PROPERTY, GameLoop.GameLoopTime);
                    p.m_targetLOSObject = target;

                }

                losChecker.Out.SendCheckLOS(p, target, new CheckLOSResponse(p.NPCStartAttackCheckLOS));
                return;
            }

            ContinueStartAttack(target);
        }

        public virtual void ContinueStartAttack(GameObject target)
        {
            var p = owner as GameNPC;

            p.StopMoving();
            p.StopMovingOnPath();

            if (p.Brain != null && p.Brain is IControlledBrain)
            {
                if ((p.Brain as IControlledBrain).AggressionState == eAggressionState.Passive)
                    return;

                GamePlayer owner = null;

                if ((owner = ((IControlledBrain)p.Brain).GetPlayerOwner()) != null)
                    owner.Stealth(false);
            }

            p.SetLastMeleeAttackTick();
            p.StartMeleeAttackTimer();

            LivingStartAttack(target);

            if (p.AttackState)
            {
                // if we're moving we need to lock down the current position
                if (p.IsMoving)
                    p.SaveCurrentPosition();

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // Archer mobs sometimes bug and keep trying to fire at max range unsuccessfully so force them to get just a tad closer.
                    p.Follow(target, p.AttackRange - 30, GameNPC.STICKMAXIMUMRANGE);
                }
                else
                {
                    p.Follow(target, GameNPC.STICKMINIMUMRANGE, GameNPC.STICKMAXIMUMRANGE);
                }
            }

        }



        /// <summary>
        /// Stops all attacks this player is making
        /// </summary>
        /// <param name="forced">Is this a forced stop or is the client suggesting we stop?</param>
        public void StopAttack(bool forced)
        {
            var p = owner as GamePlayer;

            if (p != null)
            {
                NextCombatStyle = null;
                NextCombatBackupStyle = null;
                LivingStopAttack(forced);
                if (p.IsAlive)
                {
                    p.Out.SendAttackMode(p.AttackState);
                }
            }
        }

        /// <summary>
        /// Stops all attacks this GameLiving is currently making.
        /// </summary>
        public void LivingStopAttack()
        {
            if (owner is GamePlayer)
            {
                StopAttack(true);
            }
            else
            {
                LivingStopAttack(true);
            }
        }

        /// <summary>
        /// Stop all attackes this GameLiving is currently making
        /// </summary>
        /// <param name="forced">Is this a forced stop or is the client suggesting we stop?</param>
        public void LivingStopAttack(bool forced)
        {
            owner.CancelEngageEffect();
            owner.AttackState = false;

            if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                owner.InterruptRangedAttack();
        }

        /// <summary>
		/// Stops all attack actions, including following target
		/// </summary>
		public void NPCStopAttack()
        {
            var p = owner as GameNPC;

            LivingStopAttack();
            p.StopFollowing();

            // Tolakram: If npc has a distance weapon it needs to be made active after attack is stopped
            if (p.Inventory != null && p.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null && p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                p.SwitchWeapon(eActiveWeaponSlot.Distance);
        }
    }
}
