using System;
using System.Collections;
using System.Collections.Generic;
using Core.AI.Brain;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS;
using Core.GS.Effects;
using Core.GS.Scripts;

namespace Core.GS.Scripts
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
		public override double AttackDamage(DbInventoryItem weapon)
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
		public override int GetResist(EDamageType damageType)
		{
			switch (damageType)
			{
				case EDamageType.Slash: return 40; // dmg reduction for melee dmg
				case EDamageType.Crush: return 40; // dmg reduction for melee dmg
				case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
				default: return 70; // dmg reduction for rest resists
			}
		}
		public override double GetArmorAF(EArmorSlot slot)
		{
			return 350;
		}
		public override double GetArmorAbsorb(EArmorSlot slot)
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
		public override void ReturnToSpawnPoint(short speed)
		{
			base.ReturnToSpawnPoint(MaxSpeed);
		}

		public override void OnAttackedByEnemy(AttackData ad)
		{
			if (IsReturningToSpawnPoint)
				return;

			base.OnAttackedByEnemy(ad);
		}
		/// <summary>
		/// Broadcast relevant messages to the raid.
		/// </summary>
		/// <param name="message">The message to be broadcast.</param>
		public void BroadcastMessage(String message)
		{
			foreach (GamePlayer player in base.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				player.Out.SendMessage(message, EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
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
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mistress of Runes NPC Initializing...");
		}
	}
}