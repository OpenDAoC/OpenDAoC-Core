using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Spectral Provisioner
public class SpectralProvisionerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SpectralProvisionerBrain()
			: base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 2000;
	}
	//private bool CanAddJunk = false;
	public override void OnAttackedByEnemy(AttackData ad)
	{
		if (Util.Chance(40) && ad != null /*&& !CanAddJunk*/ && ad.Attacker is GamePlayer && ad.Attacker != null)
		{
			//ItemTemplate sackJunk = GameServer.Database.FindObjectByKey<ItemTemplate>("sack_of_decaying_junk");
			//InventoryItem item = GameInventoryItem.Create(sackJunk);

			//foreach (GamePlayer player in Body.GetPlayersInRadius(500))
			//{
			//if (!player.IsAlive) continue;
			//item.OwnerID = player.ObjectId;
			//item.IsDropable = true;			//Make sure it's droppable
			//item.IsIndestructible = false;	//make sure it's destructible
			//player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);				
			//}				
			//new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDecayingJunk), Util.Random(25000,35000));
			//CanAddJunk = true;
			if(ad.Attacker is not GameSummonedPet)
				Body.CastSpell(SpectralDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		}
		base.OnAttackedByEnemy(ad);
	}
	//private int ResetDecayingJunk(ECSGameTimer timer)
    //{
		//CanAddJunk = false;
		//return 0;
    //}
	public static bool point1check = false;
	public static bool point2check = false;
	public static bool point3check = false;
	public static bool point4check = false;
	public static bool point5check = false;
	public static bool point6check = false;
	public static bool point7check = false;
	public static bool point8check = false;
	private Point3D point1 = new Point3D(30050, 39425, 17004);
	private Point3D point2 = new Point3D(30940, 39418, 17004);
	private Point3D point3 = new Point3D(32065, 40205, 17004);
	private Point3D point4 = new Point3D(32075, 42378, 17004);
	private Point3D point5 = new Point3D(32072, 40376, 17006);
	private Point3D point6 = new Point3D(32967, 39369, 17007);
	private Point3D point7 = new Point3D(32057, 38494, 17007);
	private Point3D point8 = new Point3D(31022, 39382, 17006);
	public override void Think()
	{
		if (Body.IsAlive)
		{
			//Point3D spawn = new Point3D(30049, 40799, 17004);
			Body.MaxSpeedBase = 300;
			Body.CurrentSpeed = 300;

			// if (HasAggro && Body.TargetObject != null)
			// {
			// 	foreach (GameNPC npc in Body.GetNPCsInRadius(800))
			// 	{
			// 		if (npc != null && npc.IsAlive && npc.PackageID == "ProvisionerBaf")
			// 			AddAggroListTo(npc.Brain as StandardMobBrain);
			// 	}
			// }

			#region Walk path
			if (!Body.IsWithinRadius(point1, 30) && point1check == false)
			{
				Body.WalkTo(point1, (short)Util.Random(195, 300));
				//log.Warn("Moving to point1, " + point1+"Corrent Pos: "+Body.X+", "+Body.Y+", "+Body.Z);
			}
			else
			{
				point1check = true;
				point8check = false;
				if (!Body.IsWithinRadius(point2, 30) && point1check == true && point2check == false)
				{
					Body.WalkTo(point2, (short)Util.Random(195, 300));
					//log.Warn("Arrived at point1,Moving to point2, " + point2);
				}
				else
				{
					point2check = true;
					if (!Body.IsWithinRadius(point3, 30) && point1check == true && point2check == true &&
						point3check == false)
					{
						Body.WalkTo(point3, (short)Util.Random(195, 300));
						//log.Warn("Arrived at point2,Moving to point3, " + point3);
					}
					else
					{
						point3check = true;
						if (!Body.IsWithinRadius(point4, 30) && point1check == true && point2check == true &&
							point3check == true && point4check == false)
						{
							Body.WalkTo(point4, (short)Util.Random(195, 300));
							//log.Warn("Arrived at point3,Moving to point4, " + point4);
						}
						else
						{
							point4check = true;
							if (!Body.IsWithinRadius(point5, 30) && point1check == true && point2check == true &&
								point3check == true && point4check == true && point5check == false)
							{
								Body.WalkTo(point5, (short)Util.Random(195, 300));
								//log.Warn("Arrived at point4,Moving to point5, " + point4);
							}
							else
							{
								point5check = true;
								if (!Body.IsWithinRadius(point6, 30) && point1check == true && point2check == true &&
								point3check == true && point4check == true && point5check == true && point6check == false)
								{
									Body.WalkTo(point6, (short)Util.Random(195, 300));
									//log.Warn("Arrived at point5,Moving to point6, " + point6);
								}
								else
								{
									point6check = true;
									if (!Body.IsWithinRadius(point7, 30) && point1check == true && point2check == true &&
									point3check == true && point4check == true && point5check == true && point6check == true && point7check == false)
									{
										Body.WalkTo(point7, (short)Util.Random(195, 300));
										//log.Warn("Arrived at point6,Moving to point7, " + point7);
									}
									else
									{
										point7check = true;
										if (!Body.IsWithinRadius(point8, 30) && point1check == true && point2check == true &&
										point3check == true && point4check == true && point5check == true && point6check == true && point7check == true && !point8check)
										{
											Body.WalkTo(point8, (short)Util.Random(195, 300));
											//log.Warn("Arrived at point7,Moving to point8, " + point8);
										}
										else
										{
											point8check = true;
											point7check = false;
											point1check = false;
											point2check = false;
											point3check = false;
											point4check = false;
											point5check = false;
											point6check = false;
											//log.Warn("Clearing flags");
										}
									}
								}
							}
						}
					}
				}
			}
            #endregion

            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
			{
				ClearAggroList();
				Body.Health = Body.MaxHealth;
			}
		}
		base.Think();
	}
	private Spell m_SpectralDD;
	private Spell SpectralDD
	{
		get
		{
			if (m_SpectralDD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.Power = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 9191;
				spell.Icon = 9191;
				spell.TooltipId = 9191;
				spell.Damage = 350;
				spell.Value = 70;
				spell.Duration = 30;
				spell.DamageType = (int)EDamageType.Spirit;
				spell.Description = "Spectral Provisioner strike back attacker and makes him move 70% slower for the spell duration.";
				spell.Name = "Spectral Strike";
				spell.Range = 2500;
				spell.SpellID = 12018;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DamageSpeedDecreaseNoVariance.ToString();
				m_SpectralDD = new Spell(spell, 60);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpectralDD);
			}
			return m_SpectralDD;
		}
	}
}
#endregion Spectral Provisioner

