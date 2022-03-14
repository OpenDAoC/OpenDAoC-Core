using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;


namespace DOL.GS
{
    public class NightSpawn : GameNPC
    {
	    
	    public override bool AddToWorld()
		{
			NightSpawnBrain sBrain = new NightSpawnBrain();
			SetOwnBrain(sBrain);
			base.AddToWorld();
			return true;
		}        
	    
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Night mobs initialising...");
		}
    }
    
}

namespace DOL.AI.Brain
{
	public class NightSpawnBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static ushort oldModel = 0;
		public static GameNPC.eFlags oldFlags = 0;

		public override void Think()
		{

			if (!Body.CurrentRegion.IsNightTime)
			{
				if (oldModel == 0)
				{
					oldModel = Body.Model;
				}
				
				if (Body.Flags != GameNPC.eFlags.CANTTARGET)
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
				if (Body.Flags != GameNPC.eFlags.DONTSHOWNAME)
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
				
				Body.Model = 1;
			}
			else
			{
				if (oldModel != 0)
				{
					Body.Model = oldModel;
					oldModel = 0;
				}
				Body.Flags = oldFlags;
			}

			base.Think();
		}
		
		
	}
}