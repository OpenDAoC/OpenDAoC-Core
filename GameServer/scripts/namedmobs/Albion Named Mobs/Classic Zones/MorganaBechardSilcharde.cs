using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

#region Morgana
namespace DOL.GS
{
	public class Morgana : GameNPC
	{
		public Morgana() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Morgana Initializing...");
		}
		public static int BechardCount = 0;
		public static int SilchardeCount = 0;
		public static int BechardMinionCount = 10;
		public static int SilchardeMinionCount = 10;
		public static int BechardDemonicMinionsCount = 0;
		public static int SilchardeDemonicMinionsCount = 0;
		public override bool AddToWorld()
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc.Brain is MorganaBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000001);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			MorganaBrain sbrain = new MorganaBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class MorganaBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MorganaBrain() : base()
		{
			AggroLevel = 0;
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
		public void BroadcastMessage2(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;

		private bool Message = false;
		private bool SpawnDemons = false;
		private bool PlayerAreaCheck = false;
		public static bool CanRemoveMorgana = false;
		private bool Morganacast = false;
		public override void Think()
		{
			if (!PlayerAreaCheck)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(1000))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						GS.Quests.Albion.Academy_50 quest = player.IsDoingQuest(typeof(GS.Quests.Albion.Academy_50)) as GS.Quests.Albion.Academy_50;
						if (quest != null && quest.Step == 1)
						{
							SpawnDemons = true;
							player.Out.SendMessage("Ha, is this all the forces of Albion have to offer? I expected a whole army leaded by my brother Arthur, but what do they send a little group of adventurers lead by a poor " + player.CharacterClass.Name + "?",eChatType.CT_Say,eChatLoc.CL_ChatWindow);
							PlayerAreaCheck = true;
						}
					}
				}
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000001);
			if(Morgana.BechardMinionCount > 0 && Morgana.BechardDemonicMinionsCount > 0 && Morgana.SilchardeMinionCount > 0 && Morgana.SilchardeDemonicMinionsCount > 0)
            {
				if(Morgana.BechardDemonicMinionsCount >= Morgana.BechardMinionCount && Morgana.SilchardeDemonicMinionsCount >= Morgana.SilchardeMinionCount)
                {
					if(!Morganacast)
                    {
						BroadcastMessage2("You sense the tower is clear of necromantic ties!");
						if (!Message)
						{
							BroadcastMessage("Morgana shouts, \"I cannot believe my creations have been undone so easily! Heed my words mortal! You may have won this battle but I shall return! On that day all who walk this realm will know what fear truly is!" +
								" The walls of Camelot shall fall and a new order, MY order, shall reign eternal!\"");
							Message = true;
						}
						foreach (GamePlayer player in Body.GetPlayersInRadius(4000))
						{
							if (player != null)
								player.Out.SendSpellCastAnimation(Body, 9103, 3);
						}
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(MorganaCast), 2000);
						Morganacast = true;
                    }
                }
            }
			if (!SpawnDemons || (CanRemoveMorgana && SpawnDemons && Bechard.BechardKilled && Silcharde.SilchardeKilled))
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					//Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					changed = true;
				}
			}
			else
			{
				if (changed)
				{
					Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
					Body.Model = Convert.ToUInt16(npcTemplate.Model);
					SpawnBechard();
					SpawnSilcharde();
					changed = false;
				}
			}
			base.Think();
		}
		private int MorganaCast(ECSGameTimer timer)
        {
			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				if (player != null)
					player.Out.SendSpellEffectAnimation(Body, Body, 9103, 0, false, 1);
			}
			CanRemoveMorgana = true;
			int resetTimer = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1h to reset encounter
			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(RestartMorgana), resetTimer);//reset whole encounter here
			return 0;
        }
		private int RestartMorgana(ECSGameTimer timer)//here we reset whole encounter
		{
			Message = false;
			PlayerAreaCheck = false;
			Morganacast = false;
			SpawnDemons = false;
			CanRemoveMorgana = false;
			Bechard.BechardKilled = false;
			Silcharde.SilchardeKilled = false;
			Morgana.SilchardeCount = 0;
			Silcharde.SilchardeKilled = false;
			Morgana.BechardCount = 0;
			Bechard.BechardKilled = false;
			Morgana.BechardDemonicMinionsCount = 0;
			Morgana.SilchardeDemonicMinionsCount = 0;
			return 0;
		}
		private void SpawnBechard()
		{
			if (Morgana.BechardCount == 0)
			{
				foreach (GameNPC mob in Body.GetNPCsInRadius(5000))
				{
					if (mob.Brain is BechardBrain)
						return;
				}
				Bechard npc = new Bechard();
				npc.X = 306044;
				npc.Y = 670253;
				npc.Z = 3028;
				npc.Heading = 3232;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
		private void SpawnSilcharde()
		{
			if (Morgana.SilchardeCount == 0)
			{
				foreach (GameNPC mob in Body.GetNPCsInRadius(5000))
				{
					if (mob.Brain is SilchardeBrain)
						return;
				}
				Silcharde npc = new Silcharde();
				npc.X = 306132;
				npc.Y = 669983;
				npc.Z = 3040;
				npc.Heading = 3148;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
			}
		}
	}
}
#endregion

