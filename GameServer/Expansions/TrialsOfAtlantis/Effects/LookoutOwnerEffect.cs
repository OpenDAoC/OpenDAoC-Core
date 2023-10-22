//to show an Icon & informations to the caster

using System.Collections.Generic;
using Core.GS.Effects;

namespace Core.GS.Expansions.TrialsOfAtlantis.Effects;

public class LookoutOwnerEffect : StaticEffect, IGameEffect
{
    public LookoutOwnerEffect() : base() { }
    public void Start(GamePlayer player) { base.Start(player); }
    public override void Stop() { base.Stop(); }
    public override ushort Icon { get { return 2616; } }
    public override string Name { get { return "Loockout"; } }
    public override IList<string> DelveInfo
    {
        get
        {
            var delveInfoList = new List<string>();
            delveInfoList.Add("Your stealth range is increased.");
            return delveInfoList;
        }
    }
}