using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class SpiritOfLordEmthoro : GameEpicBoss
	{
		public SpiritOfLordEmthoro() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Spirit of Lord Emthoro Initializing...");
		}
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
			if (source is GamePlayer || source is GamePet)
			{
				Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
				if (!source.IsWithinRadius(spawn, 440)) //take no damage
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System,
							eChatLoc.CL_ChatWindow);

					base.TakeDamage(source, damageType, 0, 0);
					return;
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
		}
		public override double AttackDamage(InventoryItem weapon)
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override int MaxHealth
		{
			get { return 30000; }
		}
		public override void Die(GameObject killer)
		{
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
					npc.Die(this);
			}
			base.Die(killer);
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(64);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

			SpiritOfLordEmthoroBrain sbrain = new SpiritOfLordEmthoroBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		public override void DealDamage(AttackData ad)
		{
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0)
				Health += ad.Damage;
			base.DealDamage(ad);
		}
	}
}
namespace DOL.AI.Brain
{
	public class SpiritOfLordEmthoroBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public SpiritOfLordEmthoroBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool CanSpawnAdd = false;
		private bool RemoveAdds = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				CanSpawnAdd = false;
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
				Body.MaxSpeedBase = npcTemplate.MaxSpeed;
				if (!RemoveAdds)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
					{
						if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
							npc.Die(Body);
					}
					RemoveAdds = true;
				}
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(3000))
				{
					if (npc != null && npc.IsAlive && npc.RespawnInterval == -1 && npc.PackageID == "EmthoroAdd")
							AddAggroListTo(npc.Brain as StandardMobBrain);
				}
				Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);				
				INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166454);
				if (target != null)
				{
					if (!target.IsWithinRadius(spawn, 420))
						Body.MaxSpeedBase = 0;
					else
						Body.MaxSpeedBase = npcTemplate.MaxSpeed;
				}
				if(CanSpawnAdd == false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdd), Util.Random(25000, 40000));
					CanSpawnAdd = true;
                }
				Body.SetGroundTarget(Body.X, Body.Y, Body.Z);
				Body.CastSpell(LifedrianPulse, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			}
			base.Think();
		}
		private int SpawnAdd(ECSGameTimer timer)
        {
			if (HasAggro && Body.IsAlive)
			{
				GameNPC add = new GameNPC();
				add.Name = Body.Name + "'s servant";
				switch(Util.Random(1,2))
                {
					case 1: add.Model = 814; break;//orc
					case 2: add.Model = 921; break;//zombie
				}				
				add.Size = (byte)Util.Random(55, 65);
				add.Level = (byte)Util.Random(55, 59);
				add.Strength = 150;
				add.Quickness = 80;
				add.MeleeDamageType = eDamageType.Crush;
				add.MaxSpeedBase = 225;
				add.PackageID = "EmthoroAdd";
				add.RespawnInterval = -1;
				add.X = Body.SpawnPoint.X + Util.Random(-100, 100);
				add.Y = Body.SpawnPoint.Y + Util.Random(-100, 100);
				add.Z = Body.SpawnPoint.Z;
				add.CurrentRegion = Body.CurrentRegion;
				add.Heading = Body.Heading;
				add.Faction = FactionMgr.GetFactionByID(64);
				add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
				StandardMobBrain brain = new StandardMobBrain();
				add.SetOwnBrain(brain);
				brain.AggroRange = 800;
				brain.AggroLevel = 100;
				add.AddToWorld();
			}
			CanSpawnAdd = false;
			return 0;
        }
		private Spell m_LifedrianPulse;
		private Spell LifedrianPulse
		{
			get
			{
				if (m_LifedrianPulse == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 10;
					spell.ClientEffect = 14352;
					spell.Icon = 14352;
					spell.TooltipId = 14352;
					spell.Damage = 80;
					spell.Name = "Lifedrain Pulse";
					spell.Range = 1500;
					spell.Radius = 440;
					spell.SpellID = 11898;
					spell.Target = eSpellTarget.Area.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Spirit;
					m_LifedrianPulse = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LifedrianPulse);
				}
				return m_LifedrianPulse;
			}
		}
	}
}

