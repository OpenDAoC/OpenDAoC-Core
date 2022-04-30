using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class DebenSeGecynde : GameEpicBoss
	{
		public DebenSeGecynde() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Deben se Gecynde Initializing...");
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
			get { return 60000; }
		}
		public override bool AddToWorld()
		{
			Model = 919;
			Level = 80;
			Name = "Deben se Gecynde";
			Size = 120;

			Strength = 280;
			Dexterity = 150;
			Constitution = 100;
			Quickness = 80;
			Piety = 200;
			Intelligence = 200;
			Charisma = 200;
			Empathy = 400;		

			MaxSpeedBase = 250;
			MaxDistance = 3500;
			TetherRange = 3800;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			DebenSeGecyndeBrain sbrain = new DebenSeGecyndeBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class DebenSeGecyndeBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DebenSeGecyndeBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		private bool spawnadds = false;
		private bool IsPulled = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				spawnadds = false;
				IsPulled = false;
				foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && (npc.Brain is DebenFighterBrain || npc.Brain is DebenMageBrain))
							npc.RemoveFromWorld();
					}
				}
			}
			if (HasAggro)
			{
				if(IsPulled==false)
                {
					foreach(GameNPC npc in Body.GetNPCsInRadius(2500))
                    {
						if(npc != null)
                        {
							if(npc.IsAlive && npc.PackageID == "DebenBaf")
                            {
								AddAggroListTo(npc.Brain as StandardMobBrain);
								IsPulled = true;
                            }
                        }
                    }
                }
				if (spawnadds == false)
				{
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnAdds), Util.Random(45000, 70000));
					spawnadds = true;
				}
			}
			base.Think();
		}
		public int SpawnAdds(ECSGameTimer timer)
        {
			if (HasAggro)
			{
				for (int i = 0; i < Util.Random(1, 3); i++)
				{
					DebenFighter npc = new DebenFighter();
					npc.X = Body.X + Util.Random(-100, 100);
					npc.Y = Body.Y + Util.Random(-100, 100);
					npc.Z = Body.Z;
					npc.Heading = Body.Heading;
					npc.CurrentRegion = Body.CurrentRegion;
					npc.RespawnInterval = -1;
					npc.AddToWorld();
				}
				for (int i = 0; i < Util.Random(1, 3); i++)
				{
					DebenMage npc = new DebenMage();
					npc.X = Body.X + Util.Random(-100, 100);
					npc.Y = Body.Y + Util.Random(-100, 100);
					npc.Z = Body.Z;
					npc.Heading = Body.Heading;
					npc.CurrentRegion = Body.CurrentRegion;
					npc.RespawnInterval = -1;
					npc.AddToWorld();
				}
			}
			spawnadds = false;
			return 0;
        }
	}
}
////////////////////////////////////////////////////////////Deben Soldiers////////////////////////////////////////////////
#region fighter
namespace DOL.GS
{
	public class DebenFighter : GameEpicNPC
	{
		public DebenFighter() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 120;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 3000; }
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(65, 68));
			Name = "thrawn ogre sceotan";
			Size = (byte)(Util.Random(100, 120));
			MaxSpeedBase = 250;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			MeleeDamageType = eDamageType.Slash;
			VisibleActiveWeaponSlots = 34;
			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			DebenFighterBrain sbrain = new DebenFighterBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class DebenFighterBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DebenFighterBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 1000;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion
#region mage 
namespace DOL.GS
{
	public class DebenMage : GameEpicNPC
	{
		public DebenMage() : base() { }

		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 30;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 160;
		}
		public override int AttackRange
		{
			get { return 350; }
			set { }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 2000; }
		}
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 60; }
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(65, 68));
			Name = "thrawn abrecan mage";
			Size = (byte)(Util.Random(100, 120));
			MaxSpeedBase = 250;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 19, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 16;
			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			DebenMageBrain sbrain = new DebenMageBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class DebenMageBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DebenMageBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
            {
				if (!Body.IsCasting && !Body.IsBeingInterrupted)
				{
					if (Body.attackComponent.AttackState)
						Body.attackComponent.NPCStopAttack();
					if (Body.IsMoving)
						Body.StopFollowing();
					Body.TurnTo(Body.TargetObject);
					switch (Util.Random(1, 2))
					{
						case 1: Body.CastSpell(Mage_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
						case 2: Body.CastSpell(Mage_DD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells)); break;
					}
				}
            }
			base.Think();
		}
        #region Spells
        private Spell m_Mage_DD;
		private Spell Mage_DD
		{
			get
			{
				if (m_Mage_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.TooltipId = 360;
					spell.Damage = 300;
					spell.Name = "Major Conflagration";
					spell.Range = 1500;
					spell.SpellID = 11883;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Heat;
					m_Mage_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mage_DD);
				}
				return m_Mage_DD;
			}
		}
		private Spell m_Mage_DD2;
		private Spell Mage_DD2
		{
			get
			{
				if (m_Mage_DD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.RecastDelay = 0;
					spell.ClientEffect = 161;
					spell.Icon = 161;
					spell.TooltipId = 161;
					spell.Damage = 300;
					spell.Name = "Major Ice Blast";
					spell.Range = 1500;
					spell.SpellID = 11884;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Cold;
					m_Mage_DD2 = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mage_DD2);
				}
				return m_Mage_DD2;
			}
		}
        #endregion
    }
}
#endregion