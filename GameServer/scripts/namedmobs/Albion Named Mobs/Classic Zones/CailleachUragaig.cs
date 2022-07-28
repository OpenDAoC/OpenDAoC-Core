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
				case eDamageType.Slash: return 20;// dmg reduction for melee dmg
				case eDamageType.Crush: return 20;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 20;// dmg reduction for melee dmg
				default: return 20;// dmg reduction for rest resists
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
			get { return 20000; }
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

			RespawnInterval = ServerProperties.Properties.SET_EPIC_QUEST_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
			CailleachUragaigBrain sbrain = new CailleachUragaigBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain)
					npc.RemoveFromWorld();
			}
			base.Die(killer);
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
			foreach (GamePlayer player in Body.GetPlayersInRadius(5000))
			{
				player.Out.SendMessage(message, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
			}
		}
		private bool SpawnAdds = false;
		private bool RemoveAdds = false;
		private bool TorchOfLight_Enabled = false;
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
				TorchOfLight_Enabled = false;
				SpawnAdds = false;

				if (!RemoveAdds)
                {
					foreach(GameNPC npc in Body.GetNPCsInRadius(8000))
                    {
						if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain)
							npc.RemoveFromWorld();
                    }
					RemoveAdds = true;
                }
			}
			if (HasAggro && Body.TargetObject != null)
			{
				RemoveAdds = false;
				if(!SpawnAdds)
                {
					SpawnTorchOfLight();
					GameLiving target = Body.TargetObject as GameLiving;
					foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
					{
						if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain brain)
                        {
							if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
								brain.AddToAggroList(target, 100);
                        }
					}
					SpawnAdds = true;
                }
				if(!TorchOfLight_Enabled)
                {
					new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetLights), 5000);
					TorchOfLight_Enabled = true;
                }
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
					spell.Damage = 200;
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
					spell.Damage = 200;
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
		private int ResetLights(ECSGameTimer timer)
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(8000))
			{
				if (npc != null && npc.IsAlive && npc.Brain is TorchOfLightBrain)
					npc.RemoveFromWorld();
			}
			return 0;
        }
		#region Spawn Torch of Light
		private void SpawnTorchOfLight()
		{
			TorchOfLight npc = new TorchOfLight();
			npc.X = 316833;
			npc.Y = 664746;
			npc.Z = 3146;
			npc.Heading = 1238;
			npc.CurrentRegion = Body.CurrentRegion;
			npc.AddToWorld();

			TorchOfLight npc2 = new TorchOfLight();
			npc2.X = 316715;
			npc2.Y = 664155;
			npc2.Z = 3141;
			npc2.Heading = 1238;
			npc2.CurrentRegion = Body.CurrentRegion;
			npc2.AddToWorld();

			TorchOfLight npc3 = new TorchOfLight();
			npc3.X = 315842;
			npc3.Y = 663685;
			npc3.Z = 3356;
			npc3.Heading = 4038;
			npc3.CurrentRegion = Body.CurrentRegion;
			npc3.AddToWorld();

			TorchOfLight npc4 = new TorchOfLight();
			npc4.X = 315857;
			npc4.Y = 665332;
			npc4.Z = 3405;
			npc4.Heading = 2043;
			npc4.CurrentRegion = Body.CurrentRegion;
			npc4.AddToWorld();
		}
		#endregion
	}
}


#region  Torch of Light
namespace DOL.GS
{
	public class TorchOfLight : GameNPC
	{
		public TorchOfLight() : base() { }

        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 140; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 100; }
		public override short Piety { get => base.Piety; set => base.Piety = 100; }
		public override short Charisma { get => base.Charisma; set => base.Charisma = 100; }
		public override short Empathy { get => base.Empathy; set => base.Empathy = 100; }

		public override bool AddToWorld()
		{
			Name = "Mithra's Torch of Light";
			Level = 65;
			Model = 665;
			Size = 45;
			Flags = (GameNPC.eFlags)44;
			TorchOfLightBrain sbrain = new TorchOfLightBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = true;
			RespawnInterval = -1;
			base.AddToWorld();
			return true;
		}
	}
}
namespace DOL.AI.Brain
{
	public class TorchOfLightBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public TorchOfLightBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 100;
			ThinkInterval = 1500;
		}
		public override void Think()
		{
			if(HasAggro && Body.TargetObject != null)
				Body.CastSpell(Torch_Of_Light_Bolt, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
			base.Think();
		}
		private Spell m_Torch_Of_Light_Bolt;
		private Spell Torch_Of_Light_Bolt
		{
			get
			{
				if (m_Torch_Of_Light_Bolt == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 20;
					spell.ClientEffect = 378;
					spell.Icon = 378;
					spell.Damage = 150;
					spell.DamageType = (int)eDamageType.Heat;
					spell.Name = "Flame Spear";
					spell.Range = 4000;
					spell.SpellID = 11895;
					spell.Target = "Enemy";
					spell.Uninterruptible = true;
					spell.MoveCast = true;
					spell.Type = eSpellType.Bolt.ToString();
					m_Torch_Of_Light_Bolt = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Torch_Of_Light_Bolt);
				}
				return m_Torch_Of_Light_Bolt;
			}
		}
	}
}
#endregion