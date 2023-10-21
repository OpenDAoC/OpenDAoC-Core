using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using log4net;

namespace Core.GS
{
	public class StealtherAbilityHandlers
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.KillsTotalDeathBlowsChanged, new CoreEventHandler(AssassinsAbilities));
		}
		
		private static void AssassinsAbilities(CoreEvent e, object sender, EventArgs arguments)
		{
			GamePlayer player = sender as GamePlayer;
			
			//Shadowblade-Blood Rage
			if (player.HasAbility(Abilities.BloodRage))
				player.CastSpell(BR, (SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells)));
			
			//Infiltrator-Heightened Awareness
			if (player.HasAbility(Abilities.HeightenedAwareness))
				player.CastSpell(HA, (SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells)));
			
			//Nightshade-Subtle Kills
			if (player.HasAbility(Abilities.SubtleKills))
				player.CastSpell(SK, (SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells)));
		}


		#region Blood Rage Spell
		protected static Spell Blood_Rage;
		public static Spell BR
		{
			get
			{
				if (Blood_Rage == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = true;
					spell.CastTime = 0;
					spell.Uninterruptible = true;
					spell.Icon = 10541;
					spell.ClientEffect = 10541;
					spell.Description = "Movement speed of the player in stealth is increased by 25% for 1 minute after they get a killing blow on a realm enemy.";
					spell.Name = "Blood Rage";
					spell.Range = 0;
					spell.Value = 25;
					spell.Duration = 60;
					spell.SpellID = 900090;
					spell.Target = "Self";
					spell.Type = ESpellType.BloodRage.ToString();
					Blood_Rage = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Reserved_Spells, Blood_Rage);
				}
				return Blood_Rage;
			}
		}
		#endregion

		#region Heightened Awareness Spell
		protected static Spell Heightened_Awareness;
		public static Spell HA
		{
			get
			{
				if (Heightened_Awareness == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = true;
					spell.CastTime = 0;
					spell.Uninterruptible = true;
					spell.Icon = 10541;
					spell.ClientEffect = 10541;
					spell.Description = "Greater Chance to Detect Stealthed Enemies for 1 minute after executing a klling blow on a realm opponent.";
					spell.Name = "Heightened Awareness";
					spell.Range = 0;
					spell.Value = 25;
					spell.Duration = 60;
					spell.SpellID = 900091;
					spell.Target = "Self";
					spell.Type = ESpellType.HeightenedAwareness.ToString();
					Heightened_Awareness = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Reserved_Spells, Heightened_Awareness);
				}
				return Heightened_Awareness;
			}
		}
		#endregion

		#region Subtle Kills Spell
		protected static Spell Subtle_Kills;
		public static Spell SK
		{
			get
			{
				if (Subtle_Kills == null)
				{
					DbSpell spell = new DbSpell();
					spell.AllowAdd = true;
					spell.CastTime = 0;
					spell.Uninterruptible = true;
					spell.Icon = 10541;
					spell.ClientEffect = 10541;
					spell.Description = "Greater chance of remaining hidden while stealthed for 1 minute after executing a killing blow on a realm opponent.";
					spell.Name = "Subtle Kills";
					spell.Range = 0;
					spell.Value = 25;
					spell.Duration = 60;
					spell.SpellID = 900092;
					spell.Target = "Self";
					spell.Type = ESpellType.SubtleKills.ToString();
					Subtle_Kills = new Spell(spell, 50);
					SkillBase.AddScriptedSpell(GlobalSpellsLines.Reserved_Spells, Subtle_Kills);
				}
				return Subtle_Kills;
			}
		}
		#endregion
	}
}
