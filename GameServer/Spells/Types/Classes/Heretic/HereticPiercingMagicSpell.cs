using System;
using System.Collections;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

[SpellHandler("HereticPiercingMagic")]
public class HereticPiercingMagicSpell : SpellHandler
{
    protected GameLiving focustarget = null;
    protected ArrayList m_focusTargets = null;
    public override void FinishSpellCast(GameLiving target)
    {
        base.FinishSpellCast(target);
        focustarget = target;
    }
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);
        if (m_focusTargets == null)
            m_focusTargets = new ArrayList();
        GameLiving living = effect.Owner as GameLiving;
        lock (m_focusTargets.SyncRoot)
        {
            if (!m_focusTargets.Contains(effect.Owner))
                m_focusTargets.Add(effect.Owner);

            MessageToCaster("You concentrated on the spell!", EChatType.CT_Spell);
        }
    }
    protected virtual void BeginEffect()
    {
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.AttackFinished, new CoreEventHandler(EventAction));
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.CastStarting, new CoreEventHandler(EventAction));
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.Moving, new CoreEventHandler(EventAction));
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.Dying, new CoreEventHandler(EventAction));
        GameEventMgr.AddHandler(m_caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(EventAction));
    }
    public void EventAction(CoreEvent e, object sender, EventArgs args)
    {
        GameLiving player = sender as GameLiving;

        if (player == null) return;
        MessageToCaster("You lose your concentration!", EChatType.CT_SpellExpires);
        RemoveEffect();
    }
    protected virtual void RemoveEffect()
    {
        if (m_focusTargets != null)
        {
            lock (m_focusTargets.SyncRoot)
            {
                foreach (GameLiving living in m_focusTargets)
                {
                    GameSpellEffect effect = FindEffectOnTarget(living, this);
                    if (effect != null)
                        effect.Cancel(false);
                }
            }
        }
        MessageToCaster("You lose your concentration!", EChatType.CT_Spell);
        if (Spell.Pulse != 0 && Spell.Frequency > 0)
            CancelPulsingSpell(Caster, Spell.SpellType);

        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.AttackFinished, new CoreEventHandler(EventAction));
        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.CastStarting, new CoreEventHandler(EventAction));
        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.Moving, new CoreEventHandler(EventAction));
        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.Dying, new CoreEventHandler(EventAction));
        GameEventMgr.RemoveHandler(m_caster, GamePlayerEvent.AttackedByEnemy, new CoreEventHandler(EventAction));
        foreach (GamePlayer player in m_caster.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            player.Out.SendInterruptAnimation(m_caster);
        }
    }

	public HereticPiercingMagicSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}