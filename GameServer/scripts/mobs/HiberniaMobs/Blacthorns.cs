using DOL.AI.Brain;
using DOL.GS;
using System.Collections.Generic;

namespace DOL.GS
{
	public class Blackthorn : GameNPC
	{
		public Blackthorn() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158473);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			BlackthornBrain sbrain = new BlackthornBrain();
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
	public class BlackthornBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BlackthornBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 500;
		}
		private protected List<GameNPC> npcs_to_call = new List<GameNPC>();
		private protected List<GameNPC> BafNpcs = new List<GameNPC>();
		private protected static GameNPC randomnpc = null;
		private protected static GameNPC RandomNpc
		{
			get { return randomnpc; }
			set { randomnpc = value; }
		}
		private protected static GameNPC npcbaf = null;
		private protected static GameNPC NpcBaf
		{
			get { return npcbaf; }
			set { npcbaf = value; }
		}
		private protected bool PickedNpc = false;
		private protected void PickRandomMob()
        {
			foreach(GameNPC npc in Body.GetNPCsInRadius(1000))
            {
				if (npc != null && npc.IsAlive && npc.Name.ToLower() == "lunantishee" && !npcs_to_call.Contains(npc) && !npc.IsControlledNPC(npc))
					npcs_to_call.Add(npc);
			}
			if(npcs_to_call.Count > 0)
            {
				if(!PickedNpc)
                {
					GameNPC mob = npcs_to_call[Util.Random(0, npcs_to_call.Count - 1)];//picking randomly mob from list
					RandomNpc = mob;
					PickedNpc = true;
                }

				foreach (GameNPC mob in Body.GetNPCsInRadius(1000))
				{
					if (RandomNpc != null && RandomNpc.IsAlive)
					{
						if (mob == RandomNpc)
							AddAggroListTo(mob.Brain as StandardMobBrain);
					}
				}
            }
        }
		private bool CanAddNpcs = false;
		private bool CanPullAditional = false;
		public override void Think()
		{
			if(!HasAggressionTable())//clear all checks and list
            {
				npcbaf = null;
				RandomNpc = null;
				PickedNpc = false;
				CanPullAditional = false;
				if (npcs_to_call.Count > 0)
					npcs_to_call.Clear();
			}
			if(Body.IsAlive && !HasAggro && !CanAddNpcs)
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
				{
					if (npc != null && npc.IsAlive)//found baf mobs
					{
						if (npc.PackageID == Body.PackageID && npc != Body && npc.Name.ToLower() == "lunantishee" && !BafNpcs.Contains(npc) && !npc.IsControlledNPC(npc))
							BafNpcs.Add(npc);
					}
				}
				CanAddNpcs = true;
			}
			if(HasAggro)
            {
				// CanAddNpcs = false;
				// if (BafNpcs.Count > 0)
				// {
				// 	foreach (GameNPC mobs in BafNpcs)
				// 	{
				// 		if (mobs != null && mobs.IsAlive)
				// 			if (mobs.Brain is StandardMobBrain)
				// 				AddAggroListTo(mobs.Brain as StandardMobBrain);
				// 		if (!mobs.IsAlive && !CanPullAditional)//check if any of baf mobs are killed
				// 		{
				// 			PickRandomMob();
				// 			CanPullAditional = true;
				// 		}
				// 	}
				// }				
			}
			base.Think();
		}
	}
}
