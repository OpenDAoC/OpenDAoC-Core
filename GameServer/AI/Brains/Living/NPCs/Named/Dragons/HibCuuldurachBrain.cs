using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.PacketHandler;

namespace Core.GS.AI.Brains;

#region Cuuldurach
public class HibCuuldurachBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public HibCuuldurachBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 5000;
		
		_roamingPathPoints.Add(new Point3D(408646, 706432, 2965));//spawn
		_roamingPathPoints.Add(new Point3D(399021, 704912, 6212));
		_roamingPathPoints.Add(new Point3D(391823, 706981, 6212));
		_roamingPathPoints.Add(new Point3D(379666, 707613, 6212));
		_roamingPathPoints.Add(new Point3D(374210, 703692, 6212));
		_roamingPathPoints.Add(new Point3D(369800, 698565, 6212));
		_roamingPathPoints.Add(new Point3D(376500, 693899, 6212));
		_roamingPathPoints.Add(new Point3D(382065, 695219, 6212));
		_roamingPathPoints.Add(new Point3D(383638, 677035, 6212));
		_roamingPathPoints.Add(new Point3D(391481, 681660, 6810));
		_roamingPathPoints.Add(new Point3D(384378, 684504, 6226));
		_roamingPathPoints.Add(new Point3D(376941, 691151, 6226));
		_roamingPathPoints.Add(new Point3D(373055, 684792, 7197));
		_roamingPathPoints.Add(new Point3D(371289, 666663, 7197));
		_roamingPathPoints.Add(new Point3D(361740, 659874, 7197));
		_roamingPathPoints.Add(new Point3D(367670, 653364, 7197));
		_roamingPathPoints.Add(new Point3D(374128, 652093, 8016));
		_roamingPathPoints.Add(new Point3D(392383, 658971, 8016));
		_roamingPathPoints.Add(new Point3D(399312, 670926, 8016));
		_roamingPathPoints.Add(new Point3D(399806, 678950, 6685));
		_roamingPathPoints.Add(new Point3D(394874, 680283, 6685));
		_roamingPathPoints.Add(new Point3D(399038, 686435, 6685));
		_roamingPathPoints.Add(new Point3D(410606, 672288, 6685));
		_roamingPathPoints.Add(new Point3D(406201, 657594, 8321));
		_roamingPathPoints.Add(new Point3D(411408, 655769, 8321));
		_roamingPathPoints.Add(new Point3D(411061, 673862, 6722));
		_roamingPathPoints.Add(new Point3D(409199, 679881, 6722));
		_roamingPathPoints.Add(new Point3D(409781, 696669, 7148));

	}
	public static bool CanGlare = false;
	public static bool CanGlare2 = false;
	public static bool CanStun = false;
	public static bool CanThrow = false;
	public static bool CanSpawnMessengers = false;
	public static bool ResetChecks = false;
	public static bool LockIsRestless = false;
	public static bool LockEndRoute = false;
	public static bool checkForMessangers = false;
	public static System.Collections.Generic.List<GameNpc> DragonAdds = new System.Collections.Generic.List<GameNpc>();
	private List<Point3D> _roamingPathPoints = new List<Point3D>();
	private int _lastRoamIndex = 0;

	public static bool m_isrestless = false;
	public static bool IsRestless
	{
		get { return m_isrestless; }
		set { m_isrestless = value; }
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			Body.Health = Body.MaxHealth;
            #region !IsRestless
            if (!IsRestless)
			{
				DragonKaboom1 = false;
				DragonKaboom2 = false;
				DragonKaboom3 = false;
				DragonKaboom4 = false;
				DragonKaboom5 = false;
				DragonKaboom6 = false;
				DragonKaboom7 = false;
				DragonKaboom8 = false;
				DragonKaboom9 = false;
				CanThrow = false;
				CanGlare = false;
				CanStun = false;
				RandomTarget = null;
				if (Glare_Enemys.Count > 0)//clear glare enemys
					Glare_Enemys.Clear();

				if (Port_Enemys.Count > 0)//clear port players
					Port_Enemys.Clear();
				if (randomlyPickedPlayers.Count > 0)//clear randomly picked players
					randomlyPickedPlayers.Clear();

				var prepareGlare = Body.TempProperties.GetProperty<EcsGameTimer>("cuuldurach_glare");
				if(prepareGlare != null)
                {
					prepareGlare.Stop();
					Body.TempProperties.RemoveProperty("cuuldurach_glare");
                }
				var prepareStun = Body.TempProperties.GetProperty<EcsGameTimer>("cuuldurach_stun");
				if (prepareStun != null)
				{
					prepareStun.Stop();
					Body.TempProperties.RemoveProperty("cuuldurach_stun");
				}
				var throwPlayer = Body.TempProperties.GetProperty<EcsGameTimer>("cuuldurach_throw");
				if (throwPlayer != null)
				{
					throwPlayer.Stop();
					Body.TempProperties.RemoveProperty("cuuldurach_throw");
				}
				var spawnMessengers = Body.TempProperties.GetProperty<EcsGameTimer>("cuuldurach_messengers");
				if (spawnMessengers != null)
				{
					spawnMessengers.Stop();
					CanSpawnMessengers = false;
					Body.TempProperties.RemoveProperty("cuuldurach_messengers");
				}                   
            }
			#endregion
			if (!checkForMessangers)
			{
				if (DragonAdds.Count > 0)
				{
					foreach (GameNpc messenger in DragonAdds)
					{
						if (messenger != null && messenger.IsAlive && messenger.Brain is CuuldurachMessengerBrain)
							messenger.RemoveFromWorld();
					}
					foreach (GameNpc glimmer in DragonAdds)
					{
						if (glimmer != null && glimmer.IsAlive && glimmer.Brain is CuuldurachSpawnedAdBrain)
							glimmer.RemoveFromWorld();
					}
					DragonAdds.Clear();
				}
				checkForMessangers = true;
			}
		}

		#region Dragon IsRestless fly route activation
		if (Body.CurrentRegion.IsPM && Body.CurrentRegion.IsNightTime == false && !LockIsRestless && !Body.InCombatInLast(30000))//Dragon will start roam
		{
			if (Glare_Enemys.Count > 0)
				Glare_Enemys.Clear();

			if (HasAggro)//if got aggro clear it
			{
				if (Body.attackComponent.AttackState && Body.IsCasting)//make sure it stop all actions
					Body.attackComponent.StopAttack();

				ClearAggroList();
			}

			IsRestless = true;//start roam
			_lastRoamIndex = 0;
			LockEndRoute = false;

			foreach (GamePlayer player in ClientService.GetPlayersOfZone(Body.CurrentZone))
			{
				player.Out.SendSoundEffect(2467, 0, 0, 0, 0, 0);//play sound effect for every player in boss currentregion
				player.Out.SendMessage($"{Body.Name} bellows from the skies, 'Let all who intrude into my domain pay heed. I will seek you out and cast you into the arms of Death if you remain here!'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
			}

			Body.Flags = ENpcFlags.FLYING;//make dragon fly mode
			ResetChecks = false;//reset it so can reset bools at end of path
			LockIsRestless = true;
		}

		if (IsRestless)
			DragonFlyingPath();//make dragon follow the path

		if (!ResetChecks && _lastRoamIndex >= _roamingPathPoints.Count)
		{
			IsRestless = false;//can roam again
			Body.ReturnToSpawnPoint(400);//move dragon to spawn so he can attack again
			Body.Flags = 0; //remove all flags
			_lastRoamIndex = 0;
			ResetChecks = true;//do it only once
		}
		if (Body.CurrentRegion.IsNightTime == true && !LockEndRoute)//reset bools to dragon can roam again
		{
			LockIsRestless = false; //roam 2nd check		
			LockEndRoute = true;
		}
		if (IsRestless)//special glare phase, during dragon roam it will cast glare like a mad
		{
			if (!CanGlare2 && !Body.IsCasting)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareGlareRoam), Util.Random(5000, 8000));//Glare at target every 5-10s
				CanGlare2 = true;
			}
		}
		#endregion
		if (HasAggro && Body.TargetObject != null)
		{
			checkForMessangers = false;
			DragonBreath();//Method that handle dragon kabooom breaths
			if (CanThrow == false && !IsRestless)
			{
				EcsGameTimer throwPlayer = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(60000, 80000));//Teleport 2-5 Players every 60-80s
				Body.TempProperties.SetProperty("cuuldurach_throw", throwPlayer);
				CanThrow = true;
			}
			if (CanGlare == false && !Body.IsCasting && !IsRestless)
			{
				EcsGameTimer prepareGlare = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareGlare), Util.Random(40000, 60000));//Glare at target every 40-60s
				Body.TempProperties.SetProperty("cuuldurach_glare", prepareGlare);
				CanGlare = true;
			}
			if (CanStun == false && !Body.IsCasting && !IsRestless)
			{
				EcsGameTimer prepareStun = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareStun), Util.Random(120000, 180000));//prepare Stun every 120s-180s
				Body.TempProperties.SetProperty("cuuldurach_stun", prepareStun);
				CanStun = true;
			}
			if (Body.HealthPercent <= 50 && CanSpawnMessengers == false && !IsRestless)
			{
				EcsGameTimer spawnMessengers = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMssengers), Util.Random(80000, 90000));//spawn messengers at 50% hp every 80/90s
				Body.TempProperties.SetProperty("cuuldurach_messengers", spawnMessengers);
				CanSpawnMessengers = true;
			}
		}
		base.Think();
	}
	#region Dragon Roaming Path
	private void DragonFlyingPath()
	{
		if (IsRestless && Body.IsAlive)
		{
			Body.MaxSpeedBase = 400;
			short speed = 350;
			
			if (Body.IsWithinRadius(_roamingPathPoints[_lastRoamIndex], 100))
				_lastRoamIndex++;

			if(_lastRoamIndex >= _roamingPathPoints.Count)
				Body.ReturnToSpawnPoint(400);
			else if(!Body.IsMoving)
				Body.WalkTo(_roamingPathPoints[_lastRoamIndex], speed);
		}
	}
	#endregion

	#region Throw Players
	List<GamePlayer> Port_Enemys = new List<GamePlayer>();
	List<GamePlayer> randomlyPickedPlayers = new List<GamePlayer>();
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	public static List<t> GetRandomElements<t>(IEnumerable<t> list, int elementsCount)//pick X elements from list
	{
		return list.OrderBy(x => Guid.NewGuid()).Take(elementsCount).ToList();
	}
	private int ThrowPlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive && HasAggro)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Port_Enemys.Contains(player))
						{
							if (player != Body.TargetObject)//dont throw main target
								Port_Enemys.Add(player);
						}
					}
				}
			}
			if (Port_Enemys.Count > 0)
			{
				randomlyPickedPlayers = GetRandomElements(Port_Enemys, Util.Random(2, 5));//pick 2-5players from list to new list

				if (randomlyPickedPlayers.Count > 0)
				{
					foreach (GamePlayer player in randomlyPickedPlayers)
					{
						if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && HasAggro && player.IsWithinRadius(Body, 2000))
						{
							player.Out.SendMessage(Body.Name + " begins flapping his wings violently. You struggle to hold your footing on the ground!",EChatType.CT_Broadcast,EChatLoc.CL_ChatWindow);
							switch (Util.Random(1, 5))
							{
								case 1: player.MoveTo(Body.CurrentRegionID, 408807, 706640, 4315, 1588); break;//lair spawn point
								case 2: player.MoveTo(Body.CurrentRegionID, 404579, 699656, 4683, 1840); break;
								case 3: player.MoveTo(Body.CurrentRegionID, 410650, 698271, 4758, 2890); break;
								case 4: player.MoveTo(Body.CurrentRegionID, 402790, 707787, 4083, 2628); break;
								case 5: player.MoveTo(Body.CurrentRegionID, 407532, 695634, 4533, 281); break;
							}
						}
					}
					randomlyPickedPlayers.Clear();//clear list after port
				}
			}
			CanThrow = false;// set to false, so can throw again
		}
		return 0;
	}
	#endregion

	#region Glare Standard
	List<string> glare_text = new List<string>()
	{
		"{0} shouts, 'I will crush your bones {1}!'",
		"{0} shouts, 'Your end is near little {1}. I will taste your flesh.'",
		"{0} shouts, '{1} like you should not enter my domain. Your body corpse will rest at my lair.'",
		"{0} shouts, 'Tasty poor {1}. I will drain your last life essence from your body.'",
	};
	List<GamePlayer> Glare_Enemys = new List<GamePlayer>();
	public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	private int PrepareGlare(EcsGameTimer timer)
	{
		if (!IsRestless && HasAggro && Body.IsAlive)
		{
			ushort DragonRange = 2500;
			foreach (GamePlayer player in Body.GetPlayersInRadius(DragonRange))
			{
				if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					if (!Glare_Enemys.Contains(player))
						Glare_Enemys.Add(player);
				}
			}
			if (Glare_Enemys.Count > 0)
			{
				GamePlayer Target = Glare_Enemys[Util.Random(0, Glare_Enemys.Count - 1)];
				RandomTarget = Target;
				if (RandomTarget != null && RandomTarget.IsAlive && RandomTarget.IsWithinRadius(Body, Dragon_DD.Range))
				{
					BroadcastMessage(String.Format("{0} stares at {1} and prepares a massive attack.", Body.Name, RandomTarget.Name));
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastGlare), 6000);
				}
				else
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlare), 2000);
			}
			else
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlare), 2000);
		}
		return 0;
	}
	private int CastGlare(EcsGameTimer timer)
	{
		if (!IsRestless && HasAggro && Body.IsAlive && RandomTarget != null && RandomTarget.IsAlive && RandomTarget.IsWithinRadius(Body, Dragon_DD.Range) && !Body.IsCasting)
		{
			Body.TargetObject = RandomTarget;
			Body.TurnTo(RandomTarget);
			Body.CastSpell(Dragon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			string glaretext = glare_text[Util.Random(0, glare_text.Count - 1)];
			RandomTarget.Out.SendMessage(String.Format(glaretext, Body.Name, RandomTarget.PlayerClass.Name), EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlare), 2000);
		return 0;
	}
	private int ResetGlare(EcsGameTimer timer)
	{
		if (Glare_Enemys.Count > 0)
			Glare_Enemys.Clear();

		RandomTarget = null;
		CanGlare = false;
		return 0;
	}
	#endregion

	#region Glare Roam
	List<string> glareroam_text = new List<string>()
	{
		"{0} shouts, 'I will crush your bones {1}!'",
		"{0} shouts, 'Your end is near little {1}. I will taste your flesh.'",
		"{0} shouts, '{1} like you should not enter my domain. Your body corpse will rest at my lair.'",
		"{0} shouts, 'Tasty poor {1}. I will drain your last life essence from your body.'",
	};
	List<GamePlayer> GlareRoam_Enemys = new List<GamePlayer>();
	public static GamePlayer randomtarget2 = null;
	public static GamePlayer RandomTarget2
	{
		get { return randomtarget2; }
		set { randomtarget2 = value; }
	}
	private int PrepareGlareRoam(EcsGameTimer timer)
	{
		if (IsRestless && Body.IsAlive)
		{
			ushort DragonRange = 5000;
			foreach (GamePlayer player in Body.GetPlayersInRadius(DragonRange))
			{
				if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					if (!GlareRoam_Enemys.Contains(player))
						GlareRoam_Enemys.Add(player);
					if (!AggroTable.ContainsKey(player))
						AggroTable.Add(player, 100);
				}
			}
			if (GlareRoam_Enemys.Count > 0)
			{
				GamePlayer Target = GlareRoam_Enemys[Util.Random(0, GlareRoam_Enemys.Count - 1)];
				RandomTarget2 = Target;
				if (RandomTarget2 != null && RandomTarget2.IsAlive && RandomTarget2.IsWithinRadius(Body, Dragon_DD2.Range))
				{
					foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
					{
						if (player != null)
							player.Out.SendMessage(String.Format("{0} stares at {1} and prepares a massive attack.", Body.Name, RandomTarget2.Name), EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
					}
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastGlareRoam), 3000);
				}
				else
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlareRoam), 2000);
			}
			else
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlareRoam), 2000);
		}
		return 0;
	}
	private int CastGlareRoam(EcsGameTimer timer)
	{
		if (IsRestless && Body.IsAlive && RandomTarget2 != null && RandomTarget2.IsAlive && RandomTarget2.IsWithinRadius(Body, Dragon_DD2.Range) && !Body.IsCasting)
		{
			Body.TargetObject = RandomTarget2;
			Body.TurnTo(RandomTarget2);
			Body.CastSpell(Dragon_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);//special roaming glare
			string glaretextroam = glareroam_text[Util.Random(0, glareroam_text.Count - 1)];
			RandomTarget2.Out.SendMessage(String.Format(glaretextroam, Body.Name, RandomTarget2.PlayerClass.Name), EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ResetGlareRoam), 2000);
		return 0;
	}
	private int ResetGlareRoam(EcsGameTimer timer)
	{
		if (GlareRoam_Enemys.Count > 0)
			GlareRoam_Enemys.Clear();

		if (IsRestless)
		{
			ClearAggroList();
		}
		RandomTarget2 = null;
		CanGlare2 = false;
		return 0;
	}
	#endregion

	#region Stun
	private int PrepareStun(EcsGameTimer timer)
	{
		if (!IsRestless && HasAggro && Body.IsAlive)
		{
			BroadcastMessage(String.Format("{0} roars horrifyingly!", Body.Name));
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(CastStun), 6000);
		}
		return 0;
	}
	private int CastStun(EcsGameTimer timer)
	{
		if (!IsRestless && HasAggro && Body.IsAlive)
			Body.CastSpell(Dragon_Stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		CanStun = false;
		return 0;
	}
	#endregion

	#region Dragon Breath Big Bang and debuff
	public static bool DragonKaboom1 = false;
	public static bool DragonKaboom2 = false;
	public static bool DragonKaboom3 = false;
	public static bool DragonKaboom4 = false;
	public static bool DragonKaboom5 = false;
	public static bool DragonKaboom6 = false;
	public static bool DragonKaboom7 = false;
	public static bool DragonKaboom8 = false;
	public static bool DragonKaboom9 = false;

	List<string> breath_text = new List<string>()
	{
			"You feel a rush of air flow past you as {0} inhales deeply!",
			"{0} takes another powerful breath as he prepares to unleash a raging inferno upon you!",
			"{0} bellows in rage and glares at all of the creatures attacking him.",
			"{0} noticeably winces from his wounds as he attempts to prepare for yet another life-threatening attack!"
	};

	private void DragonBreath()
	{
		string message = breath_text[Util.Random(0, breath_text.Count - 1)];
		if (Body.HealthPercent <= 90 && DragonKaboom1 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom1 = true;
		}
		if (Body.HealthPercent <= 80 && DragonKaboom2 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom2 = true;
		}
		if (Body.HealthPercent <= 70 && DragonKaboom3 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom3 = true;
		}
		if (Body.HealthPercent <= 60 && DragonKaboom4 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom4 = true;
		}
		if (Body.HealthPercent <= 50 && DragonKaboom5 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom5 = true;
		}
		if (Body.HealthPercent <= 40 && DragonKaboom6 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom6 = true;
		}
		if (Body.HealthPercent <= 30 && DragonKaboom7 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom7 = true;
		}
		if (Body.HealthPercent <= 20 && DragonKaboom8 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom8 = true;
		}
		if (Body.HealthPercent <= 10 && DragonKaboom9 == false && !Body.IsCasting && !IsRestless)
		{
			BroadcastMessage(String.Format(message, Body.Name));
			Body.CastSpell(Dragon_PBAOE, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DragonCastDebuff), 5000);
			DragonKaboom9 = true;
		}
	}
	private int DragonCastDebuff(EcsGameTimer timer) 
	{
		Body.CastSpell(Dragon_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
		return 0;
	}
	#endregion

	#region Messengers
	private int SpawnMssengers(EcsGameTimer timer)
	{
		for (int i = 0; i <= Util.Random(3, 5); i++)
		{
			CuuldurachMessenger messenger = new CuuldurachMessenger();
			messenger.X = 408752 + Util.Random(-100, 100);
			messenger.Y = 706546 + Util.Random(-100, 100);
			messenger.Z = 2974;
			messenger.Heading = Body.Heading;
			messenger.CurrentRegion = Body.CurrentRegion;
			messenger.AddToWorld();
		}
		CanSpawnMessengers = false;
		return 0;
	}
	#endregion

	#region Spells
	private Spell m_Dragon_DD2;
	private Spell Dragon_DD2
	{
		get
		{
			if (m_Dragon_DD2 == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5702;
				spell.Icon = 5702;
				spell.TooltipId = 5702;
				spell.Damage = 2000;
				spell.Name = "Cuuldurach's Glare";
				spell.Range = 5000;//very long range cause dragon is flying and got big aggro
				spell.Radius = 1000;
				spell.SpellID = 11959;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Dragon_DD2 = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_DD2);
			}
			return m_Dragon_DD2;
		}
	}
	private Spell m_Dragon_DD;
	private Spell Dragon_DD
	{
		get
		{
			if (m_Dragon_DD == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5702;
				spell.Icon = 5702;
				spell.TooltipId = 5702;
				spell.Damage = 1500;
				spell.Name = "Cuuldurach's Glare";
				spell.Range = 1500;
				spell.Radius = 1000;
				spell.SpellID = 11960;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Dragon_DD = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_DD);
			}
			return m_Dragon_DD;
		}
	}
	private Spell m_Dragon_PBAOE;
	private Spell Dragon_PBAOE
	{
		get
		{
			if (m_Dragon_PBAOE == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 6;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5702;
				spell.Icon = 5702;
				spell.TooltipId = 5702;
				spell.Damage = 2400;
				spell.Name = "Cuuldurach's Breath";
				spell.Range = 0;
				spell.Radius = 2000;
				spell.SpellID = 11961;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Dragon_PBAOE = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_PBAOE);
			}
			return m_Dragon_PBAOE;
		}
	}
	private Spell m_Dragon_Stun;
	private Spell Dragon_Stun
	{
		get
		{
			if (m_Dragon_Stun == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 5703;
				spell.Icon = 5703;
				spell.TooltipId = 5703;
				spell.Duration = 30;
				spell.Name = "Dragon's Stun";
				spell.Range = 0;
				spell.Radius = 2000;
				spell.SpellID = 11962;
				spell.Target = "Enemy";
				spell.Type = ESpellType.Stun.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Body;
				m_Dragon_Stun = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_Stun);
			}
			return m_Dragon_Stun;
		}
	}
	private Spell m_Dragon_Debuff;
	private Spell Dragon_Debuff
	{
		get
		{
			if (m_Dragon_Debuff == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 0;
				spell.ClientEffect = 4576;
				spell.Icon = 5702;
				spell.TooltipId = 5702;
				spell.Duration = 120;
				spell.Value = 50;
				spell.Name = "Dragon's Breath";
				spell.Description = "Decreases a target's given resistance to Spirit magic by 50%";
				spell.Range = 0;
				spell.Radius = 2000;
				spell.SpellID = 11963;
				spell.Target = "Enemy";
				spell.Type = ESpellType.SpiritResistDebuff.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Spirit;
				m_Dragon_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_Debuff);
			}
			return m_Dragon_Debuff;
		}
	}
	#endregion
}
#endregion Cuuldurach

