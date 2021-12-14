/*
Mistress of Runes.
<author>Kelt</author>
 */
using System;
using System.Collections.Generic;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using System.Reflection;
using System.Collections;
using DOL.AI.Brain;


namespace DOL.GS.Scripts
{

	public class MistressOfRunes : GameEpicMistress
	{
		//Re-Cast every 20 seconds.
		public const int SpearRecastInterval = 20;
		//Re-Cast every 30 seconds.
		public const int NearsightRecastInterval = 30;

		/// <summary>
		/// Set Mistress of Runes Stats and Equiptemplate
		/// </summary>
		public override bool AddToWorld()
		{
			LoadEquipmentTemplateFromDatabase("Mistress_of_Runes");
			Realm = eRealm.None;
			Model = 163;
			Size = 50;
			Level = 63;
			Strength = 855;
			Dexterity = 250;
			Quickness = 90;
			Constitution = 1200;
			Intelligence = 320;
			Health = MaxHealth;
			Piety = 220;
			Empathy = 220;
			Charisma = 220;
			Faction = FactionMgr.GetFactionByID(779);
			Faction.AddFriendFaction(FactionMgr.GetFactionByID(778));
			Name = "Mistress of Runes";
			base.AddToWorld();
			BroadcastLivingEquipmentUpdate();
			SetOwnBrain(new MistressBrain());
			
			return true;
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (log.IsInfoEnabled)
				log.Info("Mistress of Runes NPC Initializing...");
		}		

        #region Runemaster AoE Spear

        /// <summary>
        /// The AoE spell.
        /// </summary>
        protected override Spell AoESpear
		{
			get
			{
				if (m_AoESpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 4;
					spell.ClientEffect = 2958;
					spell.Icon = 2958;
					spell.Damage = 1000 * MistressDifficulty / 100;
					spell.Name = "Odin's Hatred";
					spell.Range = 1000;
					spell.Radius = 450;
					spell.SpellID = 2958;
					//spell.Duration = SpearRecastInterval;
					spell.Target = "Enemy";
					spell.Type = eSpellType.DirectDamage.ToString();
					spell.Uninterruptible = true;
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

		/// <summary>
		/// The Nearsight spell.
		/// </summary>
		protected override Spell Nearsight
		{
			get
			{
				if (m_NearsightSpell == null)
				{
					DBSpell spell = new DBSpell();
					spell.AllowAdd = false;
					spell.CastTime = 2;
					spell.Uninterruptible = true;
					spell.ClientEffect = 2735;
					spell.Icon = 2735;
					spell.Description = "Nearsight";
					spell.Name = "Diminish Vision";
					spell.Range = 1500;
					spell.Radius = 1500;
					//spell.Duration = NearsightRecastInterval;
					spell.Value = 65;
					spell.Duration = 90 * MistressDifficulty / 100;
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