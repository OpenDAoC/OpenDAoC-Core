/*
Thane Dyggve.
<author>Kelt</author>
 */
using System;
using DOL.Database;
using DOL.Events;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts.DOL.AI.Brain;

namespace DOL.GS.Scripts
{
	public class ThaneDyggve : GameEpicBoss
	{
		public ThaneDyggve() : base()
		{ }
		/// <summary>
		/// Add Thane Dyggve to World
		/// </summary>
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9913);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			MeleeDamageType = eDamageType.Crush;
			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(187));
			RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			ScalingFactor = 60;
			base.SetOwnBrain(new ThaneDyggveBrain());
			LoadedFromScript = false; //load from database
			SaveIntoDatabase();
			base.AddToWorld();			
			return true;
		}	
		public override double AttackDamage(InventoryItem weapon)
		{
			return base.AttackDamage(weapon) * Strength / 100;
		}
		public override int AttackRange
		{
			get
			{ return 350;}
			set{ }
		}
		public override bool HasAbility(string keyName)
		{
			if (IsAlive && keyName == GS.Abilities.CCImmunity)
				return true;

			return base.HasAbility(keyName);
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 40; // dmg reduction for melee dmg
				case eDamageType.Crush: return 40; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
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
		public override int MaxHealth
		{
			get { return 30000; }
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Thane Dyggve NPC Initializing...");
		}
	}

	namespace DOL.AI.Brain
	{
		public class ThaneDyggveBrain : StandardMobBrain
		{
			protected String[] m_MjollnirAnnounce;
			protected bool castsMjollnir = true;
			private bool CanCastSpell = false;
			public ThaneDyggveBrain() : base()
			{
				CanBAF = false;
				m_MjollnirAnnounce = new String[]
				{
					"You feel your energy draining and {0} summons powerful lightning hammers!",
					"{0} takes another energy drain as he prepares to unleash a raging Mjollnir upon you!"
				};
			}
			public override void Think()
			{
				if(!HasAggressionTable())
                {
					FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
					Body.Health = Body.MaxHealth;
					CanCastSpell = false;
				}
				if (Body.InCombat && Body.IsAlive && HasAggro)
				{
					if (Body.TargetObject != null)
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
						{
							if (npc != null && npc.IsAlive && npc.PackageID == "ThaneDyggveBaf")
								AddAggroListTo(npc.Brain as StandardMobBrain);
						}
						if (!CanCastSpell)
						{
							new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastMjollnir), 2000);
							CanCastSpell = true;
						}
						if (Body.IsCasting)
						{
							if (castsMjollnir)
							{
								int messageNo = Util.Random(1, m_MjollnirAnnounce.Length) - 1;
								BroadcastMessage(String.Format(m_MjollnirAnnounce[messageNo], Body.Name));
							}
							castsMjollnir = false;
						}
						else
							castsMjollnir = true;
					}
				}
				base.Think();
			}		
			/// <summary>
			/// Broadcast relevant messages to the raid.
			/// </summary>
			/// <param name="message">The message to be broadcast.</param>
			public void BroadcastMessage(String message)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
				{
					player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
				}
			}	

			/// <summary>
			/// Cast Mjollnir on the Target
			/// </summary>
			/// <param name="timer">The timer that started this cast.</param>
			/// <returns></returns>
			private int CastMjollnir(ECSGameTimer timer)
			{
				Body.CastSpell(Mjollnir, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
				new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetMjollnir), 30000);
				return 0;
			}	
			private int ResetMjollnir(ECSGameTimer timer)
            {				
				CanCastSpell = false;
				return 0;
            }
			#region MjollnirSpell
			private Spell m_Mjollnir;
			/// <summary>
			/// The Mjollnir spell.
			/// </summary>
			protected Spell Mjollnir
			{
				get
				{
					if (m_Mjollnir == null)
					{
						DBSpell spell = new DBSpell();
						spell.AllowAdd = false;
						spell.CastTime = 4;
						spell.Uninterruptible = true;
						spell.RecastDelay = 30;
						spell.ClientEffect = 3541;
						spell.Icon = 3541;
						spell.Description = "Damages the target for 800.";
						spell.Name = "Command Mjollnir";
						spell.Range = 1500;
						spell.Radius = 350;
						spell.Value = 0;
						spell.Duration = 0;
						spell.Damage = 500;
						spell.DamageType = 12;
						spell.SpellID = 3541;
						spell.Target = "Enemy";
						spell.MoveCast = false;
						spell.Type = eSpellType.DirectDamageNoVariance.ToString();
						m_Mjollnir = new Spell(spell, 50);
						SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Mjollnir);
					}
					return m_Mjollnir;
				}
			}
			#endregion		
		}
	}
}