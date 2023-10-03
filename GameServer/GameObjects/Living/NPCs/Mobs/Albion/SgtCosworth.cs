using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class SgtCosworth : GameNPC
	{
		public SgtCosworth() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12122);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			SgtCosworthBrain sbrain = new SgtCosworthBrain();
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
	public class SgtCosworthBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SgtCosworthBrain() : base()
		{
			AggroLevel = 40;
			AggroRange = 400;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				if (Body.HealthPercent <= 66)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
					{
						if (npc != null && npc.IsAlive && npc.PackageID == "SgtCosworthBaf")
							AddAggroListTo(npc.Brain as StandardMobBrain);
					}
				}
			}
			base.Think();
		}
	}
}
