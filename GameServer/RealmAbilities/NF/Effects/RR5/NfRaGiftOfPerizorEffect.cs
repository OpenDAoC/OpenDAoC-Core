using System.Collections.Generic;
using Core.GS.Effects;

namespace Core.GS.RealmAbilities;

public class NfRaGiftOfPerizorEffect : TimedEffect
{
    private GamePlayer EffectOwner;

    public NfRaGiftOfPerizorEffect()
        : base(RealmAbilities.NfRaGiftOfPerizorAbility.DURATION)
    { }

    public override void Start(GameLiving target)
    {
        base.Start(target);
        foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            p.Out.SendSpellEffectAnimation(target, target, 7036, 0, false, 1);
        }
        EffectOwner = target as GamePlayer;
    }
    public override void Stop()
    {
        if (EffectOwner != null) EffectOwner.TempProperties.RemoveProperty("GiftOfPerizorOwner");
        base.Stop();
    }

    public override string Name { get { return "Gift Of Perizor"; } }
    public override ushort Icon { get { return 3090; } }

    // Delve Info
    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>();
            list.Add("Buff group with 25% damage reduction for 60 seconds, return damage reduced as power.");
            return list;
        }
    }
}