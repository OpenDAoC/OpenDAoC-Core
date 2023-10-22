using System;
using Core.Database.Tables;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("ValkyrieOffensiveProc")]
public class ValkyrieOffensiveProcSpell : SpellHandler
{
    /// <summary>
    /// Constants data change this to modify chance increase or decrease
    /// </summary>
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);
        // "Your weapon is blessed by the gods!"
        // "{0}'s weapon glows with the power of the gods!"
        EChatType chatType = EChatType.CT_SpellPulse;
        if (Spell.Pulse == 0)
        {
            chatType = EChatType.CT_Spell;
        }
        MessageToLiving(effect.Owner, Spell.Message1, chatType);
        MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, true)), chatType, effect.Owner);
        GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        if (!noMessages)
        {
            MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
            MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, true)), EChatType.CT_SpellExpires, effect.Owner);
        }
        GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackFinished, new CoreEventHandler(EventHandler));
        return 0;
    }

    public void EventHandler(CoreEvent e, object sender, EventArgs arguments)
    {
        AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
        if (args == null || args.AttackData == null)
        {
            return;
        }
        AttackData ad = args.AttackData;
        if (ad.AttackResult != EAttackResult.HitUnstyled && ad.AttackResult != EAttackResult.HitStyle)
            return;

        int baseChance = 0;
        if (ad.AttackType == EAttackType.Ranged)
        {
            baseChance = (int)(Spell.Frequency * .0001);
        }
        else if (ad.IsMeleeAttack)
        {
            baseChance = ((int)Spell.Frequency);
            if (sender is GamePlayer)
            {
                GamePlayer player = (GamePlayer)sender;
                DbInventoryItem leftWeapon = player.Inventory.GetItem(EInventorySlot.LeftHandWeapon);
                // if we can use left weapon, we have currently a weapon in left hand and we still have endurance,
                // we can assume that we are using the two weapons.
                if (player.attackComponent.CanUseLefthandedWeapon && leftWeapon != null && leftWeapon.Object_Type != (int)EObjectType.Shield)
                {
                    baseChance /= 2;
                }
            }
        }

        if (Util.Chance(15))
        {
            Spell m_procSpell = SkillBase.GetSpellByID((int)Spell.Value);
            ISpellHandler handler = ScriptMgr.CreateSpellHandler((GameLiving)sender, m_procSpell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
            if (handler != null)
            {
                if (m_procSpell.Target == ESpellTarget.ENEMY)
                    handler.StartSpell(ad.Target);
                else if (m_procSpell.Target == ESpellTarget.SELF)
                    handler.StartSpell(ad.Attacker);
            }
        }

    }

    // constructor
    public ValkyrieOffensiveProcSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}