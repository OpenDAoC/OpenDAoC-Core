using System;
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Database;
using System.Collections.Generic;
namespace DOL.GS.Scripts
{
    public class Herald : GameNPC
    {

        public Herald() : base() { }
        public override bool AddToWorld()
        {
            Model = 2026;
            Name = "Faelyn";
            GuildName = "Atlas Herald";
            Level = 50;
            Size = 60;
            Flags |= eFlags.PEACE;
            base.AddToWorld();
            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;
            
            TurnTo(player, 500);

            DOLCharacters[] chars = (DOLCharacters[])GameServer.Database.SelectObjects<DOLCharacters>("RealmPoints > 0 ORDER BY RealmPoints DESC LIMIT 25");
            List<string> list = new List<string>();
            
            list.Add("Top 25 Highest Realm Points:\n\n");
            int count = 1;
            foreach (DOLCharacters chr in chars)
            {
                string str = "#" + count + ": " + chr.Name + "(" + chr.Realm + ") - " + chr.RealmPoints + " realm points\n";
                count++;
                list.Add(str);
            }

            player.Out.SendCustomTextWindow("Realm Point Herald", list);

            return true;
        }
    }
}