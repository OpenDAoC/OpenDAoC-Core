using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Erisus : GameNPC
	{
		public Erisus() : base() { }
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12268);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			ErisusBrain sbrain = new ErisusBrain();
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
	public class ErisusBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ErisusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
			ThinkInterval = 1000;
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
				{
					if (npc != null && npc.IsAlive && npc.PackageID == "ErisusBaf")
						AddAggroListTo(npc.Brain as StandardMobBrain);
				}
			}
			base.Think();
		}
	}
}



