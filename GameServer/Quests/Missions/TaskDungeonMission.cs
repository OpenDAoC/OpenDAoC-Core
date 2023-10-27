using System;
using System.Reflection;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.World;
using log4net;

namespace Core.GS.Quests;

public class TaskDungeonMission : AMission
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    public enum ETdMissionType : int
	{
		Clear = 0,
		Boss = 1,
		Specific = 2,
	}

	public enum EDungeonType : int
	{
		Melee = 0,
		Ranged = 1,
	}

	private EDungeonType m_dungeonType = EDungeonType.Melee;
	public EDungeonType DungeonType
	{
		get { return m_dungeonType; }
	}

	private ETdMissionType m_missionType;
	public ETdMissionType TDMissionType
	{
		get { return m_missionType; }
	}

	private TaskDungeonInstance m_taskRegion;
    public TaskDungeonInstance TaskRegion
	{
		get { return m_taskRegion; }
	}

	private int m_current = 0;
	public int Current
	{
		get { return m_current; }
	}

	private long m_total = 0;
	public long Total
	{
		get { return m_total; }
	}

	private string m_bossName = "";
	public string BossName
	{
		get { return m_bossName; }
	}

	private string m_targetName = "";
	public string TargetName
	{
		get { return m_targetName; }
	}
	
	private bool[] m_mobIsAlive;

	public TaskDungeonMission(object owner, EDungeonType dungeonType = EDungeonType.Ranged)
		: base(owner)
	{
        log.Info("INFO: Successfully entered TaskDungeonMission!");
		GamePlayer player = owner as GamePlayer;

        if (owner is GroupUtil)
        {
            player = (owner as GroupUtil).Leader;
            //Assign the mission to the group.
            (owner as GroupUtil).Mission = this;
        }

		if (player == null)
			return;

		//check level range and get region id from it
		ushort rid = GetRegionFromLevel(player.Level, player.Realm, dungeonType);

        TaskDungeonInstance instance = (TaskDungeonInstance)WorldMgr.CreateInstance(rid, typeof(TaskDungeonInstance));
        m_taskRegion = instance;
        instance.Mission = this;

		//Dinberg: I've removed instance level, and have commented this out so it compiles.
        //I dont have time to implement the rest right now, 
        //m_taskRegion.InstanceLevel = GetLevelFromPlayer(player);

        //Infact, this clearly isnt in use. I'll fix it to use the new instance system, and then itll work.
        //Do that later this week ^^.

        
        //Lets load the region from the InstanceXElementDB!


        //First we get the instance keyname.
        string keyname = "TaskDungeon" + rid + ".1"; //TODO; variations, eg .2, .3 etc.
        instance.LoadFromDatabase(keyname);

        //Now, search for the boss and possible targets in the instance!
        foreach (GameNpc npc in instance.Objects)
        {
            if (npc == null)
                continue;

            if (npc.Name.ToLower() != npc.Name)
            {
                if (m_bossName == "")
                    m_bossName = npc.Name; //Some instances have multiple bosses, eg Gregorian - why break?
                else if (Util.Chance(50))
                    m_bossName = npc.Name;
            } //else what if we aren't looking at a boss, but a normal mob?
            else
                if (Util.Chance(20) || m_targetName == "")
                    m_targetName = npc.Name;
        }

		int specificCount = 0;
        
        //Draw the mission type before we do anymore counting...
        if (Util.Chance(40) && m_bossName != "")
            m_missionType = ETdMissionType.Boss;
        else if (Util.Chance(20) && m_targetName != "")
            m_missionType = ETdMissionType.Specific;
        else
            m_missionType = ETdMissionType.Clear;
            
        //Now, count if we need to.
        if (m_missionType != ETdMissionType.Boss)
        {
            foreach (GameNpc entry in instance.Objects)
            {
                if (entry == null)
                    continue;

                //Now, if we want all mobs, get all mobs...
                if (m_missionType == ETdMissionType.Clear)
                    specificCount++;
                else if (entry.Name == m_targetName) 
                    //only count target mobs for specific dungeons.
                    specificCount++;
            }
        }

        //append the count to the total!
        m_total = specificCount;
        
        //Set the mission description again if owner is group, otherwise
        //mission description is always "Clear" before entering the dungeon.
        if (owner is GroupUtil)
            UpdateMission();
        
        m_mobIsAlive = new bool[m_total];
        for(int i = 0; i < m_total; i++)
            m_mobIsAlive[i] = true;
	}

    //Dinberg: removed this void. Handled in TaskDungeonInstance
	//private static byte GetLevelFromPlayer(GamePlayer player)

	#region Albion Task Instances
    //Burial Tomb
	private static ushort[] burial_tomb_long = new ushort[] { 295, 400, 402, 404 };
	private static ushort[] burial_tomb_laby = new ushort[] { 293, 294, 401, 403 };
    //Forgotten Mines
    private static ushort[] forgotten_mines_long = new ushort[] { 256, 257, 258, };
    private static ushort[] forgotten_mines_laby = new ushort[] { 410, 411, 412, 413, 414 };
    //Desecrated Ground
    private static ushort[] desecrated_grounds_long = new ushort[] { 297, 298, 409 };
    private static ushort[] desecrated_grounds_laby = new ushort[] { 296, 405, 406, 407, 408 };
    //Funerary Hall
    private static ushort[] funerary_hall_long = new ushort[] { 379, 382, 383, 419 };
    private static ushort[] funerary_hall_laby = new ushort[] { 415, 416, 417, 418 };
    //Sundered Tombs
    private static ushort[] sundered_tombs_long = new ushort[] { 386, 387, 388, };
    private static ushort[] sundered_tombs_laby = new ushort[] { 420, 421, 422, 423, 424 };
    #endregion
    #region Midgard Task Instances
    //Damp Cavern
	private static ushort[] damp_cavern_long = new ushort[] { 278, 279, 280, 301 };
	private static ushort[] damp_cavern_laby = new ushort[] { 300, 302, 303, 304 };
    //Forgotten Sepulchre
    private static ushort[] forgotten_sepulchre_long = new ushort[] { 281, 282, 283, 307 };
    private static ushort[] forgotten_sepulchre_laby = new ushort[] { 305, 306, 308, 309 };
    //The Concealed Guardhouse
    private static ushort[] the_concealed_guardhouse_long = new ushort[] { 284, 285, 286 };
    private static ushort[] the_concealed_guardhouse_laby = new ushort[] { 310, 311, 312, 313, 314 };
    //The Gossamer Grotto
    private static ushort[] the_gossamer_grotto_long = new ushort[] { 287, 288, 289 };
    private static ushort[] the_gossamer_grotto_laby = new ushort[] { 315, 316, 317, 318, 319 };
    //Underground Tunnel
    private static ushort[] underground_tunnel_long = new ushort[] { 290, 291, 292 };
    private static ushort[] underground_tunnel_laby = new ushort[] { 320, 321, 322, 323, 324 };
    #endregion
    #region Hibernia Task Instances
    //The Cursed Barrow
	private static ushort[] the_cursed_barrow_long = new ushort[] { 427, 428, 431, 454 };
	private static ushort[] the_cursed_barrow_laby = new ushort[] { 450, 451, 452, 453 };
    //Dismal Grotto
    private static ushort[] dismal_grotto_long = new ushort[] { 432, 441, 444, 459 };
    private static ushort[] dismal_grotto_laby = new ushort[] { 455, 456, 457, 458 };
    //The Accursed Caves
    private static ushort[] the_accursed_caves_long = new ushort[] { 464, 477, 478, 481 };
    private static ushort[] the_accursed_caves_laby = new ushort[] { 460, 461, 462, 463 };
    //Dark Cavern
    private static ushort[] dark_cavern_long = new ushort[] { 445, 448, 449 };
    private static ushort[] dark_cavern_laby = new ushort[] { 465, 466, 467, 468, 469 };
    //Unused Mine
    private static ushort[] unused_mine_long = new ushort[] { 474, 482, 485, 486 };
    private static ushort[] unused_mine_laby = new ushort[] { 048, 471, 472, 473 };
    #endregion

	private static ushort GetRegionFromLevel(byte level, ERealm realm, EDungeonType dungeonType)
	{
        //return 286;

        //TODO: fill this properly for all levels
        //Done by Argoras: filled all the Values for possible Realms and levels

        if (level <= 10)
        {
            switch (realm)
            {
                case ERealm.Albion:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(burial_tomb_long);
                	else
                		return GetRandomRegion(burial_tomb_laby);
                case ERealm.Midgard:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(damp_cavern_long);
                	else
                		return GetRandomRegion(damp_cavern_laby);
                case ERealm.Hibernia:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(the_cursed_barrow_long);
                	else
                		return GetRandomRegion(the_cursed_barrow_laby);
            }
        }
        else if (level > 10 && level <= 20)
        {
            switch (realm)
            {
                case ERealm.Albion:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(forgotten_mines_long);
                	else
                		return GetRandomRegion(forgotten_mines_laby);
                case ERealm.Midgard:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(forgotten_sepulchre_long);
                	else
                		return GetRandomRegion(forgotten_sepulchre_laby);
                case ERealm.Hibernia:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(dismal_grotto_long);
                	else
                		return GetRandomRegion(dismal_grotto_laby);
            }
        }
        else if (level > 20 && level <= 30)
        {
            switch (realm)
            {
                case ERealm.Albion:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(desecrated_grounds_long);
                	else
                		return GetRandomRegion(desecrated_grounds_laby);
                case ERealm.Midgard:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(the_concealed_guardhouse_long);
                	else
                		return GetRandomRegion(the_concealed_guardhouse_laby);
                case ERealm.Hibernia:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(the_accursed_caves_long);
                	else
                		return GetRandomRegion(the_accursed_caves_laby);
            }
        }
        else if (level > 30 && level <= 40)
        {
            switch (realm)
            {
                case ERealm.Albion:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(funerary_hall_long);
                	else
                		return GetRandomRegion(funerary_hall_laby);
                case ERealm.Midgard:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(the_gossamer_grotto_long);
                	else
                		return GetRandomRegion(the_gossamer_grotto_laby);
                case ERealm.Hibernia:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(dark_cavern_long);
                	else
                		return GetRandomRegion(dark_cavern_laby);
            }
        }
        else if (level > 40)
        {
            switch (realm)
            {
                case ERealm.Albion:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(sundered_tombs_long);
                	else
                		return GetRandomRegion(sundered_tombs_laby);
                case ERealm.Midgard:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(underground_tunnel_long);
                	else
                		return GetRandomRegion(underground_tunnel_laby);
                case ERealm.Hibernia:
                	if(dungeonType == EDungeonType.Ranged)
                        return GetRandomRegion(unused_mine_long);
                	else
                		return GetRandomRegion(unused_mine_laby);
            }
        }
        return 0;
	}

	private static ushort GetRandomRegion(ushort[] regions)
	{
		return regions[Util.Random(0, regions.Length - 1)];
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		if (e != GameLivingEvent.EnemyKilled)
			return;
		if (sender is GamePlayer && (sender as GamePlayer).CurrentRegion != m_taskRegion)
			return;

		EnemyKilledEventArgs eargs = args as EnemyKilledEventArgs;

		switch (m_missionType)
		{
			case ETdMissionType.Boss:
				{
					if (eargs.Target.Name == m_bossName)
						FinishMission();
					break;
				}
			case ETdMissionType.Specific:
				{
					if (eargs.Target.Name == m_targetName)
					{
						if(m_mobIsAlive[eargs.Target.ObjectID - 1])
						{
							m_mobIsAlive[eargs.Target.ObjectID - 1] = false;
							m_current++;
							UpdateMission();
							if (m_current == m_total)
								FinishMission();
							else
							{
                                /* - Dinberg, disabled this. Messages extremely annoying.
								if (m_owner is GamePlayer)
								{
									(m_owner as GamePlayer).Out.SendMessage((m_total - m_current) + " " + m_targetName + " Left", eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_ChatWindow);
								}
								else if (m_owner is Group)
								{
									foreach (GamePlayer player in (m_owner as Group).GetPlayersInTheGroup())
									{
										player.Out.SendMessage((m_total - m_current) + " " + m_targetName + " Left", eChatType.CT_ScreenCenter, eChatLoc.CL_ChatWindow);
									}
								}
                                 */
							}
						}
					}
					break;
				}
			case ETdMissionType.Clear:
				{
					if(m_mobIsAlive[eargs.Target.ObjectID - 1])
					{
						m_mobIsAlive[eargs.Target.ObjectID - 1] = false;
						m_current++;
						UpdateMission();
						if (m_current == m_total)
							FinishMission();
						else
						{
                            /*
							if (m_owner is GamePlayer)
							{
								(m_owner as GamePlayer).Out.SendMessage((m_total - m_current) + " Creatures Left", eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_ChatWindow);
							}
							else if (m_owner is Group)
							{
								foreach (GamePlayer player in (m_owner as Group).GetPlayersInTheGroup())
								{
									player.Out.SendMessage((m_total - m_current) + " Creatures Left", eChatType.CT_ScreenCenter, eChatLoc.CL_ChatWindow);
								}
							}
                             */
						}						   	
					}
					break;
				}
		}
	}

	/*
	 * [Task] You have been asked to kill Dralkden the Thirster in the nearby caves.
	 * [Task] You have been asked to clear the nearby caves.
	 * [Task] You have been asked to clear the nearby caves. 19 creatures left!
	 * [Task] You have been asked to kill 6 acidic clouds in the nearby caves!
	 */

	public override string Description
	{
		get
		{
			switch (m_missionType)
			{
				case ETdMissionType.Boss: return "You have been asked to kill " + m_bossName + " in the nearby caves.";
				case ETdMissionType.Specific: return "You have been asked to kill " + m_total + " " + m_targetName + " in the nearby caves.";
				case ETdMissionType.Clear:
					{
						//Additional check if region is null in case of group mission.
						//Otherwise else condition is used with m_total = 0.
						if ((m_owner is GamePlayer && (m_owner as GamePlayer).CurrentRegion != m_taskRegion) ||
						    (m_owner is GroupUtil && m_taskRegion == null))
                        {
                            return "You have been asked to clear the nearby caves.";
                        }
                        else
                        {
                            bool test = m_total - m_current == 1;
                            return "You have been asked to clear the nearby caves. " + (m_total - m_current) + " creature" + (test == true ? "" : "s") + " left!";
                        }
                    }
				default: return "No description for mission type " + m_missionType.ToString();
			}
		}
	}

	public override long RewardRealmPoints
	{
		get { return 0; }
	}

	public override long RewardMoney
	{
		get {
			GamePlayer player = m_owner as GamePlayer;
			if (m_owner is GroupUtil)
				player = (m_owner as GroupUtil).Leader;
			return player.Level * player.Level * 100;
		}
	}

	private int XPMagicNumber
	{
		get
		{
			switch (m_missionType)
			{
				case ETdMissionType.Clear: return 75;
				case ETdMissionType.Boss:
				case ETdMissionType.Specific: return 50;
			}
			return 0;
		}
	}

	public override long RewardXP
	{
		get
		{
			GamePlayer player = m_owner as GamePlayer;
			if (m_owner is GroupUtil)
				player = (m_owner as GroupUtil).Leader;
			long amount = XPMagicNumber * player.Level;
			if (player.Level > 1)
				amount += XPMagicNumber * (player.Level - 1);
			return amount;
			/*
			long XPNeeded = (m_owner as GamePlayer).ExperienceForNextLevel - (m_owner as GamePlayer).ExperienceForCurrentLevel;
			return (long)(XPNeeded * 0.50 / (m_owner as GamePlayer).Level); // 50% of total xp for level
			 */
		}
	}

	public override void FinishMission()
	{
		if (m_owner is GamePlayer)
		{
			(m_owner as GamePlayer).Out.SendMessage("Mission Complete", EChatType.CT_ScreenCenter, EChatLoc.CL_ChatWindow);
		}
		else if (m_owner is GroupUtil)
		{
			foreach (GamePlayer player in (m_owner as GroupUtil).GetPlayersInTheGroup())
			{
				player.Out.SendMessage("Mission Complete", EChatType.CT_ScreenCenter, EChatLoc.CL_ChatWindow);
			}
		}
		base.FinishMission();
	}
}

