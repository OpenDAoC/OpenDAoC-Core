using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Eques : GameNPC
	{
		public Eques() : base() { }
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12907);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			MaxDistance = 0;
			TetherRange = 0;

			EquesBrain.point1check = false;
			EquesBrain.point2check = false;
			EquesBrain.point3check = false;
			EquesBrain.point4check = false;
			EquesBrain.point5check = false;
			EquesBrain.point6check = false;
			EquesBrain.point7check = false;
			EquesBrain.point8check = false;

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
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public EquesBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public static bool point1check = false;
		public static bool point2check = false;
		public static bool point3check = false;
		public static bool point4check = false;
		public static bool point5check = false;
		public static bool point6check = false;
		public static bool point7check = false;
		public static bool point8check = false;
		public override void Think()
		{
			Point3D point1 = new Point3D(32964, 41982, 15007);
			Point3D point2 = new Point3D(32083, 42010, 14767);
			Point3D point3 = new Point3D(32098, 37126, 14767);
			Point3D point4 = new Point3D(32974, 37123, 15007);
			Point3D point5 = new Point3D(33758, 37121, 15007);
			Point3D point6 = new Point3D(34641, 37118, 14767);
			Point3D point7 = new Point3D(34652, 41983, 14767);
			Point3D point8 = new Point3D(33748, 41983, 15007);
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
						if (AggroTable.Count > 0)
							ClearAggroList();//clear list if it contain any aggroed players
					}
				}
			}
			if (!Body.InCombat && !HasAggro)
            {
				#region Walk points
				if (!Body.IsWithinRadius(point1, 30) && point1check == false)
				{
					Body.WalkTo(point1, 100);
				}
				else
				{
					point1check = true;
					point8check = false;
					if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
					{
						Body.WalkTo(point2, 100);
					}
					else
					{
						point2check = true;
						if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
							point3check == false)
						{
							Body.WalkTo(point3, 100);
						}
						else
						{
							point3check = true;
							if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
								point3check == true && point4check == false)
							{
								Body.WalkTo(point4, 100);
							}
							else
							{
								point4check = true;
								if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == false)
								{
									Body.WalkTo(point5, 100);
								}
								else
								{
									point5check = true;
									if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == true && point6check == false)
									{
										Body.WalkTo(point6, 100);
									}
									else
									{
										point6check = true;
										if (!Body.IsWithinRadius(point7, 30) && point1check == true && point2check == true &&
										point3check == true && point4check == true && point5check == true && point6check == true 
										&& point7check == false)
										{
											Body.WalkTo(point7, 100);
										}
										else
										{
											point7check = true;
											if (!Body.IsWithinRadius(point8, 30) && point1check == true && point2check == true &&
											point3check == true && point4check == true && point5check == true && point6check == true
											&& point7check == true && point8check == false)
											{
												Body.WalkTo(point8, 100);
											}
											else
											{
												point8check = true;
												point1check = false;
												point2check = false;
												point3check = false;
												point4check = false;
												point5check = false;
												point6check = false;
												point7check = false;
											}
										}
									}
								}
							}
						}
					}
				}
				#endregion
			}
            base.Think();
		}
	}
}
