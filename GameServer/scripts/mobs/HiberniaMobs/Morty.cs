using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class Morty : GameNPC
	{
		public Morty() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50013);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			//RespawnInterval = Util.Random(3600000, 7200000);

			MortyBrain sbrain = new MortyBrain();
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
	public class MortyBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MortyBrain() : base()
		{
			AggroLevel = 0;
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
			//uint minute = WorldMgr.GetCurrentGameTime() / 1000 / 60 % 60;
			//log.Warn("Current time: " + hour + ":" + minute);
			if (hour >= 8 && hour < 12)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					changed = false;
				}
			}
			else
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			base.Think();
		}
	}
}

