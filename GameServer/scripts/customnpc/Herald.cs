using System;
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Database;
using System.Collections.Generic;
using System.Linq;

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

            DOLCharacters[] chars = GameServer.Database.SelectObjects<DOLCharacters>(DB.Column("RealmPoints").IsGreatherThan(0).And(DB.Column("RealmPoints").IsLessThan(70000000))).OrderByDescending(x => x.RealmPoints).Take(25).ToArray();
            List<string> list = new List<string>();
            
            list.Add("Top 25 Highest Realm Points:\n\n");
            int count = 1;
            foreach (DOLCharacters chr in chars)
            {
                var realm = "";
                
                switch (chr.Realm)
                {
                    case 1:
                        realm = "Alb";
                        break;
                    case 2:
                        realm = "Mid";
                        break;
                    case 3:
                        realm = "Hib";
                        break;
                }

                string str = "#" + count + ": " + chr.Name + " (" + realm + ") - " + chr.RealmPoints + " realm points\n";
                list.Add(str);
                count++;
            }

            player.Out.SendCustomTextWindow("Realm Point Herald", list);

            return true;
        }
    }
}