using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class Anilius : GameNPC
	{
		public Anilius() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12254);
			LoadTemplate(npcTemplate);

			AniliusBrain sbrain = new AniliusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			return base.AddToWorld();
		}
		public override void Die(GameObject killer)
		{
			if (Brain is AniliusBrain brain)
			{
				foreach (AniliusAdd add in brain.Adds)
				{
					if (add != null && add.IsAlive)
						add.RemoveFromWorld();
				}
				brain.Adds.Clear();
			}
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class AniliusBrain : StandardMobBrain
	{
		public AniliusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool SpawnAdds = false;
		private bool RemoveAdds = false;
		public List<AniliusAdd> Adds { get; } = new();

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				if (!RemoveAdds)
				{
					foreach (AniliusAdd add in Adds)
					{
						if (add != null && add.IsAlive)
							add.RemoveFromWorld();
					}
					Adds.Clear();
					RemoveAdds = true;
				}
				SpawnAdds = false;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (!SpawnAdds)
				{
					SpawnAniliusAdds();
					SpawnAdds = true;
				}
				Adds.RemoveAll(add => add == null || !add.IsAlive);

				if (PullFriends(npc => npc.Brain is AniliusAddBrain && Adds.Contains(npc as AniliusAdd), 1500) > 0)
					Message.MessageToArea(Body, "Anilius lets out a piercing hiss, and serpents slither from the shadows!", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
			base.Think();
		}
		private void SpawnAniliusAdds()
		{
			for (int i = 0; i < 3; i++)
			{
				AniliusAdd npc = new AniliusAdd();
				npc.X = Body.X + Util.Random(-50, 50);
				npc.Y = Body.Y + Util.Random(-50, 50);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
				Adds.Add(npc);
			}
		}
	}
}
#region Pilus adds
namespace DOL.GS
{
	public class AniliusAdd : GameNPC
	{
		public AniliusAdd() : base() { }
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12292);
			LoadTemplate(npcTemplate);

			AniliusAddBrain sbrain = new AniliusAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			return base.AddToWorld();
		}
	}
}
namespace DOL.AI.Brain
{
	public class AniliusAddBrain : StandardMobBrain
	{
		public AniliusAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
	}
}
#endregion
