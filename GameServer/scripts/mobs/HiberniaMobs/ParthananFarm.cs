using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using System;

#region Amalgamate Parthanan
namespace DOL.GS
{
	public class AmalgamateParthanan : GameNPC
	{
		public AmalgamateParthanan() : base() { }
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				GameLiving target = source as GameLiving;			
				if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit || target.AttackWeapon.Object_Type == (int)eObjectType.RecurvedBow || target.AttackWeapon.Object_Type == (int)eObjectType.Fired)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to this form of attack!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
			if (source is GameNPC)
			{
				GameNPC npc = source as GameNPC;
				if (npc.AttackWeapon != null && npc.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
				{
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
					damageType == eDamageType.Energy || damageType == eDamageType.Heat
					|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit)
				{
					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157792);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			AmalgamateParthananBrain sbrain = new AmalgamateParthananBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}
		public override void Die(GameObject killer)
        {
			if (PackageID == "ParthananBossLoughDerg")
			{
				ParthananFarmController1Brain.LoughDergBoss = 0;
				ParthananFarmController1Brain.MobsToKillLoughDerg = Util.Random(60, 80);
			}
			if (PackageID == "ParthananBossConnacht")
			{
				ParthananFarmController2Brain.ConnachtBoss = 0;
				ParthananFarmController2Brain.MobsToKillConnacht= Util.Random(60, 80);
			}
			if (PackageID == "ParthananBossConnacht2")
			{
				ParthananFarmController2bBrain.Connacht2Boss = 0;
				ParthananFarmController2bBrain.MobsToKillConnacht2 = Util.Random(60, 80);
			}
			if (PackageID == "ParthananBossLoughGur")
			{
				ParthananFarmController3Brain.LoughGurBoss = 0;
				ParthananFarmController3Brain.MobsToKillLoughGur = Util.Random(60, 80);
			}
			if (PackageID == "ParthananBossLoughGur2")
			{
				ParthananFarmController3bBrain.LoughGur2Boss = 0;
				ParthananFarmController3bBrain.MobsToKillLoughGur2 = Util.Random(60, 80);
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class AmalgamateParthananBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public AmalgamateParthananBrain() : base()
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

#region Parthanans
namespace DOL.GS
{
	public class Parthanan : GameNPC
	{
		public Parthanan() : base() { }

		public override bool AddToWorld()
		{

			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			ParthananBrain sbrain = new ParthananBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			if (!ParthananFarmController1Brain.SacrificeParthanan1)
			{
				if (PackageID == "ParthananLoughDerg")
					++ParthananFarmController1Brain.ParthanansKilledFarm1;
			}
			if (!ParthananFarmController2Brain.SacrificeParthanan2)
			{
				if (PackageID == "ParthananConnacht")
					++ParthananFarmController2Brain.ParthanansKilledFarm2;
			}
			if (!ParthananFarmController2bBrain.SacrificeParthanan2b)
			{
				if (PackageID == "ParthananConnacht2")
					++ParthananFarmController2bBrain.ParthanansKilledFarm2b;
			}
			if (!ParthananFarmController3Brain.SacrificeParthanan3)
			{
				if (PackageID == "ParthananLoughGur")
					++ParthananFarmController3Brain.ParthanansKilledFarm3;
			}
			if (!ParthananFarmController3bBrain.SacrificeParthanan3b)
			{
				if (PackageID == "ParthananLoughGur2")
					++ParthananFarmController3bBrain.ParthanansKilledFarm3b;
			}
			base.Die(killer);
		}
    }
}
namespace DOL.AI.Brain
{
	public class ParthananBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public ParthananBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
        public override void AttackMostWanted()
        {
			if (!Body.IsControlledNPC(Body))
			{
				if (Body.PackageID == "ParthananLoughDerg")
				{
					if (ParthananFarmController1Brain.SacrificeParthanan1)
						return;
					else
						base.AttackMostWanted();
				}
				if (Body.PackageID == "ParthananConnacht")
				{
					if (ParthananFarmController2Brain.SacrificeParthanan2)
						return;
					else
						base.AttackMostWanted();
				}
				if (Body.PackageID == "ParthananConnacht2")
				{
					if (ParthananFarmController2bBrain.SacrificeParthanan2b)
						return;
					else
						base.AttackMostWanted();
				}
				if (Body.PackageID == "ParthananLoughGur")
				{
					if (ParthananFarmController3Brain.SacrificeParthanan3)
						return;
					else
						base.AttackMostWanted();
				}
				if (Body.PackageID == "ParthananLoughGur2")
				{
					if (ParthananFarmController3bBrain.SacrificeParthanan3b)
						return;
					else
						base.AttackMostWanted();
				}
			}
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			if (Body.IsAlive && !Body.IsControlledNPC(Body))
			{
				#region Lough Derg Parthnanans
				if (Body.PackageID == "ParthananLoughDerg")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController1Brain.LoughDergBoss == 1)
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
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController1Brain.SacrificeParthanan1)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController1Brain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
				}
				#endregion
				#region Connacht Parthnanans
				if (Body.PackageID == "ParthananConnacht")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController2Brain.ConnachtBoss == 1)
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
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController2Brain.SacrificeParthanan2)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2Brain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
				}
				///
				///////////////////////////////// 2nd farm
				///
				if (Body.PackageID == "ParthananConnacht2")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController2bBrain.Connacht2Boss == 1)
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
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}

					if (ParthananFarmController2bBrain.SacrificeParthanan2b)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2bBrain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
				}
				#endregion
				#region Lough Gur Parthanans
				if (Body.PackageID == "ParthananLoughGur")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController3Brain.LoughGurBoss == 1)
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
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}
					if (ParthananFarmController3Brain.SacrificeParthanan3)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3Brain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
				}
				///
				/////////////////////////////////////////////// 2nd farm
				///
				if (Body.PackageID == "ParthananLoughGur2")
				{
					INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164845);
					if (ParthananFarmController3bBrain.LoughGur2Boss == 1)
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
					else
					{
						if (changed)
						{
							Body.Flags = (GameNPC.eFlags)npcTemplate.Flags;
							Body.Model = Convert.ToUInt16(npcTemplate.Model);
							changed = false;
						}
					}
					if (ParthananFarmController3bBrain.SacrificeParthanan3b)
					{
						ClearAggroList();
						Body.StopAttack();
						foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3bBrain)
							{
								if (!Body.IsWithinRadius(npc, 50))
									Body.WalkTo(npc.X, npc.Y, npc.Z, Body.MaxSpeedBase);
								else
									Body.Die(npc);
							}
						}
					}
				}
				#endregion
			}
            base.Think();
		}
	}
}
#endregion

