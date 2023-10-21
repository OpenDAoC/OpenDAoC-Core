using System;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.PacketHandler;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.PlaySound, IsNullableQ = true)]
public class PlaySoundAction : AAction<ushort, ESoundType>
{

	public PlaySoundAction(GameNpc defaultNPC, Object p, Object q)
		: base(defaultNPC, EActionType.PlaySound, p, q)
	{
	}


	public PlaySoundAction(GameNpc defaultNPC, ushort id, ESoundType type)
		: this(defaultNPC, (object)id, (object)type)
	{
	}


	public PlaySoundAction(GameNpc defaultNPC, ushort id)
		: this(defaultNPC, (object)id, (object)ESoundType.Divers)
	{
	}


	public override void Perform(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
		ushort message = P;
		player.Out.SendPlaySound(Q, P);
	}
}