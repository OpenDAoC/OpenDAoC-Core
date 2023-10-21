﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Language;

namespace Core.GS
{
	public static class GamePlayerUtil
	{
		#region Spot and Area Description / Translation
		/// <summary>
		/// Get Spot Description Checking Any Area with Description or Zone Description
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="spot"></param>
		/// <returns></returns>
		public static string GetSpotDescription(this Region reg, IPoint3D spot)
		{
			return reg.GetSpotDescription(spot.X, spot.Y, spot.Z);
		}
		
		/// <summary>
		/// Get Spot Description Checking Any Area with Description or Zone Description
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static string GetSpotDescription(this Region reg, int x, int y, int z)
		{
			if (reg != null)
			{
				var area = reg.GetAreasOfSpot(x, y, z).OfType<AbstractArea>().FirstOrDefault(a => a.DisplayMessage && !string.IsNullOrEmpty(a.Description));
				
				if (area != null)
					return area.Description;
				
				var zone = reg.GetZone(x, y);
				
				if (zone != null)
					return zone.Description;
				
				return reg.Description;
			}
			
			return string.Empty;
		}

		/// <summary>
		/// Get Spot Description Checking Any Area with Description or Zone Description and Try Translating it
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="client"></param>
		/// <param name="spot"></param>
		/// <returns></returns>
		public static string GetTranslatedSpotDescription(this Region reg, GameClient client, IPoint3D spot)
		{
			return reg.GetTranslatedSpotDescription(client, spot.X, spot.Y, spot.Z);
		}
		
		/// <summary>
		/// Get Spot Description Checking Any Area with Description or Zone Description and Try Translating it
		/// </summary>
		/// <param name="reg"></param>
		/// <param name="client"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static string GetTranslatedSpotDescription(this Region reg, GameClient client, int x, int y, int z)
		{
			if (reg != null)
			{
				var area = reg.GetAreasOfSpot(x, y, z).OfType<AbstractArea>().FirstOrDefault(a => a.DisplayMessage);
				
				// Try Translate Area First
				if (area != null)
				{
					var lng = LanguageMgr.GetTranslation(client, area) as DbLanguageArea;
					
					if (lng != null && !string.IsNullOrEmpty(lng.ScreenDescription))
						return lng.ScreenDescription;
							
					return area.Description;
				}
				
				var zone = reg.GetZone(x, y);
				
				// Try Translate Zone
				if (zone != null)
				{
					var lng = LanguageMgr.GetTranslation(client, zone) as DbLanguageZone;
					if (lng != null)
						return lng.ScreenDescription;
					
					return zone.Description;
				}
				
				return reg.Description;
			}
			
			return string.Empty;			
		}
		
		/// <summary>
		/// Get Player Spot Description Checking Any Area with Description or Zone Description and Try Translating it
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static string GetTranslatedSpotDescription(this GamePlayer player)
		{
			return player.GetTranslatedSpotDescription(player.CurrentRegion, player.X, player.Y, player.Z);
		}
		
		/// <summary>
		/// Get Player Spot Description Checking Any Area with Description or Zone Description and Try Translating it
		/// </summary>
		/// <param name="player"></param>
		/// <param name="region"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static string GetTranslatedSpotDescription(this GamePlayer player, Region region, int x, int y, int z)
		{
			return player.Client.GetTranslatedSpotDescription(region, x, y, z);
		}
		
		/// <summary>
		/// Get Client Spot Description Checking Any Area with Description or Zone Description and Try Translating it
		/// </summary>
		/// <param name="client"></param>
		/// <param name="region"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		public static string GetTranslatedSpotDescription(this GameClient client, Region region, int x, int y, int z)
		{
			return region.GetTranslatedSpotDescription(client, x, y, z);
		}
		
		/// <summary>
		/// Get Player Spot Description Checking Any Area with Description or Zone Description 
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static string GetSpotDescription(this GamePlayer player)
		{
			return player.GetTranslatedSpotDescription();
		}
		
		/// <summary>
		/// Get Player's Bind Spot Description Checking Any Area with Description or Zone Description 
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static string GetBindSpotDescription(this GamePlayer player)
		{
			return player.GetTranslatedSpotDescription(WorldMgr.GetRegion((ushort)player.BindRegion), player.BindXpos, player.BindYpos, player.BindZpos);
		}
		#endregion
		
		#region player skills / bonuses
		/// <summary>
		/// Updates all disabled skills to player
		/// </summary>
		public static void UpdateDisabledSkills(this GamePlayer player)
		{
			player.Out.SendDisableSkill(player.GetAllUsableSkills().Select(skt => skt.Item1).Where(sk => !(sk is Specialization))
			         .Union(player.GetAllUsableListSpells().SelectMany(sl => sl.Item2))
			         .Select(sk => new Tuple<Skill, int>(sk, player.GetSkillDisabledDuration(sk))).ToArray());
		}

