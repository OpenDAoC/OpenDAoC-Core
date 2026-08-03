using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Movement;

namespace DOL.GS
{
	public class Eques : GameNPC
	{
		public Eques() : base() { }

		private const short PATROL_SPEED = 100;

		private static readonly (int X, int Y, int Z)[] _patrolPoints =
		[
			(32964, 41982, 15007),
			(32083, 42010, 14767),
			(32098, 37126, 14767),
			(32974, 37123, 15007),
			(33758, 37121, 15007),
			(34641, 37118, 14767),
			(34652, 41983, 14767),
			(33748, 41983, 15007)
		];

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12907);
			LoadTemplate(npcTemplate);
			TetherRange = 0;
			CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

			EquesBrain sbrain = new EquesBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class EquesBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public EquesBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (Body.IsMoving)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
				{
					if (player != null)
					{
						if (player.IsAlive && player.Client.Account.PrivLevel == 1)
							AddToAggroList(player, 10);//aggro players if roaming
					}
					if (player == null || !player.IsAlive || player.Client.Account.PrivLevel != 1)
					{
						if (HasAggro)
							ClearAggroList();//clear list if it contain any aggroed players
					}
				}
			}
			base.Think();
		}
	}
}
