using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SisterBlythe : GameEpicNPC
	{
		public SisterBlythe() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sister Blythe Initializing...");
		}
		public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
						damageType == EDamageType.Energy || damageType == EDamageType.Heat
						|| damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
						damageType == EDamageType.Crush || damageType == EDamageType.Thrust
						|| damageType == EDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GameSummonedPet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " can't be attacked from this distance!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(DbInventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 10000; }
		}
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is SisterBlytheBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12982);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			SpawnExecutioners();

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			SisterBlytheBrain sbrain = new SisterBlytheBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(4500))
			{
				if (npc != null && npc.IsAlive && npc.Brain is FallenExecutionerBrain)
					npc.Die(this);
			}
			base.Die(killer);
        }
        private void SpawnExecutioners()
		{
			Point3D spawn = new Point3D(322192, 671493, 2764);
			for (int i = 0; i < 4; i++)
			{
				FallenExecutioner npc = new FallenExecutioner();
				npc.X = spawn.X + Util.Random(-150, 150);
				npc.Y = spawn.Y + Util.Random(-150, 150);
				npc.Z = spawn.Z;
				npc.Heading = Heading;
				npc.CurrentRegion = CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class SisterBlytheBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SisterBlytheBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
			}
		}
		public static int FallenExecutionerCount = 0;
		private bool Message1 = false;
		private bool Message2 = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				Message1 = false;
				Message2 = false;
			}
			if(HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				if (!Message1)
                {
					switch(Util.Random(1,2))
                    {
						case 1: BroadcastMessage("Sister Blythe shouts in a language you cannot understand!"); break;
						case 2: BroadcastMessage(String.Format("{0} says, \"Come my pets! Let us show these fools what comes of failure!\"", Body.Name)); break;
					}
					if(FallenExecutionerCount > 0)
						BroadcastMessage("The fallen executioner says, \"By your command!\"");
					Message1 = true;
                }
				foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
				{
					if (npc != null && npc.IsAlive && npc.Brain is FallenExecutionerBrain brain)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 10);
					}
				}
				if(FallenExecutionerCount < 4)
                {
					if (!Message2)
					{
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SisterSummonEffect), Util.Random(6000,10000));
						Message2 = true;
					}
				}
			}
			base.Think();
		}
		private int SisterSummonEffect(EcsGameTimer timer)
		{
			if (Body.IsAlive && HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 6040, 0, false, 0x01);
				}
				BroadcastMessage("Sister Blythe says, \"Witness the power of Lord Arawn!\"");
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMoreExecutioners), 3000);
			}
			return 0;
		}
		private int SpawnMoreExecutioners(EcsGameTimer timer)
        {
			if (Body.IsAlive && HasAggro)
			{
				SpawnExecutioners();
				Message2 = false;
			}
			return 0;
        }
		private void SpawnExecutioners()
		{
			Point3D spawn = new Point3D(322192, 671493, 2764);
			for (int i = 0; i < 4; i++)
			{
				if (FallenExecutionerCount < 4)
				{
					FallenExecutioner npc = new FallenExecutioner();
					npc.X = spawn.X + Util.Random(-150, 150);
					npc.Y = spawn.Y + Util.Random(-150, 150);
					npc.Z = spawn.Z;
					npc.Heading = Body.Heading;
					npc.CurrentRegion = Body.CurrentRegion;
					npc.AddToWorld();
				}
			}
		}
	}
}
#region fallen executioners
namespace DOL.GS
{
	public class FallenExecutioner : GameNPC
	{
		public FallenExecutioner() : base() { }

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160685);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			++SisterBlytheBrain.FallenExecutionerCount;

			FallenExecutionerBrain sbrain = new FallenExecutionerBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		
        public override void Die(GameObject killer)
        {
			--SisterBlytheBrain.FallenExecutionerCount;
            base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class FallenExecutionerBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public FallenExecutionerBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion