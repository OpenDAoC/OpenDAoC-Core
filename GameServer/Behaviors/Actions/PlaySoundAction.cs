using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using DOL.GS.PacketHandler;

namespace DOL.GS.Behaviour.Actions
{
	[Action(ActionType = EActionType.PlaySound, IsNullableQ = true)]
	public class PlaySoundAction : AAction<ushort, ESoundType>
	{

		public PlaySoundAction(GameNPC defaultNPC, Object p, Object q)
			: base(defaultNPC, EActionType.PlaySound, p, q)
		{
		}


		public PlaySoundAction(GameNPC defaultNPC, ushort id, ESoundType type)
			: this(defaultNPC, (object)id, (object)type)
		{
		}


		public PlaySoundAction(GameNPC defaultNPC, ushort id)
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
}