#region Spectral Provisioner Spawner
public class SpectralProvisionerSpawnerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log =
		log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public SpectralProvisionerSpawnerBrain()
		: base()
	{
		AggroLevel = 0;
		AggroRange = 500;
	}
	private bool CanSpawnProvisioner = false;
	public override void Think()
	{
		if(Body.IsAlive)
		{
			if(!CanSpawnProvisioner)
			{
				foreach(GamePlayer player in Body.GetPlayersInRadius(500))
				{
					if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						SpawnSpectralProvisioner(player);
						CanSpawnProvisioner = true;
					}
				}
			}
		}
		base.Think();
	}
	public void SpawnSpectralProvisioner(GamePlayer player)
	{
		foreach (GameNpc npc in Body.GetNPCsInRadius(8000))
		{
			if (npc.Brain is SpectralProvisionerBrain)
				return;
		}
		SpectralProvisioner boss = new SpectralProvisioner();
		boss.X = Body.X;
		boss.Y = Body.Y;
		boss.Z = Body.Z;
		boss.Heading = Body.Heading;
		boss.CurrentRegion = Body.CurrentRegion;
		boss.AddToWorld();
		if (player != null)
			log.Debug("Player "+player.Name + " initialized Spectral Provisioner spawn event.");
	}
}
#endregion Spectral Provisioner Spawner