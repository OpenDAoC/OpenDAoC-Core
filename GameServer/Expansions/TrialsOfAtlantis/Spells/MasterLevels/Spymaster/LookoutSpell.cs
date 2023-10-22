using System;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Expansions.TrialsOfAtlantis.Effects;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

[SpellHandler("Loockout")]
public class LookoutSpell : SpellHandler
{
    private GameLiving m_target;

    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        if (!(selectedTarget is GamePlayer)) return false;
        if (!selectedTarget.IsSitting)
        {
            MessageToCaster("Target must be sitting!", EChatType.CT_System);
            return false;
        }

        return base.CheckBeginCast(selectedTarget);
    }

    public override void OnEffectStart(GameSpellEffect effect)
    {
        m_target = effect.Owner as GamePlayer;
        if (m_target == null) return;
        if (!m_target.IsAlive || m_target.ObjectState != GameLiving.eObjectState.Active ||
            !m_target.IsSitting) return;
        Caster.BaseBuffBonusCategory[(int)EProperty.Skill_Stealth] += 100;
        GameEventMgr.AddHandler(m_target, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
        GameEventMgr.AddHandler(Caster, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
        new LookoutOwnerEffect().Start(Caster);
        base.OnEffectStart(effect);
    }

    public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
    {
        Caster.BaseBuffBonusCategory[(int)EProperty.Skill_Stealth] -= 100;
        GameEventMgr.RemoveHandler(Caster, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
        GameEventMgr.RemoveHandler(m_target, GamePlayerEvent.Moving, new CoreEventHandler(PlayerAction));
        return base.OnEffectExpires(effect, noMessages);
    }

    private void PlayerAction(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = (GamePlayer)sender;
        if (player == null) return;
        MessageToLiving((GameLiving)player, "You are moving. Your concentration fades!",
            EChatType.CT_SpellResisted);
        GameSpellEffect effect = SpellHandler.FindEffectOnTarget(m_target, "Loockout");
        if (effect != null) effect.Cancel(false);
        IGameEffect effect2 = SpellHandler.FindStaticEffectOnTarget(Caster, typeof(LookoutOwnerEffect));
        if (effect2 != null) effect2.Cancel(false);
        OnEffectExpires(effect, true);
    }

    public LookoutSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}