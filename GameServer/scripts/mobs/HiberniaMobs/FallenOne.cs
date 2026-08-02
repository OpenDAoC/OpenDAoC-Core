using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class FallenOne : HideableNpc
	{
		public FallenOne() : base() { }
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160689);
			LoadTemplate(npcTemplate);

			SetHidden(!CurrentRegion.IsNightTime);
			FallenOneBrain sbrain = new FallenOneBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class FallenOneBrain : StandardMobBrain
	{
		public FallenOneBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 300;
		}
		public override void Think()
		{
			bool hidden = !Body.CurrentRegion.IsNightTime && !Body.InCombat;

			if (((HideableNpc)Body).SetHidden(hidden) && !hidden)
				Message.MessageToZone(Body.CurrentZone, "A chill settles over the ground as the fallen one rises from the earth.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);

			base.Think();
		}
	}
}
