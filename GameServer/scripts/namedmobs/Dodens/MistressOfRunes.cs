/*
Mistress of Runes.
<author>Kelt</author>
 */
using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using System.Collections;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS.Scripts
{
	public class MistressOfRunes : GameEpicBoss
	{
		protected String m_DeathAnnounce;	
		public MistressOfRunes() : base()
		{
			m_DeathAnnounce = "{0} has been killed and loses her power.";
		}
		/// <summary>
		/// Add Mistress Of Runes to World
		/// </summary>
		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9907);
			LoadTemplate(npcTemplate);
			Strength = npcTemplate.Strength;
			Dexterity = npcTemplate.Dexterity;
			Constitution = npcTemplate.Constitution;
			Quickness = npcTemplate.Quickness;
			Piety = npcTemplate.Piety;
			Intelligence = npcTemplate.Intelligence;
			Empathy = npcTemplate.Empathy;

			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(778));
			LoadedFromScript = false; //load from database
			SaveIntoDatabase();
			base.AddToWorld();
			BroadcastLivingEquipmentUpdate();
			base.SetOwnBrain(new MistressOfRunesBrain());			
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
		/// <summary>
		/// Return to spawn point, Mistress of Runes can't be attacked while it's
		/// on it's way.
		/// </summary>
		public override void WalkToSpawn()
		{
			EvadeChance = 100;
			WalkToSpawn(MaxSpeed);
		}

		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (EvadeChance == 100)
				return;

			base.OnAttackedByEnemy(ad);
		}
		/// <summary>
		/// Handle event notifications.
		/// </summary>
		/// <param name="e">The event that occured.</param>
		/// <param name="sender">The sender of the event.</param>
		public override void Notify(DOLEvent e, object sender)
		{
			base.Notify(e, sender);
			// When Mistress of Runes arrives at its spawn point, make it vulnerable again.

			if (e == GameNPCEvent.ArriveAtTarget)
				EvadeChance = 0;
		}	
		/// <summary>
		/// Broadcast relevant messages to the raid.
		/// </summary>
		/// <param name="message">The message to be broadcast.</param>
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in base.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
			}
		}	
		/// <summary>
		/// Invoked when Mistress of Runes dies.
		/// </summary>
		/// <param name="killer">The living that got the killing blow.</param>
		public override void Die(GameObject killer)
		{
			BroadcastMessage(String.Format(m_DeathAnnounce, Name));
			base.StopCurrentSpellcast();
			base.Die(killer);
		}
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mistress of Runes NPC Initializing...");
		}
	}
}
namespace DOL.AI.Brain
{
	public class MistressOfRunesBrain : StandardMobBrain
	{
		protected String[] m_SpearAnnounce;
		protected String m_NearsightAnnounce;

		//Re-Cast every 15 seconds.
		public const int SpearRecastInterval = 15;
		//Re-Cast every 60 seconds.
		public const int NearsightRecastInterval = 60;

		protected bool castsSpear = true;
		protected bool castsNearsight = true;

		public MistressOfRunesBrain() : base()
		{
			AggroLevel = 200;
			AggroRange = 500;

			m_SpearAnnounce = new String[] { "{0} casts a magical flaming spear on {1}!",
					"{0} drops a flaming spear from above!",
					"{0} uses all her might to create a flaming spear.",
					"{0} casts a dangerous spell!" };
			m_NearsightAnnounce = "{0} can no longer see properly and everyone in the vicinity!";
		}

		/// <summary>
		/// Set Mistress of Runes difficulty in percent of its max abilities
		/// 100 = full strength
		/// </summary>
		public virtual int MistressDifficulty
		{
			get { return GS.ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
		}

		public override void Think()
		{
			if (Body.InCombat && Body.IsAlive && HasAggro)
			{
				if (Body.TargetObject != null)
				{
					foreach (GamePlayer player in Body.GetPlayersInRadius(2000))
					{
						if (player == null || !player.IsAlive || !player.IsVisibleTo(Body))
							return;

						//cast nearsight
						CheckNearsight(player);

						//cast AoE Spears
						new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(timer => CastSpear(timer, player)), 4000);
					}
				}
			}
			base.Think();
		}

