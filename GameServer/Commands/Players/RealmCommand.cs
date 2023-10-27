﻿using System;
using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Keeps;
using Core.GS.Languages;
using Core.GS.Server;

namespace Core.GS.Commands;

[Command(
   "&realm",
   EPrivLevel.Player,
	 "Displays the current realm status.", "/realm")]
public class RealmCommand : ACommandHandler, ICommandHandler
{
	/*          Realm status
	 *
	 * Albion Keeps:
	 * Caer Benowyc: OwnerRealm (Guild)
	 * Caer Berkstead: OwnerRealm (Guild)
	 * Caer Erasleigh: OwnerRealm (Guild)
	 * Caer Boldiam: OwnerRealm (Guild)
	 * Caer Sursbrooke: OwnerRealm (Guild)
	 * Caer Hurbury: OwnerRealm (Guild)
	 * Caer Renaris: OwnerRealm (Guild)
	 *
	 * Midgard Keeps:
	 * Bledmeer Faste: OwnerRealm (Guild)
	 * Notmoor Faste: OwnerRealm (Guild)
	 * Hlidskialf Faste: OwnerRealm (Guild)
	 * Blendrake Faste: OwnerRealm (Guild)
	 * Glenlock Faste: OwnerRealm (Guild)
	 * Fensalir Faste: OwnerRealm (Guild)
	 * Arvakr Faste: OwnerRealm (Guild)
	 *
	 * Hibernia Keeps:
	 * Dun Chrauchon: OwnerRealm (Guild)
	 * Dun Crimthainn: OwnerRealm (Guild)
	 * Dun Bolg: OwnerRealm (Guild)
	 * Dun na nGed: OwnerRealm (Guild)
	 * Dun da Behnn: OwnerRealm (Guild)
	 * Dun Scathaig: OwnerRealm (Guild)
	 * Dun Ailinne: OwnerRealm (Guild)
	 *
	 * Darkness Falls: DFOwnerRealm
	 *
	 * Type '/relic' to display the relic status.
	 */
	
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "realm"))
			return;

		string albKeeps = "";
		string midKeeps = "";
		string hibKeeps = "";
		ICollection<AGameKeep> keepList = GameServer.KeepManager.GetFrontierKeeps();
		ICollection<AGameKeep> albKeepList = GameServer.KeepManager.GetKeepsOfRegion(1);
		ICollection<AGameKeep> midKeepList = GameServer.KeepManager.GetKeepsOfRegion(100);
		ICollection<AGameKeep> hibKeepList = GameServer.KeepManager.GetKeepsOfRegion(200);

		foreach (AGameKeep keep in albKeepList)
		{
			if (keep.Name.ToLower().Contains("myrddin") || keep.Name.ToLower().Contains("excalibur"))
				continue;
			
			if (keep is GameKeep)
			{
				albKeeps += KeepStringBuilder(keep);
			}
				
		}

		foreach (AGameKeep keep in midKeepList)
		{
			if (keep.Name.ToLower().Contains("grallarhorn") || keep.Name.ToLower().Contains("mjollner"))
				continue;
			if (keep is GameKeep)
			{
				midKeeps += KeepStringBuilder(keep);
			}
				
		}
		
		foreach (AGameKeep keep in hibKeepList)
		{
			if (keep.Name.ToLower().Contains("dagda") || keep.Name.ToLower().Contains("lamfhota"))
				continue;
			
			if (keep is GameKeep)
			{
				hibKeeps += KeepStringBuilder(keep);
			}
				
		}
		
		// foreach (AbstractGameKeep keep in keepList)
		// {
		// 	if (keep is GameKeep)
		// 	{
		// 		switch (keep.OriginalRealm)
		// 		{
		// 			case eRealm.Albion:
		// 				albKeeps += KeepStringBuilder(keep);
		// 				break;
		// 			case eRealm.Hibernia:
		// 				hibKeeps += KeepStringBuilder(keep);
		// 				break;
		// 			case eRealm.Midgard:
		// 				midKeeps += KeepStringBuilder(keep);
		// 				break;
		// 		}
		// 	}
		// }
		var realmInfo = new List<string>();
		realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.AlbKeeps") + ":");
		realmInfo.Add(albKeeps);
		realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.MidKeeps") + ":");
		realmInfo.Add(midKeeps);
		realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.HibKeeps") + ":");
		realmInfo.Add(hibKeeps);

		if (ServerProperty.ALLOW_ALL_REALMS_DF)
		{
			realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.DarknessFalls") + ": All Realms");
		}
		else
		{
			realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.DarknessFalls") + ": " + GlobalConstants.RealmToName(DfEnterJumpPoint.DarknessFallOwner));
			if (DfEnterJumpPoint.LastRealmSwapTick + DfEnterJumpPoint.GracePeriod >= GameLoopMgr.GameLoopTime)
			{
				var pve = DfEnterJumpPoint.LastRealmSwapTick + DfEnterJumpPoint.GracePeriod - GameLoopMgr.GameLoopTime;
				string realmName = "";
				if (DfEnterJumpPoint.PreviousOwner == ERealm._LastPlayerRealm || 
				    DfEnterJumpPoint.PreviousOwner == ERealm.Hibernia)
					realmName = "Hibernia";
				if (DfEnterJumpPoint.PreviousOwner == ERealm._FirstPlayerRealm ||
				    DfEnterJumpPoint.PreviousOwner == ERealm.Albion)
					realmName = "Albion";
				if (DfEnterJumpPoint.PreviousOwner == ERealm.Midgard)
					realmName = "Midgard";
				if(realmName != "")
					realmInfo.Add(realmName + " can enter Darkness Falls for another " + TimeSpan.FromMilliseconds(pve).Minutes + "m " + TimeSpan.FromMilliseconds(pve).Seconds + "s");
			}	
		}
		
		realmInfo.Add(" ");
		realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.UseRelicCommand"));
		client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.Title"), realmInfo);
	}

	private string KeepStringBuilder(AGameKeep keep)
	{
		string buffer = "";
		buffer += keep.Name + ": " + GlobalConstants.RealmToName(keep.Realm);
		if (keep.Guild != null)
		{
			buffer += " (" + keep.Guild.Name + ")";
		}
		buffer += "\n";
		return buffer;
	}
}