﻿using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class Dagar : GameEpicNPC
	{
		public Dagar() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Dagar Initializing...");
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
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override int MaxHealth
		{
			get { return 8000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159616);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(EInventorySlot.TwoHandWeapon, 956, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = EDamageType.Crush;
			Faction = FactionMgr.GetFactionByID(150);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(150));
			DagarBrain sbrain = new DagarBrain();
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
	public class DagarBrain : StandardMobBrain
	{
		public DagarBrain()
			: base()
		{
			AggroLevel = 100;
			AggroRange = 500;
		}
		public static bool IsPulled = false;
		public override void Think()
		{
			if (HasAggro && Body.TargetObject != null)
			{
				foreach (GameNpc npc in Body.GetNPCsInRadius(2000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "DagarBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain);
						}
					}
				}
			}
			base.Think();
		}
	}
}