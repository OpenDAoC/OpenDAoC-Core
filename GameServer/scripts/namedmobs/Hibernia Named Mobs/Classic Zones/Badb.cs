using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Badb : GameEpicBoss
	{
		public Badb() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Badb Initializing...");
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
			get { return 30000; }
		}
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 250; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Badb";
			Model = 1883;
			Level = 70;
			Size = 50;
			MaxDistance = 2500;
			TetherRange = 2600;			

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 468, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Crush;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			BadbBrain sbrain = new BadbBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC adds in GetNPCsInRadius(8000))
			{
				if (adds != null && adds.IsAlive && adds.Brain is BadbWraithBrain)
					adds.RemoveFromWorld();
			}
			base.Die(killer);
        }		
	}
}
namespace DOL.AI.Brain
{
	public class BadbBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BadbBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		ushort oldModel;
		GameNPC.eFlags oldFlags;
		bool changed;
		public override void Think()
		{
			if (Body.CurrentRegion.IsNightTime == false)
			{
				if (changed == false)
				{
					oldFlags = Body.Flags;
					Body.Flags ^= GameNPC.eFlags.CANTTARGET;
					Body.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
					Body.Flags ^= GameNPC.eFlags.PEACE;

					if (oldModel == 0)
						oldModel = Body.Model;

					Body.Model = 1;
					foreach (GameNPC adds in Body.GetNPCsInRadius(8000))
					{
						if (adds != null && adds.IsAlive && adds.Brain is BadbWraithBrain)
							adds.RemoveFromWorld();
					}
					changed = true;
				}
			}
			if (Body.CurrentRegion.IsNightTime)
			{
				if (changed)
				{
					Body.Flags = oldFlags;
					Body.Model = oldModel;
					CreateWraiths();
					changed = false;
				}
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if(HasAggro && Body.TargetObject != null)
            {
				foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
				{
					if (npc != null && npc.IsAlive && npc.Brain is BadbWraithBrain brain)
					{
						GameLiving target = Body.TargetObject as GameLiving;
						if (!brain.HasAggro && target.IsAlive && target != null)
							brain.AddToAggroList(target, 10);
					}
				}
				if (Util.Chance(50) && !Body.IsCasting)
					Body.CastSpell(BadbDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells),false);
			}
			base.Think();
		}
		#region Create Wraiths
		private void CreateWraiths()
		{
			for (int i = 0; i < 3; i++)
			{
				BadbWraith wraith = new BadbWraith();
				wraith.X = 384497 + Util.Random(-100, 100);
				wraith.Y = 745145 + Util.Random(-100, 100);
				wraith.Z = 4888;
				wraith.Heading = 3895;
				wraith.CurrentRegion = Body.CurrentRegion;
				wraith.AddToWorld();
			}

			for (int i = 0; i < 3; i++)
			{
				BadbWraith wraith = new BadbWraith();
				wraith.X = 383922 + Util.Random(-100, 100);
				wraith.Y = 745175 + Util.Random(-100, 100);
				wraith.Z = 4888;
				wraith.Heading = 669;
				wraith.CurrentRegion = Body.CurrentRegion;
				wraith.AddToWorld();
			}

			for (int i = 0; i < 3; i++)
			{
				BadbWraith wraith = new BadbWraith();
				wraith.X = 384146 + Util.Random(-100, 100);
				wraith.Y = 744564 + Util.Random(-100, 100);
				wraith.Z = 4888;
				wraith.Heading = 1859;
				wraith.CurrentRegion = Body.CurrentRegion;
				wraith.AddToWorld();
			}

			for (int i = 0; i < 3; i++)
			{
				BadbWraith wraith = new BadbWraith();
				wraith.X = 384677 + Util.Random(-100, 100);
				wraith.Y = 744788 + Util.Random(-100, 100);
				wraith.Z = 4888;
				wraith.Heading = 2621;
				wraith.CurrentRegion = Body.CurrentRegion;
				wraith.AddToWorld();
			}
		}
		#endregion
		#region Spells
		private Spell m_BadbDD;
		public Spell BadbDD
		{
			get
			{
				if (m_BadbDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3;
					spell.Power = 0;
					spell.RecastDelay = 10;
					spell.ClientEffect = 13533;
					spell.Icon = 13533;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Voices of Pain";
					spell.Range = 1500;
					spell.SpellID = 11874;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					m_BadbDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BadbDD);
				}
				return m_BadbDD;
			}
		}
		#endregion
	}
}

#region Badb Raven Wraith
namespace DOL.GS
{
	public class BadbWraith : GameNPC
	{
		public BadbWraith() : base()
		{
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 15;// dmg reduction for melee dmg
				case eDamageType.Crush: return 15;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 15;// dmg reduction for melee dmg
				default: return 15;// dmg reduction for rest resists
			}
		}
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int MaxHealth
		{
			get { return 2200; }
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 200;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.10;
		}
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
        public override short Strength { get => base.Strength; set => base.Strength = 150; }
        public override bool AddToWorld()
		{
			Model = 441;
			Name = "raven wraith";
			Level = (byte)Util.Random(58, 64);
			Size = (byte)Util.Random(50, 55);
			RespawnInterval = -1;
			RoamingRange = 200;
			Flags = eFlags.GHOST;

			LoadedFromScript = true;
			BadbWraithBrain sbrain = new BadbWraithBrain();
			SetOwnBrain(sbrain);
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class BadbWraithBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public BadbWraithBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			base.Think();
		}
	}
}
#endregion