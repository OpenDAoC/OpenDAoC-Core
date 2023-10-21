using System;
using System.Collections.Generic;
using DOL.GS;

namespace DOL.AI.Brain;

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
            #region Lough Derg
            if (Body.PackageID == "ParthananLoughDerg")
			{
				if (ParthananFarmController1Brain.SacrificeParthanan1 && ParthananFarmController1Brain.ParthansCanDie)
					return;
				else
					base.AttackMostWanted();
			}
            #endregion
            #region Connacht
            if (Body.PackageID == "ParthananConnacht")
			{
				if (ParthananFarmController2Brain.SacrificeParthanan2 && ParthananFarmController2Brain.ParthansCanDie2)
					return;
				else
					base.AttackMostWanted();
			}
			//2nd farm
			if (Body.PackageID == "ParthananConnacht2")
			{
				if (ParthananFarmController2bBrain.SacrificeParthanan2b && ParthananFarmController2bBrain.ParthansCanDie2b)
					return;
				else
					base.AttackMostWanted();
			}
            #endregion
            #region Lough Gur
            if (Body.PackageID == "ParthananLoughGur")
			{
				if (ParthananFarmController3Brain.SacrificeParthanan3 && ParthananFarmController3Brain.ParthansCanDie3)
					return;
				else
					base.AttackMostWanted();
			}
			//2nd farm
			if (Body.PackageID == "ParthananLoughGur2")
			{
				if (ParthananFarmController3bBrain.SacrificeParthanan3b && ParthananFarmController3bBrain.ParthansCanDie3b)
					return;
				else
					base.AttackMostWanted();
			}
            #endregion
        }
    }
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;
	private protected bool setbrain = false;
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
						Body.Flags ^= ENpcFlags.CANTTARGET;
						Body.Flags ^= ENpcFlags.DONTSHOWNAME;
						Body.Flags ^= ENpcFlags.PEACE;

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
						Body.Flags = (ENpcFlags)npcTemplate.Flags;
						Body.Model = Convert.ToUInt16(npcTemplate.Model);
						changed = false;
					}
				}

				if (ParthananFarmController1Brain.SacrificeParthanan1 && ParthananFarmController1Brain.ParthansCanDie)
				{
					ClearAggroList();
					Body.StopAttack();
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc.Brain is ParthananFarmController1Brain)
						{
							if (!Body.IsWithinRadius(npc, 50))
								Body.WalkTo(npc, Body.MaxSpeedBase);
							else
								Body.Die(npc);
						}
					}
				}
				else
				{
					if (Body.Model == 1)
					{
						foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController1Brain)
							{
								if (Body.IsWithinRadius(npc, 50))
									Body.Die(npc);
							}
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
						Body.Flags ^= ENpcFlags.CANTTARGET;
						Body.Flags ^= ENpcFlags.DONTSHOWNAME;
						Body.Flags ^= ENpcFlags.PEACE;

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
						Body.Flags = (ENpcFlags)npcTemplate.Flags;
						Body.Model = Convert.ToUInt16(npcTemplate.Model);
						changed = false;
					}
				}

				if (ParthananFarmController2Brain.SacrificeParthanan2 && ParthananFarmController2Brain.ParthansCanDie2)
				{
					ClearAggroList();
					Body.StopAttack();
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc.Brain is ParthananFarmController2Brain)
						{
							if (!Body.IsWithinRadius(npc, 50))
								Body.WalkTo(npc, Body.MaxSpeedBase);
							else
								Body.Die(npc);
						}
					}
				}
				else
				{
					if (Body.Model == 1)
					{
						foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2Brain)
							{
								if (Body.IsWithinRadius(npc, 50))
									Body.Die(npc);
							}
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
						Body.Flags ^= ENpcFlags.CANTTARGET;
						Body.Flags ^= ENpcFlags.DONTSHOWNAME;
						Body.Flags ^= ENpcFlags.PEACE;

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
						Body.Flags = (ENpcFlags)npcTemplate.Flags;
						Body.Model = Convert.ToUInt16(npcTemplate.Model);
						changed = false;
					}
				}

				if (ParthananFarmController2bBrain.SacrificeParthanan2b && ParthananFarmController2bBrain.ParthansCanDie2b)
				{
					ClearAggroList();
					Body.StopAttack();
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc.Brain is ParthananFarmController2bBrain)
						{
							if (!Body.IsWithinRadius(npc, 50))
								Body.WalkTo(npc, Body.MaxSpeedBase);
							else
								Body.Die(npc);
						}
					}
				}
				else
				{
					if (Body.Model == 1)
					{
						foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController2bBrain)
							{
								if (Body.IsWithinRadius(npc, 50))
									Body.Die(npc);
							}
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
						Body.Flags ^= ENpcFlags.CANTTARGET;
						Body.Flags ^= ENpcFlags.DONTSHOWNAME;
						Body.Flags ^= ENpcFlags.PEACE;

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
						Body.Flags = (ENpcFlags)npcTemplate.Flags;
						Body.Model = Convert.ToUInt16(npcTemplate.Model);
						changed = false;
					}
				}
				if (ParthananFarmController3Brain.SacrificeParthanan3 && ParthananFarmController3Brain.ParthansCanDie3)
				{
					ClearAggroList();
					Body.StopAttack();
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc.Brain is ParthananFarmController3Brain)
						{
							Point3D point = new Point3D(npc.X, npc.Y, npc.Z);
							if (!Body.IsWithinRadius(npc, 50))
								Body.WalkTo(point, 100);
							else
								Body.Die(npc);
						}
					}
				}
				else
                {
					if (Body.Model == 1)
					{
						foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3Brain)
							{
								if (Body.IsWithinRadius(npc, 50))
									Body.Die(npc);
							}
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
						Body.Flags ^= ENpcFlags.CANTTARGET;
						Body.Flags ^= ENpcFlags.DONTSHOWNAME;
						Body.Flags ^= ENpcFlags.PEACE;

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
						Body.Flags = (ENpcFlags)npcTemplate.Flags;
						Body.Model = Convert.ToUInt16(npcTemplate.Model);
						changed = false;
					}
				}
				if (ParthananFarmController3bBrain.SacrificeParthanan3b && ParthananFarmController3bBrain.ParthansCanDie3b)
				{
					ClearAggroList();
					Body.StopAttack();
					foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
					{
						if (npc.Brain is ParthananFarmController3bBrain)
						{
							if (!Body.IsWithinRadius(npc, 50))
								Body.WalkTo(npc, Body.MaxSpeedBase);
							else
								Body.Die(npc);
						}
					}
				}
				else
				{
					if (Body.Model == 1)
					{
						foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
						{
							if (npc.Brain is ParthananFarmController3bBrain)
							{
								if (Body.IsWithinRadius(npc, 50))
									Body.Die(npc);
							}
						}
					}
				}
			}
			#endregion
		}
		base.Think();
	}
}

