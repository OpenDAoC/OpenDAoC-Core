using System;
using Core.Database.Tables;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.Spells;
using Core.GS.World;

namespace Core.GS.AI;

#region Xanxicar
public class XanxicarBrain : StandardMobBrain
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	public XanxicarBrain() : base()
	{
		AggroLevel = 100;
		AggroRange = 600;
		ThinkInterval = 1500;
	}
    #region Check Flags/Port,DD list/broadcast
    public static GamePlayer randomtarget = null;
	public static GamePlayer RandomTarget
	{
		get { return randomtarget; }
		set { randomtarget = value; }
	}
	public static GamePlayer randomtarget2 = null;
	public static GamePlayer RandomTarget2
	{
		get { return randomtarget2; }
		set { randomtarget2 = value; }
	}
	public static bool IsTargetPicked = false;
	public static bool IsTargetPicked2 = false;
	public static bool Bomb1 = false;
	public static bool Bomb2 = false;
	public static bool Bomb3 = false;
	public static bool Bomb4 = false;
	private bool RemoveAdds = false;
    System.Collections.Generic.List<GamePlayer> Port_Enemys = new System.Collections.Generic.List<GamePlayer>();
	System.Collections.Generic.List<GamePlayer> DD_Enemys = new System.Collections.Generic.List<GamePlayer>();
	public void BroadcastMessage(String message)
	{
		foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
		}
	}
    #endregion

    #region Throw player
    public int ThrowPlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1)
					{
						if (!Port_Enemys.Contains(player))
						{
							if (player != Body.TargetObject)
								Port_Enemys.Add(player);
						}
					}
				}
			}
			if (Port_Enemys.Count > 0)
			{
				GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
				RandomTarget = Target;
				if (RandomTarget.IsAlive && RandomTarget != null && HasAggro)
				{
					BroadcastMessage(RandomTarget.Name + " is hurled into the air!");
					RandomTarget.MoveTo(62, 32338, 32387, 16539, 1830);
					Port_Enemys.Remove(RandomTarget);
				}
			}
			RandomTarget = null;//reset random target to null
			IsTargetPicked = false;
		}
		return 0;
	}
    #endregion

    #region Pick Glare Target
    public int GlarePlayer(EcsGameTimer timer)
	{
		if (Body.IsAlive)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
			{
				if (player != null)
				{
					if (player.IsAlive && player.Client.Account.PrivLevel == 1 && player.PlayerClass.ID != 12)
					{
						if (!DD_Enemys.Contains(player))
								DD_Enemys.Add(player);
					}
				}
			}
			if (DD_Enemys.Count > 0)
			{
				GamePlayer Target = DD_Enemys[Util.Random(0, DD_Enemys.Count - 1)];
				RandomTarget2 = Target;
				if (RandomTarget2.IsAlive && RandomTarget2 != null && HasAggro)
				{
					BroadcastMessage("Xanxicar preparing glare at "+ RandomTarget2.Name +"!");
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(DoGlare), 5000);
				}
			}
		}
		return 0;
	}
	public int DoGlare(EcsGameTimer timer)
	{
		GameObject oldTarget = Body.TargetObject;
		Body.TargetObject = RandomTarget2;
		Body.TurnTo(RandomTarget2);
		if (Body.TargetObject != null && RandomTarget2.IsAlive)
			Body.CastSpell(XanxicarGlare, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

		if (oldTarget != null) Body.TargetObject = oldTarget;
		RandomTarget2 = null;//reset random target to null
		IsTargetPicked2 = false;
		return 0;
	}
    #endregion

    #region PBAOE
    public int BombAnnounce(EcsGameTimer timer)
	{
		BroadcastMessage(String.Format("Xanxicar bellows in rage and prepares massive stomp at all of the creatures attacking him."));
		if (Body.IsAlive && HasAggro)
		{
			Body.StopFollowing();
			Body.CastSpell(XanxicarStomp, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
		}
		return 0;
	}
	#endregion

	#region Think()
	public override void Think()
	{
		if (!CheckProximityAggro())
		{
			//set state to RETURN TO SPAWN
			FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
			Body.Health = Body.MaxHealth;
			RandomTarget = null;//throw
			RandomTarget2 = null;//glare
			IsTargetPicked = false;//throw
			IsTargetPicked2 = false;//glare
			Bomb1 = false;
			Bomb2 = false;
			Bomb3 = false;
			Bomb4 = false;
			SpawnAddsOnce = false;
			CheckForSingleAdd = false;
			XanxicarianChampion.XanxicarianChampionCount = 0;
			if (!RemoveAdds)
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is XanxicarianChampionBrain)
							npc.RemoveFromWorld();
					}
				}
				RemoveAdds = true;
			}
		}
		if (Body.InCombat && Body.IsAlive && HasAggro)
		{
			RemoveAdds = false;
			if (IsTargetPicked == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(ThrowPlayer), Util.Random(20000, 40000));//timer to port and pick player
				IsTargetPicked = true;
			}
			if (IsTargetPicked2 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(GlarePlayer), Util.Random(35000, 45000));//timer to glare at player
				IsTargetPicked2 = true;
			}
			if(Body.HealthPercent <= 80 && Bomb1 == false)
            {
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(BombAnnounce), 1000);
				Bomb1 = true;
			}
			if (Body.HealthPercent <= 60 && Bomb2 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(BombAnnounce), 1000);
				Bomb2 = true;
			}
			if (Body.HealthPercent <= 40 && Bomb3 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(BombAnnounce), 1000);
				Bomb3 = true;
			}
			if (Body.HealthPercent <= 20 && Bomb4 == false)
			{
				new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(BombAnnounce), 1000);
				Bomb4 = true;
			}
			if(Body.HealthPercent<=50)
            {
				if(SpawnAddsOnce==false && XanxicarianChampion.XanxicarianChampionCount == 0)
                {
					SpawnAdds();
					SpawnAddsOnce = true;
                }
				if(SpawnAddsOnce && CheckForSingleAdd==false && XanxicarianChampion.XanxicarianChampionCount == 0)
                {
					new EcsGameTimer(Body, new EcsGameTimer.EcsTimerCallback(SpawnMoreAdds), Util.Random(15000, 25000));//spawn 1 add every 25-35s
					CheckForSingleAdd = true;
                }
            }
		}
		base.Think();
	}
    #endregion

    #region adds
    public static bool SpawnAddsOnce = false;
	public static bool CheckForSingleAdd = false;
	public void SpawnAdds()
    {
		for (int i = 0; i < 5; i++)
		{
			XanxicarianChampion Add = new XanxicarianChampion();
			Add.X = Body.X + Util.Random(-150, 150);
			Add.Y = Body.Y + Util.Random(-150, 150);
			Add.Z = Body.Z;
			Add.Level = 65;
			Add.CurrentRegion = Body.CurrentRegion;
			Add.Heading = Body.Heading;
			Add.AddToWorld();
		}
	}
	public int SpawnMoreAdds(EcsGameTimer timer)
    {
		if (XanxicarianChampion.XanxicarianChampionCount == 0 && HasAggro)
		{
			XanxicarianChampion Add = new XanxicarianChampion();
			Add.X = Body.X + Util.Random(-150, 150);
			Add.Y = Body.Y + Util.Random(-150, 150);
			Add.Z = Body.Z;
			Add.Level = 65;
			Add.CurrentRegion = Body.CurrentRegion;
			Add.Heading = Body.Heading;
			Add.AddToWorld();
			CheckForSingleAdd = false;
		}
		return 0;
    }
    #endregion

    #region spells
    private Spell m_XanxicarStomp;
	private Spell XanxicarStomp
	{
		get
		{
			if (m_XanxicarStomp == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 6;
				spell.RecastDelay = 10;
				spell.ClientEffect = 1695;
				spell.Icon = 1695;
				spell.TooltipId = 1695;
				spell.Damage = 2500;
				spell.Name = "Xanxicar's Stomp";
				spell.Range = 0;
				spell.Radius = 2500;
				spell.SpellID = 11802;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Energy;
				m_XanxicarStomp = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_XanxicarStomp);
			}
			return m_XanxicarStomp;
		}
	}
	private Spell m_XanxicarGlare;
	private Spell XanxicarGlare
	{
		get
		{
			if (m_XanxicarGlare == null)
			{
				DbSpell spell = new DbSpell();
				spell.AllowAdd = false;
				spell.CastTime = 0;
				spell.RecastDelay = 2;
				spell.ClientEffect = 1678;
				spell.Icon = 1678;
				spell.TooltipId = 1678;
				spell.Damage = 1500;
				spell.Name = "Xanxicar's Glare";
				spell.Range = 1500;
				spell.Radius = 450;
				spell.SpellID = 11803;
				spell.Target = ESpellTarget.ENEMY.ToString();
				spell.Type = ESpellType.DirectDamageNoVariance.ToString();
				spell.Uninterruptible = true;
				spell.DamageType = (int)EDamageType.Energy;
				m_XanxicarGlare = new Spell(spell, 70);
				SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_XanxicarGlare);
			}
			return m_XanxicarGlare;
		}
	}
    #endregion
}
#endregion Xanxicar

#region Xanxicarian Champion
public class XanxicarianChampionBrain : StandardMobBrain
{
	public XanxicarianChampionBrain()
		: base()
	{
		AggroLevel = 100;
		AggroRange = 1500;
		ThinkInterval = 1500;
	}
	public override void Think()
	{
		base.Think();
	}
}
#endregion Xanxicarian Champion