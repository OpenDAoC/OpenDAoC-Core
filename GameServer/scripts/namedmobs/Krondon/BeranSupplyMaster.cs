using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class BeranSupplyMaster : GameEpicBoss
	{
		public BeranSupplyMaster() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Beran the Supply Master Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 80;// dmg reduction for rest resists
			}
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 700;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.45;
		}
		public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158369);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			Level = Convert.ToByte(npcTemplate.Level);
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			BeranSupplyMasterBrain sbrain = new BeranSupplyMasterBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(2500))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Name.ToLower() == "onstal hyrde" && npc.RespawnInterval == -1)
						npc.Die(npc);
				}
			}
			base.Die(killer);
        }
    }
}
namespace DOL.AI.Brain
{
	public class BeranSupplyMasterBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BeranSupplyMasterBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool Ignite_Barrel = false;
		public static bool BringAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				Ignite_Barrel = false;
				BringAdds = false;
				foreach(GameNPC npc  in Body.GetNPCsInRadius(2500))
                {
					if(npc != null)
                    {
						if (npc.IsAlive && npc.Name.ToLower() == "onstal hyrde" && npc.RespawnInterval == -1)
							npc.Die(npc);
                    }
                }
			}
			if (HasAggro)
			{
				if(Ignite_Barrel == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(IgniteBarrel), Util.Random(15000, 35000));
					Ignite_Barrel = true;
                }
				if (BringAdds == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CallForHelp), Util.Random(35000, 65000));
					BringAdds = true;
				}
			}
			base.Think();
		}
		public int CallForHelp(ECSGameTimer timer)
        {
			if (HasAggro)
			{
				for (int i = 0; i < Util.Random(2, 4); i++)
				{
					GameNPC add = new GameNPC();
					add.Name = "Onstal Hyrde";
					add.Model = 919;
					add.Level = (byte)(Util.Random(64, 68));
					add.Size = (byte)(Util.Random(100, 115));
					add.X = Body.X + Util.Random(-100, 100);
					add.Y = Body.Y + Util.Random(-100, 100);
					add.Z = Body.Z;
					add.CurrentRegion = Body.CurrentRegion;
					add.Heading = Body.Heading;
					add.RespawnInterval = -1;
					add.MaxSpeedBase = 225;
					add.Faction = FactionMgr.GetFactionByID(8);
					add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));
					StandardMobBrain brain = new StandardMobBrain();
					add.SetOwnBrain(brain);
					brain.AggroRange = 1000;
					brain.AggroLevel = 100;
					add.AddToWorld();
				}
				BroadcastMessage(String.Format(Body.Name + " yells for help."));
			}
			BringAdds = false;
			return 0;
        }
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public int IgniteBarrel(ECSGameTimer timer)
        {
			if (HasAggro)
			{
				BarrelExplosive npc = new BarrelExplosive();
				switch (Util.Random(1, 5))
				{
					case 1:
						npc.X = 49409;
						npc.Y = 26596;
						npc.Z = 17161;
						break;
					case 2:
						npc.X = 48547;
						npc.Y = 26031;
						npc.Z = 17156;
						break;
					case 3:
						npc.X = 48508;
						npc.Y = 26695;
						npc.Z = 17159;
						break;
					case 4:
						npc.X = 48484;
						npc.Y = 27274;
						npc.Z = 17159;
						break;
					case 5:
						npc.X = 49226;
						npc.Y = 27325;
						npc.Z = 17247;
						break;
				}
				npc.RespawnInterval = -1;
				npc.Heading = Body.Heading;
				npc.CurrentRegion = Body.CurrentRegion;
				npc.AddToWorld();
				BroadcastMessage(String.Format(Body.Name + " ignites barrel."));
				Ignite_Barrel = false;
				Body.TurnTo(npc);
				Body.Emote(eEmote.LetsGo);
			}
			return 0;
		}
	}
}
//////////////////////////////////////////////////////////////////////Barrel-Explosion-Mob///////////////////////////////////////////////////
namespace DOL.GS
{
	public class BarrelExplosive : GameNPC
	{
		public BarrelExplosive() : base() { }

        public override void StartAttack(GameObject target)
        {
        }
        public override int MaxHealth
		{
			get { return 20000; }
		}
		public override bool AddToWorld()
		{
			Model = 665;
			Name = "Explosion";
			Dexterity = 200;
			Piety = 200;
			Intelligence = 200;
			Empathy = 200;
			Level = 80;
			RespawnInterval = -1;
			Size = 100;
			Flags ^= eFlags.DONTSHOWNAME;
			Flags ^= eFlags.CANTTARGET;
			Flags ^= eFlags.STATUE;

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			BarrelExplosiveBrain sbrain = new BarrelExplosiveBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			bool success = base.AddToWorld();
			if (success)
			{
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Explode), 8000); //8 seconds until this will explode and deal heavy heat dmg
			}
			return success;
		}

        protected int Show_Effect(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				foreach (GamePlayer player in GetPlayersInRadius(8000))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(this, this, 5976, 0, false, 0x01);
				}
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1200);
			}
			return 0;
		}
		protected int DoCast(ECSGameTimer timer)
		{
			if (IsAlive)
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1200);
			return 0;
		}
		protected int Explode(ECSGameTimer timer)
		{
			if (IsAlive)
			{
				SetGroundTarget(X, Y, Z);			
				CastSpell(Barrel_aoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
				new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(KillBomb), 500);
			}
			return 0;
		}
		public int KillBomb(ECSGameTimer timer)
		{
			if (IsAlive)
				RemoveFromWorld();
			return 0;
		}
		private Spell m_Barrel_aoe;
		private Spell Barrel_aoe
		{
			get
			{
				if (m_Barrel_aoe == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2308;
					spell.Icon = 2308;
					spell.TooltipId = 2308;
					spell.Damage = 1200;
					spell.Name = "Explosion";
					spell.Radius = 1000;
					spell.Range = 1000;
					spell.SpellID = 11880;
					spell.Target = eSpellTarget.Area.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_Barrel_aoe = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Barrel_aoe);
				}
				return m_Barrel_aoe;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class BarrelExplosiveBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BarrelExplosiveBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}