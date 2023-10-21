using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS;

#region Golestandt
public class AlbGolestandt : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public AlbGolestandt() : base()
	{
		m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
			"A glowing light begins to form on the mound that served as {0}'s lair." };
	}

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Golestandt Initializing...");
	}
	#region Custom Methods
	public static ushort LairRadius
	{
		get { return 2000; }
	}
	/// <summary>
	/// Create dragon's lair after it was loaded from the DB.
	/// </summary>
	/// <param name="obj"></param>
	public override void LoadFromDatabase(DataObject obj)
	{
		base.LoadFromDatabase(obj);
		String[] dragonName = Name.Split(new char[] { ' ' });
		WorldMgr.GetRegion(CurrentRegionID).AddArea(new Area.Circle(String.Format("{0}'s Lair",
			dragonName[0]),
			X, Y, 0, LairRadius + 200));
	}
	public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		if (source is GamePlayer || source is GameSummonedPet)
		{
			if (!IsWithinRadius(spawnPoint,LairRadius))//dragon take 0 dmg is it's out of his lair
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
						truc.Out.SendMessage(Name + " is immune to any damage!", EChatType.CT_System,
							EChatLoc.CL_ChatWindow);
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
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	/// <summary>
	/// Post a message in the server news and award a dragon kill point for
	/// every XP gainer in the raid.
	/// </summary>
	/// <param name="killer">The living that got the killing blow.</param>
	protected void ReportNews(GameObject killer)
	{
		int numPlayers = GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE).Count;
		String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
		NewsMgr.CreateNews(message, killer.Realm, ENewsType.PvE, true);

		if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				if (player.IsEligibleToGiveMeritPoints)
				{
					GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
				}
			}
		}
	}
	/// <summary>
	/// Award dragon kill point for each XP gainer.
	/// </summary>
	/// <returns>The number of people involved in the kill.</returns>
	protected int AwardDragonKillPoint()
	{
		int count = 0;
		foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
		{
			player.KillsDragon++;
			player.Achieve(AchievementUtil.AchievementName.Dragon_Kills);
			count++;
		}
		return count;
	}
	public override void Die(GameObject killer)
	{
			// debug
			if (killer == null)
				log.Error("Dragon Killed: killer is null!");
			else
				log.Debug("Dragon Killed: killer is " + killer.Name + ", attackers:");
			bool canReportNews = true;
			// due to issues with attackers the following code will send a notify to all in area in order to force quest credit
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
			{
				player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));
				if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
				{
					if (player.Client.Account.PrivLevel == (int)EPrivLevel.Player)
						canReportNews = false;
				}
			}

			var spawnMessengers = TempProperties.GetProperty<EcsGameTimer>("golestandt_messengers");
			if (spawnMessengers != null)
			{
				spawnMessengers.Stop();
				TempProperties.RemoveProperty("golestandt_messengers");
			}

		AwardDragonKillPoint();
			base.Die(killer);

			foreach (String message in m_deathAnnounce)
			{
				BroadcastMessage(String.Format(message, Name));
			}
			if (canReportNews)
			{
				ReportNews(killer);
			}
	}
	#endregion
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40;// dmg reduction for melee dmg
			case EDamageType.Crush: return 40;// dmg reduction for melee dmg
			case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
			default: return 70;// dmg reduction for rest resists
		}
	}
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 300000; }
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		if (AlbGolestandtBrain.IsRestless)
			return;

		base.ReturnToSpawnPoint(speed);
	}
	public override void StartAttack(GameObject target)
	{
		if (AlbGolestandtBrain.IsRestless)
			return;
		else
			base.StartAttack(target);
	}
	private static Point3D spawnPoint = new Point3D(391344, 755419, 395);
	public override ushort SpawnHeading { get => base.SpawnHeading; set => base.SpawnHeading = 2071; }
	public override Point3D SpawnPoint { get => spawnPoint; set => base.SpawnPoint = spawnPoint; }
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60157497);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
		#region All bools here
		AlbGolestandtBrain.ResetChecks = false;
		AlbGolestandtBrain.IsRestless = false;
		AlbGolestandtBrain.LockIsRestless = false;
		AlbGolestandtBrain.CanSpawnMessengers = false;
		AlbGolestandtBrain.checkForMessangers = false;
		AlbGolestandtBrain.LockIsRestless = false;
		AlbGolestandtBrain.CanGlare = false;
		AlbGolestandtBrain.CanGlare2 = false;
		AlbGolestandtBrain.RandomTarget = null;
		AlbGolestandtBrain.RandomTarget2 = null;
		AlbGolestandtBrain.CanStun = false;
		AlbGolestandtBrain.CanThrow = false;
		AlbGolestandtBrain.DragonKaboom1 = false;
		AlbGolestandtBrain.DragonKaboom2 = false;
		AlbGolestandtBrain.DragonKaboom3 = false;
		AlbGolestandtBrain.DragonKaboom4 = false;
		AlbGolestandtBrain.DragonKaboom5 = false;
		AlbGolestandtBrain.DragonKaboom6 = false;
		AlbGolestandtBrain.DragonKaboom7 = false;
		AlbGolestandtBrain.DragonKaboom8 = false;
		AlbGolestandtBrain.DragonKaboom9 = false;
		#endregion
		MeleeDamageType = EDamageType.Crush;
		Faction = FactionMgr.GetFactionByID(31);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
		AlbGolestandtBrain sbrain = new AlbGolestandtBrain();
		SetOwnBrain(sbrain);
		sbrain.Start();
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}

	public override void EnemyKilled(GameLiving enemy)
	{
		if (enemy is GamePlayer player)
		{
			foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfZone(CurrentZone))
				otherPlayer.Out.SendMessage($"{Name} roars in triumph as another {player.PlayerClass.Name} falls before his might.", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}

		base.EnemyKilled(enemy);
	}

	public override bool IsVisibleToPlayers => true;//this make dragon think all the time, no matter if player is around or not
}
#endregion Golestandt

