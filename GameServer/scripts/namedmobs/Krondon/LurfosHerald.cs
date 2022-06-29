using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class LurfosHerald : GameEpicBoss
	{
		public LurfosHerald() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Lurfos the Herald Initializing...");
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
		public override bool AddToWorld()
		{
			Model = 919;
			Level = (byte)(Util.Random(75, 78));
			Name = "Lurfos the Herald";
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
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.TwoHanded);

			VisibleActiveWeaponSlots = 34;
			MeleeDamageType = eDamageType.Slash;
			Faction = FactionMgr.GetFactionByID(8);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(8));

			LurfosHeraldBrain sbrain = new LurfosHeraldBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void OnAttackEnemy(AttackData ad)
        {
			if(ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
				if(LurfosHeraldBrain.IsColdWeapon)
					CastSpell(Weapon_Cold, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				if (LurfosHeraldBrain.IsHeatWeapon)
					CastSpell(Weapon_Heat, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
            base.OnAttackEnemy(ad);
        }
		private Spell m_Weapon_Heat;
		private Spell Weapon_Heat
		{
			get
			{
				if (m_Weapon_Heat == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 1;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.TooltipId = 360;
					spell.Damage = 300;
					spell.Name = "Fire Strike";
					spell.Range = 500;
					spell.Radius = 300;
					spell.SpellID = 11885;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Heat;
					m_Weapon_Heat = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Weapon_Heat);
				}
				return m_Weapon_Heat;
			}
		}
		private Spell m_Weapon_Cold;
		private Spell Weapon_Cold
		{
			get
			{
				if (m_Weapon_Cold == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 1;
					spell.ClientEffect = 161;
					spell.Icon = 161;
					spell.TooltipId = 161;
					spell.Damage = 300;
					spell.Name = "Ice Strike";
					spell.Range = 500;
					spell.Radius = 300;
					spell.SpellID = 11886;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.DamageType = (int)eDamageType.Cold;
					m_Weapon_Cold = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Weapon_Cold);
				}
				return m_Weapon_Cold;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class LurfosHeraldBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public LurfosHeraldBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsColdWeapon = false;
		public static bool IsHeatWeapon = false;
		public static bool IsNormalWeapon = false;
		public static bool StartSwitchWeapons = false;
		public int SwitchToCold(ECSGameTimer timer)
        {
			if (HasAggro)
			{
				IsColdWeapon = true;
				IsHeatWeapon = false;
				foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 4075, 0, false, 1);
				}
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 0, 27);
				Body.Inventory = template.CloseTemplate();
				Body.BroadcastLivingEquipmentUpdate();
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SwitchToHeat), Util.Random(30000, 50000));
			}
			return 0;
        }
		public int SwitchToHeat(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				IsHeatWeapon = true;
				IsColdWeapon = false;
				foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					if (player != null)
						player.Out.SendSpellEffectAnimation(Body, Body, 4051, 0, false, 1);
				}
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 0, 21);
				Body.Inventory = template.CloseTemplate();
				Body.BroadcastLivingEquipmentUpdate();
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SwitchToCold), Util.Random(30000, 50000));
			}
			return 0;
		}
		public int SwitchToNormal(ECSGameTimer timer)
		{
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 7, 0, 0);
			Body.Inventory = template.CloseTemplate();
			Body.BroadcastLivingEquipmentUpdate();
			return 0;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsColdWeapon = false;
				IsHeatWeapon = false;
				StartSwitchWeapons = false;
				if (IsNormalWeapon==false)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SwitchToNormal), 1000);
					IsNormalWeapon = true;
                }
			}
			if (HasAggro && Body.TargetObject != null)
			{
				IsNormalWeapon = false;
				if(StartSwitchWeapons==false)
                {
					switch(Util.Random(1,2))
                    {
						case 1: new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SwitchToCold), 1000); break;
						case 2: new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SwitchToHeat), 1000); break;
					}					
					StartSwitchWeapons =true;
                }
			}
			base.Think();
		}
	}
}