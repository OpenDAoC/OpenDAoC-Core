using System;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps;
using DOL.Database;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&battlegrounds",
        ePrivLevel.Player,
        "Info about battlegrounds",
        "/battlegrounds")]
    public class BattlegroundCommand : ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            GamePlayer player = client.Player;
            if (player == null) return;

            var bgs = new List<DbBattleground>(GameServer.Database.SelectAllObjects<DbBattleground>());
            
            bgs.Sort((a, b) => b.MinLevel.CompareTo(a.MinLevel));

            var info = new List<string>();

            foreach (DbBattleground bg in bgs)
            {
                string bgName = WorldMgr.GetRegion((ushort)bg.RegionID)?.Description ?? "Battleground " + bg.RegionID;

                int rank = bg.MaxRealmLevel / 10;
                int level = bg.MaxRealmLevel % 10;
                string rrDisplay = $"{rank}L{level}";

                info.Add($"{bgName} ({bg.MinLevel}-{bg.MaxLevel}, {rrDisplay})");

                var regionKeeps = GameServer.KeepManager.GetKeepsOfRegion((ushort)bg.RegionID);
                AbstractGameKeep centralKeep = null;
                
                foreach (AbstractGameKeep k in regionKeeps)
                {
                    if (k is GameKeep && !k.IsPortalKeep)
                    {
                        centralKeep = k;
                        break;
                    }
                }
                string keepName = "Unknown Keep";
                if (centralKeep != null)
                {
                    keepName = centralKeep.Name;
                    if (keepName.Contains(" KID:"))
                    {
                        keepName = keepName.Split(new string[] { " KID:" }, StringSplitOptions.None)[0];
                    }
                }

                string realmName = "None";
                if (centralKeep != null)
                {
                    switch (centralKeep.Realm)
                    {
                        case eRealm.Albion: realmName = "Albion"; break;
                        case eRealm.Midgard: realmName = "Midgard"; break;
                        case eRealm.Hibernia: realmName = "Hibernia"; break;
                    }
                }

                info.Add($"{keepName}: {realmName}");
                info.Add(" "); 
            }

            client.Out.SendCustomTextWindow("[ Battlegrounds Info ]", info);
        }
    }
}