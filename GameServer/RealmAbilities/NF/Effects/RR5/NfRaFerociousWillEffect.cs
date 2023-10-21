using System.Collections.Generic;
using Core.GS.Effects;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaFerociousWillEffect : TimedEffect
{

	private int m_currentBonus = 25;

	public NfRaFerociousWillEffect()
		: base(30000)
	{
		
	}

	private GameLiving owner;

	public override void Start(GameLiving target)
    {
        base.Start(target);
        owner = target;
        GamePlayer player = target as GamePlayer;
        if (player != null)
        {
            foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
            }
        }
        owner.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] += m_currentBonus;
    }

	public override string Name { get { return "Ferocious Will"; } }

	public override ushort Icon { get { return 3064; } }

	public override void Stop()
	{
		owner.BaseBuffBonusCategory[(int)EProperty.ArmorAbsorption] -= m_currentBonus;
		base.Stop();
	}

	public int SpellEffectiveness
	{
		get { return 100; }
	}

	public override IList<string> DelveInfo
	{
		get
		{
			var list = new List<string>();
			list.Add("Gives the Berzerker an ABS buff of 25%. Lasts 30 seconds total.");
			return list;
		}
	}
}