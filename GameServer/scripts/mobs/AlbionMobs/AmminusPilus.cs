using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
	public class AmminusPilus : GameNPC
	{
		public AmminusPilus() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12888);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			SpawnPilusFury();

			AmminusPilusBrain sbrain = new AmminusPilusBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;
			SaveIntoDatabase();
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
			}
			return success;
		}
		#region Show Effects
		protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 5920, 0, false, 0x01);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1000);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1000);
			return 0;
		}
		#endregion
		public override void Die(GameObject killer)
        {
			foreach(GameNPC npc in GetNPCsInRadius(5000))
            {
				if (npc.IsAlive && npc != null && npc.Brain is PilusFuryBrain)
					npc.RemoveFromWorld();
            }
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc != null && npc.Brain is PilusAddBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
        }
		private void SpawnPilusFury()
        {
			foreach (GameNPC fury in GetNPCsInRadius(5000))
			{
				if (fury.Brain is PilusFuryBrain)
					return;
			}
			PilusFury npc = new PilusFury();
			npc.X = 33072;
			npc.Y = 43263;
			npc.Z = 15360;
			npc.Heading = 3003;
			npc.CurrentRegion = CurrentRegion;
			npc.AddToWorld();
		}
    }
}
namespace DOL.AI.Brain
{
	public class AmminusPilusBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AmminusPilusBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		private bool SpawnAdds = false;
		public override void Think()
		{
			if(!HasAggressionTable())
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc.IsAlive && npc != null && npc.Brain is PilusAddBrain)
						npc.RemoveFromWorld();
				}
				SpawnAdds = false;
            }
			if(HasAggro && Body.TargetObject != null)
            {
				if (!SpawnAdds)
				{
					SpawnPilusAdds();
					SpawnAdds = true;
				}
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is PilusAddBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
			}
			base.Think();
		}
		private void SpawnPilusAdds()
		{
			for (int i = 0; i < 4; i++)
			{
				PilusAdd npc = new PilusAdd();
				npc.X = Body.X + Util.Random(-100, 100);
				npc.Y = Body.Y + Util.Random(-100, 100);
				npc.Z = Body.Z;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
#region Pilus adds
namespace DOL.GS
{
	public class PilusAdd : GameNPC
	{
		public PilusAdd() : base() { }
        #region Stats
        public override short Constitution { get => base.Constitution; set => base.Constitution = 100; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 180; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 100; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "centurio princeps preatorii";
			Level = (byte)Util.Random(37, 39);
			Size = (byte)Util.Random(50, 60);
			Model = 108;
			Race = 2009;
			BodyType = 11;

			PilusAddBrain sbrain = new PilusAddBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class PilusAddBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public PilusAddBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 400;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion