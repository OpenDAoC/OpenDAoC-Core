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
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Sister Blythe Initializing...");
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
						damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
						damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " can't be attacked from this distance!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
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
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
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
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		public static int FallenExecutionerCount = 0;
		private bool Message1 = false;
		private bool Message2 = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
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
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SisterSummonEffect), Util.Random(6000,10000));
						Message2 = true;
					}
				}
			}
			base.Think();
		}
		private int SisterSummonEffect(ECSGameTimer timer)
		{
			if (Body.IsAlive && HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 6040, 0, false, 0x01);
				}
				BroadcastMessage("Sister Blythe says, \"Witness the power of Lord Arawn!\"");
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnMoreExecutioners), 3000);
			}
			return 0;
		}
		private int SpawnMoreExecutioners(ECSGameTimer timer)
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