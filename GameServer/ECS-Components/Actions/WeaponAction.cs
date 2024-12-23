using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;

namespace DOL.GS
{
    public class WeaponAction
    {
        private GameLiving _owner;
        private GameObject _target;
        private DbInventoryItem _attackWeapon;
        private DbInventoryItem _leftWeapon;
        private double _effectiveness;
        private int _interval;
        private Style _combatStyle;
        private int _leftHandSwingCount;
        private bool _isDualWieldAttack; // Not necessarily true even if _leftHandSwingCount is > 0, for example H2H isn't technically dual wield.

        // The active weapon slot, ranged attack type, and ammo at the time the ammo was released
        public eActiveWeaponSlot ActiveWeaponSlot { get; }
        public eRangedAttackType RangedAttackType { get; }
        public DbInventoryItem Ammo { get; }

        public bool HasAmmoReachedTarget { get; private set; } // Used to not cancel the release animation. A bit clunky, may not work perfectly.

        public WeaponAction(GameLiving owner, GameObject target, DbInventoryItem attackWeapon, DbInventoryItem leftWeapon, double effectiveness, int interval, Style combatStyle)
        {
            _owner = owner;
            _target = target;
            _attackWeapon = attackWeapon;
            _leftWeapon = leftWeapon;
            _effectiveness = effectiveness;
            _interval = interval;
            _combatStyle = combatStyle;
            ActiveWeaponSlot = owner.ActiveWeaponSlot;
        }

        public WeaponAction(GameLiving owner, GameObject target, DbInventoryItem attackWeapon, double effectiveness, int interval, eRangedAttackType rangedAttackType, DbInventoryItem ammo)
        {
            _owner = owner;
            _target = target;
            _attackWeapon = attackWeapon;
            _effectiveness = effectiveness;
            _interval = interval;
            RangedAttackType = rangedAttackType;
            Ammo = ammo;
            ActiveWeaponSlot = owner.ActiveWeaponSlot;
        }

        public int Execute(ECSGameTimer timer)
        {
            HasAmmoReachedTarget = true;
            Execute();
            return 0;
        }

        public void Execute()
        {
            // 1.89
            //- Pets will no longer continue to attack a character after the character has stealthed.
            // 1.88
            //- Monsters, pets and Non-Player Characters (NPCs) will now halt their pursuit when the character being chased stealths.

            _leftHandSwingCount = _owner.attackComponent.CalculateLeftHandSwingCount(_attackWeapon, _leftWeapon);
            _isDualWieldAttack = IsDualWieldAttack(_attackWeapon, _leftWeapon, _owner, _leftHandSwingCount);

            if (!MakeMainHandAttack(_attackWeapon, _leftWeapon, _combatStyle, _effectiveness, out AttackData mainHandAttackData))
                return;

            MakeOffHandAttack(out AttackData leftHandAttackData); // This returns the last attack for H2H, not sure if this is correct.

            switch (mainHandAttackData.AttackResult)
            {
                case eAttackResult.NoTarget:
                case eAttackResult.TargetDead:
                {
                    _owner.OnTargetDeadOrNoTarget();
                    return;
                }
                case eAttackResult.NotAllowed_ServerRules:
                case eAttackResult.NoValidTarget:
                {
                    _owner.attackComponent.StopAttack();
                    return;
                }
                case eAttackResult.OutOfRange:
                    break;
            }

            // Unstealth before attack animation.
            if (_owner is GamePlayer playerOwner)
                playerOwner.Stealth(false);

            // Show the animation.
            if (mainHandAttackData.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle && leftHandAttackData != null)
                ShowAttackAnimation(leftHandAttackData, _leftWeapon);
            else
                ShowAttackAnimation(mainHandAttackData, _attackWeapon);

            // Mobs' heading isn't updated after they start attacking, so we update it after they swing.
            if (_owner is GameNPC npcOwner)
                npcOwner.TurnTo(mainHandAttackData.Target);

            return;
        }

