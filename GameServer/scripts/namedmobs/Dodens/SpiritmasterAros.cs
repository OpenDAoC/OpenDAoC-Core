using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS;
using System.Collections.Generic;

namespace DOL.GS
{
	public class SpiritmasterAros : GameEpicBoss
	{
		public SpiritmasterAros() : base() { }

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Aros the Spiritmaster Initializing...");
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 30;// dmg reduction for melee dmg
				case eDamageType.Crush: return 30;// dmg reduction for melee dmg
				case eDamageType.Thrust: return 30;// dmg reduction for melee dmg
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
			get { return 40000; }
		}
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9916);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;
			RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(779));

			SpiritmasterArosBrain sbrain = new SpiritmasterArosBrain();
			SetOwnBrain(sbrain);
			LoadedFromScript = false;//load from database
			SaveIntoDatabase();
			base.AddToWorld();
			return true;
		}
        public override void StartAttack(GameObject target)//Aros is Caster so he will not use melee attacks
        {
        }
        public override void Die(GameObject killer)
        {
			foreach (GameNPC npc in GetNPCsInRadius(5000))
			{
				if (npc != null)
				{
					if (npc.IsAlive && npc.Brain is SpiritmasterArosPetBrain)
						npc.Die(this);
				}
			}

			switch (Util.Random(1, 2))
			{
				case 1: BroadcastMessage("'You will remember my name! " + Name + "!'"); break;
				case 2: BroadcastMessage(Name + " trips and falls on the hard stone floor."); break;
			}					
			base.Die(killer);
        }
        public override void DealDamage(AttackData ad)
        {
			if (ad != null && ad.AttackType == AttackData.eAttackType.Spell && ad.Damage > 0 && ad.DamageType == eDamageType.Cold)
				Health += ad.Damage / 6;
            base.DealDamage(ad);
        }
    }
}
namespace DOL.AI.Brain
{
	public class SpiritmasterArosBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public SpiritmasterArosBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
			ThinkInterval = 1500;
		}
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
			}
		}
		List<string> Aros_bomb_text = new List<string>()
		{
			"Aros the Spiritmaster begins to perform a ritual!",
			"Aros the Spiritmaster is powerful and begins a threatening attack!",
			"Feeling strong and powerful, Aros the Spiritmaster prepares a deadly spell.",
			"Aros the Spiritmaster begins a magic of mental destruction!"
		};
        public override void Think()
		{
			if(Body.IsAlive)
            {
				if (!Body.Spells.Contains(Aros_Bomb))
					Body.Spells.Add(Aros_Bomb);
				if (!Body.Spells.Contains(Aros_DD))
					Body.Spells.Add(Aros_DD);
				if (!Body.Spells.Contains(Aros_Debuff))
					Body.Spells.Add(Aros_Debuff);
			}
			if (!HasAggressionTable())
			{
				//set state to RETURN TO SPAWN
				FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
				Body.Health = Body.MaxHealth;
				DebuffTarget = null;
				CanCastDebuff = false;
				if (Enemys_To_Debuff.Count > 0)
					Enemys_To_Debuff.Clear();
			}
			if (HasAggro && Body.TargetObject != null)
			{
				SummonPet();
				GameLiving target = Body.TargetObject as GameLiving;
				foreach (GameNPC npc in Body.GetNPCsInRadius(2000))
				{
					if (npc != null)
					{
						if (npc.IsAlive && npc.Brain is SpiritmasterArosPetBrain brain)
						{
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
				if (!Body.IsCasting && !Body.IsMoving && target != null)
				{
					foreach (Spell spells in Body.Spells)
					{
						if (spells != null)
						{
							if (Body.IsMoving && target.IsWithinRadius(target, spells.Range))
								Body.StopFollowing();
							else
								Body.Follow(target, spells.Range - 50, 5000);
							if(spells == Aros_Bomb && Body.GetSkillDisabledDuration(Aros_Bomb) == 0)
                            {
								string message = Aros_bomb_text[Util.Random(0, Aros_bomb_text.Count - 1)];
								BroadcastMessage(message);
							}
							if(spells == Aros_Debuff && DebuffTarget != null)
								BroadcastMessage(Body.Name+" weakens "+ DebuffTarget.Name + " and everyone around!");
							//Body.TurnTo(target);
							if (Util.Chance(100))
							{
								if (!Body.IsCasting && Body.GetSkillDisabledDuration(Aros_Bomb) == 0)									
									Body.CastSpell(Aros_Bomb, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								else if (Body.GetSkillDisabledDuration(Aros_Debuff) == 0)
								{
									new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickDebuffTarget), 200);
									if (DebuffTarget != null && CanCastDebuff)
									{
										Body.TargetObject = DebuffTarget;
										Body.CastSpell(Aros_Debuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
									}
								}
								else if (!Body.IsCasting && Body.GetSkillDisabledDuration(Aros_Bomb) > 0 && Body.GetSkillDisabledDuration(Aros_Debuff) > 0)
								{
									Body.TurnTo(target);
									Body.CastSpell(Aros_DD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
								}
							}
						}
					}
				}
			}
			base.Think();
		}
		private void SummonPet()
        {
			foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
			{
				if (npc.IsAlive && npc.Brain is SpiritmasterArosPetBrain)
					return;
			}
			if (HasAggro)
			{
				SpiritmasterArosPet pet = new SpiritmasterArosPet();
				pet.X = Body.X;
				pet.Y = Body.Y - 100;
				pet.Z = Body.Z;
				pet.Heading = Body.Heading;
				pet.CurrentRegionID = Body.CurrentRegionID;
				pet.AddToWorld();
			}
		}
		#region Aros Debuff
		public static bool CanCastDebuff = false;
		List<GamePlayer> Enemys_To_Debuff = new List<GamePlayer>();
		public static GamePlayer debufftarget = null;
		public static GamePlayer DebuffTarget
		{
			get { return debufftarget; }
			set { debufftarget = value; }
		}
		public int PickDebuffTarget(ECSGameTimer timer)
		{
			if (HasAggro)
			{
				foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
				{
					if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && player.CharacterClass.ID != (int)eCharacterClass.Necromancer)
						{
						if (!Enemys_To_Debuff.Contains(player))
							Enemys_To_Debuff.Add(player);
					}
				}
				if (Enemys_To_Debuff.Count > 0)
				{
					if (CanCastDebuff == false && Body.GetSkillDisabledDuration(Aros_Debuff) == 0)
					{
						GamePlayer Target = Enemys_To_Debuff[Util.Random(0, Enemys_To_Debuff.Count - 1)];//pick random target from list
						DebuffTarget = Target;//set random target to static RandomTarget
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetDebuff), 5000);
						CanCastDebuff = true;
					}
				}
			}
			return 0;
		}
		public int ResetDebuff(ECSGameTimer timer)
		{
			DebuffTarget = null;
			CanCastDebuff = false;
			return 0;
		}
		#endregion
		#region Spells
		private Spell m_Aros_DD;
		private Spell Aros_DD
		{
			get
			{
				if (m_Aros_DD == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 3.5;
					spell.RecastDelay = 0;
					spell.ClientEffect = 2610;
					spell.Icon = 2610;
					spell.TooltipId = 2610;
					spell.Damage = 500;
					spell.DamageType = (int)eDamageType.Cold;
					spell.Name = "Extinguish Lifeforce";
					spell.Range = 1500;
					spell.SpellID = 11916;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Aros_DD = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aros_DD);
				}
				return m_Aros_DD;
			}
		}
		private Spell m_Aros_Bomb;
		private Spell Aros_Bomb
		{
			get
			{
				if (m_Aros_Bomb == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 5;
					spell.RecastDelay = Util.Random(35,45);
					spell.ClientEffect = 2797;
					spell.Icon = 2797;
					spell.TooltipId = 2797;
					spell.Damage = 1400;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Soul Annihilation";
					spell.Range = 0;
					spell.Radius = 800;
					spell.SpellID = 11917;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.Uninterruptible = true;
					m_Aros_Bomb = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aros_Bomb);
				}
				return m_Aros_Bomb;
			}
		}
		private Spell m_Aros_Debuff;
		private Spell Aros_Debuff
		{
			get
			{
				if (m_Aros_Debuff == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = Util.Random(15,25);
					spell.ClientEffect = 4575;
					spell.Icon = 4575;
					spell.TooltipId = 4575;
					spell.Value = 40;
					spell.Duration = 60;
					spell.DamageType = (int)eDamageType.Spirit;
					spell.Name = "Negate Spirit";
					spell.Range = 1500;
					spell.Radius = 500;
					spell.SpellID = 11918;
					spell.Target = "Enemy";
					spell.Type = eSpellType.SpiritResistDebuff.ToString();
					spell.Message1 = "You feel more vulnerable to spirit magic!";
					spell.Message2 = "{0} seems vulnerable to spirit magic!";
					spell.Uninterruptible = true;
					m_Aros_Debuff = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Aros_Debuff);
				}
				return m_Aros_Debuff;
			}
		}
		#endregion
	}
}
////////////////////////////////////////////////////////////////////////Aros Spirit Champion//////////////////////////////////////////////////////
namespace DOL.GS
{
	public class SpiritmasterArosPet : GameNPC
	{
		public override int MaxHealth
		{
			get { return 8000; }
		}
		public override int GetResist(eDamageType damageType)
		{
			switch (damageType)
			{
				case eDamageType.Slash: return 25; // dmg reduction for melee dmg
				case eDamageType.Crush: return 25; // dmg reduction for melee dmg
				case eDamageType.Thrust: return 25; // dmg reduction for melee dmg
				default: return 25; // dmg reduction for rest resists
			}
		}
		public override void OnAttackEnemy(AttackData ad)
		{
			if (ad != null && (ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
			{
				if (Util.Chance(25) && (!ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.StunImmunity) || !ad.Target.effectListComponent.ContainsEffectForEffectType(eEffect.Stun)) && ad.Target.IsAlive)
					CastSpell(SpiritChampion_stun, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			}
			base.OnAttackEnemy(ad);
		}
		public override double GetArmorAF(eArmorSlot slot)
		{
			return 300;
		}
		public override double GetArmorAbsorb(eArmorSlot slot)
		{
			// 85% ABS is cap.
			return 0.25;
		}
		public override void WalkToSpawn()
		{
			if (IsAlive)
				return;
			base.WalkToSpawn();
		}
		public override short Strength { get => base.Strength; set => base.Strength = 150; }
		public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
		List<ushort> spirit_champion_models = new List<ushort>()
		{
			153,162,137,146,773,784,169,178,185,194
		};
		public static int ArosPetCount = 0;
		public override bool AddToWorld()
		{
			Name = "spirit champion";
			Model = spirit_champion_models[Util.Random(0, spirit_champion_models.Count - 1)];
			GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
			template.AddNPCEquipment(eInventorySlot.TorsoArmor, 295, 0, 0, 0); //Slot,model,color,effect,extension
			template.AddNPCEquipment(eInventorySlot.ArmsArmor, 297, 0);
			template.AddNPCEquipment(eInventorySlot.LegsArmor, 296, 0);
			template.AddNPCEquipment(eInventorySlot.HandsArmor, 298, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.FeetArmor, 299, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.HeadArmor, 1216, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.Cloak, 677, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 310, 0, 0, 0);
			template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 79, 0, 0, 0);
			Inventory = template.CloseTemplate();
			SwitchWeapon(eActiveWeaponSlot.Standard);

			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(779));

			VisibleActiveWeaponSlots = 16;
			++ArosPetCount;
			Size = 50;
			Level = 62;
			MaxSpeedBase = 225;
			BlockChance = 40;
			RespawnInterval = -1;
			Flags ^= eFlags.GHOST;
			Realm = eRealm.None;
			SpiritmasterArosPetBrain adds = new SpiritmasterArosPetBrain();
			adds.AggroRange = 600;
			adds.AggroLevel = 100;
			SetOwnBrain(adds);
			base.AddToWorld();
			return true;
		}
        public override void Die(GameObject killer)
        {
			--ArosPetCount;
            base.Die(killer);
        }
        public override void DropLoot(GameObject killer) //no loot
		{
		}
		public override long ExperienceValue => 0;
		private Spell m_SpiritChampion_stun;
		private Spell SpiritChampion_stun
		{
			get
			{
				if (m_SpiritChampion_stun == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 0;
					spell.RecastDelay = 2;
					spell.ClientEffect = 2165;
					spell.Icon = 2132;
					spell.TooltipId = 2132;
					spell.Duration = 5;
					spell.Description = "Target is stunned and cannot move or take any other action for the duration of the spell.";
					spell.Name = "Stun";
					spell.Range = 400;
					spell.SpellID = 11915;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Stun.ToString();
					m_SpiritChampion_stun = new Spell(spell, 70);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_SpiritChampion_stun);
				}
				return m_SpiritChampion_stun;
			}
		}
	}
}
namespace DOL.AI.Brain
{
	public class SpiritmasterArosPetBrain : StandardMobBrain
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public SpiritmasterArosPetBrain()
		{
			AggroLevel = 100;
			AggroRange = 450;
		}
		public override void Think()
		{
			if (!HasAggressionTable())
			{
				foreach (GameNPC aros in Body.GetNPCsInRadius(5000))
				{
					if (aros != null)
					{
						if (aros.IsAlive && aros.Brain is SpiritmasterArosBrain)
							Body.Follow(aros, 100, 5000);
					}
				}
			}
			if (HasAggro)
			{
				foreach (GameNPC aros in Body.GetNPCsInRadius(5000))
				{
					if (aros != null)
					{
						if (aros.IsAlive && aros.Brain is SpiritmasterArosBrain brain)
						{
							GameLiving target = Body.TargetObject as GameLiving;
							if (target != null)
							{
								if (!brain.HasAggro)
									brain.AddToAggroList(target, 100);
							}
						}
					}
				}
			}
			base.Think();
		}
	}
}