		public override void CheckNPCAggro()
		{
			if (Body.attackComponent.AttackState)
				return;

			foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange))
			{
				if (!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active)
					continue;

				if (!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
					continue;

				if (m_aggroTable.ContainsKey(npc))
					continue; // add only new NPCs

				if (npc.Brain != null && npc.Brain is IControlledBrain)
				{
					if (CalculateAggroLevelToTarget(npc) > 0)
					{
						AddToAggroList(npc, (npc.Level + 1) << 1);
					}
				}
			}
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
		/// Try to find a potential target for Nearsight.
		/// </summary>
		/// <returns>Whether or not a target was picked.</returns>
		public bool PickNearsightTarget()
		{
			MistressOfRunes mistress = Body as MistressOfRunes;
			if (mistress == null) return false;

			ArrayList inRangeLiving = new ArrayList();

			lock ((m_aggroTable as ICollection).SyncRoot)
			{
				Dictionary<GameLiving, long>.Enumerator enumerator = m_aggroTable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameLiving living = enumerator.Current.Key;
					if (living != null &&
						living.IsAlive &&
						living.EffectList.GetOfType<NecromancerShadeEffect>() == null &&
						!mistress.IsWithinRadius(living, mistress.AttackRange))
					{
						inRangeLiving.Add(living);
					}
				}
			}

			if (inRangeLiving.Count > 0)
			{
				return CheckNearsight((GameLiving)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
			}

			return false;
		}

		/// <summary>
		/// Called whenever the Thane Dyggve's body sends something to its brain.
		/// </summary>
		/// <param name="e">The event that occured.</param>
		/// <param name="sender">The source of the event.</param>
		/// <param name="args">The event details.</param>
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			base.Notify(e, sender, args);
		}
		/// <summary>
		/// Cast Spears on the Target
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private int CastSpear(ECSGameTimer timer, GameLiving target)
		{
			if (target == null || !target.IsAlive)
				return 0;

			bool cast = Body.CastSpell(AoESpear, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			if (Body.GetSkillDisabledDuration(AoESpear) > 0)
			{
				cast = false;
			}
			if (castsSpear && cast && Body.IsCasting)
			{
				castsSpear = false;
				int messageNo = Util.Random(1, m_SpearAnnounce.Length) - 1;
				BroadcastMessage(String.Format(m_SpearAnnounce[messageNo], Body.Name, target.Name));
			}
			else
			{
				castsSpear = true;
			}
			return 0;
		}

		#region Nearsight Method
		private const int m_NearsightChance = 100;

		/// <summary>
		/// Chance to cast Nearsight when a potential target has been detected.
		/// </summary>
		protected int NearsightChance
		{
			get { return m_NearsightChance; }
		}

		private GameLiving m_NearsightTarget;

		/// <summary>
		/// The target for the next Nearsight attack.
		/// </summary>
		private GameLiving NearsightTarget
		{
			get { return m_NearsightTarget; }
			set { m_NearsightTarget = value; PrepareToNearsight(); }
		}

		/// <summary>
		/// Check whether or not to Nearsight at this target.
		/// </summary>
		/// <param name="target">The potential target.</param>
		/// <returns>Whether or not the spell was cast.</returns>
		public bool CheckNearsight(GameLiving target)
		{
			if (target == null || NearsightTarget != null) return false;
			bool success = Util.Chance(NearsightChance);
			if (success)
				NearsightTarget = target;
			return success;
		}

		/// <summary>
		/// Announce the Nearsight and start the 2 second timer.
		/// </summary>
		private void PrepareToNearsight()
		{
			if (NearsightTarget == null) return;
			Body.TurnTo(NearsightTarget);

			new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastNearsight), 2000);
		}

		/// <summary>
		/// Cast Nearsight on the target.
		/// </summary>
		/// <param name="timer">The timer that started this cast.</param>
		/// <returns></returns>
		private int CastNearsight(ECSGameTimer timer)
		{
			// Turn around to the target and cast Nearsight, then go back to the original
			// target, if one exists.

			GameObject oldTarget = Body.TargetObject;
			Body.TargetObject = NearsightTarget;
			Body.Z = Body.SpawnPoint.Z; // this is a fix to correct Z errors that sometimes happen during Mistress fights
			Body.TurnTo(NearsightTarget);
			bool cast = Body.CastSpell(Nearsight, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
			if (Body.GetSkillDisabledDuration(Nearsight) > 0)
			{
				cast = false;
			}
			if (castsNearsight && cast && Body.IsCasting)
			{
				castsNearsight = false;
				BroadcastMessage(String.Format(m_NearsightAnnounce, NearsightTarget.Name));
			}
			else
			{
				castsNearsight = true;
			}
			NearsightTarget = null;
			if (oldTarget != null) Body.TargetObject = oldTarget;
			return 0;
		}

		#endregion

		#region Runemaster AoE Spear
		private Spell m_AoESpell;
		/// <summary>
		/// The AoE spell.
		/// </summary>
		protected Spell AoESpear
		{
			get
			{
				if (m_AoESpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.Uninterruptible = true;
					spell.CastTime = 3;
					spell.ClientEffect = 2958;
					spell.Icon = 2958;
					spell.Damage = 450 * MistressDifficulty / 100;
					spell.Name = "Odin's Hatred";
					spell.Range = 1000;
					spell.Radius = 450;
					spell.SpellID = 2958;
					spell.RecastDelay = SpearRecastInterval;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamageNoVariance.ToString();
					spell.MoveCast = false;
					spell.DamageType = (int)eDamageType.Energy; //Energy DMG Type
					m_AoESpell = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AoESpell);
				}
				return m_AoESpell;
			}
		}

		#endregion

		#region Runemaster Nearsight
		private Spell m_NearsightSpell;
		/// <summary>
		/// The Nearsight spell.
		/// </summary>
		protected Spell Nearsight
		{
			get
			{
				if (m_NearsightSpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.Uninterruptible = true;
					spell.CastTime = 1;
					spell.ClientEffect = 2735;
					spell.Icon = 2735;
					spell.Description = "Nearsight";
					spell.Name = "Diminish Vision";
					spell.Range = 1500;
					spell.Radius = 1500;
					spell.RecastDelay = NearsightRecastInterval;
					spell.Value = 65;
					spell.Duration = 45 * MistressDifficulty / 100;
					spell.Damage = 0;
					spell.DamageType = (int)eDamageType.Energy;
					spell.SpellID = 2735;
					spell.Target = "Enemy";
					spell.Type = eSpellType.Nearsight.ToString();
					spell.Message1 = "You are blinded!";
					spell.Message2 = "{0} is blinded!";
					m_NearsightSpell = new Spell(spell, 60);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_NearsightSpell);
				}
				return m_NearsightSpell;
			}
		}
		#endregion
	}
}