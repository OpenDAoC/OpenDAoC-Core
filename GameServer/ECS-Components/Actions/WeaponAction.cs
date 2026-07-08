using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.Spells;
using DOL.GS.Styles;
using static DOL.GS.GameObject;

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
        private int _extraSwings;

        public long AttackRoundEndTime { get; private set; }
        public bool IsAttackRoundFinished => GameServiceUtils.ShouldTick(AttackRoundEndTime);

        public eActiveWeaponSlot ActiveWeaponSlot { get; }
        public eRangedAttackType RangedAttackType { get; }
        public DbInventoryItem Ammo { get; }
        public byte StyleChainStage { get; }

        public bool HasConsumedBlockRound { get; set; } // Used to prevent multihit attacks from consuming multiple block rounds.
        public bool HasAmmoReachedTarget { get; private set; } // Used to not cancel the release animation. A bit clunky, may not work perfectly.
        public DualWieldMechanic DualWieldMechanic { get; private set; }
        public byte SwingsExecuted { get; private set; }

        public WeaponAction(GameLiving owner, GameObject target, DbInventoryItem attackWeapon, DbInventoryItem leftWeapon, double effectiveness, int interval, Style combatStyle, byte styleChainStage)
        {
            _owner = owner;
            _target = target;
            _attackWeapon = attackWeapon;
            _leftWeapon = leftWeapon;
            _effectiveness = effectiveness;
            _interval = interval;
            _combatStyle = combatStyle;
            ActiveWeaponSlot = owner.ActiveWeaponSlot;
            StyleChainStage = styleChainStage;
        }

        public WeaponAction(GameLiving owner, GameObject target, DbInventoryItem attackWeapon, double effectiveness, int interval, eRangedAttackType rangedAttackType, DbInventoryItem ammo)
        {
            _owner = owner;
            _target = target;
            _attackWeapon = attackWeapon;
            _effectiveness = effectiveness;
            _interval = interval;
            ActiveWeaponSlot = owner.ActiveWeaponSlot;
            RangedAttackType = rangedAttackType;
            Ammo = ammo;
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

            if (!MakeMainHandAttack(out AttackData mainHandAttackData))
                return;

            AttackRoundEndTime = GameLoop.GameLoopTime + _interval;
            AttackData extraAttackData = null;
            DbInventoryItem lastExtraWeapon = null;

            for (int i = 0; i < _extraSwings; i++)
            {
                if (i % 2 == 0)
                {
                    lastExtraWeapon = _leftWeapon;
                    extraAttackData = InitiateAttack(_leftWeapon, null);
                }
                else
                {
                    lastExtraWeapon = _attackWeapon;
                    extraAttackData = InitiateAttack(_attackWeapon, null);
                }

                FinalizeAttack(extraAttackData);
            }

            switch (mainHandAttackData.AttackResult)
            {
                case eAttackResult.HitStyle:
                {
                    if (mainHandAttackData.StyleEffects != null)
                    {
                        foreach (ISpellHandler proc in mainHandAttackData.StyleEffects)
                            proc.StartSpell(mainHandAttackData.Target);
                    }

                    break;
                }
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

            // Show H2H multihit attacks.
            if (_owner is GamePlayer playerOwner)
            {
                string multihitMessage = null;

                if (_extraSwings == 2)
                    multihitMessage = "Triple attack!";
                else if (_extraSwings == 3)
                    multihitMessage = "Quad attack!";

                if (!string.IsNullOrEmpty(multihitMessage))
                    playerOwner.Out.SendMessage(multihitMessage, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }

            // Show the animation.
            if (mainHandAttackData.AttackResult is not eAttackResult.HitUnstyled and not eAttackResult.HitStyle && extraAttackData != null)
                ShowAttackAnimation(extraAttackData, lastExtraWeapon);
            else
                ShowAttackAnimation(mainHandAttackData, _attackWeapon);

            if (_owner.HasAbilityType(typeof(AtlasOF_PreventFlight)) &&
                Util.Chance(35) &&
                _target is GameLiving livingTarget &&
                _owner.IsObjectInFront(livingTarget, 120) &&
                livingTarget.IsMoving &&
                livingTarget.GetAngle(_owner) is >= 150 and < 210)
            {
                Spell spell = SkillBase.GetSpellByID(7083);

                if (spell != null)
                {
                    ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(_owner, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellHandler?.StartSpell(livingTarget);
                }
            }
        }

        private int CalculateExtraSwings()
        {
            DualWieldMechanic = DualWieldMechanic.None;

            if (!_owner.attackComponent.CanUseLefthandedWeapon ||
                _leftWeapon == null ||
                (eObjectType) _leftWeapon.Object_Type is eObjectType.Shield)
            {
                return 0;
            }

            if (_owner is GameNPC npcOwner)
            {
                if (_attackWeapon == null ||
                    _attackWeapon.SlotPosition is not Slot.RIGHTHAND ||
                    npcOwner.LeftHandSwingChance <= 0)
                {
                    return 0;
                }

                DualWieldMechanic = DualWieldMechanic.Classic;
                double random = _owner.RandomProvider.GetPseudoDouble(RandomContextFactory.DualWield()) * 100;
                return random < npcOwner.LeftHandSwingChance ? 1 : 0;
            }

            if (_owner is not GamePlayer || _attackWeapon == null)
                return 0;

            // Left Axe.
            if (_owner.GetBaseSpecLevel(Specs.Left_Axe) > 0)
            {
                DualWieldMechanic = DualWieldMechanic.Classic;
                return 1;
            }

            // DW / CD.
            double leftHandSwingChance = _owner.attackComponent.CalculateDwCdLeftHandSwingChance();

            if (leftHandSwingChance > 0)
            {
                DualWieldMechanic = DualWieldMechanic.Classic;
                return _owner.RandomProvider.GetPseudoDouble(RandomContextFactory.DualWield()) < leftHandSwingChance ? 1 : 0;
            }

            // H2H.
            (double doubleChance, double tripleChance, double quadChance) = _owner.attackComponent.CalculateHthSwingChances();

            if (doubleChance > 0)
            {
                DualWieldMechanic = DualWieldMechanic.HandToHand;
                double random = _owner.RandomProvider.GetPseudoDouble(RandomContextFactory.DualWield());

                if (random < doubleChance)
                    return 1;

                tripleChance += doubleChance;

                if (random < tripleChance)
                    return 2;

                quadChance += tripleChance;

                if (random < quadChance)
                    return 3;
            }

            return 0;
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

        private eAttackResult? CheckAttackPrecondition()
        {
            if (_target is not GameLiving livingTarget)
                return _target == null ? eAttackResult.NoTarget : eAttackResult.NoValidTarget;

            if (!livingTarget.IsAlive)
                return eAttackResult.TargetDead;

            if (livingTarget.CurrentRegionID != _owner.CurrentRegionID || livingTarget.ObjectState is not eObjectState.Active)
                return eAttackResult.NoValidTarget;

            bool isRangedAttack = ActiveWeaponSlot is eActiveWeaponSlot.Distance;

            if (!isRangedAttack &&
                _owner is GamePlayer &&
                livingTarget is not GameKeepComponent &&
                (!_owner.IsObjectInFront(livingTarget, 120) || !_owner.TargetInView))
            {
                return eAttackResult.TargetNotVisible;
            }

            if (!isRangedAttack)
            {
                int attackRange = _owner.attackComponent.AttackRange;

                if (_combatStyle != null)
                {
                    StyleProcInfo styleProcInfo = _combatStyle.Procs.FirstOrDefault(static x => x.Spell.SpellType is eSpellType.StyleRange);

                    if (styleProcInfo != null)
                        attackRange = (int) styleProcInfo.Spell.Value;
                }

                if (!_owner.IsWithinRadius(livingTarget, attackRange))
                    return eAttackResult.OutOfRange;
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(_owner, livingTarget, GameLoop.GameLoopTime - _owner.attackComponent.attackAction.RoundWithNoAttackTime <= 1500))
                return eAttackResult.NotAllowed_ServerRules;

            // SelectiveBlindness check used to be here.

            if (livingTarget.HasAbility(Abilities.DamageImmunity))
                return eAttackResult.NoValidTarget;

            return null;
        }

        private bool MakeMainHandAttack(out AttackData attackData)
        {
            eAttackResult? precondition = CheckAttackPrecondition();

            if (precondition.HasValue)
            {
                attackData = CreateAttackData(_attackWeapon, _combatStyle);
                attackData.AttackResult = precondition.Value;
                _owner.attackComponent.attackAction.LastAttackData = attackData;
                _owner.attackComponent.SendInvalidAttackMessage(_target, precondition.Value);
                return false;
            }

            _extraSwings = CalculateExtraSwings();
            bool isDualWieldAttack = DualWieldMechanic is not DualWieldMechanic.None;

            int animationId = 0;
            byte usedHand = 0;

            // Determine the weapon and animation to use.
            if (_extraSwings > 0)
            {
                if (isDualWieldAttack)
                    usedHand = 2;

                if (_combatStyle == null)
                    animationId = -2; // Virtual code for both weapons swing animation.
            }
            else if (_attackWeapon != null)
            {
                if (isDualWieldAttack && _combatStyle == null && Util.Chance(50))
                {
                    _attackWeapon = _leftWeapon;
                    usedHand = 1;
                    animationId = -1; // Virtual code for left weapon swing animation.
                }
            }

            attackData = InitiateAttack(_attackWeapon, _combatStyle);

            _owner.attackComponent.attackAction.LastAttackData = attackData;
            _owner.attackComponent.UsedHandOnLastDualWieldAttack = attackData.IsHit ? usedHand : (byte) 0;

            if (_combatStyle == null)
                attackData.AnimationId = animationId;

            // 1.89:
            // - Characters who are attacked by stealthed archers will now target the attacking archer if the attacked player does not already have a target.
            if (_owner.IsStealthed &&
                attackData.AttackType is AttackData.eAttackType.Ranged &&
                (attackData.AttackResult is eAttackResult.HitUnstyled or eAttackResult.HitStyle))
            {
                if (_target is GamePlayer playerTarget && playerTarget.TargetObject == null)
                    playerTarget.Out.SendChangeTarget(attackData.Attacker);
            }

            FinalizeAttack(attackData);
            return true;
        }

        private AttackData CreateAttackData(DbInventoryItem weapon, Style style)
        {
            return new()
            {
                Attacker = _owner,
                Target = _target as GameLiving,
                Style = style,
                DamageType = _owner.attackComponent.AttackDamageType(weapon, this),
                AttackType = AttackData.GetAttackType(weapon, this, _owner),
                Weapon = weapon,
                Interval = _interval,
                IsOffHand = weapon != null && weapon.SlotPosition is Slot.LEFTHAND
            };
        }

        private AttackData InitiateAttack(DbInventoryItem weapon, Style style)
        {
            AttackData ad = CreateAttackData(weapon, style);
            _owner.attackComponent.MakeAttack(this, ad, _target, weapon, style, _effectiveness, _interval);
            SwingsExecuted++;
            return ad;
        }

        private void FinalizeAttack(AttackData attackData)
        {
            GameLiving target = attackData.Target;

            // Notify the target of our attack (sends damage messages, should be before damage).
            target.OnAttackedByEnemy(attackData);

            // Deal damage and start effects.
            // `AttackData` doesn't contain the armor / shield hit, so we must again fetch it from the target's inventory.
            if (attackData.AttackResult is eAttackResult.HitUnstyled or eAttackResult.HitStyle)
            {
                _owner.DealDamage(attackData);
                HandleDamageAdd(_owner, attackData);
                _owner.CheckWeaponMagicalEffect(attackData);
                HandleDamageShields(attackData);
                target.OnArmorHit(attackData, target.Inventory?.GetItem((eInventorySlot) attackData.ArmorHitLocation));
            }
            else if (attackData.AttackResult is eAttackResult.Blocked)
                target.OnArmorHit(attackData, target.ActiveLeftWeapon);

            HandleReflexAttack(_owner, target, attackData.AttackResult);
        }

        private static void HandleDamageAdd(GameLiving owner, AttackData ad)
        {
            List<ECSGameSpellEffect> damageAddEffects = owner.effectListComponent.GetSpellEffects(eEffect.DamageAdd);

            if (damageAddEffects == null || damageAddEffects.Count == 0)
                return;

            List<ECSGameSpellEffect> unaffectedDamageAdds = null;
            List<ECSGameSpellEffect> regularDamageAdds = null;

            foreach (ECSGameSpellEffect effect in damageAddEffects)
            {
                if (!effect.IsActive)
                    continue;

                if (effect.SpellHandler.Spell.EffectGroup == 99999)
                {
                    unaffectedDamageAdds ??= GameLoop.GetListForTick<ECSGameSpellEffect>();
                    unaffectedDamageAdds.Add(effect);
                }
                else
                {
                    regularDamageAdds ??= GameLoop.GetListForTick<ECSGameSpellEffect>();
                    regularDamageAdds.Add(effect);
                }
            }

            if (unaffectedDamageAdds != null)
            {
                foreach (ECSGameSpellEffect damageAdd in unaffectedDamageAdds)
                   (damageAdd.SpellHandler as DamageAddSpellHandler).Handle(ad, 1.0);
            }

            if (regularDamageAdds != null)
            {
                regularDamageAdds.Sort(static (a, b) => b.SpellHandler.Spell.Damage.CompareTo(a.SpellHandler.Spell.Damage));
                int numRegularDmgAddsApplied = 0;

                foreach (ECSGameSpellEffect damageAdd in regularDamageAdds)
                {
                    double effectiveness = damageAdd.Effectiveness * Math.ScaleB(1.0, -numRegularDmgAddsApplied);
                    (damageAdd.SpellHandler as DamageAddSpellHandler).Handle(ad, effectiveness);
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
            {
                if (!damageShield.IsActive)
                    continue;

                double effectiveness = damageShield.Effectiveness;
                (damageShield.SpellHandler as DamageShieldSpellHandler).Handle(ad, effectiveness);
            }
        }

        private static void HandleReflexAttack(GameLiving attacker, GameLiving target, eAttackResult attackResult)
        {
            // 1.65 behavior:
            // https://forums.jeuxonline.info/sujet/250789/attaque-reflexe-moine
            // https://web.archive.org/web/20050108223232/http://forums.drunkenfriar.com/viewtopic.php?t=47#1435&sid=037826ba0db6f1217513823c90303a52
            // In summary:
            // * Only works against frontal attacks.
            // * Only works against attacks that actually hit the target.
            // * Can counter the offhand from dual wield attacks.
            // * Counter attacks can miss or be defended against.
            // * Has a weird interaction with Friar's Boon, allowing counter attacks to be styled (not implemented here).
            // * May have some kind of internal cooldown (not affecting offhands somehow) or be based on the Friar's attack speed (not implemented here).

            if (!target.effectListComponent.ContainsEffectForEffectType(eEffect.ReflexAttack) || !target.IsObjectInFront(attacker, 120))
                return;

            switch (attackResult)
            {
                case eAttackResult.HitStyle:
                case eAttackResult.HitUnstyled:
                {
                    int attackSpeed = target.AttackSpeed(target.ActiveWeapon);
                    WeaponAction weaponAction = new(target, attacker, target.ActiveWeapon, null, 1.0, attackSpeed, null, 0);
                    AttackData ad = weaponAction.InitiateAttack(target.ActiveWeapon, null); // Don't call `WeaponAction.Execute` here.

                    // If we get hit by Reflex Attack (it can miss), send a "you were hit" message to the attacker manually
                    // since it will not be done automatically as this attack is not processed by regular attacking code.
                    if (ad.AttackResult is eAttackResult.HitUnstyled)
                    {
                        GamePlayer playerAttacker = attacker as GamePlayer;
                        playerAttacker?.Out.SendMessage($"{target.Name} counter-attacks you for {ad.Damage} damage.", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                    }

                    break;
                }
            }
        }
    }

    public enum DualWieldMechanic : byte
    {
        None,
        Classic,
        HandToHand
    }
}
