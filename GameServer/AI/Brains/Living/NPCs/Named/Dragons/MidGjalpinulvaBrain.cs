using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;

namespace Core.GS.AI.Brains;

#region Gjalpinulva
public class MidGjalpinulvaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MidGjalpinulvaBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 800;
		ThinkInterval = 5000;
		
		_roamingPathPoints.Add(new Point3D(712650, 1016043, 5106));
		_roamingPathPoints.Add(new Point3D(710579, 1007943, 5106));
		_roamingPathPoints.Add(new Point3D(703830, 998367, 5106));
		_roamingPathPoints.Add(new Point3D(695888, 990438, 5106));
		_roamingPathPoints.Add(new Point3D(695600, 979446, 5106));
		_roamingPathPoints.Add(new Point3D(701990, 980841, 5106));
		_roamingPathPoints.Add(new Point3D(709579, 986573, 5106));
		_roamingPathPoints.Add(new Point3D(714571, 984901, 5106));
		_roamingPathPoints.Add(new Point3D(719998, 983284, 5106));
		_roamingPathPoints.Add(new Point3D(721001, 993999, 5106));
		_roamingPathPoints.Add(new Point3D(720992, 999819, 5106));
		_roamingPathPoints.Add(new Point3D(728387, 1010676, 5106));
		_roamingPathPoints.Add(new Point3D(737301, 1010536, 5106));
		_roamingPathPoints.Add(new Point3D(736273, 1000467, 5106));
		_roamingPathPoints.Add(new Point3D(729920, 999398, 5106));
		_roamingPathPoints.Add(new Point3D(727483, 987398, 5106));
		_roamingPathPoints.Add(new Point3D(722107, 982002, 5106));
		_roamingPathPoints.Add(new Point3D(722974, 978111, 5106));
		_roamingPathPoints.Add(new Point3D(731811, 979376, 6057));
		_roamingPathPoints.Add(new Point3D(741124, 981185, 6057));
		_roamingPathPoints.Add(new Point3D(745175, 992884, 6057));
		_roamingPathPoints.Add(new Point3D(746278, 1001302, 5341));
		_roamingPathPoints.Add(new Point3D(746067, 1006105, 5341));
		_roamingPathPoints.Add(new Point3D(747528, 1010486, 5341));
		_roamingPathPoints.Add(new Point3D(747080, 1023245, 5341));
		_roamingPathPoints.Add(new Point3D(727530, 1027210, 5341));
		_roamingPathPoints.Add(new Point3D(715303, 1025848, 5341));
		_roamingPathPoints.Add(new Point3D(708888, 1021439, 3014));//spawn
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
	public static List<GameNpc> DragonAdds = new List<GameNpc>();
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

				var prepareGlare = Body.TempProperties.GetProperty<EcsGameTimer>("gjalpinulva_glare");
				if (prepareGlare != null)
				{
					prepareGlare.Stop();
					Body.TempProperties.RemoveProperty("gjalpinulva_glare");
				}
				var prepareStun = Body.TempProperties.GetProperty<EcsGameTimer>("gjalpinulva_stun");
				if (prepareStun != null)
				{
					prepareStun.Stop();
					Body.TempProperties.RemoveProperty("gjalpinulva_stun");
				}
				var throwPlayer = Body.TempProperties.GetProperty<EcsGameTimer>("gjalpinulva_throw");
				if (throwPlayer != null)
				{
					throwPlayer.Stop();
					Body.TempProperties.RemoveProperty("gjalpinulva_throw");
				}
				var spawnMessengers = Body.TempProperties.GetProperty<EcsGameTimer>("gjalpinulva_messengers");
				if (spawnMessengers != null)
				{
					spawnMessengers.Stop();
					CanSpawnMessengers = false;
					Body.TempProperties.RemoveProperty("gjalpinulva_messengers");
				}
			}
			#endregion
			if (!checkForMessangers)
			{
				if (DragonAdds.Count > 0)
				{
					foreach (GameNpc messenger in DragonAdds)
					{
						if (messenger != null && messenger.IsAlive && messenger.Brain is GjalpinulvaMessengerBrain)
							messenger.RemoveFromWorld();
					}
					foreach (GameNpc drakulv in DragonAdds)
					{
						if (drakulv != null && drakulv.IsAlive && drakulv.Brain is GjalpinulvaSpawnedAdBrain)
							drakulv.RemoveFromWorld();
					}
					DragonAdds.Clear();
				}
				checkForMessangers = true;
			}
		}

		#region Dragon IsRestless fly route activation
		if (Body.CurrentRegion.IsPM && Body.CurrentRegion.IsNightTime == false && !LockIsRestless && !Body.InCombatInLast(30000) && _lastRoamIndex < _roamingPathPoints.Count)//Dragon will start roam
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
				player.Out.SendMessage("A booming voice echoes through the canyons, 'I grow restless. Who has dared to enter my domain? I shall freeze their flesh and grind their bones to dust!'", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
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
			LockIsRestless = false;	//roam 2nd check		
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
			if(CanThrow == false && !IsRestless)
            {
				EcsGameTimer throwPlayer = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(60000, 80000));//Teleport 2-5 Players every 60-80s
				Body.TempProperties.SetProperty("gjalpinulva_throw", throwPlayer);
				CanThrow = true;
            }
			if (CanGlare == false && !Body.IsCasting && !IsRestless)
			{
				EcsGameTimer prepareGlare = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareGlare), Util.Random(40000, 60000));//Glare at target every 40-60s
				Body.TempProperties.SetProperty("gjalpinulva_glare", prepareGlare);
				CanGlare = true;
			}
			if (CanStun == false && !Body.IsCasting && !IsRestless)
			{
				EcsGameTimer prepareStun = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(PrepareStun), Util.Random(120000, 180000));//prepare Stun every 120s-180s
				Body.TempProperties.SetProperty("gjalpinulva_stun", prepareStun);
				CanStun = true;
			}
			if(Body.HealthPercent <= 50 && CanSpawnMessengers == false && !IsRestless)
            {
				EcsGameTimer spawnMessengers = new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMssengers), Util.Random(80000, 90000));//spawn messengers at 50% hp every 80/90s
				Body.TempProperties.SetProperty("gjalpinulva_messengers", spawnMessengers);
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
							player.Out.SendMessage(Body.Name + " begins flapping her wings violently. You struggle to hold your footing on the ground!", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
							switch (Util.Random(1, 5))
							{
								case 1: player.MoveTo(100, 708632, 1021688, 3721, 2499); break;//lair spawn point
								case 2: player.MoveTo(100, 713073, 1015679, 3833, 441); break;
								case 3: player.MoveTo(100, 713388, 1024499, 3833, 1372); break;
								case 4: player.MoveTo(100, 705812, 1024952, 3833, 2573); break;
								case 5: player.MoveTo(100, 706019, 1018867, 3833, 3521); break;
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
		"Odin will have to do without your aid at Ragnarök!",
		"There shall be no valkyries bearing you this day!",
		"May your corpse rot on Nastrand!",
		"My aunt has a wonderful place reserved for you in Niflheim!",
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
		if(!IsRestless && HasAggro && Body.IsAlive && RandomTarget != null && RandomTarget.IsAlive && RandomTarget.IsWithinRadius(Body, Dragon_DD.Range) && !Body.IsCasting)
        {
			Body.TargetObject = RandomTarget;
			Body.TurnTo(RandomTarget);
			Body.CastSpell(Dragon_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			string glaretext = glare_text[Util.Random(0, glare_text.Count - 1)];
			RandomTarget.Out.SendMessage(glaretext, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
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
		"Odin will have to do without your aid at Ragnarök!",
		"There shall be no valkyries bearing you this day!",
		"May your corpse rot on Nastrand!",
		"My aunt has a wonderful place reserved for you in Niflheim!",
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
			RandomTarget2.Out.SendMessage(glaretextroam, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
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
			BroadcastMessage(String.Format("{0} looks mindfully around.", Body.Name));
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

	#region Dragon Breath Big Bang
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
			"{0} takes another powerful breath as she prepares to unleash a raging blazy storm upon you!",
			"{0} bellows in rage and glares at all of the creatures attacking her.",
			"{0} noticeably winces from her wounds as she attempts to prepare for yet another life-threatening attack!"
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
		for(int i = 0; i <= Util.Random(3,5); i++)
        {
			GjalpinulvaMessenger messenger = new GjalpinulvaMessenger();
			messenger.X = 708770 + Util.Random(-100, 100);
			messenger.Y = 1021639 + Util.Random(-100, 100);
			messenger.Z = 3030;
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
				spell.ClientEffect = 5701;
				spell.Icon = 5701;
				spell.TooltipId = 5701;
				spell.Damage = 2000;
				spell.Name = "Gjalpinulva's Glare";
				spell.Range = 5000;//very long range cause dragon is flying and got big aggro
				spell.Radius = 1000;
				spell.SpellID = 11954;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Cold;
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
				spell.ClientEffect = 5701;
				spell.Icon = 5701;
				spell.TooltipId = 5701;
				spell.Damage = 1500;
				spell.Name = "Gjalpinulva's Glare";
				spell.Range = 1500;
				spell.Radius = 1000;
				spell.SpellID = 11953;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Cold;
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
				spell.ClientEffect = 5701;
				spell.Icon = 5701;
				spell.TooltipId = 5701;
				spell.Damage = 2400;
				spell.Name = "Gjalpinulva's Breath";
				spell.Range = 0;
				spell.Radius = 2000;
				spell.SpellID = 11952;
				spell.Target = "Enemy";
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Cold;
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
				spell.SpellID = 11951;
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
				spell.ClientEffect = 2976;
				spell.Icon = 5701;
				spell.TooltipId = 5701;
				spell.Duration = 120;
				spell.Value = 50;
				spell.Name = "Dragon's Breath";
				spell.Description = "Decreases a target's given resistance to Cold magic by 50%";
				spell.Range = 0;
				spell.Radius = 2000;
				spell.SpellID = 11964;
				spell.Target = "Enemy";
				spell.Type = ESpellType.ColdResistDebuff.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Cold;
				m_Dragon_Debuff = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Dragon_Debuff);
			}
			return m_Dragon_Debuff;
		}
	}
	#endregion
}
#endregion Gjalpinulva

#region Gjalpinulva's messengers
public class GjalpinulvaMessengerBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public GjalpinulvaMessengerBrain()
	{
		AggroLevel = 100;
		AggroRange = 500;
	}

	private protected bool ChoosePath = false;
	private protected bool ChoosePath1 = false;
	private protected bool ChoosePath2 = false;
	private protected bool ChoosePath3 = false;
	private protected bool ChoosePath4 = false;
	private protected bool CanSpawnDrakulvs = false;
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

	private protected bool path4point1 = false;
	private protected bool path4point2 = false;
	private protected bool path4point3 = false;

    #region Path1
    private protected void Path1()
    {
		Point3D point1 = new Point3D(710329,1019375,2824);
		Point3D point2 = new Point3D(710514, 1016616, 2893);
		Point3D point3 = new Point3D(710434, 1013684, 2783);

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
					if(CanSpawnDrakulvs==false)
                    {
						SpawnDrakulvs();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnDrakulvs = true;
                    }
				}
			}
		}
	}
    #endregion

    #region Path2
    private protected void Path2()
    {
		Point3D point1 = new Point3D(706980, 1019434, 2824);
		Point3D point2 = new Point3D(702629, 1021259, 2800);
		Point3D point3 = new Point3D(699391, 1019292, 2681);

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
					if (CanSpawnDrakulvs == false)
					{
						SpawnDrakulvs();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnDrakulvs = true;
					}
				}
			}
		}
	}
    #endregion

    #region Path3
    private protected void Path3()
	{
		Point3D point1 = new Point3D(710841, 1023038, 2824);
		Point3D point2 = new Point3D(714212, 1025142, 2782);

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
				if (CanSpawnDrakulvs == false)
				{
					SpawnDrakulvs();
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
					CanSpawnDrakulvs = true;
				}
			}
		}
	}
    #endregion

    #region Path4
    private protected void Path4()
    {
		Point3D point1 = new Point3D(706824, 1023914, 2759);
		Point3D point2 = new Point3D(708924, 1025927, 2817);
		Point3D point3 = new Point3D(712828, 1026645, 2824);

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
					if (CanSpawnDrakulvs == false)
					{
						SpawnDrakulvs();
						new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RemoveMessenger), 1000);
						CanSpawnDrakulvs = true;
					}
				}
			}
		}
	}
	#endregion

	#endregion
	private protected int RemoveMessenger(EcsGameTimer timer)
    {
		if(Body.IsAlive)
        {
			Body.RemoveFromWorld();
        }
		return 0;
    }
	private protected void SpawnDrakulvs()
	{
		for (int i = 0; i <= Util.Random(3, 5); i++)
		{
			GjalpinulvaSpawnedAdd add = new GjalpinulvaSpawnedAdd();
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
#endregion Gjalpinulva's messengers

#region Gjalpinulva's spawned adds
public class GjalpinulvaSpawnedAdBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public GjalpinulvaSpawnedAdBrain() : base()
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

	private protected bool path2point1 = false;
	private protected bool path2point2 = false;
	private protected bool path2point3 = false;

	private protected bool path3point1 = false;
	private protected bool path3point2 = false;

	private protected bool path4point1 = false;
	private protected bool path4point2 = false;
	private protected bool path4point3 = false;

    #region Path1
    private protected void Path1()
    {
		Point3D point1 = new Point3D(710646, 1016748, 2918);
		Point3D point2 = new Point3D(710546, 1018812, 2824);
		Point3D point3 = new Point3D(708814, 1021611, 3028);

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
					path1point3 = true;
			}
		}
	}
    #endregion

    #region Path2
    private protected void Path2()
	{
		Point3D point1 = new Point3D(702705, 1020839, 2818);
		Point3D point2 = new Point3D(707015, 1019589, 2824);
		Point3D point3 = new Point3D(708814, 1021611, 3028);
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
					path2point3 = true;
			}
		}
	}
    #endregion

    #region Path3
    private protected void Path3()
	{
		Point3D point1 = new Point3D(712485, 1024302, 2824);
		Point3D point2 = new Point3D(708814, 1021611, 3028);
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
				path3point2 = true;
		}
	}
    #endregion

    #region Path4
    private protected void Path4()
	{
		Point3D point1 = new Point3D(709203, 1025740, 2824);
		Point3D point2 = new Point3D(706957, 1024039, 2745);
		Point3D point3 = new Point3D(708814, 1021611, 3028);

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
					path4point3 = true;
			}
		}
	}
    #endregion

    #endregion
}
#endregion Gjalpinulva's spawned adds