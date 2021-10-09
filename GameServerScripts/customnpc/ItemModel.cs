//edited by loki for current SVN 2018


using DOL.Database;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS {
    public class ItemModel : GameNPC {
        public string TempProperty = "ItemModel";
        private int Chance;
        private Random rnd = new Random();

        //placeholder prices
        //500 lowbie stuff
        //2k low toa
        //4k high toa
        //10k dragonsworn
        //20k champion
        private int lowbie = 450;
        private int festive = 900;
        private int toageneric = 1800; //effects price in RPs
        private int artifact = 4800;
        private int epic = 4000;
        private int dragon = 9000;
        private int champion = 18000;
        private int cloakcheap = 18000;
        private int cloakexpensive = 61749;

        public override bool AddToWorld()
        {
            base.AddToWorld();
            return true;
        }

        public override bool Interact(GamePlayer player)
        {
            if (base.Interact(player))
            {
                TurnTo(player, 500);
                InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
                if (item == null)
                {
                    SendReply(player, "Hello there! \n" +
                    "I can offer a variety of aesthetics... for those willing to pay for it.\n" +
                    "Hand me the item and then we can talk prices.");
                }
                else
                {
                    ReceiveItem(player, item);
                }
                return true;
            }
            return false;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            /*
            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;
            GamePlayer player = (GamePlayer)source;
            TurnTo(player.X, player.Y);

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);

            if (item == null)
            {
                SendReply(player, "I need an item to work on!");
                return false;
            }
            int model = item.Model;
            int tmpmodel = int.Parse(str);
            if (tmpmodel != 0) model = tmpmodel;
            SetModel(player, model);
            SendReply(player, "I have changed your item's model, you can now use it.");
            */

            if (!base.WhisperReceive(source, str)) return false;
            if (!(source is GamePlayer)) return false;

            int modelIDToAssign = 0;
            int price = 0;

            GamePlayer player = source as GamePlayer;
            TurnTo(player.X, player.Y);
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);

            if (item == null)
            {
                SendReply(player, "I need an item to work on!");
                return false;
            }

            switch (str.ToLower())
            { 
                #region helms
                case "dragonsworn helm":
                    price = dragon;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 3864;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 3862;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 3863;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 3866;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 3865;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Scale:
                            modelIDToAssign = 3867;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 3861;
                            break;
                    }
                    break;
                case "crown of zahur":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 1839;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 1840;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 1841;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "crown of zahur variant":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 1842;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 1843;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 1844;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "winged helm":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 2223;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 2225;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 2224;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "oceanus helm":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2253;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2289;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2271;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2256;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2292;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2274;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2262;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2280;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 2298;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2265;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2277;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 2301;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2268;
                            break;
                    }
                    break;
                case "stygia helm":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2307;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2343;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2325;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2310;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2346;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2328;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2316;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2334;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 2352;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2313;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2331;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 2355;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2322;
                            break;
                    }
                    break;
                case "volcanus helm":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2361;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2397;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2379;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2364;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2400;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2382;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2370;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2388;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 2406;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2373;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2385;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 2409;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2376;
                            break;
                    }
                    break;
                case "aerus helm":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2415;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2451;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2433;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2418;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2454;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2436;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2424;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2442;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 2460;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2421;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2439;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 2463;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2430;
                            break;
                    }
                    break;
                #endregion

                #region torsos
                case "dragonsworn breastplate":
                    price = dragon;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 3783;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 3758;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 3778;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                case eRealm.Midgard:
                                    modelIDToAssign = 3778;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 3773;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 3768;
                            break;
                    }
                    break;
                case "eirene's chest":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 2226;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 2228;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 2227;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "naliah's robe":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 2516;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 2517;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 2518;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "guard of valor":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 2475;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 2476;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 2477;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "golden scarab vest":
                    price = artifact;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 2187;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 2189;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 2188;
                            break;
                        default:
                            modelIDToAssign = 0;
                            break;
                    }
                    break;
                case "oceanus breastplate":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1619;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 1621;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1623;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1640;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 1641;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1642;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1848;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1849;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 1850;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2101;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2102;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1773;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2092;
                            break;
                    }
                    break;
                case "stygia breastplate":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2515;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2515;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2515;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2135; //lol leopard print
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2136;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2137;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1757;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1758;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 1759;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1809;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1810;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1791;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2124;
                            break;
                    }
                    break;
                case "volcanus breastplate":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2513;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2513;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2513;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2176;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2178;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2177;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1780;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1781;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 1782;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1694;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1695;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1714;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1703;
                            break;
                    }
                    break;
                case "aerus breastplate":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2245;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2246;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2247;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Leather:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 2144;
                                    break;
                                case eRealm.Hibernia:
                                    modelIDToAssign = 2146;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 2145;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Studded:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1798;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1799;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;

                        case eObjectType.Reinforced:
                            modelIDToAssign = 1800;
                            break;

                        case eObjectType.Chain:
                            switch (source.Realm)
                            {
                                case eRealm.Albion:
                                    modelIDToAssign = 1736;
                                    break;
                                case eRealm.Midgard:
                                    modelIDToAssign = 1737;
                                    break;
                                default:
                                    modelIDToAssign = 0;
                                    break;
                            }
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1738;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1685;
                            break;
                    }
                    break;
                case "class epic chestpiece":
                    price = epic;
                    switch ((eCharacterClass)player.CharacterClass.ID)
                    {
                        //alb
                        case eCharacterClass.Armsman:
                            modelIDToAssign = 688;
                            break;
                        case eCharacterClass.Cabalist:
                            modelIDToAssign = 682;
                            break;
                        case eCharacterClass.Cleric:
                            modelIDToAssign = 713;
                            break;
                        case eCharacterClass.Friar:
                            modelIDToAssign = 797;
                            break;
                        case eCharacterClass.Infiltrator:
                            modelIDToAssign = 792;
                            break;
                        case eCharacterClass.Mercenary:
                            modelIDToAssign = 718;
                            break;
                        case eCharacterClass.Minstrel:
                            modelIDToAssign = 3380;
                            break;
                        case eCharacterClass.Necromancer:
                            modelIDToAssign = 1266;
                            break;
                        case eCharacterClass.Paladin:
                            modelIDToAssign = 693;
                            break;
                        case eCharacterClass.Reaver:
                            modelIDToAssign = 1267;
                            break;
                        case eCharacterClass.Scout:
                            modelIDToAssign = 728;
                            break;
                        case eCharacterClass.Sorcerer:
                            modelIDToAssign = 804;
                            break;
                        case eCharacterClass.Theurgist:
                            modelIDToAssign = 733;
                            break;
                        case eCharacterClass.Wizard:
                            modelIDToAssign = 798;
                            break;

                        //mid
                        case eCharacterClass.Berserker:
                            modelIDToAssign = 751;
                            break;
                        case eCharacterClass.Bonedancer:
                            modelIDToAssign = 1187;
                            break;
                        case eCharacterClass.Healer:
                            modelIDToAssign = 698;
                            break;
                        case eCharacterClass.Hunter:
                            modelIDToAssign = 756;
                            break;
                        case eCharacterClass.Runemaster:
                            modelIDToAssign = 703;
                            break;
                        case eCharacterClass.Savage:
                            modelIDToAssign = 1192;
                            break;
                        case eCharacterClass.Shadowblade:
                            modelIDToAssign = 761;
                            break;
                        case eCharacterClass.Shaman:
                            modelIDToAssign = 766;
                            break;
                        case eCharacterClass.Skald:
                            modelIDToAssign = 771;
                            break;
                        case eCharacterClass.Spiritmaster:
                            modelIDToAssign = 799;
                            break;
                        case eCharacterClass.Thane:
                            modelIDToAssign = 3370;
                            break;
                        case eCharacterClass.Warrior:
                            modelIDToAssign = 776;
                            break;

                        //hib
                        case eCharacterClass.Animist:
                            modelIDToAssign = 1186;
                            break;
                        case eCharacterClass.Bard:
                            modelIDToAssign = 734;
                            break;
                        case eCharacterClass.Blademaster:
                            modelIDToAssign = 782;
                            break;
                        case eCharacterClass.Champion:
                            modelIDToAssign = 810;
                            break;
                        case eCharacterClass.Druid:
                            modelIDToAssign = 739;
                            break;
                        case eCharacterClass.Eldritch:
                            modelIDToAssign = 744;
                            break;
                        case eCharacterClass.Enchanter:
                            modelIDToAssign = 781;
                            break;
                        case eCharacterClass.Hero:
                            modelIDToAssign = 708;
                            break;
                        case eCharacterClass.Mentalist:
                            modelIDToAssign = 745;
                            break;
                        case eCharacterClass.Nightshade:
                            modelIDToAssign = 746;
                            break;
                        case eCharacterClass.Ranger:
                            modelIDToAssign = 815;
                            break;
                        case eCharacterClass.Valewalker:
                            modelIDToAssign = 1003;
                            break;
                        case eCharacterClass.Warden:
                            modelIDToAssign = 805;
                            break;
                    }
                    break;
                #endregion

                #region sleeves
                case "foppish sleeves":
                    price = artifact;
                    modelIDToAssign = 1732;
                    break;
                case "arms of the wind":
                    price = artifact;
                    modelIDToAssign = 1733;
                    break;
                case "oceanus sleeves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 1625;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 1639;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1847;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 2100;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1770;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2091;
                            break;
                    }
                    break;
                case "stygia sleeves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2152;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2134;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1756;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1808;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1788;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2123;
                            break;
                    }
                    break;
                case "volcanus sleeves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2161;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2175;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1779;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1693;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1711;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1702;
                            break;
                    }
                    break;
                case "aerus sleeves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2237;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2143;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1797;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1735;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1747;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1684;
                            break;
                    }
                    break;
                #endregion

                #region pants
                case "wing's dive":
                    price = artifact;
                    modelIDToAssign = 1767;
                    break;
                case "alvarus' leggings":
                    price = artifact;
                    modelIDToAssign = 1744;
                    break;
                case "oceanus pants":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 1631;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 1646;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1854;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 2107;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1778;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2098;
                            break;
                    }
                    break;
                case "stygia pants":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2158;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2141;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1763;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1815;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1796;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2130;
                            break;
                    }
                    break;
                case "volcanus pants":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2167;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2182;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1786;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1700;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1718;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1709;
                            break;
                    }
                    break;
                case "aerus pants":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2243;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2150;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1804;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1742;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1754;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1691;
                            break;
                    }
                    break;
                #endregion

                #region boots
                case "enyalio's boots":
                    price = artifact;
                    modelIDToAssign = 2488;
                    break;
                case "flamedancer's boots":
                    price = artifact;
                    modelIDToAssign = 1731;
                    break;
                case "oceanus boots":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 1629;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 1643;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1851;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 2104;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1775;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2095;
                            break;
                    }
                    break;
                case "stygia boots":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2157;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2139;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1761;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1813;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1793;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2127;
                            break;
                    }
                    break;
                case "volcanus boots":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2166;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2180;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1784;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1698;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1716;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1706;
                            break;
                    }
                    break;
                case "aerus boots":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2242;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2148;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1802;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1740;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1752;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1688;
                            break;
                    }
                    break;
                #endregion

                #region gloves
                case "maddening scalars":
                    price = artifact;
                    modelIDToAssign = 1746;
                    break;
                case "sharkskin gloves":
                    price = artifact;
                    modelIDToAssign = 1734;
                    break;
                case "oceanus gloves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 1620;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 1645;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1853;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 2106;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1776;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2097;
                            break;
                    }
                    break;
                case "stygia gloves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2248;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2140;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1762;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1814;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1794;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 2129;
                            break;
                    }
                    break;
                case "volcanus gloves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2249;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2181;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1785;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1699;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1717;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1708;
                            break;
                    }
                    break;
                case "aerus gloves":
                    price = toageneric;
                    switch ((eObjectType)item.Object_Type)
                    {
                        case eObjectType.Cloth:
                            modelIDToAssign = 2250;
                            break;

                        case eObjectType.Leather:
                            modelIDToAssign = 2149;
                            break;

                        case eObjectType.Studded:
                        case eObjectType.Reinforced:
                            modelIDToAssign = 1803;
                            break;

                        case eObjectType.Chain:
                            modelIDToAssign = 1741;
                            break;
                        case eObjectType.Scale:
                            modelIDToAssign = 1753;
                            break;
                        case eObjectType.Plate:
                            modelIDToAssign = 1690;
                            break;
                    }
                    break;
                #endregion

                #region cloaks 
                // :fademe:
                //I wish we had the champion cloaks

                case "realm cloak":
                    price = cloakexpensive;
                    switch ((eRealm)player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 3800;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 3802;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 3801;
                            break;
                    }
                    break;

                case "dragonslayer cloak":
                    price = cloakexpensive;
                    switch ((eRealm)player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 4105;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 4109;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 4107;
                            break;
                    }
                    break;

                case "dragonsworn cloak": 
                    price = cloakcheap;
                    modelIDToAssign = 3790;
                    break;

                case "valentines cloak":
                    price = cloakcheap;
                    modelIDToAssign = 3752;
                    break;

                case "winter cloak":
                    price = cloakcheap;
                    modelIDToAssign = 4115;
                    break;

                case "clean leather cloak":
                    price = cloakcheap;
                    modelIDToAssign = 3637;
                    break;

                case "corrupt leather cloak":
                    price = cloakcheap;
                    modelIDToAssign = 3634;
                    break;

                case "cloudsong":
                    price = cloakcheap;
                    modelIDToAssign = 1727;
                    break;

                case "shades of mist": //lol spelled it right here so its broken until I rebuild
                    price = cloakcheap;
                    modelIDToAssign = 1726;
                    break;

                case "magma cloak":
                    price = cloakcheap;
                    modelIDToAssign = 1725;
                    break;

                case "stygian cloak":
                    price = cloakcheap;
                    modelIDToAssign = 1724;
                    break;

                case "aerus cloak":
                    price = cloakcheap;
                    modelIDToAssign = 1720;
                    break;

                case "oceanus cloak":
                    price = cloakcheap;
                    modelIDToAssign = 1722;
                    break;

                case "harpy feather cloak":
                    price = cloakcheap;
                    modelIDToAssign = 1721;
                    break;
                  
                case "healer's embrace":
                    price = cloakcheap;
                    modelIDToAssign = 1723;
                    break;

                #endregion

                #region weapons

                #region 1h wep
                case "traitor's dagger 1h":
                    price = artifact;
                    modelIDToAssign = 1668;
                    break;
                case "traitor's axe 1h":
                    price = artifact;
                    modelIDToAssign = 3452;
                    break;
                case "croc tooth dagger 1h":
                    price = artifact;
                    modelIDToAssign = 1669;
                    break;
                case "croc tooth axe 1h":
                    price = artifact;
                    modelIDToAssign = 3451;
                    break;
                case "golden spear 1h":
                    price = artifact;
                    modelIDToAssign = 1807;
                    break;
                case "malice axe 1h":
                    price = artifact;
                    modelIDToAssign = 2109;
                    break;
                case "malice hammer 1h":
                    price = artifact;
                    modelIDToAssign = 3447;
                    break;
                case "bruiser 1h":
                    price = artifact;
                    modelIDToAssign = 1671;
                    break;
                case "battler hammer 1h":
                    price = artifact;
                    modelIDToAssign = 3453;
                    break;
                case "battler sword 1h":
                    price = artifact;
                    modelIDToAssign = 1670;
                    break;
                case "scepter of the meritorious":
                    price = artifact;
                    modelIDToAssign = 1672;
                    break;
                case "hilt 1h":
                    price = epic;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 673;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 670;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 674;
                            break;
                    }
                    break;
                case "rolling pin":
                    price = epic;
                    modelIDToAssign = 3458;
                    break;
                case "wakazashi":
                    price = champion;
                    modelIDToAssign = 2209;
                    break;
                case "turkey leg":
                    price = champion;
                    modelIDToAssign = 3454;
                    break;
                case "key":
                    price = champion;
                    modelIDToAssign = 3455;
                    break;
                case "cleaver":
                    price = epic;
                    modelIDToAssign = 654;
                    break;
                case "khopesh":
                    price = epic;
                    modelIDToAssign = 2195;
                    break;
                case "torch":
                    price = champion;
                    modelIDToAssign = 3471;
                    break;
                case "stein":
                    price = champion;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 3440;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 3443;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 3438;
                            break;
                    }
                    break;
                case "hot metal rod":
                    price = champion;
                    modelIDToAssign = 2984;
                    break;

                //hand to hand
                case "snakecharmer's fist":
                    price = artifact;
                    modelIDToAssign = 2469;
                    break;

                case "scorched fist":
                    price = toageneric;
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3726;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3728;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3730;
                            break;
                    }
                    break;

                case "dragonsworn fist":
                    price = dragon;
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3843;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3845;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3847;
                            break;
                    }
                    break;

                //flex
                case "snakecharmer's whip":
                    price = artifact;
                    modelIDToAssign = 2119;
                    break;

                case "scorched whip":
                    price = toageneric;
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3697;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3696;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3698;
                            break;
                    }
                    break;

                case "dragonsworn whip":
                    price = dragon;
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3814;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3815;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3813;
                            break;
                    }
                    break;


                #endregion

                #region 2h wep

                case "pickaxe":
                    price = epic;
                    modelIDToAssign = 2983;
                    break;

                    //axe
                case "malice axe 2h":
                    price = artifact;
                    modelIDToAssign = 2110;
                    break;
                case "scorched axe 2h":
                    price = toageneric;
                    modelIDToAssign = 3705;
                    break;
                case "magma axe 2h":
                    price = toageneric;
                    modelIDToAssign = 2217;
                    break;

                //spears
                case "golden spear 2h":
                    price = artifact;
                    modelIDToAssign = 1662;
                    break;
                case "dragon spear 2h":
                    price = dragon;
                    modelIDToAssign = 3819;
                    break;
                case "scorched spear 2h":
                    price = toageneric;
                    modelIDToAssign = 3714;
                    break;
                case "trident spear 2h":
                    price = toageneric;
                    modelIDToAssign = 2191;
                    break;

                    //hammers
                case "bruiser hammer 2h":
                    price = artifact;
                    modelIDToAssign = 2113;
                    break;
                case "battler hammer 2h":
                    price = artifact;
                    modelIDToAssign = 3448;
                    break;
                case "malice hammer 2h":
                    price = artifact;
                    modelIDToAssign = 3449;
                    break;
                case "scorched hammer 2h":
                    price = toageneric;
                    modelIDToAssign = 3704;
                    break;
                case "magma hammer 2h":
                    price = toageneric;
                    modelIDToAssign = 2215;
                    break;

                //swords
                case "battler sword 2h":
                    price = artifact;
                    modelIDToAssign = 1670;
                    break;
                case "scorched sword 2h":
                    price = toageneric;
                    modelIDToAssign = 3701;
                    break;
                case "katana 2h":
                    price = epic;
                    modelIDToAssign = 2208;
                    break;
                case "khopesh 2h":
                    price = epic;
                    modelIDToAssign = 2196;
                    break;
                case "hilt 2h":
                    price = epic;
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            modelIDToAssign = 672;
                            break;
                        case eRealm.Midgard:
                            modelIDToAssign = 671;
                            break;
                        case eRealm.Hibernia:
                            modelIDToAssign = 675;
                            break;
                    } 
                    break;

                //thrust
                case "scorched thrust 2h":
                    price = toageneric;
                    modelIDToAssign = 3700;
                    break;
                case "dragon thrust 2h":
                    price = dragon;
                    modelIDToAssign = 3817;
                    break;

                //staffs
                case "traldor's oracle":
                    price = artifact;
                    modelIDToAssign = 1658;
                    break;
                case "trident of the gods":
                    price = artifact;
                    modelIDToAssign = 1660;
                    break;
                case "tartaros gift":
                    price = artifact;
                    modelIDToAssign = 1659;
                    break;
                case "dragonsworn staff":
                    price = dragon;
                    modelIDToAssign = 3827;
                    break;
                case "scorched staff":
                    price = toageneric;
                    modelIDToAssign = 3710;
                    break;

                    //scythes
                case "dragonsworn scythe":
                    price = artifact;
                    modelIDToAssign = 3825;
                    break;
                case "magma scythe":
                    price = toageneric;
                    modelIDToAssign = 2213;
                    break;
                case "scorched scythe":
                    price = toageneric;
                    modelIDToAssign = 3708;
                    break;
                case "scythe of kings":
                    price = artifact;
                    modelIDToAssign = 3450;
                    break;
                case "snakechamer's scythe":
                    price = artifact;
                    modelIDToAssign = 2111;
                    break;

                //polearms
                case "dragonsworn pole":
                    price = dragon;
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3832;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3833;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3831;
                            break;
                    }
                    break;
                case "pole of kings":
                    price = artifact;
                    modelIDToAssign = 1661;
                    break;
                case "scorched pole":
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Slash:
                            modelIDToAssign = 3715;
                            break;
                        case eDamageType.Crush:
                            modelIDToAssign = 3716;
                            break;
                        case eDamageType.Thrust:
                            modelIDToAssign = 3714;
                            break;
                    }
                    break;
                case "golden pole":
                    price = artifact;
                    modelIDToAssign = 1662;
                    break;

                #endregion

                #region class weapons

                case "class epic 1h":
                    price = champion;
                    switch ((eCharacterClass)player.CharacterClass.ID)
                    {
                        //alb
                        case eCharacterClass.Armsman:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3296;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3295;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3294;
                                    break;
                            }
                            break;
                        case eCharacterClass.Cabalist:
                            modelIDToAssign = 3264;
                            break;
                        case eCharacterClass.Cleric:
                            modelIDToAssign = 3282;
                            break;
                        case eCharacterClass.Friar:
                            modelIDToAssign = 3272; 
                            break;
                        case eCharacterClass.Infiltrator:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3270;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3269;
                                    break;
                            }
                            break;
                        case eCharacterClass.Mercenary:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3285;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3284;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3283;
                                    break;
                            }
                            break;
                        case eCharacterClass.Minstrel:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3277;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3276;
                                    break;
                            }
                            break;
                        case eCharacterClass.Necromancer:
                            modelIDToAssign = 3268;
                            break;
                        case eCharacterClass.Paladin:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3305;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3304;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3303;
                                    break;
                            }
                            break;
                        case eCharacterClass.Reaver:
                            if((eObjectType)item.Object_Type == eObjectType.Flexible)
                            {
                                modelIDToAssign = 3292;
                            } else
                            {
                                switch ((eDamageType)item.Type_Damage)
                                {
                                    case eDamageType.Thrust:
                                        modelIDToAssign = 3291;
                                        break;
                                    case eDamageType.Slash:
                                        modelIDToAssign = 3290;
                                        break;
                                    case eDamageType.Crush:
                                        modelIDToAssign = 3289;
                                        break;
                                }
                            }
                            break;
                        case eCharacterClass.Scout:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3274;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3273;
                                    break;
                            }
                            break;
                        case eCharacterClass.Sorcerer:
                            modelIDToAssign = 3265;
                            break;
                        case eCharacterClass.Theurgist:
                            modelIDToAssign = 3266;
                            break;
                        case eCharacterClass.Wizard:
                            modelIDToAssign = 3267;
                            break;

                        //mid
                        case eCharacterClass.Berserker:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3323;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3321;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3325;
                                    break;
                                case eObjectType.LeftAxe:
                                    modelIDToAssign = 3321;
                                    break;
                            }
                            break;
                        case eCharacterClass.Bonedancer:
                            modelIDToAssign = 3311;
                            break;
                        case eCharacterClass.Healer:
                            modelIDToAssign = 3335;
                            break;
                        case eCharacterClass.Hunter:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Spear:
                                    if((eDamageType)item.Type_Damage == eDamageType.Thrust)
                                    {
                                        modelIDToAssign = 3319;
                                    } else { 
                                        modelIDToAssign = 3320; }
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3317;
                                    break;
                            }
                            break;
                        case eCharacterClass.Runemaster:
                            modelIDToAssign = 3309;
                            break;
                        case eCharacterClass.Savage:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3329;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3327;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3331;
                                    break;
                                case eObjectType.HandToHand:
                                    modelIDToAssign = 3333;
                                    break;
                            }
                            break;
                        case eCharacterClass.Shadowblade:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Axe:
                                    modelIDToAssign = 3315;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3313;
                                    break;
                            }
                            break;
                        case eCharacterClass.Shaman:
                            modelIDToAssign = 3337;
                            break;
                        case eCharacterClass.Skald:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3341;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3339;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3343;
                                    break;
                            }
                            break;
                        case eCharacterClass.Spiritmaster:
                            modelIDToAssign = 3310;
                            break;
                        case eCharacterClass.Thane:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3347;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3345;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3349;
                                    break;
                            }
                            break;
                        case eCharacterClass.Warrior:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3353;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3351;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3355;
                                    break;
                            }
                            break;

                        //hib
                        case eCharacterClass.Animist:
                            modelIDToAssign = 3229;
                            break;
                        case eCharacterClass.Bard:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3235;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3236;
                                    break;
                            }
                            break;
                        case eCharacterClass.Blademaster:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3244;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3246;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3245;
                                    break;
                            }
                            break;
                        case eCharacterClass.Champion:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3251;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3253;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3252;
                                    break;
                            }
                            break;
                        case eCharacterClass.Druid:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3247;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3248;
                                    break;
                            }
                             break;
                        case eCharacterClass.Eldritch:
                            modelIDToAssign = 3226;
                            break;
                        case eCharacterClass.Enchanter:
                            modelIDToAssign = 3227;
                            break;
                        case eCharacterClass.Hero:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3256;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3258;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3257;
                                    break;
                            }
                            break;
                        case eCharacterClass.Mentalist:
                            modelIDToAssign = 3228;
                            break;
                        case eCharacterClass.Nightshade:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3233;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3234;
                                    break;
                            }
                            break;
                        case eCharacterClass.Ranger:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3242;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3241;
                                    break;
                            }
                            break;
                        case eCharacterClass.Valewalker:
                            modelIDToAssign = 3231;
                            break;
                        case eCharacterClass.Warden:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3249;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3250;
                                    break;
                            }
                            break;
                    }
                    break;

                case "class epic 2h":
                    price = champion;
                    switch ((eCharacterClass)player.CharacterClass.ID)
                    {
                        //alb
                        case eCharacterClass.Armsman:
                            if((eObjectType)item.Object_Type == eObjectType.PolearmWeapon)
                            {
                                switch ((eDamageType)item.Type_Damage)
                                {
                                    case eDamageType.Thrust:
                                        modelIDToAssign = 3297;
                                        break;
                                    case eDamageType.Slash:
                                        modelIDToAssign = 3297;
                                        break;
                                    case eDamageType.Crush:
                                        modelIDToAssign = 3298;
                                        break;
                                }
                            }else
                            {
                                switch ((eDamageType)item.Type_Damage)
                                {
                                    case eDamageType.Thrust:
                                        modelIDToAssign = 3301;
                                        break;
                                    case eDamageType.Slash:
                                        modelIDToAssign = 3300;
                                        break;
                                    case eDamageType.Crush:
                                        modelIDToAssign = 3302;
                                        break;
                                }
                            }
                            break;
                        case eCharacterClass.Cabalist:
                            modelIDToAssign = 3264;
                            break;
                        case eCharacterClass.Cleric:
                            modelIDToAssign = 3282;
                            break;
                        case eCharacterClass.Friar:
                            modelIDToAssign = 3271;
                            break;
                        case eCharacterClass.Necromancer:
                            modelIDToAssign = 3268;
                            break;
                        case eCharacterClass.Paladin:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3307;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3306;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3308;
                                    break;
                            }
                            break;
                        case eCharacterClass.Sorcerer:
                            modelIDToAssign = 3265;
                            break;
                        case eCharacterClass.Theurgist:
                            modelIDToAssign = 3266;
                            break;
                        case eCharacterClass.Wizard:
                            modelIDToAssign = 3267;
                            break;

                        //mid
                        case eCharacterClass.Berserker:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3324;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3322;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3326;
                                    break;
                            }
                            break;
                        case eCharacterClass.Bonedancer:
                            modelIDToAssign = 3311;
                            break;
                        case eCharacterClass.Healer:
                            modelIDToAssign = 3335;
                            break;
                        case eCharacterClass.Hunter:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Spear:
                                    if ((eDamageType)item.Type_Damage == eDamageType.Thrust)
                                    {
                                        modelIDToAssign = 3319;
                                    }
                                    else
                                    {
                                        modelIDToAssign = 3320;
                                    }
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3318;
                                    break;
                            }
                            break;
                        case eCharacterClass.Runemaster:
                            modelIDToAssign = 3309;
                            break;
                        case eCharacterClass.Savage:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3330;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3328;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3332;
                                    break;
                            }
                            break;
                        case eCharacterClass.Shadowblade:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Axe:
                                    modelIDToAssign = 3316;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3314;
                                    break;
                            }
                            break;
                        case eCharacterClass.Shaman:
                            modelIDToAssign = 3338;
                            break;
                        case eCharacterClass.Skald:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3342;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3340;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3344;
                                    break;
                            }
                            break;
                        case eCharacterClass.Spiritmaster:
                            modelIDToAssign = 3310;
                            break;
                        case eCharacterClass.Thane:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3348;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3346;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3350;
                                    break;
                            }
                            break;
                        case eCharacterClass.Warrior:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Hammer:
                                    modelIDToAssign = 3354;
                                    break;
                                case eObjectType.Axe:
                                    modelIDToAssign = 3352;
                                    break;
                                case eObjectType.Sword:
                                    modelIDToAssign = 3356;
                                    break;
                            }
                            break;

                        //hib
                        case eCharacterClass.Animist:
                            modelIDToAssign = 3229;
                            break;
                        case eCharacterClass.Champion:
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Slash:
                                    modelIDToAssign = 3254;
                                    break;
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3254;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3253;
                                    break;
                            }
                            break;
                        case eCharacterClass.Eldritch:
                            modelIDToAssign = 3226;
                            break;
                        case eCharacterClass.Enchanter:
                            modelIDToAssign = 3227;
                            break;
                        case eCharacterClass.Hero:
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3259;
                                    break;
                                case eObjectType.Blunt:
                                    modelIDToAssign = 3260;
                                    break;
                                case eObjectType.Piercing:
                                    modelIDToAssign = 3261;
                                    break;
                                case eObjectType.CelticSpear:
                                    modelIDToAssign = 3263;
                                    break;
                                case eObjectType.LargeWeapons:
                                    //yay weird daoc weapon structure
                                    switch ((eDamageType)item.Type_Damage)
                                    {
                                        case eDamageType.Slash:
                                            modelIDToAssign = 3259;
                                            break;
                                        case eDamageType.Thrust:
                                            modelIDToAssign = 3261;
                                            break;
                                        case eDamageType.Crush:
                                            modelIDToAssign = 3260;
                                            break;
                                    }
                                    break;

                            }
                            break;
                        case eCharacterClass.Mentalist:
                            modelIDToAssign = 3228;
                            break;
                        case eCharacterClass.Valewalker:
                            modelIDToAssign = 3231;
                            break;
                    }
                    break;
                #endregion

                #endregion

                #region shields
                case "aten's shield":
                    price = artifact;
                    modelIDToAssign = 1663;
                    break;
                case "cyclop's eye":
                    price = artifact;
                    modelIDToAssign = 1664;
                    break;
                case "shield of khaos":
                    price = artifact;
                    modelIDToAssign = 1665;
                    break;
                case "oceanus shield":
                    {
                        price = toageneric;
                        if (item.Type_Damage == 1)//small shield
                        {
                            modelIDToAssign = 2192;
                        }
                        else if (item.Type_Damage == 2)
                        {
                            modelIDToAssign = 2193;
                        }
                        else if (item.Type_Damage == 3)
                        {
                            modelIDToAssign = 2194;
                        }
                        break;
                    }
                case "aerus shield":
                    {
                        price = toageneric;
                        if (item.Type_Damage == 1)//small shield
                        {
                            modelIDToAssign = 2210;
                        }
                        else if (item.Type_Damage == 2)
                        {
                            modelIDToAssign = 2211;
                        }
                        else if (item.Type_Damage == 3)
                        {
                            modelIDToAssign = 2212;
                        }
                        break;
                    }
                case "magma shield":
                    {
                        price = toageneric;
                        if (item.Type_Damage == 1)//small shield
                        {
                            modelIDToAssign = 2218;
                        }
                        else if (item.Type_Damage == 2)
                        {
                            modelIDToAssign = 2219;
                        }
                        else if (item.Type_Damage == 3)
                        {
                            modelIDToAssign = 2220;
                        }
                        break;
                    }
                case "minotaur shield":
                    price = toageneric;
                    modelIDToAssign = 3554;
                    break;

                #endregion

                #region ranged weapons/instruments
                //case "dragonslayer harp": probably doesn't work
                //     break;
                case "class epic harp":
                    price = epic;
                    if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Bard)
                    {
                        modelIDToAssign = 3239;
                    }
                    else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Minstrel)
                    {
                        modelIDToAssign = 3280;
                    }
                    else
                    {
                        price = 0;
                    }
                    break;
                case "labyrinth harp":
                    price = toageneric;
                    modelIDToAssign = 3731;
                    break;
                case "class epic bow":
                    price = champion;
                    if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Scout)
                    {
                        modelIDToAssign = 3275;
                    }
                    else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Hunter)
                    {
                        modelIDToAssign = 3365;
                    }
                    else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Ranger)
                    {
                        modelIDToAssign = 3243;
                    }
                    else
                    {
                        price = 0;
                    }
                    break;
                case "fool's bow":
                    price = artifact;
                    modelIDToAssign = 1666;
                    break;
                case "braggart's bow":
                    price = artifact;
                    modelIDToAssign = 1667;
                    break;
                case "labyrinth bow":
                    price = toageneric;
                    modelIDToAssign = 3706;
                    break;
                    #endregion

            }

            if (price > 0)
            {

                if (player.RealmPoints < price)
                {
                    SendReply(player, "I'm sorry, but you cannot afford my services currently.");
                    return false;
                }

                int model = item.Model;
                if (modelIDToAssign != 0) model = modelIDToAssign;
                SetModel(player, model);
                SendReply(player, "Thanks for the donation. " +
                    "I have changed your item's model, you can now use it. \n\n" +
                    "I look forward to doing business with you in the future.");
                player.RealmPoints -= price;
                player.RespecRealm();
                SetRealmLevel(player, (int)player.RealmPoints);

                return true;
            }
            else
            {
                SendReply(player, "I'm sorry, I seem to have gotten confused. Please start over. \n" + 
                                    "If you repeatedly get this message, please file a bug ticket on how you recreate it.");
                return false;
            }


        }

       
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer t = source as GamePlayer;
            if (t == null || item == null) return false;
            if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
            {
                t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            switch (item.Item_Type)
            {
                case Slot.HELM:
                    SendReply(t, "A fine piece of headwear. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Dragonsworn Helm](" + dragon + " RPs)\n" +
                        "[Crown of Zahur] (" + artifact + " RPs)\n" +
                        "[Crown of Zahur variant] (" + artifact + " RPs)\n" +
                        "[Winged Helm] (" + artifact + " RPs)\n" +
                        "[Oceanus Helm] (" + toageneric + " RPs)\n" +
                        "[Stygia Helm] (" + toageneric + " RPs)\n" +
                        "[Volcanus Helm] (" + toageneric + " RPs)\n" +
                        "[Aerus Helm] (" + toageneric + " RPs)\n" +
                        "");
                    break;

                case Slot.TORSO:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Dragonsworn Breastplate](" + dragon + " RPs)\n" +
                        "[Class Epic Chestpiece](" + epic + " RPs)\n" +
                        "[Eirene's Chest](" + artifact + " RPs)\n" +
                        "[Naliah's Robe](" + artifact + " RPs)\n" +
                        "[Guard of Valor](" + artifact + " RPs)\n" +
                        "[Golden Scarab Vest](" + artifact + " RPs)\n" +
                        "[Oceanus Breastplate] (" + toageneric + " RPs)\n" +
                        "[Stygia Breastplate] (" + toageneric + " RPs)\n" +
                        "[Volcanus Breastplate] (" + toageneric + " RPs)\n" +
                        "[Aerus Breastplate] (" + toageneric + " RPs)\n" +
                        
                        "");
                    break;

                case Slot.ARMS:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Foppish Sleeves] (" + artifact + " RPs)\n" +
                        "[Arms of the Wind] (" + artifact + " RPs)\n" +
                        "[Oceanus Sleeves] (" + toageneric + " RPs)\n" +
                        "[Stygia Sleeves] (" + toageneric + " RPs)\n" +
                        "[Volcanus Sleeves] (" + toageneric + " RPs)\n" +
                        "[Aerus Sleeves] (" + toageneric + " RPs)\n" +
                        
                        "");
                    break;

                case Slot.LEGS:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Wings Dive] (" + artifact + " RPs)\n" +
                        "[Alvarus' Leggings] (" + artifact + " RPs)\n" +
                        "[Oceanus Pants] (" + toageneric + " RPs)\n" +
                        "[Stygia Pants] (" + toageneric + " RPs)\n" +
                        "[Volcanus Pants] (" + toageneric + " RPs)\n" +
                        "[Aerus Pants] (" + toageneric + " RPs)\n" +
                        
                        "");
                    break;

                case Slot.HANDS:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Maddening Scalars] (" + artifact + " RPs)\n" +
                        "[Sharkskin Gloves] (" + artifact + " RPs)\n" +
                        "[Oceanus Gloves] (" + toageneric + " RPs)\n" +
                        "[Stygia Gloves] (" + toageneric + " RPs)\n" +
                        "[Volcanus Gloves] (" + toageneric + " RPs)\n" +
                        "[Aerus Gloves] (" + toageneric + " RPs)\n" +
                        
                        "");
                    break;

                case Slot.FEET:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Enyalio's Boots] (" + artifact + " RPs)\n" +
                        "[Flamedancer's Boots] (" + artifact + " RPs)\n" +
                        "[Oceanus Boots] (" + toageneric + " RPs)\n" +
                        "[Stygia Boots] (" + toageneric + " RPs)\n" +
                        "[Volcanus Boots] (" + toageneric + " RPs)\n" +
                        "[Aerus Boots] (" + toageneric + " RPs)\n" +
                        
                        "");
                    break;

                case Slot.CLOAK:
                    SendReply(t, "This looks like it has protected you nicely. \n" +
                        "I can apply the following skins: \n\n" +
                      /*  "[Realm Cloak] (" + cloakexpensive + " RPs)\n" +
                        "[Dragonslayer Cloak] (" + cloakexpensive + " RPs)\n" +
                        "[Dragonsworn Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Valentines Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Winter Cloak] (" + cloakcheap + " RPs)\n" + //yep all these IDs are too new too
                      //another one for the model wizards
                        "[Clean Leather Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Corrupt Leather Cloak] (" + cloakcheap + " RPs)\n" + */
                        "[Cloudsong] (" + cloakcheap + " RPs)\n" +
                        "[Shades of Mist] (" + cloakcheap + " RPs)\n" +
                        "[Harpy Feather Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Healer's Embrace] (" + cloakcheap + " RPs)\n" +
                        "[Oceanus Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Magma Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Stygian Cloak] (" + cloakcheap + " RPs)\n" +
                        "[Aerus Cloak] (" + cloakcheap + " RPs)\n" +
                        "");
                    break;        

                case Slot.RIGHTHAND:
                    SendReply(t, "Ah, I know a highly lethal weapon when I see it. \n" +
                        "I can apply the following skins: \n\n");
                    Console.WriteLine($"Damage type: {(eObjectType)item.Object_Type}");
                    if ((eObjectType)item.Object_Type == eObjectType.HandToHand)
                    {
                        SendReply(t,
                                    "[Snakecharmer's Fist](" + artifact + " RPs)\n" +
                                    "[Scorched Fist](" + toageneric + " RPs)\n" +
                                    "[Dragonsworn Fist](" + dragon + " RPs)\n" +
                                    "");
                    }
                    if ((eObjectType)item.Object_Type == eObjectType.Flexible)
                    {
                        SendReply(t,
                                    "[Snakecharmer's Whip](" + artifact + " RPs)\n" +
                                    "[Scorched Whip](" + toageneric + " RPs)\n" +
                                    "[Dragonsworn Whip](" + dragon + " RPs)\n" +
                                    "");
                    }
                    else
                    {
                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                SendReply(t,
                                    "[Traitor's Dagger 1h](" + artifact + " RPs)\n" +
                                    "[Croc Tooth Dagger 1h](" + artifact + " RPs)\n" +
                                    "[Golden Spear 1h](" + artifact + " RPs)\n" +
                                    "[Wakazashi](" + epic + " RPs)" +
                                    "");
                                break;

                            case eDamageType.Crush:
                                SendReply(t,
                                    "[Battler Hammer 1h](" + artifact + " RPs)\n" +
                                    "[Malice Hammer 1h](" + artifact + " RPs)\n" +
                                    "[Bruiser Hammer 1h](" + artifact + " RPs)\n" +
                                    "[Scepter of the Meritorious](" + artifact + " RPs)\n" +
                                    "[Rolling Pin](" + epic + " RPs)\n" +
                                    "[Stein](" + epic + " RPs)" +
                                    "[Turkey Leg](" + champion + " RPs)" +
                                    "");
                                break;

                            case eDamageType.Slash:
                                SendReply(t,
                                    "[Croc Tooth Axe 1h](" + artifact + " RPs)\n" +
                                    "[Traitor's Axe 1h](" + artifact + " RPs)\n" +
                                    "[Malice Axe 1h](" + artifact + " RPs)\n" +
                                    "[Battler Sword 1h](" + artifact + " RPs)\n" +
                                    "[Khopesh](" + epic + " RPs)\n" +
                                    "[Cleaver](" + epic + " RPs)" +
                                    "");
                                break;
                        }
                        SendReply(t, "Or, perhaps you'd just prefer a [hilt 1h] (" + dragon + " RPs) \n" +
                                     "");
                    }
                    SendReply(t, "Additionally, I can apply an [class epic 1h] " + champion + " skin. \n");
                    break;


                case Slot.LEFTHAND:
                    if ((eObjectType)item.Object_Type == eObjectType.Shield)
                    {
                        SendReply(t, "A sturdy barricade to ward the blows of your enemies. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Aten's Shield](" + artifact + " RPs)\n" +
                        "[Cyclop's Eye](" + artifact + " RPs)\n" +
                        "[Shield of Khaos](" + artifact + " RPs)\n" +
                        "[Oceanus Shield](" + toageneric + " RPs)\n" +
                        "[Aerus Shield](" + toageneric + " RPs)\n" +
                        "[Magma Shield](" + toageneric + " RPs)\n" +
                        "[Minotaur Shield](" + toageneric + " RPs)\n" +
                        "");
                    }
                    else
                    {
                        goto case Slot.RIGHTHAND;
                    }

                    break;

                case Slot.TWOHAND:
                    SendReply(t, "Ah, I know a highly lethal weapon when I see it. \n" +
                        "I can apply the following skins: \n\n");
                    if ((eObjectType)item.Object_Type == eObjectType.Staff)
                    {
                        SendReply(t,
                                    "[Dragonsworn Staff](" + dragon + " RPs)\n" +
                                    "[Traldor's Oracle](" + artifact + " RPs)\n" +
                                    "[Trident of the Gods](" + artifact + " RPs)\n" +
                                    "[Tartaros Gift](" + artifact + " RPs)\n" +
                                    "[Scorched Staff](" + toageneric + " RPs)\n" +
                                    "");
                    } else if ((eObjectType)item.Object_Type == eObjectType.Scythe)
                    {
                        SendReply(t,
                                    "[Dragonsworn Scythe](" + dragon + " RPs)\n" +
                                    "[Scythe of Kings](" + artifact + " RPs)\n" +
                                    "[Snakechamer's Scythe](" + artifact + " RPs)\n" +
                                    "[Magma Scythe](" + toageneric + " RPs)\n" +
                                    "[Scorched Scythe](" + toageneric + " RPs)\n" +
                                    "");
                    } else if ((eObjectType)item.Object_Type == eObjectType.PolearmWeapon)
                    {
                        SendReply(t,
                                    "[Dragonsworn Pole](" + dragon + " RPs)\n" +
                                    "[Pole of Kings](" + artifact + " RPs)\n" +
                                    "[Golden Pole](" + toageneric + " RPs)\n" +
                                    "[Scorched Pole](" + toageneric + " RPs)\n" +
                                    "");
                    }
                    else if ((eObjectType)item.Object_Type == eObjectType.Spear || (eObjectType)item.Object_Type == eObjectType.CelticSpear)
                    {
                        SendReply(t,
                                    "[Golden Spear 2h](" + artifact + " RPs)\n" +
                                    "[Dragon Spear 2h](" + dragon + " RPs)\n" +
                                    "[Scorched Spear 2h](" + toageneric + " RPs)\n" +
                                    "[Trident Spear 2h](" + toageneric + " RPs)\n" +
                                    "");
                    }
                    else
                    {
                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                SendReply(t,
                                    "[Scorched Thrust 2h](" + toageneric + " RPs)\n" +
                                    "[Dragon Thrust 2h](" + toageneric + " RPs)\n" +
                                    "[Katana 2h](" + epic + " RPs)\n" +
                                    "[Pickaxe](" + epic + " RPs)\n" +
                                    "");
                                break;

                            case eDamageType.Crush:
                                SendReply(t,
                                    "[Battler Hammer 2h](" + artifact + " RPs)\n" +
                                    "[Malice Hammer 2h](" + artifact + " RPs)\n" +
                                    "[Bruiser Hammer 2h](" + artifact + " RPs)\n" +
                                    "[Scorched Hammer 2h](" + toageneric + " RPs)\n" +
                                    "[Magma Hammer 2h](" + toageneric + " RPs)\n" +
                                    "[Pickaxe](" + epic + " RPs)\n" +
                                    "");
                                break;

                            case eDamageType.Slash:
                                SendReply(t,
                                    "[Malice Axe 2h](" + artifact + " RPs)\n" +
                                    "[Scorched Axe 2h](" + toageneric + " RPs)\n" +
                                    "[Magma Axe 2h](" + toageneric + " RPs)\n" +
                                    "[Battler Sword 2h](" + artifact + " RPs)\n" +
                                    "[Scorched Sword 2h](" + toageneric + " RPs)\n" +
                                    "[Katana 2h](" + epic + " RPs)\n" +
                                    "");
                                break;
                        }
                    }
                    SendReply(t, "Or, perhaps you'd just prefer a [hilt 2h] (" + epic + " RPs) \n" +
                                "Additionally, I can apply an [class epic 2h] (" + champion + " RPs) skin. \n");
                    break;

                case Slot.RANGED:
                    if ((eObjectType)item.Object_Type == eObjectType.Instrument)
                    {
                        SendReply(t, "This looks like it plays beautiful music. \n" +
                        "I can apply the following skins: \n\n" +
                        //"[Dragonslayer Harp](" + dragon + " RPs)\n" + //these too
                        "[Class Epic Harp](" + epic + " RPs)\n" +
                        "[Labyrinth Harp](" + toageneric + " RPs)\n" +
                        "");
                    }
                    else
                    {
                        SendReply(t, "Nothing like bringing death from afar. \n" +
                        "I can apply the following skins: \n\n" +
                        //"[Dragonslayer Bow](" + dragon + " RPs)\n" +
                        "[Class Epic Bow](" + epic + " RPs)\n" +
                        "[Braggart's Bow](" + artifact + " RPs)\n" +
                        "[Fool's Bow](" + artifact + " RPs)\n" +
                        "[Labyrinth Bow](" + toageneric + " RPs)\n" +
                        "");
                    }

                    break;
            }

            SendReply(t, ""
                         );
            t.TempProperties.setProperty(TempProperty, item);
            return false;
        }

        #region setrealmlevel
        public void SetRealmLevel(GamePlayer player, int rps)
        {
            if (player == null)
                return;

            if (rps == 0) { player.RealmLevel = 1; }
            else
            if (rps is >= 25 and < 125) { player.RealmLevel = 2; }
            else
            if (rps is >= 125 and < 350) { player.RealmLevel = 3; }
            else
            if (rps is >= 350 and < 750) { player.RealmLevel = 4; }
            else
            if (rps is >= 750 and < 1375) { player.RealmLevel = 5; }
            else
            if (rps is >= 750 and < 1375) { player.RealmLevel = 6; }
            else
            if (rps is >= 2275 and < 3500) { player.RealmLevel = 7; }
            else
            if (rps is >= 3500 and < 5100) { player.RealmLevel = 8; }
            else
            if (rps is >= 5100 and < 7125) { player.RealmLevel = 9; }
            else
            //2l0
            if (rps is >= 7125 and < 9625) { player.RealmLevel = 10; }
            else
            if (rps is >= 9625 and < 12650) { player.RealmLevel = 11; }
            else
            if (rps is >= 16250 and < 20475) { player.RealmLevel = 12; }
            else
            if (rps is >= 20475 and < 25375) { player.RealmLevel = 13; }
            else
            if (rps is >= 25375 and < 31000) { player.RealmLevel = 14; }
            else
            if (rps is >= 31000 and < 37400) { player.RealmLevel = 15; }
            else
            if (rps is >= 37400 and < 44625) { player.RealmLevel = 16; }
            else
            if (rps is >= 44625 and < 52725) { player.RealmLevel = 17; }
            else
            if (rps is >= 52725 and < 61750) { player.RealmLevel = 18; }
            else
            if (rps is >= 61750 and < 71750) { player.RealmLevel = 19; }
            else
            //3l0
            if (rps is >= 71750) {
                player.RealmPoints = 71750;
                player.RealmLevel = 20; }


            player.Out.SendUpdatePlayer();
            player.Out.SendCharStatsUpdate();
            player.Out.SendUpdatePoints();
            player.UpdatePlayerStatus();
        }
        #endregion

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }

        public void SetModel(GamePlayer player, int number)
        {
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
            player.TempProperties.removeProperty(TempProperty);

            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return;

            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Model = number;
            GameServer.Database.AddObject(unique);
            InventoryItem newInventoryItem = GameInventoryItem.Create(unique as ItemTemplate);
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            player.RemoveBountyPoints(300);
            player.SaveIntoDatabase();
        }
    }
}