#region Cuuldurach's messengers
public class CuuldurachMessengerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public CuuldurachMessengerBrain()
	{
		AggroLevel = 100;
		AggroRange = 500;
	}

	private protected bool ChoosePath = false;
	private protected bool ChoosePath1 = false;
	private protected bool ChoosePath2 = false;
	private protected bool ChoosePath3 = false;
	private protected bool ChoosePath4 = false;
	private protected bool CanSpawnGlimmers = false;
	public override void Think()
	{
		if (Body.IsAlive)
		{
			if (ChoosePath == false)
			{
				switch (Util.Random(1, 4))//choose which path messenger will walk
				{
					case 1: ChoosePath1 = true; break;
					case 2: ChoosePath2 = true; break;
					case 3: ChoosePath3 = true; break;
					case 4: ChoosePath4 = true; break;
				}
				ChoosePath = true;
			}
			if (ChoosePath1)
				Path1();
			if (ChoosePath2)
				Path2();
			if (ChoosePath3)
				Path3();
			if (ChoosePath4)
				Path4();
		}
		base.Think();
	}

	#region Messengers Paths
	private short speed = 225;
	private protected bool path1point1 = false;
	private protected bool path1point2 = false;
	private protected bool path1point3 = false;

	private protected bool path2point1 = false;
	private protected bool path2point2 = false;
	private protected bool path2point3 = false;

	private protected bool path3point1 = false;
	private protected bool path3point2 = false;
	private protected bool path3point3 = false;

	private protected bool path4point1 = false;
	private protected bool path4point2 = false;
	private protected bool path4point3 = false;

	#region Path1
	private protected void Path1()
	{
		Point3D point1 = new Point3D(407371, 704161, 2760);
		Point3D point2 = new Point3D(405306, 701159, 3491);
		Point3D point3 = new Point3D(404443, 699515, 3783);

		if (!Body.IsWithinRadius(point1, 30) && path1point1 == false)
		{
			Body.WalkTo(point1, speed);
		}
		else
		{
			path1point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path1point1 == true && path1point2 == false)
			{
				Body.WalkTo(point2, speed);
			}
			else
			{
				path1point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path1point1 == true && path1point2 == true
					&& path1point3 == false)
				{
					Body.WalkTo(point3, speed);
				}
				else
				{
					path1point3 = true;
					if (CanSpawnGlimmers == false)
					{
						SpawnGlimmers();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnGlimmers = true;
					}
				}
			}
		}
	}
	#endregion

	#region Path2
	private protected void Path2()
	{
		Point3D point1 = new Point3D(411517, 704273, 2759);
		Point3D point2 = new Point3D(410828, 699897, 3490);
		Point3D point3 = new Point3D(410713, 698129, 3619);

		if (!Body.IsWithinRadius(point1, 30) && path2point1 == false)
		{
			Body.WalkTo(point1, speed);
		}
		else
		{
			path2point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path2point1 == true && path2point2 == false)
			{
				Body.WalkTo(point2, speed);
			}
			else
			{
				path2point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path2point1 == true && path2point2 == true
					&& path2point3 == false)
				{
					Body.WalkTo(point3, speed);
				}
				else
				{
					path2point3 = true;
					if (CanSpawnGlimmers == false)
					{
						SpawnGlimmers();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnGlimmers = true;
					}
				}
			}
		}
	}
	#endregion

	#region Path3
	private protected void Path3()
	{
		Point3D point1 = new Point3D(410511, 709059, 2760);
		Point3D point2 = new Point3D(405922, 709976, 2735);
		Point3D point3 = new Point3D(403053, 707551, 2474);

		if (!Body.IsWithinRadius(point1, 30) && path3point1 == false)
		{
			Body.WalkTo(point1, speed);
		}
		else
		{
			path3point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path3point1 == true && path3point2 == false)
			{
				Body.WalkTo(point2, speed);
			}
			else
			{
				path3point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path3point1 == true && path3point2 == true
					&& path3point3 == false)
				{
					Body.WalkTo(point3, speed);
				}
				else
				{
					path3point3 = true;
					if (CanSpawnGlimmers == false)
					{
						SpawnGlimmers();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnGlimmers = true;
					}
				}
			}
		}
	}
	#endregion

	#region Path4
	private protected void Path4()
	{
		Point3D point1 = new Point3D(405898, 707838, 2760);
		Point3D point2 = new Point3D(403716, 705502, 2660);
		Point3D point3 = new Point3D(401242, 704226, 3277);

		if (!Body.IsWithinRadius(point1, 30) && path4point1 == false)
		{
			Body.WalkTo(point1, speed);
		}
		else
		{
			path4point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path4point1 == true && path4point2 == false)
			{
				Body.WalkTo(point2, speed);
			}
			else
			{
				path4point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path4point1 == true && path4point2 == true
					&& path4point3 == false)
				{
					Body.WalkTo(point3, speed);
				}
				else
				{
					path4point3 = true;
					if (CanSpawnGlimmers == false)
					{
						SpawnGlimmers();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnGlimmers = true;
					}
				}
			}
		}
	}
	#endregion

	#endregion
	private protected int RemoveMessenger(EcsGameTimer timer)
	{
		if (Body.IsAlive)
		{
			Body.RemoveFromWorld();
		}
		return 0;
	}
	private protected void SpawnGlimmers()
	{
		for (int i = 0; i <= Util.Random(3, 5); i++)
		{
			CuuldurachSpawnedAdd add = new CuuldurachSpawnedAdd();
			add.X = Body.X + Util.Random(-200, 200);
			add.Y = Body.Y + Util.Random(-200, 200);
			add.Z = Body.Z;
			add.Heading = Body.Heading;
			add.CurrentRegion = Body.CurrentRegion;
			if (ChoosePath1)
				add.PackageID = "ChoosePath1";
			if (ChoosePath2)
				add.PackageID = "ChoosePath2";
			if (ChoosePath3)
				add.PackageID = "ChoosePath3";
			if (ChoosePath4)
				add.PackageID = "ChoosePath4";
			add.AddToWorld();
		}
	}
}
#endregion Cuuldurach's messengers

