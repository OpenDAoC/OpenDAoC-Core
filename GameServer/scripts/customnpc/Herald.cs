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

            DOLCharacters[] chars = (DOLCharacters[])GameServer.Database.SelectObjects<DOLCharacters>("RealmPoints > 0 AND RealmPoints < 70000000 ORDER BY RealmPoints DESC LIMIT 25");
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
                count++;
                list.Add(str);
            }

            player.Out.SendCustomTextWindow("Realm Point Herald", list);

            return true;
        }
    }
}