#region Bechard
namespace DOL.GS
{
	public class Bechard : GameEpicBoss
	{
		public Bechard() : base() { }
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
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 60;// dmg reduction for rest resists
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
			get { return 15000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000009);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			++Morgana.BechardCount;
			BechardKilled = false;

			BechardBrain sbrain = new BechardBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public static bool BechardKilled = false;
		public override void Die(GameObject killer)
		{
			--Morgana.BechardCount;
			BechardKilled = true;
			SpawnDemonic();
			base.Die(killer);
		}
		private void SpawnDemonic()
		{
			Point3D spawn = new Point3D(306041, 670103, 3310);
			for (int i = 0; i < Morgana.BechardMinionCount; i++)
			{
				DemonicMinion npc = new DemonicMinion();
				npc.X = spawn.X + Util.Random(-150, 150);
				npc.Y = spawn.Y + Util.Random(-150, 150);
				npc.Z = spawn.Z;
				npc.Heading = 3148;
				npc.CurrentRegion = CurrentRegion;
				npc.PackageID = "BechardMinion";
				npc.AddToWorld();
			}
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0 && ad.DamageType == eDamageType.Body)
			{
				Health += ad.Damage;
			}
			base.DealDamage(ad);
		}
	}
}
namespace DOL.AI.Brain
{
	public class BechardBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BechardBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro && Body.TargetObject != null)
            {
				GameLiving target = Body.TargetObject as GameLiving;
				foreach(GameNPC npc in Body.GetNPCsInRadius(2000))
                {
					if(npc != null && npc.IsAlive && npc.Brain is SilchardeBrain brain)
                    {
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
                    }
                }					
            }
			base.Think();
		}
    }
}
#endregion

#region Silcharde
namespace DOL.GS
{
	public class Silcharde : GameEpicBoss
	{
		public Silcharde() : base() { }
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
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 60;// dmg reduction for rest resists
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
			get { return 15000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000008);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			++Morgana.SilchardeCount;
			SilchardeKilled = false;

			SilchardeBrain sbrain = new SilchardeBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}

		public static bool SilchardeKilled = false;
		public override void Die(GameObject killer)
		{
			SilchardeKilled = true;
			--Morgana.SilchardeCount;
			SpawnDemonic();
			base.Die(killer);
		}
		private void SpawnDemonic()
		{
			Point3D spawn = new Point3D(306041, 670103, 3310);
			for (int i = 0; i < Morgana.SilchardeMinionCount; i++)
			{
				DemonicMinion npc = new DemonicMinion();
				npc.X = spawn.X + Util.Random(-150, 150);
				npc.Y = spawn.Y + Util.Random(-150, 150); 
				npc.Z = spawn.Z;
				npc.Heading = 3148;
				npc.CurrentRegion = CurrentRegion;
				npc.PackageID = "SilchardeMinion";
				npc.AddToWorld();
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class SilchardeBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SilchardeBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is BechardBrain brain)
					{
						if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
							brain.AddToAggroList(target, 100);
					}
				}
			}
			base.Think();
		}
	}
}
#endregion

#region demonic minion
namespace DOL.GS
{
	public class DemonicMinion : GameNPC
	{
		public DemonicMinion() : base() { }

		public override bool AddToWorld()
		{
			Name = "demonic minion";
			Level = (byte)Util.Random(30, 33);
			Model = 606;
			RoamingRange = 200;
			MaxSpeedBase = 245;
			Flags = eFlags.FLYING;

			DemonicMinionBrain sbrain = new DemonicMinionBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			if (PackageID == "BechardMinion")
				++Morgana.BechardDemonicMinionsCount;
			if (PackageID == "SilchardeMinion")
				++Morgana.SilchardeDemonicMinionsCount;
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class DemonicMinionBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DemonicMinionBrain() : base()
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