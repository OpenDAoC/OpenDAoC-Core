﻿
using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using System.Collections;
using DOL.Language;
using DOL.GS.Keeps;
using DOL.GS.ServerRules;

namespace DOL.GS.Commands
{
	[Command(
	"&relics",
	new string[] {"&relic"},
	EPrivLevel.Player,
	"Displays the current relic status.", "/relics")]
   public class RelicsCommand : AbstractCommandHandler, ICommandHandler
   {
        /*          Relic status
         *
         * Albion Relics:
         * Strength: OwnerRealm
         * Power: OwnerRealm
         *
         * Midgard Relics:
         * Strength: OwnerRealm
         * Power: OwnerRealm
         *
         * Hibernia Relics:
         * Strength: OwnerRealm
         * Power: OwnerRealm
         *
         * Use '/realm' for Realm Info.
         */

        public void OnCommand(GameClient client, string[] args)
        {
			if (IsSpammingCommand(client.Player, "relic"))
				return;

            string albStr = "", albPwr = "", midStr = "", midPwr = "", hibStr = "", hibPwr = "";
			var relicInfo = new List<string>();
            


            #region Reformat Relics  '[Type]: [OwnerRealm]'
            foreach (GameStaticRelic relic in RelicMgr.getNFRelics())
            {
                string relicLoc = "";
                if (relic.Realm == ERealm.None)
                {
                    relicLoc = $" ({relic.CurrentZone.Description})";
                }

                switch (relic.OriginalRealm)
                {
                    case ERealm.Albion:
                        {
                            if (relic.RelicType == eRelicType.Strength)
								albStr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Strength") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            if (relic.RelicType == eRelicType.Magic)
								albPwr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Power") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            break;
                        }

                    case ERealm.Midgard:
                        {
                            if (relic.RelicType == eRelicType.Strength)
								midStr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Strength") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            if (relic.RelicType == eRelicType.Magic)
								midPwr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Power") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            break;
                        }

                    case ERealm.Hibernia:
                        {
                            if (relic.RelicType == eRelicType.Strength)
								hibStr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Strength") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            if (relic.RelicType == eRelicType.Magic)
								hibPwr = LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Power") + ": " + GlobalConstants.RealmToName(relic.Realm) + relicLoc + " | " + RelicMgr.GetDaysSinceCapture(relic) + "d ago";
                            break;
                        }
                }
            }
            #endregion

            relicInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.AlbRelics")+ ":");
            relicInfo.Add(albStr);
            relicInfo.Add(albPwr);
            relicInfo.Add("");
            relicInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.MidRelics") + ":");
            relicInfo.Add(midStr);
            relicInfo.Add(midPwr);
            relicInfo.Add("");
            relicInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.HibRelics") + ":");
            relicInfo.Add(hibStr);
            relicInfo.Add(hibPwr);
            relicInfo.Add("");
            relicInfo.Add(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.UseRealmCommand"));

            client.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Relic.Title"), relicInfo);
        }
   }
}