#region Parthanan Farm Controllers
#region Lough Derg
namespace DOL.GS
{
	public class ParthananFarmController1 : GameNPC
	{
		public ParthananFarmController1() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController1Brain sbrain = new ParthananFarmController1Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController1Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController1Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static int ParthanansKilledFarm1 = 0; // Lough Derg
		public static bool SacrificeParthanan1 = false;
		public static int LoughDergBoss = 0;

		public static int MobsToKillLoughDerg = 60;
		public static int m_mobstokillloughderg
		{
			get { return m_mobstokillloughderg; }
			set { m_mobstokillloughderg = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm1 >= MobsToKillLoughDerg)
			{
				SacrificeParthanan1 = true;
				SpawnBigOne();
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughDerg")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughDerg";
			boss.AddToWorld();
			ParthanansKilledFarm1 = 0;
			LoughDergBoss = 1;
			SacrificeParthanan1 = false;
		}
	}
}
#endregion
#region Connacht
namespace DOL.GS
{
	public class ParthananFarmController2 : GameNPC
	{
		public ParthananFarmController2() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController2Brain sbrain = new ParthananFarmController2Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController2Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController2Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static int ParthanansKilledFarm2 = 0; // Connacht
		public static bool SacrificeParthanan2 = false;
		public static int ConnachtBoss = 0;
		public static int MobsToKillConnacht = 60;
		public static int m_mobstokillconnacht
		{
			get { return m_mobstokillconnacht; }
			set { m_mobstokillconnacht = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm2 >= MobsToKillConnacht)
			{
				SacrificeParthanan2 = true;
				SpawnBigOne();
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossConnacht")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossConnacht";
			boss.AddToWorld();
			ParthanansKilledFarm2 = 0;
			ConnachtBoss = 1;
			SacrificeParthanan2 = false;
		}
	}
}
/// <summary>
/// /////////////////////////////////////////////////////2nd farm
/// </summary>
namespace DOL.GS
{
	public class ParthananFarmController2b : GameNPC
	{
		public ParthananFarmController2b() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController2bBrain sbrain = new ParthananFarmController2bBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController2bBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController2bBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}
		public static int ParthanansKilledFarm2b = 0; // Connacht
		public static bool SacrificeParthanan2b = false;
		public static int Connacht2Boss = 0;
		public static int MobsToKillConnacht2 = 60;
		public static int m_mobstokillconnacht2
		{
			get { return m_mobstokillconnacht2; }
			set { m_mobstokillconnacht2 = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm2b >= MobsToKillConnacht2)
			{
				SacrificeParthanan2b = true;
				SpawnBigOne();
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossConnacht2")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossConnacht2";
			boss.AddToWorld();
			ParthanansKilledFarm2b = 0;
			Connacht2Boss = 1;
			SacrificeParthanan2b = false;
		}
	}
}
#endregion
#region Lough Gur
namespace DOL.GS
{
	public class ParthananFarmController3 : GameNPC
	{
		public ParthananFarmController3() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController3Brain sbrain = new ParthananFarmController3Brain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController3Brain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController3Brain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}