public class ParthananFarmController1Brain : APlayerVicinityBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public ParthananFarmController1Brain()
		: base()
	{
		ThinkInterval = 1000;
	}
	public static bool BossIsUP = false;
	public static int ParthanansKilledFarm1 = 0; // Lough Derg
	public static bool SacrificeParthanan1 = false;
	public static int LoughDergBoss = 0;
	public static List<GameNpc> MinParthAround = new List<GameNpc>();
	public static bool ParthansCanDie = false;

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
			if (SacrificeParthanan1)
			{
				foreach(GameNpc npc in Body.GetNPCsInRadius(3000))
                {
					if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughDerg" && !MinParthAround.Contains(npc))
						MinParthAround.Add(npc);
                }
				if(MinParthAround.Count >= 5)
					SpawnBigOne();
				if(MinParthAround.Count >= 5)
					ParthansCanDie = true;
			}
		}
		if (SacrificeParthanan1 && MinParthAround.Count == 0 && BossIsUP)
		{
			SacrificeParthanan1 = false;
			LoughDergBoss = 1;
		}
	}

	public override void KillFSM()
	{
	}

	public void SpawnBigOne()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
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
		BossIsUP = true;
	}
}

public class ParthananFarmController2Brain : APlayerVicinityBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public ParthananFarmController2Brain()
		: base()
	{
		ThinkInterval = 1000;
	}
	public static bool BossIsUP2 = false;
	public static int ParthanansKilledFarm2 = 0; // Connacht
	public static bool SacrificeParthanan2 = false;
	public static int ConnachtBoss = 0;
	public static List<GameNpc> MinParthAround2 = new List<GameNpc>();
	public static bool ParthansCanDie2 = false;

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
			if (SacrificeParthanan2)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(3000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananConnacht" && !MinParthAround2.Contains(npc))
						MinParthAround2.Add(npc);
				}
				if (MinParthAround2.Count >= 5)
					SpawnBigOne();
				if (MinParthAround2.Count >= 5)
					ParthansCanDie2 = true;
			}
		}
		if (SacrificeParthanan2 && MinParthAround2.Count == 0 && BossIsUP2)
		{
			SacrificeParthanan2 = false;
			ConnachtBoss = 1;
		}
	}

	public override void KillFSM()
	{
		
	}

	public void SpawnBigOne()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
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
		BossIsUP2 = true;
	}
}