        public void ShowAttackAnimation(AttackData ad, DbInventoryItem weapon)
        {
            bool showAnimation = false;

            switch (ad.AttackResult)
            {
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                case eAttackResult.Evaded:
                case eAttackResult.Parried:
                case eAttackResult.Missed:
                case eAttackResult.Blocked:
                case eAttackResult.Fumbled:
                {
                    showAnimation = true;
                    break;
                }
            }

            if (!showAnimation)
                return;

            GameLiving defender = ad.Target;

            if (showAnimation)
            {
                // http://dolserver.sourceforge.net/forum/showthread.php?s=&threadid=836
                byte resultByte = 0;
                int attackersWeapon = (weapon == null) ? 0 : weapon.Model;
                int defendersWeapon = 0;

                switch (ad.AttackResult)
                {
                    case eAttackResult.Missed:
                    {
                        resultByte = 0;
                        break;
                    }
                    case eAttackResult.Evaded:
                    {
                        resultByte = 3;
                        break;
                    }
                    case eAttackResult.Fumbled:
                    {
                        resultByte = 4;
                        break;
                    }
                    case eAttackResult.HitUnstyled:
                    {
                        resultByte = 10;
                        break;
                    }
                    case eAttackResult.HitStyle:
                    {
                        resultByte = 11;
                        break;
                    }
                    case eAttackResult.Parried:
                    {
                        resultByte = 1;

                        if (defender.ActiveWeapon != null)
                            defendersWeapon = defender.ActiveWeapon.Model;

                        break;
                    }
                    case eAttackResult.Blocked:
                    {
                        resultByte = 2;

                        if (defender.Inventory != null)
                        {
                            DbInventoryItem lefthand = defender.ActiveLeftWeapon;

                            if (lefthand != null && (eObjectType) lefthand.Object_Type is eObjectType.Shield)
                                defendersWeapon = lefthand.Model;
                        }

                        break;
                    }
                }

                IEnumerable visiblePlayers = defender.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE);

                if (visiblePlayers == null)
                    return;

                foreach (GamePlayer player in visiblePlayers)
                {
                    if (player == null)
                        return;

                    int animationId;

                    switch (ad.AnimationId)
                    {
                        case -1:
                        {
                            animationId = player.Out.OneDualWeaponHit;
                            break;
                        }
                        case -2:
                        {
                            animationId = player.Out.BothDualWeaponHit;
                            break;
                        }
                        default:
                        {
                            animationId = ad.AnimationId;
                            break;
                        }
                    }

                    // It only affects the attacker's client, but for some reason, the attack animation doesn't play when the defender is different than the actually selected target.
                    // The lack of feedback makes fighting Spiritmasters very awkward because of the intercept mechanic. So until this get figured out, we'll instead play the hit animation on the attacker's selected target.
                    // Ranged attacks can be delayed (which makes the selected target unreliable) and don't seem to be affect by this anyway, so they must be ignored.
                    GameObject animationTarget = player != _owner || ActiveWeaponSlot is eActiveWeaponSlot.Distance || _target == defender ? defender : _target;
                    player.Out.SendCombatAnimation(_owner, animationTarget,
                                                   (ushort) attackersWeapon, (ushort) defendersWeapon,
                                                   animationId, 0, resultByte, defender.HealthPercent);
                }
            }
        }

        public static bool IsDualWieldAttack(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon, GameLiving attacker, int leftHandSwingCount)
        {
            if (attacker is GameNPC npcAttacker)
                return npcAttacker.LeftHandSwingChance > 0; // We can't rely on object types for NPCs.

            // I'm not sure this has to be that complicated for players.
            if (leftHandSwingCount > 0)
            {
                return (eObjectType) mainWeapon.Object_Type is not eObjectType.HandToHand &&
                    (eObjectType) leftWeapon?.Object_Type is not eObjectType.HandToHand &&
                    (eObjectType) mainWeapon.Object_Type is not eObjectType.TwoHandedWeapon &&
                    (eObjectType) mainWeapon.Object_Type is not eObjectType.Thrown &&
                    mainWeapon.SlotPosition is not Slot.RANGED;
            }
            else if (mainWeapon != null)
            {
                if (mainWeapon.Item_Type is Slot.TWOHAND || mainWeapon.SlotPosition is Slot.RANGED)
                    return false;

                if (leftWeapon != null && (eObjectType) leftWeapon.Object_Type is not eObjectType.Shield)
                    return true;
            }

            return false;
        }

