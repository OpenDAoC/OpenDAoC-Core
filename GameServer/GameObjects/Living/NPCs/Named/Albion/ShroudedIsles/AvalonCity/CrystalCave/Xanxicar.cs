using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS;

#region Xanxicar
public class Xanxicar : GameEpicBoss
{
	protected String[] m_deathAnnounce;
	public Xanxicar() : base() 
	{
		m_deathAnnounce = new String[] { "The earth lurches beneath your feet as {0} staggers and topples to the ground.",
			"A glowing light begins to form on the mound that served as {0}'s lair." };
	}
	[ScriptLoadedEvent]
	public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
	{
		if (log.IsInfoEnabled)
			log.Info("Xanxicar Initializing...");
	}
	public override int GetResist(EDamageType damageType)
	{
		switch (damageType)
		{
			case EDamageType.Slash: return 40; // dmg reduction for melee dmg
			case EDamageType.Crush: return 40; // dmg reduction for melee dmg
			case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
			default: return 70; // dmg reduction for rest resists
		}
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 350;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.20;
	}
	public override int MaxHealth
	{
		get { return 100000; }
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
	public override bool HasAbility(string keyName)
	{
		if (IsAlive && keyName == GS.Abilities.CCImmunity)
			return true;

		return base.HasAbility(keyName);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(102);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

		Faction = FactionMgr.GetFactionByID(10);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(10));

		XanxicarBrain sbrain = new XanxicarBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = false;//load from database
		SaveIntoDatabase();
		base.AddToWorld();
		return true;
	}
    #region Die/Boradcast/ReportNews/AwardDragonKillPoints
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
			count++;
		}
		return count;
	}
    #endregion
}
#endregion Xanxicar

#region Xanxicarian Champion
public class XanxicarianChampion : GameEpicNPC
{
	public XanxicarianChampion() : base() { }

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
	public override double AttackDamage(DbInventoryItem weapon)
	{
		return base.AttackDamage(weapon) * Strength / 100;
	}
	public override double GetArmorAF(EArmorSlot slot)
	{
		return 300;
	}
	public override double GetArmorAbsorb(EArmorSlot slot)
	{
		// 85% ABS is cap.
		return 0.25;
	}
	public override int MaxHealth
	{
		get { return 8000; }
	}

	public static int XanxicarianChampionCount = 0;
	public override void Die(GameObject killer)
	{
		--XanxicarianChampionCount;
		base.Die(killer);
	}
	public override bool AddToWorld()
	{
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(91);
		LoadTemplate(npcTemplate);
		Strength = npcTemplate.Strength;
		Dexterity = npcTemplate.Dexterity;
		Constitution = npcTemplate.Constitution;
		Quickness = npcTemplate.Quickness;
		Piety = npcTemplate.Piety;
		Intelligence = npcTemplate.Intelligence;
		Empathy = npcTemplate.Empathy;
		++XanxicarianChampionCount;

		Faction = FactionMgr.GetFactionByID(10);
		Faction.AddFriendFaction(FactionMgr.GetFactionByID(10));
		RespawnInterval = -1;

		XanxicarianChampionBrain sbrain = new XanxicarianChampionBrain();
		SetOwnBrain(sbrain);
		LoadedFromScript = true;
		base.AddToWorld();
		return true;
	}
}
#endregion Xanxicarian Champion