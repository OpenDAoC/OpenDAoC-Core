using System;
using System.Collections.Generic;
using DOL.GS.Keeps;
using DOL.GS.ServerRules;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	   "&realm",
	   ePrivLevel.Player,
		 "Displays the current realm status.", "/realm")]
	public class RealmCommandHandler : AbstractCommandHandler, ICommandHandler
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

			string albKeeps = string.Empty;
			string midKeeps = string.Empty;
			string hibKeeps = string.Empty;
			ICollection<AbstractGameKeep> keepList = GameServer.KeepManager.GetFrontierKeeps();
			ICollection<AbstractGameKeep> albKeepList = GameServer.KeepManager.GetKeepsOfRegion(1);
			ICollection<AbstractGameKeep> midKeepList = GameServer.KeepManager.GetKeepsOfRegion(100);
			ICollection<AbstractGameKeep> hibKeepList = GameServer.KeepManager.GetKeepsOfRegion(200);

			foreach (AbstractGameKeep keep in albKeepList)
			{
				if (keep.IsPortalKeep)
					continue;

				if (keep.Name.Contains("myrddin", StringComparison.OrdinalIgnoreCase) ||
					keep.Name.Contains("excalibur", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				
				if (keep is GameKeep)
					albKeeps += KeepStringBuilder(keep);
			}

			foreach (AbstractGameKeep keep in midKeepList)
			{
				if (keep.IsPortalKeep)
					continue;

				if (keep.Name.Contains("grallarhorn", StringComparison.OrdinalIgnoreCase) ||
					keep.Name.Contains("mjollner", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (keep is GameKeep)
					midKeeps += KeepStringBuilder(keep);
			}
			
			foreach (AbstractGameKeep keep in hibKeepList)
			{
				if (keep.IsPortalKeep)
					continue;

				if (keep.Name.Contains("dagda", StringComparison.OrdinalIgnoreCase) ||
					keep.Name.Contains("lamfhota", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				
				if (keep is GameKeep)
					hibKeeps += KeepStringBuilder(keep);
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

			if (ServerProperties.Properties.ALLOW_ALL_REALMS_DF)
			{
				realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.DarknessFalls") + ": All Realms");
			}
			else
			{
				realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.DarknessFalls") + ": " + GlobalConstants.RealmToName(DFEnterJumpPoint.DarknessFallOwner));
				if (DFEnterJumpPoint.LastRealmSwapTick + DFEnterJumpPoint.GracePeriod >= GameLoop.GameLoopTime)
				{
					var pve = DFEnterJumpPoint.LastRealmSwapTick + DFEnterJumpPoint.GracePeriod - GameLoop.GameLoopTime;
					string realmName = string.Empty;
					if (DFEnterJumpPoint.PreviousOwner == eRealm._LastPlayerRealm || 
					    DFEnterJumpPoint.PreviousOwner == eRealm.Hibernia)
						realmName = "Hibernia";
					if (DFEnterJumpPoint.PreviousOwner == eRealm._FirstPlayerRealm ||
					    DFEnterJumpPoint.PreviousOwner == eRealm.Albion)
						realmName = "Albion";
					if (DFEnterJumpPoint.PreviousOwner == eRealm.Midgard)
						realmName = "Midgard";
					if(realmName != string.Empty)
						realmInfo.Add(realmName + " can enter Darkness Falls for another " + TimeSpan.FromMilliseconds(pve).Minutes + "m " + TimeSpan.FromMilliseconds(pve).Seconds + "s");
				}	
			}
			
			realmInfo.Add(" ");
			realmInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.UseRelicCommand"));
			client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Realm.Title"), realmInfo);
		}

		private string KeepStringBuilder(AbstractGameKeep keep)
		{
			string buffer = string.Empty;
			buffer += keep.Name + ": " + GlobalConstants.RealmToName(keep.Realm);
			if (keep.Guild != null)
			{
				buffer += " (" + keep.Guild.Name + ")";
			}
			buffer += "\n";
			return buffer;
		}
	}
}
