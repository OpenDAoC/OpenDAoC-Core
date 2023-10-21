using System;
using Core.GS.ECS;
using Core.GS.PacketHandler;
using Core.GS.ServerProperties;

namespace Core.GS.AI.Brains;

#region Morgana
public class MorganaBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public MorganaBrain() : base()
	{
		AggroLevel = 0;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			player.Out.SendMessage(message, EChatType.CT_Say, EChatLoc.CL_ChatWindow);
		}
	}
	public void BroadcastMessage2(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
	ushort oldModel;
	ENpcFlags oldFlags;
	bool changed;

	private bool Message = false;
	private bool SpawnDemons = false;
	private bool PlayerAreaCheck = false;
	public static bool CanRemoveMorgana = false;
	private bool Morganacast = false;
	public override void Think()
	{
		if (!PlayerAreaCheck)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(1000))
			{
				if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
				{
					GS.Quests.Albion.AcademyLvl50EpicAlbQuest quest = player.IsDoingQuest(typeof(GS.Quests.Albion.AcademyLvl50EpicAlbQuest)) as GS.Quests.Albion.AcademyLvl50EpicAlbQuest;
					if (quest != null && quest.Step == 1)
					{
						SpawnDemons = true;
						player.Out.SendMessage("Ha, is this all the forces of Albion have to offer? I expected a whole army leaded by my brother Arthur, but what do they send a little group of adventurers lead by a poor " + player.PlayerClass.Name + "?",EChatType.CT_Say,EChatLoc.CL_ChatWindow);
						PlayerAreaCheck = true;
					}
				}
			}
		}
		INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(700000001);
		if(Morgana.BechardMinionCount > 0 && Morgana.BechardDemonicMinionsCount > 0 && Morgana.SilchardeMinionCount > 0 && Morgana.SilchardeDemonicMinionsCount > 0)
        {
			if(Morgana.BechardDemonicMinionsCount >= 10 && Morgana.SilchardeDemonicMinionsCount >= 10)
            {
				if(!Morganacast)
                {
					BroadcastMessage2("You sense the tower is clear of necromantic ties!");
					if (!Message)
					{
						BroadcastMessage("Morgana shouts, \"I cannot believe my creations have been undone so easily! Heed my words mortal! You may have won this battle but I shall return! On that day all who walk this realm will know what fear truly is!" +
							" The walls of Camelot shall fall and a new order, MY order, shall reign eternal!\"");
						Message = true;
					}
					foreach (GamePlayer player in Body.GetPlayersInRadius(4000))
					{
						if (player != null)
							player.Out.SendSpellCastAnimation(Body, 9103, 3);
					}
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(MorganaCast), 2000);
					Morganacast = true;
                }
            }
        }
		if (!SpawnDemons || (CanRemoveMorgana && SpawnDemons && Bechard.BechardKilled && Silcharde.SilchardeKilled))
		{
			if (changed == false)
			{
				oldFlags = Body.Flags;
				Body.Flags ^= ENpcFlags.CANTTARGET;
				Body.Flags ^= ENpcFlags.DONTSHOWNAME;
				//Body.Flags ^= GameNPC.eFlags.PEACE;

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
				SpawnBechard();
				SpawnSilcharde();
				changed = false;
			}
		}
		base.Think();
	}
	private int MorganaCast(EcsGameTimer timer)
    {
		foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
		{
			if (player != null)
				player.Out.SendSpellEffectAnimation(Body, Body, 9103, 0, false, 1);
		}
		CanRemoveMorgana = true;
		int resetTimer = Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1h to reset encounter
		new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(RestartMorgana), resetTimer);//reset whole encounter here
		return 0;
    }
	private int RestartMorgana(EcsGameTimer timer)//here we reset whole encounter
	{
		Message = false;
		PlayerAreaCheck = false;
		Morganacast = false;
		SpawnDemons = false;
		CanRemoveMorgana = false;
		Bechard.BechardKilled = false;
		Silcharde.SilchardeKilled = false;
		Morgana.SilchardeCount = 0;
		Silcharde.SilchardeKilled = false;
		Morgana.BechardCount = 0;
		Bechard.BechardKilled = false;
		Morgana.BechardDemonicMinionsCount = 0;
		Morgana.SilchardeDemonicMinionsCount = 0;
		return 0;
	}
	private void SpawnBechard()
	{
		if (Morgana.BechardCount == 0)
		{
			foreach (GameNpc mob in Body.GetNPCsInRadius(5000))
			{
				if (mob.Brain is BechardBrain)
					return;
			}
			Bechard npc = new Bechard();
			npc.X = 306044;
			npc.Y = 670253;
			npc.Z = 3028;
			npc.Heading = 3232;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.AddToWorld();
		}
	}
	private void SpawnSilcharde()
	{
		if (Morgana.SilchardeCount == 0)
		{
			foreach (GameNpc mob in Body.GetNPCsInRadius(5000))
			{
				if (mob.Brain is SilchardeBrain)
					return;
			}
			Silcharde npc = new Silcharde();
			npc.X = 306132;
			npc.Y = 669983;
			npc.Z = 3040;
			npc.Heading = 3148;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.AddToWorld();
		}
	}
}
#endregion Morgana

#region Bechard
public class BechardBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public BechardBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			foreach(GameNpc npc in Body.GetNPCsInRadius(2000))
			{
				if(npc != null && npc.IsAlive && npc.Brain is SilchardeBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 100);
				}
			}					
		}
		base.Think();
	}
}
#endregion Bechard

#region Silcharde
public class SilchardeBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public SilchardeBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
		}
		if (HasAggro && Body.TargetObject != null)
		{
			GameLiving target = Body.TargetObject as GameLiving;
			foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is BechardBrain brain)
				{
					if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
						brain.AddToAggroList(target, 100);
				}
			}
		}
		base.Think();
	}
}
#endregion Silcharde

#region Demonic Minion
public class DemonicMinionBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public DemonicMinionBrain() : base()
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
#endregion Demonic Minion