#region Golestandt's messengers
public class GolestandtMessenger : GameNpc
{
	public override bool IsVisibleToPlayers => true;

	public override int MaxHealth
	{
		get { return 1500; }
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 10; // dmg reduction for melee dmg
			case EDamageType.Crush: return 10; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 10; // dmg reduction for melee dmg
			default: return 20; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.10;
	}
	public override void StartAttack(GameObject target)//messengers do not attack, these just run to point
	{
	}
	public override bool AddToWorld()
	{
		Model = 2386;
		Name = "Golestandt's messenger";
		Size = 80;
		Level = (byte)Util.Random(50, 55);
		RespawnInterval = -1;
		Realm = ERealm.None;
		MaxSpeedBase = 225;
		Faction = FactionMgr.GetFactionByID(31);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
		GolestandtMessengerBrain adds = new GolestandtMessengerBrain();

		if (!AlbGolestandtBrain.DragonAdds.Contains(this))
			AlbGolestandtBrain.DragonAdds.Add(this);

		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
}
#endregion Golestandt's messengers

#region Golestandt's spawned adds
public class GolestandtSpawnedAdd : GameNpc
{
	public override bool IsVisibleToPlayers => true;

	public GolestandtSpawnedAdd() : base() { }

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
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 200;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.15;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override void ReturnToSpawnPoint(short speed)
	{
		return;
	}
	public override long ExperienceValue => 0;
	public override int MaxHealth
	{
		get { return 5000; }
	}
	List<string> adds_names = new List<string>()
	{
			"granite giant stonelord","granite giant pounder","granite giant outlooker",
	};
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 120; }
	public override bool AddToWorld()
	{
		Name = adds_names[Util.Random(0, adds_names.Count - 1)];
		switch (Name)
		{
			case "granite giant stonelord": Model = 2386; Size = (byte)Util.Random(150, 170); break;
			case "granite giant pounder": Model = 2386; Size = (byte)Util.Random(130, 150); break;
			case "granite giant outlooker": Model = 2386; Size = (byte)Util.Random(130, 140); break;
		}
		Level = (byte)Util.Random(60, 64);
		Faction = FactionMgr.GetFactionByID(31);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(31));
		RespawnInterval = -1;

		MaxSpeedBase = 225;
		GolestandtSpawnedAdBrain sbrain = new GolestandtSpawnedAdBrain();

		if (!AlbGolestandtBrain.DragonAdds.Contains(this))
			AlbGolestandtBrain.DragonAdds.Add(this);

		SetOwnBrain(sbrain);
		sbrain.Start();
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Golestandt's spawned adds
