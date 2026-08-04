using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Scripts
{
	public class SpectralProvisioner : GameEpicBoss
	{
	public SpectralProvisioner()
		: base() { }
		// The old script randomized the speed of each waypoint-to-waypoint walk between 195 and 300.
		public const short PATROL_SPEED = 250;

		private static readonly (int X, int Y, int Z)[] _patrolPoints =
		[
			(30050, 39425, 17004),
			(30940, 39418, 17004),
			(32065, 40205, 17004),
			(32075, 42378, 17004),
			(32072, 40376, 17006),
			(32967, 39369, 17007),
			(32057, 38494, 17007),
			(31022, 39382, 17006)
		];
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40;// dmg reduction for melee dmg
				case eDamageType.Crush: return 40;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GameSummonedPet)
			{
				if (damageType == eDamageType.Heat || damageType == eDamageType.Spirit || damageType == eDamageType.Cold) //take no damage
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GameSummonedPet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage("The Spectral Provisioner is immune to this form of attack.", eChatType.CT_System,eChatLoc.CL_ChatWindow);

					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override double GetArmorAF(eArmorSlot slot)
	    {
		    return 350;
	    }
		

		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == "CCImmunity")
				return true;

			return base.HasAbility(keyName);
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}

		public override short MaxSpeedBase => (short) (191 + Level * 2);
		public override int MaxHealth => 100000;

		public override int MeleeAttackRange => 180;
		public override bool AddToWorld()
		{
			Level = 77;
			Gender = eGender.Neutral;
			BodyType = 11; // undead
			TetherRange = 0;
			RoamingRange = 0;
			MaxSpeedBase = 300;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166427);
			LoadTemplate(npcTemplate);
			CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);
			SpectralProvisionerBrain sBrain = new SpectralProvisionerBrain();
			SetOwnBrain(sBrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	   
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Spectral Provisioner NPC Initializing...");
		}
		public override void StartAttack(GameObject target)
        {
        }
		public override bool IsVisibleToPlayers => true;
	}  
}

namespace DOL.AI.Brain
{
    public class SpectralProvisionerBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SpectralProvisionerBrain()
				: base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 2000;
		}
		//private bool CanAddJunk = false;
		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (Util.Chance(40) && ad != null /*&& !CanAddJunk*/ && ad.Attacker is GamePlayer && ad.Attacker != null)
			{
				//ItemTemplate sackJunk = GameServer.Database.FindObjectByKey<ItemTemplate>("sack_of_decaying_junk");
				//InventoryItem item = GameInventoryItem.Create(sackJunk);

				//foreach (GamePlayer player in Body.GetPlayersInRadius(500))
				//{
				//if (!player.IsAlive) continue;
				//item.OwnerID = player.ObjectId;
				//item.IsDropable = true;			//Make sure it's droppable
				//item.IsIndestructible = false;	//make sure it's destructible
				//player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);				
				//}				
				//new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDecayingJunk), Util.Random(25000,35000));
				//CanAddJunk = true;
				if(ad.Attacker is not GameSummonedPet)
					Body.CastSpell(SpectralDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			}
			base.OnAttackedByEnemy(ad);
		}
		//private int ResetDecayingJunk(ECSGameTimer timer)
        //{
			//CanAddJunk = false;
			//return 0;
        //}
		public override void Think()
		{
			if (Body.IsAlive)
			{
				Body.MaxSpeedBase = 300;

				if (!Body.IsMovingOnPath)
					Body.MoveOnPath(SpectralProvisioner.PATROL_SPEED);

				// if (HasAggro && Body.TargetObject != null)
				// {
				// 	foreach (GameNPC npc in Body.GetNPCsInRadius(800))
				// 	{
				// 		if (npc != null && npc.IsAlive && npc.PackageID == "ProvisionerBaf")
				// 			AddAggroListTo(npc.Brain as StandardMobBrain);
				// 	}
				// }

				if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
				{
					ClearAggroList();
					Body.Health = Body.MaxHealth;
				}
			}
			base.Think();
		}
		private Spell m_SpectralDD;
		private Spell SpectralDD
		{
			get
			{
				if (m_SpectralDD == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.Power = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 9191;
					spell.Icon = 9191;
					spell.TooltipId = 9191;
					spell.Damage = 350;
					spell.Value = 70;
					spell.Duration = 30;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Description = "Spectral Provisioner strike back attacker and makes him move 70% slower for the spell duration.";
					spell.Name = "Spectral Strike";
					spell.Range = 2500;
					spell.SpellID = 12018;
					spell.Target = eSpellTarget.ENEMY.ToString();
					spell.Type = eSpellType.DamageSpeedDecreaseNoVariance.ToString();
					m_SpectralDD = new Spell(spell, 60);
				}
				return m_SpectralDD;
			}
		}
	}
}
#region Spectral Provisioner Spawner
namespace DOL.GS
{
    public class SpectralProvisionerSpawner : GameNPC
	{
		public SpectralProvisionerSpawner() : base()
		{
		}
		public override bool AddToWorld()
		{
			Name = "Spectral Provisioner Spawner";
			GuildName = "DO NOT REMOVE";
			Level = 50;
			Model = 665;
			RespawnInterval = 5000;
			Flags = (GameNPC.eFlags)28;

			SpectralProvisionerSpawnerBrain sbrain = new SpectralProvisionerSpawnerBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
    public class SpectralProvisionerSpawnerBrain : StandardMobBrain
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public SpectralProvisionerSpawnerBrain()
			: base()
		{
			AggroLevel = 0;
			AggroRange = 500;
		}
		private bool CanSpawnProvisioner = false;
		public override void Think()
		{
			if(Body.IsAlive)
            {
				if(!CanSpawnProvisioner)
                {
					foreach(GamePlayer player in Body.GetPlayersInRadius(500))
                    {
						if(player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
							SpawnSpectralProvisioner(player);
							CanSpawnProvisioner = true;
                        }
                    }
                }
            }
			base.Think();
		}
		public void SpawnSpectralProvisioner(GamePlayer player)
		{
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc.Brain is SpectralProvisionerBrain)
					return;
			}
			SpectralProvisioner boss = new SpectralProvisioner();
			boss.X = Body.X;
			boss.Y = Body.Y;
			boss.Z = Body.Z;
			boss.Heading = Body.Heading;
			boss.CurrentRegion = Body.CurrentRegion;
			boss.AddToWorld();
			if (player != null)
				log.Debug("Player "+player.Name + " initialized Spectral Provisioner spawn event.");
		}
	}
}
#endregion