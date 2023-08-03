﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Styles;

namespace DOL.GS
{
	public class BossJuggernaut : GameEpicBoss
	{
		public BossJuggernaut() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Juggernaut Initializing...");
		}
		public static int TauntID = 103;
		public static int TauntClassID = 2; //armsman
		public static Style taunt = SkillBase.GetStyleByID(TauntID, TauntClassID);
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 20;// dmg reduction for melee dmg
				case EDamageType.Crush: return 20;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
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
			get { return 40000; }
		}
		#region Stats
		public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
		public override short Piety { get => base.Piety; set => base.Piety = 200; }
		public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
		public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		public override short Strength { get => base.Strength; set => base.Strength = 500; }
		#endregion
		public override bool AddToWorld()
		{
			Name = "Juggernaut";
			Model = 137;
			Level = 75;
			Size = 200;
			ParryChance = 50;
			MaxDistance = 3500;
			TetherRange = 3600;
			if (!Styles.Contains(taunt))
				Styles.Add(taunt);

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 403, 43, 0, 0);//modelID,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 405, 43);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 404, 43);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 406, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 407, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.HeadArmor, 831, 43, 0, 0);
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 577, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = EDamageType.Slash;
			RespawnInterval = ServerProperties.ServerProperties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			EpicJuggernautBrain sbrain = new EpicJuggernautBrain();
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
	public class EpicJuggernautBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public EpicJuggernautBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 800;
			ThinkInterval = 1500;
		}

		public override void Think()
		{
			if (!CheckProximityAggro())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
			}
			if (HasAggro && Body.TargetObject != null)
				Body.styleComponent.NextCombatStyle = BossJuggernaut.taunt;
			base.Think();
		}
	}
}
