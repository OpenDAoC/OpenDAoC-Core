using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class CailleachUragaig : GameEpicBoss
	{
		public CailleachUragaig() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Cailleach Uragaig Initializing...");
		}
		public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
		{
			if (source is GamePlayer || source is GamePet)
			{
				if (IsOutOfTetherRange)
				{
					if (damageType == eDamageType.Body || damageType == eDamageType.Cold ||
						damageType == eDamageType.Energy || damageType == eDamageType.Heat
						|| damageType == eDamageType.Matter || damageType == eDamageType.Spirit ||
						damageType == eDamageType.Crush || damageType == eDamageType.Thrust
						|| damageType == eDamageType.Slash)
					{
						GamePlayer truc;
						if (source is GamePlayer)
							truc = (source as GamePlayer);
						else
							truc = ((source as GamePet).Owner as GamePlayer);
						if (truc != null)
							truc.Out.SendMessage(Name + " can't be attacked from this distance!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
						base.TakeDamage(source, damageType, 0, 0);
						return;
					}
				}
				else //take dmg
				{
					base.TakeDamage(source, damageType, damageAmount, criticalAmount);
				}
			}
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
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc.Brain is CailleachUragaigBrain)
					return false;
			}
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(12941);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CailleachUragaigBrain sbrain = new CailleachUragaigBrain();
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
	public class CailleachUragaigBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public CailleachUragaigBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 500;
			ThinkInterval = 1500;
		}
		bool AggroMessage = false;
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(CailleachUragaigDD))
					Body.Spells.Add(CailleachUragaigDD);
            }
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				AggroMessage = false;
			}
			if (HasAggro && Body.TargetObject != null)
			{
				if(!AggroMessage)
                {
					BroadcastMessage(String.Format("{0} says, \"Father! Lend me your Torch of Light so we may be delivered from these aggressors!\"",Body.Name));
					AggroMessage = true;
                }
				if (Body.HealthPercent <= 30)
				{
					foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
					{
						if (npc != null && npc.IsAlive && npc.PackageID == "CailleachUragaigBaf")
							AddAggroListTo(npc.Brain as StandardMobBrain);
					}
				}
				if(!Body.IsCasting && Util.Chance(20))
					Body.CastSpell(CailleachUragaigDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.Think();
		}
		#region Spells
		private Spell m_CailleachUragaigDD;
		private Spell CailleachUragaigDD
		{
			get
			{
				if (m_CailleachUragaigDD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.RecastDelay = 0;
					spell.ClientEffect = 378;
					spell.Icon = 378;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Flame Spear";
					spell.Range = 1800;
					spell.SpellID = 11983;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Bolt.ToString();
					m_CailleachUragaigDD = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CailleachUragaigDD);
				}
				return m_CailleachUragaigDD;
			}
		}
		private Spell m_CailleachUragaigDD2;
		private Spell CailleachUragaigDD2
		{
			get
			{
				if (m_CailleachUragaigDD2 == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.RecastDelay = Util.Random(10,20);
					spell.ClientEffect = 378;
					spell.Icon = 378;
					spell.Damage = 400;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Flame Spear";
					spell.Range = 1800;
					spell.SpellID = 11983;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = eSpellType.Bolt.ToString();
					m_CailleachUragaigDD2 = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_CailleachUragaigDD2);
				}
				return m_CailleachUragaigDD2;
			}
		}
		#endregion
	}
}


