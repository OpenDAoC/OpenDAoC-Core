using System;
using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.World;

namespace Core.GS.RealmAbilities;

public class NfRaBloodDrinkingEffect : TimedEffect
{
    private GamePlayer EffectOwner;

    public NfRaBloodDrinkingEffect()
        : base(RealmAbilities.NfRaBlooddrinkingAbility.DURATION)
    { }

    public override void Start(GameLiving target)
    {
        base.Start(target);
        if (target is GamePlayer)
        {
            EffectOwner = target as GamePlayer;
            foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, NfRaBlooddrinkingAbility.EFFECT, 0, false, 1);
            }
            GameEventMgr.AddHandler(EffectOwner, GameLivingEvent.AttackFinished, new CoreEventHandler(OnAttack));
            GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Dying, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.RegionChanged, new CoreEventHandler(PlayerLeftWorld));
        }
    }
    public override void Stop()
    {
        if (EffectOwner != null)
        {
            GameEventMgr.RemoveHandler(EffectOwner, GameLivingEvent.AttackFinished, new CoreEventHandler(OnAttack));
            GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Dying, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Linkdeath, new CoreEventHandler(PlayerLeftWorld));
            GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.RegionChanged, new CoreEventHandler(PlayerLeftWorld));
        }
        base.Stop();
    }

    /// <summary>
    /// Called when a player leaves the game
    /// </summary>
    /// <param name="e">The event which was raised</param>
    /// <param name="sender">Sender of the event</param>
    /// <param name="args">EventArgs associated with the event</param>
    protected void PlayerLeftWorld(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        NfRaBloodDrinkingEffect BloodDrinking = (NfRaBloodDrinkingEffect)player.EffectList.GetOfType<NfRaBloodDrinkingEffect>();
        if (BloodDrinking != null)
            BloodDrinking.Cancel(false);
    }

    /// <summary>
    /// Called when a player leaves the game
    /// </summary>
    /// <param name="e">The event which was raised</param>
    /// <param name="sender">Sender of the event</param>
    /// <param name="args">EventArgs associated with the event</param>
    protected void OnAttack(CoreEvent e, object sender, EventArgs arguments)
    {
        AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
        if (args == null || args.AttackData == null)
        {
            return;
        }
        if (args.AttackData.SpellHandler != null) return;
        if (args.AttackData.AttackResult != EAttackResult.HitUnstyled
            && args.AttackData.AttackResult != EAttackResult.HitStyle)
            return;

        AttackData ad = args.AttackData;
        GameLiving living = sender as GameLiving;

        if (living == null) return;
        if (!MatchingDamageType(ref ad)) return;

        double healPercent = NfRaBlooddrinkingAbility.HEALPERCENT;
        int healAbsorbed = (int)(0.01 * healPercent * (ad.Damage + ad.CriticalDamage));
        if (healAbsorbed > 0)
        {
            if (living.Health < living.MaxHealth)
            {
                //TODO correct messages
                MessageToLiving(living, string.Format("Blooddrinking ability is healing you for {0} health points!", healAbsorbed), EChatType.CT_Spell);
                foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    //heal effect
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, 3011, 0, false, 1);
                }
                living.Health = living.Health + healAbsorbed;
            }
            else
                MessageToLiving(living, string.Format("You are already fully healed!"), EChatType.CT_Spell);
        }
    }


    // Check if Melee
    protected virtual bool MatchingDamageType(ref AttackData ad)
    {

        if (ad == null || (ad.AttackResult != EAttackResult.HitStyle && ad.AttackResult != EAttackResult.HitUnstyled))
            return false;
        if (!ad.IsMeleeAttack && ad.AttackType != EAttackType.Ranged)
            return false;

        return true;
    }
    /// <summary>
    /// sends a message to a living
    /// </summary>
    /// <param name="living"></param>
    /// <param name="message"></param>
    /// <param name="type"></param>
    public void MessageToLiving(GameLiving living, string message, EChatType type)
    {
        if (living is GamePlayer && message != null && message.Length > 0)
        {
            living.MessageToSelf(message, type);
        }
    }
    public override string Name { get { return "Blooddrinking"; } }
    public override ushort Icon { get { return 1843; } }

    // Delve Info
    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>();
            list.Add("Cause the Shadowblade to be healed for 20% of all damage he does for 30 seconds");
            return list;
        }
    }
}