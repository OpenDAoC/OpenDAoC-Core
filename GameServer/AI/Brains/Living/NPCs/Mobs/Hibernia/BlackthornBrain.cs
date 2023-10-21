using System.Collections.Generic;
using Core.GS;

namespace Core.AI.Brain;

public class BlackthornBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BlackthornBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 400;
		ThinkInterval = 500;
	}
	private protected List<GameNpc> npcs_to_call = new List<GameNpc>();
	private protected List<GameNpc> BafNpcs = new List<GameNpc>();
	private protected static GameNpc randomnpc = null;
	private protected static GameNpc RandomNpc
	{
		get { return randomnpc; }
		set { randomnpc = value; }
	}
	private protected static GameNpc npcbaf = null;
	private protected static GameNpc NpcBaf
	{
		get { return npcbaf; }
		set { npcbaf = value; }
	}
	private protected bool PickedNpc = false;
	private protected void PickRandomMob()
    {
		foreach(GameNpc npc in Body.GetNPCsInRadius(1000))
        {
			if (npc != null && npc.IsAlive && npc.Name.ToLower() == "lunantishee" && !npcs_to_call.Contains(npc) && !npc.IsControlledNPC(npc))
				npcs_to_call.Add(npc);
		}
		if(npcs_to_call.Count > 0)
        {
			if(!PickedNpc)
            {
				GameNpc mob = npcs_to_call[Util.Random(0, npcs_to_call.Count - 1)];//picking randomly mob from list
				RandomNpc = mob;
				PickedNpc = true;
            }

			foreach (GameNpc mob in Body.GetNPCsInRadius(1000))
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
		if(!CheckProximityAggro())//clear all checks and list
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
			foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
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