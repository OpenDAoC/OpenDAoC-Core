using System;
using Core.Events;
using Core.GS.Behaviour;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.MoveTo)]
public class MoveToAction : AAction<GameLocation,GameLiving>
{

	public MoveToAction(GameNpc defaultNPC,  Object p, Object q)
		: base(defaultNPC, EActionType.MoveTo, p, q)
	{ }

	public MoveToAction(GameNpc defaultNPC, GameLocation location, GameLiving npc)
		: this(defaultNPC, (object)location,(object) npc) { }
	
	public override void Perform(CoreEvent e, object sender, EventArgs args)
	{
		GameLiving npc = Q;

		if (P is GameLocation)
		{
			GameLocation location = (GameLocation)P;
			npc.MoveTo(location.RegionID, location.X, location.Y, location.Z, location.Heading);
		}
		else
		{
			GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
			npc.MoveTo(player.CurrentRegionID, player.X, player.Y, player.Z, (ushort)player.Heading);
		}
	}
}