using System;
using System.Reflection;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using log4net;

namespace DOL.GS.Behaviour.Actions
{
	[Action(ActionType = EActionType.TrainerWindow, IsNullableP = true)]
	public class TrainerWindowAction : AAction<int?, GameNPC>
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public TrainerWindowAction(GameNPC defaultNPC)
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
}