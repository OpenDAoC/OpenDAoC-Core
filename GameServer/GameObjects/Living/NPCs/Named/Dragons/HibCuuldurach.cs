using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS;

#region Cuuldurach
public class HibCuuldurach : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public HibCuuldurach() : base()
	{
		m_deathAnnounce = new String[] { "The hills seem to weep for the loss of their king." };
	}

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Cuuldurach the Glimmer King Initializing...");
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
			if (!IsWithinRadius(spawnPoint, LairRadius))//dragon take 0 dmg is it's out of his lair
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

			var spawnMessengers = TempProperties.GetProperty<EcsGameTimer>("cuuldurach_messengers");
			if (spawnMessengers != null)
			{
				spawnMessengers.Stop();
				TempProperties.RemoveProperty("cuuldurach_messengers");
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
		if (HibCuuldurachBrain.IsRestless)
			return;

		base.ReturnToSpawnPoint(speed);
	}
	public override void StartAttack(GameObject target)
	{
		if (HibCuuldurachBrain.IsRestless)
			return;
		else
			base.StartAttack(target);
	}
	private static Point3D spawnPoint = new Point3D(408646, 706432, 2965);
	public override ushort SpawnHeading { get => base.SpawnHeading; set => base.SpawnHeading = 1764; }
	public override Point3D SpawnPoint { get => spawnPoint; set => base.SpawnPoint = spawnPoint; }
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(678903);
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
		HibCuuldurachBrain.ResetChecks = false;
		HibCuuldurachBrain.IsRestless = false;
		HibCuuldurachBrain.LockIsRestless = false;
		HibCuuldurachBrain.CanSpawnMessengers = false;
		HibCuuldurachBrain.checkForMessangers = false;
		HibCuuldurachBrain.LockIsRestless = false;
		HibCuuldurachBrain.CanGlare = false;
		HibCuuldurachBrain.CanGlare2 = false;
		HibCuuldurachBrain.RandomTarget = null;
		HibCuuldurachBrain.RandomTarget2 = null;
		HibCuuldurachBrain.CanStun = false;
		HibCuuldurachBrain.CanThrow = false;
		HibCuuldurachBrain.DragonKaboom1 = false;
		HibCuuldurachBrain.DragonKaboom2 = false;
		HibCuuldurachBrain.DragonKaboom3 = false;
		HibCuuldurachBrain.DragonKaboom4 = false;
		HibCuuldurachBrain.DragonKaboom5 = false;
		HibCuuldurachBrain.DragonKaboom6 = false;
		HibCuuldurachBrain.DragonKaboom7 = false;
		HibCuuldurachBrain.DragonKaboom8 = false;
		HibCuuldurachBrain.DragonKaboom9 = false;
		#endregion
		MeleeDamageType = EDamageType.Slash;
		Faction = FactionMgr.GetFactionByID(83);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(83));
		HibCuuldurachBrain sbrain = new HibCuuldurachBrain();
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
				otherPlayer.Out.SendMessage($"{Name} laughs at the {player.PlayerClass.Name} who has fallen beneath his crushing blow.", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}

		base.EnemyKilled(enemy);
	}

	public override bool IsVisibleToPlayers => true;//this make dragon think all the time, no matter if player is around or not
}
#endregion Cuuldurach

#region Cuuldurach's messengers
public class CuuldurachMessenger : GameNpc
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
		Model = 2389;
		Name = "Cuuldurach's messenger";
		Size = 50;
		Level = (byte)Util.Random(50, 55);
		RespawnInterval = -1;
		Realm = ERealm.None;
		MaxSpeedBase = 225;
		Faction = FactionMgr.GetFactionByID(83);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(83));
		CuuldurachMessengerBrain adds = new CuuldurachMessengerBrain();

		if (!HibCuuldurachBrain.DragonAdds.Contains(this))
			HibCuuldurachBrain.DragonAdds.Add(this);

		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
}
#endregion Cuuldurach's messengers

#region Cuuldurach's spawned adds
public class CuuldurachSpawnedAdd : GameNpc
{
	public CuuldurachSpawnedAdd() : base() { }

	public override bool IsVisibleToPlayers => true;

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
			"glimmer geist","glimmer knight","glimmer deathwatcher",
	};
	public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
	public override short Strength { get => base.Strength; set => base.Strength = 120; }
	public override bool AddToWorld()
	{
		Name = adds_names[Util.Random(0, adds_names.Count - 1)];
		switch (Name)
		{
			case "glimmer knight": Model = 2390; Size = (byte)Util.Random(50, 55); break;
			case "glimmer deathwatcher": Model = 2389; Size = (byte)Util.Random(50, 55); break;
			case "glimmer geist": Model = 2388; Size = (byte)Util.Random(50, 55); break;
		}
		Level = (byte)Util.Random(60, 64);
		Faction = FactionMgr.GetFactionByID(83);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(83));
		RespawnInterval = -1;

		MaxSpeedBase = 225;
		CuuldurachSpawnedAdBrain sbrain = new CuuldurachSpawnedAdBrain();

		if (!HibCuuldurachBrain.DragonAdds.Contains(this))
			HibCuuldurachBrain.DragonAdds.Add(this);

		SetOwnBrain(sbrain);
		sbrain.Start();
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Cuuldurach's spawned adds