#region Cuuldurach's spawned adds
public class CuuldurachSpawnedAdBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public CuuldurachSpawnedAdBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 1000;
		ThinkInterval = 1500;
	}

	public override void Think()
	{
		if (Body.PackageID == "ChoosePath1" && !Body.InCombat && !HasAggro)
			Path1();
		if (Body.PackageID == "ChoosePath2" && !Body.InCombat && !HasAggro)
			Path2();
		if (Body.PackageID == "ChoosePath3" && !Body.InCombat && !HasAggro)
			Path3();
		if (Body.PackageID == "ChoosePath4" && !Body.InCombat && !HasAggro)
			Path4();

		base.Think();
	}
	#region Paths
	private protected bool path1point1 = false;
	private protected bool path1point2 = false;
	private protected bool path1point3 = false;
	private protected bool path1point4 = false;

	private protected bool path2point1 = false;
	private protected bool path2point2 = false;
	private protected bool path2point3 = false;
	private protected bool path2point4 = false;

	private protected bool path3point1 = false;
	private protected bool path3point2 = false;
	private protected bool path3point3 = false;
	private protected bool path3point4 = false;

	private protected bool path4point1 = false;
	private protected bool path4point2 = false;
	private protected bool path4point3 = false;
	private protected bool path4point4 = false;

	#region Path1
	private protected void Path1()
	{
		Point3D point1 = new Point3D(404443, 699515, 3783);
		Point3D point2 = new Point3D(405306, 701159, 3491);
		Point3D point3 = new Point3D(407371, 704161, 2760);
		Point3D point4 = new Point3D(408646, 706432, 2965);

		if (!Body.IsWithinRadius(point1, 30) && path1point1 == false)
		{
			Body.WalkTo(point1, 200);
		}
		else
		{
			path1point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path1point1 == true && path1point2 == false)
			{
				Body.WalkTo(point2, 200);
			}
			else
			{
				path1point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path1point1 == true && path1point2 == true
					&& path1point3 == false)
				{
					Body.WalkTo(point3, 200);
				}
				else
				{
					path1point3 = true;
					if (!Body.IsWithinRadius(point4, 30) && path1point1 == true && path1point2 == true
					&& path1point3 == true && path1point4 == false)
					{
						Body.WalkTo(point4, 200);
					}
					else
						path1point4 = true;
				}
			}
		}
	}
	#endregion

	#region Path2
	private protected void Path2()
	{
		Point3D point1 = new Point3D(410713, 698129, 3619);
		Point3D point2 = new Point3D(410828, 699897, 3490);
		Point3D point3 = new Point3D(411424, 704307, 2758);
		Point3D point4 = new Point3D(408646, 706432, 2965);

		if (!Body.IsWithinRadius(point1, 30) && path2point1 == false)
		{
			Body.WalkTo(point1, 200);
		}
		else
		{
			path2point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path2point1 == true && path2point2 == false)
			{
				Body.WalkTo(point2, 200);
			}
			else
			{
				path2point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path2point1 == true && path2point2 == true
					&& path2point3 == false)
				{
					Body.WalkTo(point3, 200);
				}
				else
				{
					path2point3 = true;
					if (!Body.IsWithinRadius(point4, 30) && path2point1 == true && path2point2 == true
					&& path2point3 == true && path2point4 == false)
					{
						Body.WalkTo(point4, 200);
					}
					else
						path2point4 = true;
				}
			}
		}
	}
	#endregion

	#region Path3
	private protected void Path3()
	{
		Point3D point1 = new Point3D(403053, 707551, 2474);
		Point3D point2 = new Point3D(405922, 709976, 2735);
		Point3D point3 = new Point3D(410511, 709059, 2760);
		Point3D point4 = new Point3D(408646, 706432, 2965);

		if (!Body.IsWithinRadius(point1, 30) && path3point1 == false)
		{
			Body.WalkTo(point1, 200);
		}
		else
		{
			path3point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path3point1 == true && path3point2 == false)
			{
				Body.WalkTo(point2, 200);
			}
			else
			{
				path3point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path3point1 == true && path3point2 == true
					&& path3point3 == false)
				{
					Body.WalkTo(point3, 200);
				}
				else
				{
					path3point3 = true;
					if (!Body.IsWithinRadius(point4, 30) && path3point1 == true && path3point2 == true
					&& path3point3 == true && path3point4 == false)
					{
						Body.WalkTo(point4, 200);
					}
					else
						path3point4 = true;
				}
			}
		}
	}
	#endregion

	#region Path4
	private protected void Path4()
	{
		Point3D point1 = new Point3D(401242, 704226, 3277);
		Point3D point2 = new Point3D(403716, 705502, 2660);
		Point3D point3 = new Point3D(405898, 707838, 2760);
		Point3D point4 = new Point3D(408646, 706432, 2965);

		if (!Body.IsWithinRadius(point1, 30) && path4point1 == false)
		{
			Body.WalkTo(point1, 200);
		}
		else
		{
			path4point1 = true;
			if (!Body.IsWithinRadius(point2, 30) && path4point1 == true && path4point2 == false)
			{
				Body.WalkTo(point2, 200);
			}
			else
			{
				path4point2 = true;
				if (!Body.IsWithinRadius(point3, 30) && path4point1 == true && path4point2 == true
					&& path4point3 == false)
				{
					Body.WalkTo(point3, 200);
				}
				else
				{
					path4point3 = true;
					if (!Body.IsWithinRadius(point4, 30) && path4point1 == true && path4point2 == true
					&& path4point3 == true && path4point4 == false)
					{
						Body.WalkTo(point4, 200);
					}
					else
						path4point4 = true;
				}
			}
		}
	}
	#endregion

	#endregion
}
#endregion Cuuldurach's spawned adds