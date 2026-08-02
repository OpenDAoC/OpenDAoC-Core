using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class VetustaAbbeyBell : GameNPC
	{
		public VetustaAbbeyBell() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Vetusta Abbey Bell";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = eFlags.DONTSHOWNAME | eFlags.CANTTARGET | eFlags.PEACE | eFlags.FLYING;

			VetustaAbbeyBellBrain sbrain = new VetustaAbbeyBellBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class VetustaAbbeyBellBrain : APlayerVicinityBrain
	{
		private uint _lastHour = uint.MaxValue;

		public VetustaAbbeyBellBrain()
			: base()
		{
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;

			if (hour == _lastHour)
				return;

			bool firstObservation = _lastHour is uint.MaxValue;
			_lastHour = hour;

			if (firstObservation)
				return;

			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				player.Out.SendSoundEffect(12, 0, 0, 0, 0, 0);
		}
	}
}
