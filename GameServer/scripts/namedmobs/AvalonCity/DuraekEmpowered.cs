using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;

namespace DOL.GS
{
	public class DuraekEmpowered : GameEpicBoss
	{
		public DuraekEmpowered() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Dura'ek the Empowered Initializing...");
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 60;// dmg reduction for melee dmg
				case eDamageType.Crush: return 60;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 60;// dmg reduction for melee dmg
				default: return 40;// dmg reduction for rest resists
			}
		}
		public override void OnAttackEnemy(AttackData ad) //on enemy actions
		{
			if (Util.Chance(35))
			{
				if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
				{
					CastSpell(HeatAoeProc, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				}
			}
			base.OnAttackEnemy(ad);
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (damageType == eDamageType.Heat || damageType == eDamageType.Cold)
				{
					GamePlayer truc;
					if (source is GamePlayer)
						truc = (source as GamePlayer);
					else
						truc = ((source as GamePet).Owner as GamePlayer);
					if (truc != null)
						truc.Out.SendMessage("Your damage is absorbed and used to heal "+Name, eChatType.CT_System, eChatLoc.CL_ChatWindow);
					Health += damageAmount + criticalAmount;
					base.TakeDamage(source, damageType, 0, 0);
					return;

				}
				else//take dmg
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
			if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
			get { return 15000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160230);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(10);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(10));

			DuraekEmpoweredBrain sbrain = new DuraekEmpoweredBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
		private Spell m_HeatAoeProc;
		private Spell HeatAoeProc
		{
			get
			{
				if (m_HeatAoeProc == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 360;
					spell.Icon = 360;
					spell.TooltipId = 360;
					spell.Damage = 150;
					spell.Name = "Flames of Dura'ek";
					spell.Range = 0;
					spell.Radius = 350;
					spell.SpellID = 11795;
					spell.Target = eSpellTarget.Enemy.ToString();
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.DamageType = (int)eDamageType.Heat;
					m_HeatAoeProc = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_HeatAoeProc);
				}

				return m_HeatAoeProc;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class DuraekEmpoweredBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public DuraekEmpoweredBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public static bool IsPulled = false;
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				IsPulled = false;
			}
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if(IsPulled==false)
                {
					foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
					{
						if (npc != null)
						{
							if (npc.IsAlive && npc.PackageID == "DuraekBaf")
							{
								AddAggroListTo(npc.Brain as StandardMobBrain); // add to aggro mobs with IssordenBaf PackageID
							}
						}
					}
					IsPulled = true;
                }
			}
			base.Think();
		}
	}
}