        private bool MakeMainHandAttack(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon, Style style, double mainHandEffectiveness, out AttackData attackData)
        {
            int animationId = 0;
            _owner.attackComponent.UsedHandOnLastDualWieldAttack = 0;

            // Determine the weapon and animation to use.
            if (_leftHandSwingCount > 0)
            {
                if (_isDualWieldAttack)
                    _owner.attackComponent.UsedHandOnLastDualWieldAttack = 2;

                if (style == null)
                    animationId = -2; // Virtual code for both weapons swing animation.
            }
            else if (mainWeapon != null)
            {
                // One of two hands is used for attack if no style, treated as a main hand attack.
                if (_isDualWieldAttack && style == null && Util.Chance(50))
                {
                    mainWeapon = leftWeapon;
                    _owner.attackComponent.UsedHandOnLastDualWieldAttack = 1;
                    animationId = -1; // Virtual code for left weapon swing animation.
                }
            }

            attackData = _owner.attackComponent.MakeAttack(this, _target, mainWeapon, style, mainHandEffectiveness, _interval, _isDualWieldAttack);

            if (style == null)
                attackData.AnimationId = animationId;

            _owner.attackComponent.attackAction.LastAttackData = attackData;

            if (attackData.Target == null ||
                attackData.AttackResult is eAttackResult.OutOfRange or
                eAttackResult.TargetNotVisible or
                eAttackResult.NotAllowed_ServerRules or
                eAttackResult.TargetDead)
            {
                return false;
            }

            // 1.89:
            // - Characters who are attacked by stealthed archers will now target the attacking archer if the attacked player does not already have a target.
            if (_owner.IsStealthed &&
                attackData.AttackType is AttackData.eAttackType.Ranged &&
                (attackData.AttackResult is eAttackResult.HitUnstyled or eAttackResult.HitStyle))
            {
                if (_target is GamePlayer playerTarget && playerTarget.TargetObject == null)
                    playerTarget.Out.SendChangeTarget(attackData.Attacker);
            }

            MakeAttack(attackData);
            return true;
        }

        private void MakeOffHandAttack(out AttackData leftHandAttackData)
        {
            leftHandAttackData = null;

            for (int i = 0; i < _leftHandSwingCount; i++)
            {
                // Savage swings - main, left, main, left.
                if (i % 2 == 0)
                    leftHandAttackData = _owner.attackComponent.MakeAttack(this, _target, _leftWeapon, null, _effectiveness, _interval, _isDualWieldAttack);
                else
                    leftHandAttackData = _owner.attackComponent.MakeAttack(this, _target, _attackWeapon, null, _effectiveness, _interval, _isDualWieldAttack);

                MakeAttack(leftHandAttackData);
            }
        }

        private void MakeAttack(AttackData attackData)
        {
            // Notify the target of our attack (sends damage messages, should be before damage)
            attackData.Target.OnAttackedByEnemy(attackData);

            // Deal damage and start effects.
            if (attackData.AttackResult is eAttackResult.HitUnstyled or eAttackResult.HitStyle)
            {
                _owner.DealDamage(attackData);
                _owner.CheckWeaponMagicalEffect(attackData, attackData.Weapon);
                HandleDamageAdd(_owner, attackData);

                if (attackData.StyleEffects.Count > 0 && attackData.AttackResult is eAttackResult.HitStyle)
                {
                    foreach (ISpellHandler proc in attackData.StyleEffects)
                        proc.StartSpell(attackData.Target);
                }
            }

            HandleDamageShields(attackData);

            if (_target is GameLiving livingTarget && livingTarget.effectListComponent.ContainsEffectForEffectType(eEffect.ReflexAttack))
                HandleReflexAttack(_owner, attackData.Target, attackData.AttackResult, _interval);
        }

