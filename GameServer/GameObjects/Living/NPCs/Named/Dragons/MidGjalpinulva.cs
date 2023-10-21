using System;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS;

#region Gjalpinulva
public class MidGjalpinulva : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public MidGjalpinulva() : base() 
	{
		m_deathAnnounce = new String[] { "A soul-piercing howl echoes throughout the land, and then all is quiet." };
	}

	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Gjalpinulva Initializing...");
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

			var spawnMessengers = TempProperties.GetProperty<EcsGameTimer>("gjalpinulva_messengers");
			if (spawnMessengers != null)
			{
				spawnMessengers.Stop();
				TempProperties.RemoveProperty("gjalpinulva_messengers");
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
		if (MidGjalpinulvaBrain.IsRestless)
			return;

		base.ReturnToSpawnPoint(speed);
    }
    public override void StartAttack(GameObject target)
    {
		if (MidGjalpinulvaBrain.IsRestless)
			return;
		else
			base.StartAttack(target);
    }
	private static Point3D spawnPoint = new Point3D(708888, 1021439, 3014);
    public override ushort SpawnHeading { get => base.SpawnHeading; set => base.SpawnHeading = 2531; }
    public override Point3D SpawnPoint { get => spawnPoint; set => base.SpawnPoint = spawnPoint; }
    public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(694189);
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
		MidGjalpinulvaBrain.ResetChecks = false;
		MidGjalpinulvaBrain.IsRestless = false;
		MidGjalpinulvaBrain.LockIsRestless = false;
		MidGjalpinulvaBrain.CanSpawnMessengers = false;
		MidGjalpinulvaBrain.LockIsRestless = false;
		MidGjalpinulvaBrain.CanGlare = false;
		MidGjalpinulvaBrain.CanGlare2 = false;
		MidGjalpinulvaBrain.RandomTarget = null;
		MidGjalpinulvaBrain.RandomTarget2 = null;
		MidGjalpinulvaBrain.CanStun = false;
		MidGjalpinulvaBrain.CanThrow = false;
		MidGjalpinulvaBrain.checkForMessangers = false;
		MidGjalpinulvaBrain.DragonKaboom1 = false;
		MidGjalpinulvaBrain.DragonKaboom2 = false;
		MidGjalpinulvaBrain.DragonKaboom3 = false;
		MidGjalpinulvaBrain.DragonKaboom4 = false;
		MidGjalpinulvaBrain.DragonKaboom5 = false;
		MidGjalpinulvaBrain.DragonKaboom6 = false;
		MidGjalpinulvaBrain.DragonKaboom7 = false;
		MidGjalpinulvaBrain.DragonKaboom8 = false;
		MidGjalpinulvaBrain.DragonKaboom9 = false;
		#endregion
		MeleeDamageType = EDamageType.Crush;
		Faction = FactionMgr.GetFactionByID(781);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(781));
		MidGjalpinulvaBrain sbrain = new MidGjalpinulvaBrain();
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
				otherPlayer.Out.SendMessage($"{Name} shouts, 'Your soul now belongs to me, {player.PlayerClass.Name}!'", EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}

		base.EnemyKilled(enemy);
	}

	public override bool IsVisibleToPlayers => true;//this make dragon think all the time, no matter if player is around or not
}
#endregion Gjalpinulva

#region Gjalpinulva's messengers
public class GjalpinulvaMessenger : GameNpc
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
		Model = 626;
		Name = "Gjalpinulva's messenger";
		Size = 50;
		Level = (byte)Util.Random(50, 55);
		RespawnInterval = -1;
		Realm = ERealm.None;
		MaxSpeedBase = 225;
		Faction = FactionMgr.GetFactionByID(781);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(781));
		GjalpinulvaMessengerBrain adds = new GjalpinulvaMessengerBrain();

		if (!MidGjalpinulvaBrain.DragonAdds.Contains(this))
			MidGjalpinulvaBrain.DragonAdds.Add(this);

		SetOwnBrain(adds);
		base.AddToWorld();
		return true;
	}
	public override void DropLoot(GameObject killer) //no loot
	{
	}
	public override long ExperienceValue => 0;
}
#endregion Gjalpinulva's messengers

#region Gjalpinulva's spawned adds
public class GjalpinulvaSpawnedAdd : GameNpc
{
	public override bool IsVisibleToPlayers => true;

	public GjalpinulvaSpawnedAdd() : base() { }

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
			"drakulv executioner","drakulv disciple","drakulv soultrapper",
	};
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 120; }
    public override bool AddToWorld()
	{
		Name = adds_names[Util.Random(0, adds_names.Count - 1)];
		switch(Name)
        {
			case "drakulv executioner": Model = 625; Size = (byte)Util.Random(130, 150); break;
			case "drakulv disciple": Model = 617; Size = (byte)Util.Random(120, 140); break;
			case "drakulv soultrapper": Model = 624; Size = (byte)Util.Random(100, 120); break;
		}
		Level = (byte)Util.Random(60, 64);
		Faction = FactionMgr.GetFactionByID(781);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(781));
		RespawnInterval = -1;

		MaxSpeedBase = 225;
		GjalpinulvaSpawnedAdBrain sbrain = new GjalpinulvaSpawnedAdBrain();

		if (!MidGjalpinulvaBrain.DragonAdds.Contains(this))
			MidGjalpinulvaBrain.DragonAdds.Add(this);

		SetOwnBrain(sbrain);
		sbrain.Start();
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Gjalpinulva's spawned adds