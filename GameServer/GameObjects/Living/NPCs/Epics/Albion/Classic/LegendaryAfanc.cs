using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class LegendaryAfanc : GameEpicBoss
	{
		public LegendaryAfanc() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Legendary Afanc Initializing...");
		}
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
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
			get { return 30000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13019);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(18);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));

			LegendaryAfancBrain sbrain = new LegendaryAfancBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void Die(GameObject killer)
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "AfancMinion")
						npc.Die(npc);
				}
			}
			base.Die(killer);
		}
	}
}
namespace DOL.AI.Brain
{
	public class LegendaryAfancBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LegendaryAfancBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool BringAdds = false;
		private bool CanPort = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				BringAdds = false;
				CanPort = false;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "AfancMinion")
								npc.Die(npc);
						}
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if (BringAdds == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Minions), Util.Random(15000, 35000));
					BringAdds = true;
				}
				if(CanPort == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(25000, 45000));
					CanPort = true;
                }
			}
			base.Think();
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_SystemWindow);
			}
		}
		private List<GamePlayer> Port_Enemys = new List<GamePlayer>();
		private int ThrowPlayer(ECSGameTimer timer)
        {
			if (HasAggro)
            {
				foreach(GamePlayer player in Body.GetPlayersInRadius(2500))
                {
					if(player != null)
                    {
						if (player.IsAlive && player.Client.Account.PrivLevel == 1 && !Port_Enemys.Contains(player))
							Port_Enemys.Add(player);
                    }
                }
				if(Port_Enemys.Count > 0)
                {
					GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
					if (Target != null && Target.IsAlive)
                    {					
						Target.MoveTo(Body.CurrentRegionID, 451486, 393503, 2754, 2390);
						if(Target.CharacterClass.ID != (int)ECharacterClass.Necromancer)
                        {
							Target.TakeDamage(Target, EDamageType.Falling, Target.MaxHealth / 5, 0);
							Target.Out.SendMessage("You take falling damage!", EChatType.CT_Important, EChatLoc.CL_ChatWindow);
						}
                    }
					CanPort = false;
				}
            }
			return 0;
        }
		public int Minions(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				for (int i = 0; i < Util.Random(2, 5); i++)
				{
					GameNPC add = new GameNPC();
					add.Name = Body.Name+"'s minion";
					add.Model = 607;
					add.Level = (byte)(Util.Random(38, 45));
					add.Size = (byte)(Util.Random(8, 12));
					add.X = Body.X + Util.Random(-100, 100);
					add.Y = Body.Y + Util.Random(-100, 100);
					add.Z = Body.Z;
					add.CurrentRegion = Body.CurrentRegion;
					add.Heading = Body.Heading;
					add.RespawnInterval = -1;
					add.MaxSpeedBase = 200;
					add.PackageID = "AfancMinion";
					add.Faction = FactionMgr.GetFactionByID(18);
					add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(18));
					StandardMobBrain brain = new StandardMobBrain();
					add.SetOwnBrain(brain);
					brain.AggroRange = 1000;
					brain.AggroLevel = 100;
					add.AddToWorld();
				}
			}
			BringAdds = false;
			return 0;
		}
	}
}