		public static int ParthanansKilledFarm3 = 0; // Lough Gur
		public static bool SacrificeParthanan3 = false;
		public static int LoughGurBoss = 0;
		public static int MobsToKillLoughGur = 60;
		public static int m_mobstokillloughgur
		{
			get { return m_mobstokillloughgur; }
			set { m_mobstokillloughgur = value; }
		}
		public override void Think()
		{
			if(ParthanansKilledFarm3 >= MobsToKillLoughGur)
            {
				SacrificeParthanan3 = true;
				SpawnBigOne();
            }
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughGur")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughGur";
			boss.AddToWorld();
			LoughGurBoss = 1;
			ParthanansKilledFarm3 = 0;
			SacrificeParthanan3 = false;
		}
	}
}
///////////////////////////////////////////////////////// 2nd farm

namespace DOL.GS
{
	public class ParthananFarmController3b : GameNPC
	{
		public ParthananFarmController3b() : base()
		{
		}
		public override bool IsVisibleToPlayers => true;
		public override bool AddToWorld()
		{
			Name = "Parthanan Farm Controller";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			ParthananFarmController3bBrain sbrain = new ParthananFarmController3bBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}

namespace DOL.AI.Brain
{
	public class ParthananFarmController3bBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log =
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ParthananFarmController3bBrain()
			: base()
		{
			AggroLevel = 0; //neutral
			AggroRange = 0;
			ThinkInterval = 1000;
		}

		public static int ParthanansKilledFarm3b = 0; // Lough Gur
		public static bool SacrificeParthanan3b = false;
		public static int LoughGur2Boss = 0;
		public static int MobsToKillLoughGur2 = 60;
		public static int m_mobstokillloughgur2
		{
			get { return m_mobstokillloughgur2; }
			set { m_mobstokillloughgur2 = value; }
		}
		public override void Think()
		{
			if (ParthanansKilledFarm3b >= MobsToKillLoughGur2)
			{
				SacrificeParthanan3b = true;
				SpawnBigOne();
			}
			base.Think();
		}
		public void SpawnBigOne()
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is AmalgamateParthananBrain && npc.PackageID == "ParthananBossLoughGur2")
					return;
			}
			AmalgamateParthanan boss = new AmalgamateParthanan();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.PackageID = "ParthananBossLoughGur2";
			boss.AddToWorld();
			LoughGur2Boss = 1;
			ParthanansKilledFarm3b = 0;
			SacrificeParthanan3b = false;
		}
	}
}
#endregion
#endregion