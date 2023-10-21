using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Behaviors;

[Action(ActionType = EActionType.TrainerWindow, IsNullableP = true)]
public class TrainerWindowAction : AAction<int?, GameNpc>
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public TrainerWindowAction(GameNpc defaultNPC)
		: base(defaultNPC, EActionType.TrainerWindow)
	{
	}
	public override void Perform(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);
		if (player != null)
			player.Out.SendTrainerWindow();
	}
}