using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.RealmAbilities;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

[SpellHandler("ThrowWeapon")]
public class ThrowWeaponSpell : DirectDamageSpell
{
    #region Disarm Weapon

    protected static Spell Disarm_Weapon;

    public static Spell Disarmed
    {
        get
        {
            if (Disarm_Weapon == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Uninterruptible = true;
                spell.Icon = 7293;
                spell.ClientEffect = 7293;
                spell.Description = "Disarms the caster.";
                spell.Name = "Throw Weapon(Disarm)";
                spell.Range = 0;
                spell.Value = 0;
                spell.Duration = 10;
                spell.SpellID = 900100;
                spell.Target = "Self";
                spell.Type = ESpellType.Disarm.ToString();
                Disarm_Weapon = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Combat_Styles_Effect, Disarm_Weapon);
            }

            return Disarm_Weapon;
        }
    }

    #endregion

    public const string DISABLE = "ThrowWeapon.Shortened.Disable.Timer";

    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        GamePlayer player = Caster as GamePlayer;
        if (player == null)
            return false;

        if (player.IsDisarmed)
        {
            MessageToCaster("You are disarmed and can't use this spell!", EChatType.CT_YouHit);
            return false;
        }

        DbInventoryItem weapon = null;

        //assign the weapon the player is using, it can be a twohanded or a standard slot weapon
        if (player.ActiveWeaponSlot.ToString() == "TwoHanded")
            weapon = player.Inventory.GetItem((EInventorySlot)12);
        if (player.ActiveWeaponSlot.ToString() == "Standard")
            weapon = player.Inventory.GetItem((EInventorySlot)10);

        //if the weapon is null, ie. they don't have an appropriate weapon active
        if (weapon == null)
        {
            MessageToCaster("Equip a weapon before using this spell!", EChatType.CT_SpellResisted);
            return false;
        }

        return base.CheckBeginCast(selectedTarget);
    }

    //Throw Weapon does not "resist"
    public override int CalculateSpellResistChance(GameLiving target)
    {
        return 0;
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        return base.OnEffectExpires(effect, noMessages);
    }


    public override void OnDirectEffect(GameLiving target)
    {
        if (target == null) return;

        if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

        // calc damage
        AttackData ad = CalculateDamageToTarget(target);
        SendDamageMessages(ad);
        DamageTarget(ad, true);
        target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
    }


    public override void DamageTarget(AttackData ad, bool showEffectAnimation)
    {
        DbInventoryItem weapon = null;
        weapon = ad.Weapon;

        if (showEffectAnimation && ad.Target != null)
        {
            byte resultByte = 0;
            int attackersWeapon = (weapon == null) ? 0 : weapon.Model;
            int defendersWeapon = 0;

            switch (ad.AttackResult)
            {
                case EAttackResult.Missed:
                    resultByte = 0;
                    break;
                case EAttackResult.Evaded:
                    resultByte = 3;
                    break;
                case EAttackResult.Fumbled:
                    resultByte = 4;
                    break;
                case EAttackResult.HitUnstyled:
                    resultByte = 10;
                    break;
                case EAttackResult.HitStyle:
                    resultByte = 11;
                    break;
                case EAttackResult.Parried:
                    resultByte = 1;
                    if (ad.Target != null && ad.Target.ActiveWeapon != null)
                    {
                        defendersWeapon = ad.Target.ActiveWeapon.Model;
                    }

                    break;
                case EAttackResult.Blocked:
                    resultByte = 2;
                    if (ad.Target != null && ad.Target.Inventory != null)
                    {
                        DbInventoryItem lefthand = ad.Target.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
                        if (lefthand != null && lefthand.Object_Type == (int)EObjectType.Shield)
                        {
                            defendersWeapon = lefthand.Model;
                        }
                    }

                    break;
            }

            foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player == null) continue;
                int animationId;
                switch (ad.AnimationId)
                {
                    case -1:
                        animationId = player.Out.OneDualWeaponHit;
                        break;
                    case -2:
                        animationId = player.Out.BothDualWeaponHit;
                        break;
                    default:
                        animationId = ad.AnimationId;
                        break;
                }
                //We don't need to send the animiation for the throwning, thats been done earlier.

                //this is for the defender, which should show the appropriate animation
                player.Out.SendCombatAnimation(null, ad.Target, (ushort)attackersWeapon, (ushort)defendersWeapon,
                    animationId, 0, resultByte, ad.Target.HealthPercent);

            }
        }

        // send animation before dealing damage else dead livings show no animation
        ad.Target.OnAttackedByEnemy(ad);
        ad.Attacker.DealDamage(ad);
        if (ad.Damage == 0 && ad.Target is GameNpc)
        {
            IOldAggressiveBrain aggroBrain = ((GameNpc)ad.Target).Brain as IOldAggressiveBrain;
            if (aggroBrain != null)
                aggroBrain.AddToAggroList(Caster, 1);
        }

    }

    public override void SendDamageMessages(AttackData ad)
    {
        GameObject target = ad.Target;
        DbInventoryItem weapon = ad.Weapon;
        GamePlayer player = Caster as GamePlayer;

        switch (ad.AttackResult)
        {
            case EAttackResult.TargetNotVisible:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.NotInView",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.OutOfRange:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.TooFarAway",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.TargetDead:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.AlreadyDead",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.Blocked:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Blocked",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.Parried:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Parried",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.Evaded:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Evaded",
                        ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.NoTarget:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.NeedTarget"),
                    EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.NoValidTarget:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.CantBeAttacked"),
                    EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.Missed:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Miss"),
                    EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.Fumbled:
                player.Out.SendMessage(
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Fumble"),
                    EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
            case EAttackResult.HitStyle:
            case EAttackResult.HitUnstyled:
                string modmessage = "";
                if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";

                string hitWeapon = "";

                switch (ServerProperty.SERV_LANGUAGE)
                {
                    case "EN":
                        if (weapon != null)
                            hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                        break;
                    case "DE":
                        if (weapon != null)
                            hitWeapon = weapon.Name;
                        break;
                    default:
                        if (weapon != null)
                            hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                        break;
                }

                if (hitWeapon.Length > 0)
                    hitWeapon = " " +
                                LanguageMgr.GetTranslation(player.Client.Account.Language,
                                    "GamePlayer.Attack.WithYour") + " " + hitWeapon;

                string attackTypeMsg =
                    LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.YouAttack");

                // intercept messages
                if (target != null && target != ad.Target)
                {
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Intercepted",
                            ad.Target.GetName(0, true), target.GetName(0, false)), EChatType.CT_YouHit,
                        EChatLoc.CL_SystemWindow);
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language,
                            "GamePlayer.Attack.InterceptedHit", attackTypeMsg, target.GetName(0, false), hitWeapon,
                            ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit,
                        EChatLoc.CL_SystemWindow);
                }
                else
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.InterceptHit",
                            attackTypeMsg, ad.Target.GetName(0, false), hitWeapon, ad.Damage, modmessage),
                        EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);

                // critical hit
                if (ad.CriticalDamage > 0)
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Critical",
                            ad.Target.GetName(0, false), ad.CriticalDamage) +
                        $" ({ad.Attacker.attackComponent.AttackCriticalChance(null, ad.Weapon)}%)",
                        EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                break;
        }
    }

    public override void FinishSpellCast(GameLiving target)
    {
        base.FinishSpellCast(target);

        //we need to make sure the spell is only disabled if the attack was a success
        int isDisabled = Caster.TempProperties.GetProperty<int>(DISABLE);

        //if this value is greater than 0 then we know that their weapon did not damage the target
        //the skill's disable timer should be set to their attackspeed 
        if (isDisabled > 0)
        {
            Caster.DisableSkill(Spell, isDisabled);

            //remove the temp property
            Caster.TempProperties.RemoveProperty(DISABLE);
        }
        else
        {
            //they disarm them selves.
            Caster.CastSpell(Disarmed, (SkillBase.GetSpellLine(GlobalSpellsLines.Combat_Styles_Effect)));
        }
    }

    public override void ApplyEffectOnTarget(GameLiving target)
    {
        GamePlayer player = target as GamePlayer;

        foreach (GamePlayer visPlayer in Caster.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
        {
            visPlayer.Out.SendCombatAnimation(Caster, target, 0x0000, 0x0000, (ushort)408, 0, 0x00,
                target.HealthPercent);
        }

        OnDirectEffect(target);

    }

    public override AttackData CalculateDamageToTarget(GameLiving target)
    {
        GamePlayer player = Caster as GamePlayer;

        if (player == null)
            return null;

        DbInventoryItem weapon = null;

        if (player.ActiveWeaponSlot.ToString() == "TwoHanded")
            weapon = player.Inventory.GetItem((EInventorySlot)12);
        if (player.ActiveWeaponSlot.ToString() == "Standard")
            weapon = player.Inventory.GetItem((EInventorySlot)10);

        if (weapon == null)
            return null;

        //create the AttackData
        AttackData ad = new AttackData();
        ad.Attacker = player;
        ad.Target = target;
        ad.Damage = 0;
        ad.CriticalDamage = 0;
        ad.WeaponSpeed = player.AttackSpeed(weapon) / 100;
        ad.DamageType = player.attackComponent.AttackDamageType(weapon);
        ad.Weapon = weapon;
        ad.IsOffHand = weapon.Hand == 2;
        //we need to figure out which armor piece they are going to hit.
        //figure out the attacktype
        switch (weapon.Item_Type)
        {
            default:
            case Slot.RIGHTHAND:
            case Slot.LEFTHAND:
                ad.AttackType = EAttackType.MeleeOneHand;
                break;
            case Slot.TWOHAND:
                ad.AttackType = EAttackType.MeleeTwoHand;
                break;
        }

        //Throw Weapon is subject to all the conventional attack results, parry, evade, block, etc.
        ad.AttackResult = ad.Target.attackComponent.CalculateEnemyAttackResult(null, ad, weapon);

        if (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle)
        {
            //we only need to calculate the damage if the attack was a success.
            double damage = player.attackComponent.AttackDamage(weapon, out _) * Effectiveness;

            if (target is GamePlayer)
                ad.ArmorHitLocation = ((GamePlayer)target).CalculateArmorHitLocation(ad);

            DbInventoryItem armor = null;
            if (target.Inventory != null)
                armor = target.Inventory.GetItem((EInventorySlot)ad.ArmorHitLocation);

            //calculate the lowerboundary of the damage
            int lowerboundary = (player.WeaponSpecLevel(weapon) - 1) * 50 / (ad.Target.EffectiveLevel + 1) + 75;
            lowerboundary = Math.Max(lowerboundary, 75);
            lowerboundary = Math.Min(lowerboundary, 125);

            damage *= (player.GetWeaponSkill(weapon) + 90.68) /
                      (ad.Target.GetArmorAF(ad.ArmorHitLocation) + 20 * 4.67);

            //If they have badge of Valor, we need to modify the damage
            if (ad.Attacker.EffectList.GetOfType<NfRaBadgeOfValorEffect>() != null)
                damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
            else
                damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));

            damage *= (lowerboundary + Util.Random(50)) * 0.01;

            ad.Modifier = (int)(damage *
                                (ad.Target.GetResist(ad.DamageType) +
                                 SkillBase.GetArmorResist(armor, ad.DamageType)) * -0.01);

            damage += ad.Modifier;

            int resist = (int)(damage * ad.Target.GetDamageResist(target.GetResistTypeForDamage(ad.DamageType)) *
                               -0.01);
            EProperty property = ad.Target.GetResistTypeForDamage(ad.DamageType);
            int secondaryResistModifier = ad.Target.SpecBuffBonusCategory[(int)property];
            int resistModifier = 0;
            resistModifier += (int)((ad.Damage + (double)resistModifier) * (double)secondaryResistModifier * -0.01);
            damage += resist;
            damage += resistModifier;
            ad.Modifier += resist;
            ad.Damage = (int)damage;
            ad.Damage = Math.Min(ad.Damage,
                (int)(player.attackComponent.AttackDamage(weapon, out _) * Effectiveness));
            ad.Damage = (int)(ad.Damage * ServerProperty.PVP_MELEE_DAMAGE);
            if (ad.Damage == 0)
                ad.AttackResult = EAttackResult.Missed;
            else
                ad.CriticalDamage = player.attackComponent.CalculateMeleeCriticalDamage(ad, null, weapon);
        }
        else
        {
            //They failed, do they do not get disarmed, and the spell is not disabled for the full duration,
            //just the modified swing speed, this is in milliseconds
            int attackSpeed = player.AttackSpeed(weapon);
            player.TempProperties.SetProperty(DISABLE, attackSpeed);
        }

        return ad;
    }

    public ThrowWeaponSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}