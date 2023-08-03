﻿using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS.Scripts
{
	public class BossWarlordDorinakka : GameEpicBoss
	{
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 40;// dmg reduction for melee dmg
				case EDamageType.Crush: return 40;// dmg reduction for melee dmg
				case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
				default: return 70;// dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}

		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * ServerProperties.ServerProperties.EPICS_DMG_MULTIPLIER;
		}
		
		public override double GetArmorAbsorb(EArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.20;
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == "CCImmunity")
				return true;

			return base.HasAbility(keyName);
		}
		public override short MaxSpeedBase => (short) (191 + Level * 2);
		public override int MaxHealth => 100000;

		public override int AttackRange
		{
			get => 180;
			set { }
		}
		
		public override byte ParryChance => 80;
		
		public override bool AddToWorld()
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 847, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(EActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = EDamageType.Slash;
			Level = 81;
			Gender = EGender.Neutral;
			BodyType = 5; // giant
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 0;
			Realm = ERealm.None;
			ParryChance = 80; // 80% parry chance

			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167787);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.ServerProperties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
			WarlordDorinakkaBrain adds = new WarlordDorinakkaBrain();
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}		
	}
}
namespace DOL.AI.Brain
{
    public class WarlordDorinakkaBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public WarlordDorinakkaBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }

			if (Body.TargetObject != null && HasAggro) //bring mobs from rooms if mobs got set PackageID="CryptLordBaf"
			{
				foreach (GameNpc npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.PackageID == "DorinakkaBaf")
						{
							AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with CryptLordBaf PackageID
						}
					}
				}
			}
            base.Think();
        }
    }
}