public class ParthananFarmController2bBrain : APlayerVicinityBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public ParthananFarmController2bBrain()
		: base()
	{
		ThinkInterval = 1000;
	}
	public static bool BossIsUP2b = false;
	public static int ParthanansKilledFarm2b = 0; // Connacht
	public static bool SacrificeParthanan2b = false;
	public static int Connacht2Boss = 0;
	public static List<GameNpc> MinParthAround2b = new List<GameNpc>();
	public static bool ParthansCanDie2b = false;

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
			if(SacrificeParthanan2b)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(3000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananConnacht2" && !MinParthAround2b.Contains(npc))
						MinParthAround2b.Add(npc);
				}
				if (MinParthAround2b.Count >= 5)
					SpawnBigOne();
				if (MinParthAround2b.Count >= 5)
					ParthansCanDie2b = true;
			}
		}
		if (SacrificeParthanan2b && MinParthAround2b.Count == 0 && BossIsUP2b)
		{
			SacrificeParthanan2b = false;
			Connacht2Boss = 1;
		}
	}

	public override void KillFSM()
	{
	}

	public void SpawnBigOne()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
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
		BossIsUP2b = true;
	}
}

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
	public static bool BossIsUP3 = false;
	public static int ParthanansKilledFarm3 = 0; // Lough Gur
	public static bool SacrificeParthanan3 = false;
	public static int LoughGurBoss = 0;
	public static List<GameNpc> MinParthAround3 = new List<GameNpc>();
	public static bool ParthansCanDie3 = false;

	public static int MobsToKillLoughGur = 60;
	public static int m_mobstokillloughgur
	{
		get { return m_mobstokillloughgur; }
		set { m_mobstokillloughgur = value; }
	}
	public override void Think()
	{
		//log.Warn("MinParthAround3 = " + MinParthAround3.Count + ", SacrificeParthanan3 = " + SacrificeParthanan3 + ", BossIsUP3 = " + BossIsUP3);
		if(ParthanansKilledFarm3 >= MobsToKillLoughGur)
        {
			SacrificeParthanan3 = true;
			if(SacrificeParthanan3)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(3000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughGur" && !MinParthAround3.Contains(npc))
						MinParthAround3.Add(npc);
				}
				if (MinParthAround3.Count >= 5)
					SpawnBigOne();
				if (MinParthAround3.Count >= 5)
					ParthansCanDie3 = true;
			}
		}
		if (SacrificeParthanan3 && MinParthAround3.Count == 0 && BossIsUP3)
		{
			SacrificeParthanan3 = false;
			LoughGurBoss = 1;
		}
		base.Think();
	}
	public void SpawnBigOne()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
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
		ParthanansKilledFarm3 = 0;
		BossIsUP3 = true;
	}
}

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

	public static bool BossIsUP3b = false;
	public static int ParthanansKilledFarm3b = 0; // Lough Gur
	public static bool SacrificeParthanan3b = false;
	public static int LoughGur2Boss = 0;
	public static List<GameNpc> MinParthAround3b = new List<GameNpc>();
	public static bool ParthansCanDie3b = false;

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
			if(SacrificeParthanan3b)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(3000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is ParthananBrain && npc.PackageID == "ParthananLoughGur2" && !MinParthAround3b.Contains(npc))
						MinParthAround3b.Add(npc);
				}
				if (MinParthAround3b.Count >= 5)
					SpawnBigOne();
				if (MinParthAround3b.Count >= 5)
					ParthansCanDie3b = true;
			}
		}
		if (SacrificeParthanan3b && MinParthAround3b.Count == 0 && BossIsUP3b)
		{
			SacrificeParthanan3b = false;
			LoughGur2Boss = 1;
		}
		base.Think();
	}
	public void SpawnBigOne()
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
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
		ParthanansKilledFarm3b = 0;
		BossIsUP3b = true;
	}
}