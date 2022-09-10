using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;

namespace DOL.GS.Scripts
{
	public class WarlordDorinakka : GameEpicBoss
	{
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
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 350;
		}

		public override double GetArmorAbsorb(eArmorSlot slot)
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
		public override short MaxSpeedBase
		{
			get => (short)(191 + (Level * 2));
			set => m_maxSpeedBase = value;
		}
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
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Slash;
			Level = 81;
			Gender = eGender.Neutral;
			BodyType = 5; // giant
			MaxDistance = 1500;
			TetherRange = 2000;
			RoamingRange = 0;
			Realm = eRealm.None;
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

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
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
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }

			if (Body.TargetObject != null && HasAggro) //bring mobs from rooms if mobs got set PackageID="CryptLordBaf"
			{
				foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
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