        private static void HandleDamageAdd(GameLiving owner, AttackData ad)
        {
            List<ECSGameSpellEffect> damageAddEffects = owner.effectListComponent.GetSpellEffects(eEffect.DamageAdd);

            if (damageAddEffects == null)
                return;

            /// [Atlas - Takii] This could probably be optimized a bit by doing the split below between "affected/unaffected by stacking"
            /// when the effect is applied in the EffectListComponent instead of every time we swing our weapon?
            List<ECSGameSpellEffect> damageAddsUnaffectedByStacking = [];

            // 1 - Apply the DmgAdds that are unaffected by stacking (usually RA-based DmgAdds, EffectGroup 99999) first regardless of their damage.
            foreach (ECSGameSpellEffect damageAdd in damageAddEffects)
            {
                if (damageAdd.SpellHandler.Spell.EffectGroup == 99999)
                {
                    damageAddsUnaffectedByStacking.Add(damageAdd);
                    (damageAdd.SpellHandler as DamageAddSpellHandler).Handle(ad, 1);
                }
            }

            // 2 - Apply regular damage adds. We only start reducing to 50% effectiveness if there is more than one regular damage add being applied.
            // "Unaffected by stacking" dmg adds also dont reduce subsequent damage adds; they are effectively outside of the stacking mechanism.
            int numRegularDmgAddsApplied = 0;

            foreach (ECSGameSpellEffect damageAdd in damageAddEffects.Except(damageAddsUnaffectedByStacking).OrderByDescending(e => e.SpellHandler.Spell.Damage))
            {
                if (damageAdd.IsBuffActive)
                {
                    (damageAdd.SpellHandler as DamageAddSpellHandler).Handle(ad, numRegularDmgAddsApplied > 0 ? 0.5 : 1.0);
                    numRegularDmgAddsApplied++;
                }
            }
        }

        private static void HandleDamageShields(AttackData ad)
        {
            List<ECSGameSpellEffect> damageShieldEffects = ad.Target.effectListComponent.GetSpellEffects(eEffect.FocusShield);

            if (damageShieldEffects == null)
                return;

            foreach (ECSGameSpellEffect damageShield in damageShieldEffects)
                (damageShield.SpellHandler as DamageShieldSpellHandler).Handle(ad, 1);
        }

        private static void HandleReflexAttack(GameLiving attacker, GameLiving target, eAttackResult attackResult, int interval)
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
                {
                    int attackSpeed = target.AttackSpeed(target.ActiveWeapon);
                    WeaponAction weaponAction = new(target, attacker, target.ActiveWeapon, null, target.Effectiveness, attackSpeed, null);
                    // Don't call `WeaponAction.Execute` here.
                    // It applies damage adds and shields, but Reflex Attack shouldn't trigger them.
                    // It would also cause a stack overflow if the target has Reflex Attack too.
                    AttackData ReflexAttackAD = target.attackComponent.LivingMakeAttack(weaponAction, attacker, target.ActiveWeapon, null, target.Effectiveness, attackSpeed, false, true);
                    target.DealDamage(ReflexAttackAD);

                    // If we get hit by Reflex Attack (it can miss), send a "you were hit" message to the attacker manually
                    // since it will not be done automatically as this attack is not processed by regular attacking code.
                    if (ReflexAttackAD.AttackResult is eAttackResult.HitUnstyled)
                    {
                        GamePlayer playerAttacker = attacker as GamePlayer;
                        playerAttacker?.Out.SendMessage($"{target.Name} counter-attacks you for {ReflexAttackAD.Damage} damage.", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    }

                    break;
                }
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