		/// <summary>
		/// Reset all disabled skills to player
		/// </summary>
		public static void ResetDisabledSkills(this GamePlayer player)
		{
			foreach (Skill skl in player.GetAllDisabledSkills())
			{
				player.RemoveDisabledSkill(skl);
			}
			
			player.UpdateDisabledSkills();
		}

		/// <summary>
		/// Delve Player Bonuses for Info Window
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static ICollection<string> GetBonusesInfo(this GamePlayer player)
		{
			var info = new List<string>();
			
			/*
			<Begin Info: Bonuses (snapshot)>
			Resistances
			 Crush: +25%/+0%
			 Slash: +28%/+0%
			 Thrust: +28%/+0%
			 Heat: +25%/+0%
			 Cold: +25%/+0%
			 Matter: +26%/+0%
			 Body: +31%/+0%
			 Spirit: +21%/+0%
			 Energy: +26%/+0%

			Special Item Bonuses
			 +20% to Power Pool
			 +3% to all Melee Damage
			 +6% to all Stat Buff Spells
			 +23% to all Heal Spells
			 +3% to Melee Combat Speed
			 +10% to Casting Speed

			Realm Rank Bonuses
			 +7 to ALL Specs

			Relic Bonuses
			 +20% to all Melee Damage

			Outpost Bonuses
			 none

			<End Info>
			 */

			//AbilityBonus[(int)((eProperty)updateResists[i])]
			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Resist"));
			info.Add(string.Format(" {2}:   {0:+0;-0}%/\t{1:+0;-0}%", player.GetModified(EProperty.Resist_Crush) - player.AbilityBonus[(int)EProperty.Resist_Crush], player.AbilityBonus[(int)EProperty.Resist_Crush], SkillBase.GetPropertyName(EProperty.Resist_Crush)));
			info.Add(string.Format(" {2}:    {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Slash) - player.AbilityBonus[(int)EProperty.Resist_Slash], player.AbilityBonus[(int)EProperty.Resist_Slash], SkillBase.GetPropertyName(EProperty.Resist_Slash)));
			info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Thrust) - player.AbilityBonus[(int)EProperty.Resist_Thrust], player.AbilityBonus[(int)EProperty.Resist_Thrust], SkillBase.GetPropertyName(EProperty.Resist_Thrust)));
			info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Heat) - player.AbilityBonus[(int)EProperty.Resist_Heat], player.AbilityBonus[(int)EProperty.Resist_Heat], SkillBase.GetPropertyName(EProperty.Resist_Heat)));
			info.Add(string.Format(" {2}:      {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Cold) - player.AbilityBonus[(int)EProperty.Resist_Cold], player.AbilityBonus[(int)EProperty.Resist_Cold], SkillBase.GetPropertyName(EProperty.Resist_Cold)));
			info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Matter) - player.AbilityBonus[(int)EProperty.Resist_Matter], player.AbilityBonus[(int)EProperty.Resist_Matter], SkillBase.GetPropertyName(EProperty.Resist_Matter)));
			info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Body) - player.AbilityBonus[(int)EProperty.Resist_Body], player.AbilityBonus[(int)EProperty.Resist_Body], SkillBase.GetPropertyName(EProperty.Resist_Body)));
			info.Add(string.Format(" {2}:     {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Spirit) - player.AbilityBonus[(int)EProperty.Resist_Spirit], player.AbilityBonus[(int)EProperty.Resist_Spirit], SkillBase.GetPropertyName(EProperty.Resist_Spirit)));
			info.Add(string.Format(" {2}:  {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Energy) - player.AbilityBonus[(int)EProperty.Resist_Energy], player.AbilityBonus[(int)EProperty.Resist_Energy], SkillBase.GetPropertyName(EProperty.Resist_Energy)));
			info.Add(string.Format(" {2}: {0:+0;-0}%/{1:+0;-0}%", player.GetModified(EProperty.Resist_Natural) - player.AbilityBonus[(int)EProperty.Resist_Natural], player.AbilityBonus[(int)EProperty.Resist_Natural], SkillBase.GetPropertyName(EProperty.Resist_Natural)));

			info.Add(" ");
			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Special"));

			//This is an Array of the bonuses that show up in the Bonus Snapshot on Live, the only ones that really need to be there.
			int[] bonusToBeDisplayed = new int[37] { 9, 10, 150, 151, 153, 154, 155, 173, 174, 179, 180, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 200, 210, 247, 248, 251, 252, 253, 254};
			for (int i = 0; i < (int)EProperty.MaxProperty; i++)
			{
				if ((player.ItemBonus[i] > 0) && ((Array.BinarySearch(bonusToBeDisplayed, i)) >= 0)) //Tiny edit here to add the binary serach to weed out the non essential bonuses
				{
					if (player.ItemBonus[i] != 0)
					{
						//LIFEFLIGHT Add, to correct power pool from showing too much
						//This is where we need to correct the display, make it cut off at the cap if
						//Same with hits and hits cap
						if (i == (int)EProperty.PowerPool)
						{
							int powercap = player.ItemBonus[(int)EProperty.PowerPoolCapBonus];
							if (powercap > 50)
							{
								powercap = 50;
							}
							int powerpool = player.ItemBonus[(int)EProperty.PowerPool];
							if (powerpool > 26)
								powerpool = 26;
							if (powerpool > powercap + 25)
							{
								int tempbonus = powercap + 25;
								info.Add(ItemBonusDescription(tempbonus, i));
							}
							else
							{
								int tempbonus = powerpool;
								info.Add(ItemBonusDescription(tempbonus, i));
							}


						}
						else if (i == (int)EProperty.MaxHealth)
						{
							int hitscap = player.ItemBonus[(int)EProperty.MaxHealthCapBonus];
							if (hitscap > 200)
							{
								hitscap = 200;
							}
							int hits = player.ItemBonus[(int)EProperty.MaxHealth];
							if (hits > hitscap + 200)
							{
								int tempbonus = hitscap + 200;
								info.Add(ItemBonusDescription(tempbonus, i));
							}
							else
							{
								int tempbonus = hits;
								info.Add(ItemBonusDescription(tempbonus, i));
							}
						}
						else
						{
							info.Add(ItemBonusDescription(player.ItemBonus[i], i));
						}
					}
				}
			}

			info.Add(" ");
			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Realm"));
			if (player.RealmLevel > 10)
				info.Add(string.Format(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Specs", player.RealmLevel / 10)));
			else
				info.Add(" none");
			info.Add(" ");
			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Relic"));

			double meleeRelicBonus = RelicMgr.GetRelicBonusModifier(player.Realm, ERelicType.Strength);
			double magicRelicBonus = RelicMgr.GetRelicBonusModifier(player.Realm, ERelicType.Magic);

			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Melee", (meleeRelicBonus * 100)));
			info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Magic", (magicRelicBonus * 100)));

			// info.Add(" ");
			// info.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Outpost"));
			// info.Add("TODO, this needs to be written");

			return info;
		}
		
		/// <summary>
		/// Helper For Bonus Description
		/// </summary>
		/// <param name="iBonus"></param>
		/// <param name="iBonusType"></param>
		/// <returns></returns>
		private static string ItemBonusDescription(int iBonus, int iBonusType)
		{
			//This displays the bonuses just like the Live servers. there is a check against the pts/% differences
			string str = ((iBonusType == 9) | (iBonusType == 150) | (iBonusType == 210) | (iBonusType == 10) | (iBonusType == 151) | (iBonusType == 186)) ? "pts of " : "% to ";  //150(Health Regen) 151(PowerRegen) and 186(Style reductions) need the prefix of "pts of " to be correct

			//we need to only display the effective bonus, cut off caps

			//Lifeflight add

			//iBonusTypes that cap at 10
			//SpellRange, ArcheryRange, MeleeSpeed, MeleeDamage, RangedDamage, ArcherySpeed,
			//CastingSpeed, ResistPierce, SpellDamage, StyleDamage
			if (iBonusType == (int)EProperty.SpellRange || iBonusType == (int)EProperty.ArcheryRange || iBonusType == (int)EProperty.MeleeSpeed
			    || iBonusType == (int)EProperty.MeleeDamage || iBonusType == (int)EProperty.RangedDamage || iBonusType == (int)EProperty.ArcherySpeed
			    || iBonusType == (int)EProperty.CastingSpeed || iBonusType == (int)EProperty.ResistPierce || iBonusType == (int)EProperty.SpellDamage || iBonusType == (int)EProperty.StyleDamage)
			{
				if (iBonus > 10)
					iBonus = 10;
			}

			//cap at 25 with no chance of going over
			//DebuffEffectivness, BuffEffectiveness, HealingEffectiveness
			//SpellDuration
			if (iBonusType == (int)EProperty.DebuffEffectivness || iBonusType == (int)EProperty.BuffEffectiveness || iBonusType == (int)EProperty.HealingEffectiveness || iBonusType == (int)EProperty.SpellDuration || iBonusType == (int)EProperty.ArcaneSyphon)
			{
				if (iBonus > 25)
					iBonus = 25;
			}
			//hitscap, caps at 200
			if (iBonusType == (int)EProperty.MaxHealthCapBonus)
			{
				if (iBonus > 200)
					iBonus = 200;
			}
			//cap at 25, but can get overcaps
			//PowerPool
			//This will need to be done in the method that calls this method.
			//if (iBonusType == (int)eProperty.PowerPool)
			//{
			//    if (iBonus > 25)
			//        iBonus = 50;
			//}
			//caps at 50
			//PowerPoolCapBonus
			if (iBonusType == (int)EProperty.PowerPoolCapBonus)
			{
				if (iBonus > 50)
					iBonus = 50;
			}

			return string.Format("+{0}{1}{2}", iBonus, str, SkillBase.GetPropertyName(((EProperty)iBonusType)));
		}
		#endregion
	}
}