//This part is done by Dinberg, but the original source im not sure of. I'm trying to tweak it the the instance
//system I've developed, and this script was partly finished so I adopted it ^^. 
public class TaskDungeonInstance : Instance
{
    public TaskDungeonInstance(ushort ID, RegionData dat) : base(ID, dat) { }

    private TaskDungeonMission m_mission;
    /// <summary>
    /// Gets/Sets the Mission of this instance.
    /// </summary>
    public TaskDungeonMission Mission
    {
        get { return m_mission; }
        set { m_mission = value; }
    }

    private int m_level;

    //Change instance level...
    //I've checked, this should be called correctly: player will be added/removed in time.
    public override void OnPlayerEnterInstance(GamePlayer player)
    {
        base.OnPlayerEnterInstance(player);
        UpdateInstanceLevel();

        //The player will not yet be in the instance, so wont receive the relevant text.
        player.Out.SendMessage("You have entered " + Description + ".", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        player.Out.SendMessage("This instance is currently level " + m_level + ".", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
    }

    public override void OnPlayerLeaveInstance(GamePlayer player)
    {
        base.OnPlayerLeaveInstance(player);
        UpdateInstanceLevel();
        //Expire task from player...
        if (player.Mission == m_mission) player.Mission.ExpireMission();
    }


    //This void is outside of Instance,
    //because i want people to think carefully about how they change levels in their instance.
    public void UpdateInstanceLevel()
    {
        m_level = (byte) GetInstanceLevel();

        // Set all mobs to that level.
        if (m_level > 0)
        {
            foreach (GameObject obj in Objects)
            {
                if (obj == null)
                    continue;

                if (obj is not GameNpc npc)
                    continue;

                npc.Level = (byte) m_level;
            }

            foreach (GamePlayer player in ClientService.GetPlayersOfRegion(this))
                player.Out.SendMessage($"This instance is now level {m_level}.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
        }
    }

    /// <summary>
    /// Expire the missions - the instance has exploded.
    /// </summary>
    public override void OnCollapse()
    {
        //We expire the mission as players can no longer reach or access the region once collapsed.
        if (Mission != null)
        Mission.ExpireMission();
        base.OnCollapse();
    }
}