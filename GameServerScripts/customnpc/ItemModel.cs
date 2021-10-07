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
        private int lowbie = 500;
        private int festive = 1000;
        private int toageneric = 2000; //effects price in RPs
        private int artifact = 4000;
        private int epic = 5000;
        private int dragon = 10000;
        private int champion = 20000;

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
                case "spooky breastplate": //yay halloween
                    /*  price = festive;
                      switch ((eObjectType)item.Object_Type)
                      {
                          case eObjectType.Cloth:
                              switch (source.Realm)
                              {
                                  case eRealm.Albion:
                                      modelIDToAssign = 2728;
                                      break;
                                  case eRealm.Hibernia:
                                      modelIDToAssign = 2759;
                                      break;
                                  case eRealm.Midgard:
                                      modelIDToAssign = 2694;
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
                                      modelIDToAssign = 2735;
                                      break;
                                  case eRealm.Hibernia:
                                      modelIDToAssign = 2766;
                                      break;
                                  case eRealm.Midgard:
                                      modelIDToAssign = 2701;
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
                                      modelIDToAssign = 2741;
                                      break;
                                  case eRealm.Midgard:
                                      modelIDToAssign = 2707;
                                      break;
                                  default:
                                      modelIDToAssign = 0;
                                      break;
                              }
                              break;

                          case eObjectType.Reinforced:
                              modelIDToAssign = 2772;
                              break;

                          case eObjectType.Chain:
                              switch (source.Realm)
                              {
                                  case eRealm.Albion:
                                      modelIDToAssign = 2747;
                                      break;
                                  case eRealm.Midgard:
                                      modelIDToAssign = 2713;
                                      break;
                                  default:
                                      modelIDToAssign = 0;
                                      break;
                              }
                              break;
                          case eObjectType.Scale:
                              modelIDToAssign = 2778;
                              break;
                          case eObjectType.Plate:
                              modelIDToAssign = 2753;
                              break;
                      }*/
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
                    price = champion;
                    modelIDToAssign = 3458;
                    break;
                #endregion

                #region 2h wep
                case "malice axe 2h":
                    price = artifact;
                    modelIDToAssign = 2110;
                    break;
                case "malice hammer 2h":
                    price = artifact;
                    modelIDToAssign = 3449;
                    break;
                case "golden spear 2h":
                    price = artifact;
                    modelIDToAssign = 1662;
                    break;
                case "bruiser hammer 2h":
                    price = artifact;
                    modelIDToAssign = 2113;
                    break;
                case "battler hammer 2h":
                    price = artifact;
                    modelIDToAssign = 3448;
                    break;
                case "battler sword 2h":
                    price = artifact;
                    modelIDToAssign = 1670;
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
                #endregion

                #region class weapons
                    //finally time to test :)
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
                            switch ((eObjectType)item.Object_Type)
                            {
                                case eObjectType.Blades:
                                    modelIDToAssign = 3254;
                                    break;
                                case eObjectType.Blunt:
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

                /*
                 * "[Aten's Shield](" + artifact + " RPs)\n" +
                    "[Cyclop's Eye](" + artifact + "RPs)\n" +
                    "[Shield of Khaos](" + artifact + "RPs)\n" +
                    "[Oceanus Shield](" + toageneric + "RPs)\n" +
                    "[Aerus Shield](" + toageneric + "RPs)\n" +
                    "[Magma Shield](" + toageneric + "RPs)\n" +
                    "[Minotaur Shield](" + toageneric + "RPs)\n" +
                 */
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
                SendReply(player, "I'm sorry, I seem to have gotten confused. Please start over.");
                return false;
            }


        }

        //lets run it and see what happens
        //load time is way better now
        //accidentally nuked my environment earlier and
        //when I got it fixed it was good again

        //shrug
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer t = source as GamePlayer;
            if (t == null || item == null) return false;
            if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
            {
                t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            Console.WriteLine($"Item type: {item.Item_Type}");

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
                        "[Class Epic Chestpiece](" + epic + "RPs)\n" +
                        "[Eirene's Chest](" + artifact + " RPs)\n" +
                        "[Naliah's Robe](" + artifact + " RPs)\n" +
                        "[Guard of Valor](" + artifact + " RPs)\n" +
                        "[Golden Scarab Vest](" + artifact + " RPs)\n" +
                        "[Oceanus Breastplate] (" + toageneric + " RPs)\n" +
                        "[Stygia Breastplate] (" + toageneric + " RPs)\n" +
                        "[Volcanus Breastplate] (" + toageneric + " RPs)\n" +
                        "[Aerus Breastplate] (" + toageneric + " RPs)\n" +
                        // "[Spooky Breastplate](" + festive + " RPs)\n" +
                        "");
                    break;


                case Slot.RIGHTHAND:
                    SendReply(t, "Ah, I know a highly lethal weapon when I see it. \n" +
                        "I can apply the following skins: \n\n");
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Thrust:
                            SendReply(t,
                                "[Traitor's Dagger 1h](" + artifact + " RPs)\n" +
                                "[Croc Tooth Dagger 1h](" + artifact + "RPs)\n" +
                                "[Golden Spear 1h](" + artifact + "RPs)\n" +
                                "Or, perhaps you'd just prefer a [hilt 1h] " + epic + " \n" +//alb 673/hib 674/mid 670
                                "");
                            break;

                        case eDamageType.Crush:
                            SendReply(t,
                                "[Battler Hammer 1h](" + artifact + " RPs)\n" +
                                "[Malice Hammer 1h](" + artifact + "RPs)\n" +
                                "[Bruiser Hammer 1h](" + artifact + "RPs)\n" +
                                "[Scepter of the Meritorious](" + artifact + "RPs)\n" +
                                "[Rolling Pin](" + champion + "RPs)\n" + //lol 3458 
                                "");
                            break;

                        case eDamageType.Slash:
                            SendReply(t,
                                "[Croc Tooth Axe 1h](" + artifact + " RPs)\n" +
                                "[Traitor's Axe 1h](" + artifact + "RPs)\n" +
                                "[Malice Axe 1h](" + artifact + "RPs)\n" +
                                "[Battler Sword 1h](" + artifact + "RPs)\n" +
                                "Or, perhaps you'd just prefer a [hilt 1h] " + epic + " \n" +
                                "");
                            break;
                    }
                    SendReply(t, "Additionally, I can apply an [class epic 1h] " + champion + " skin. \n");

                    break;


                case Slot.LEFTHAND:
                    if ((eObjectType)item.Object_Type == eObjectType.Shield)
                    {
                        SendReply(t, "A sturdy barricade to ward the blows of your enemies. \n" +
                        "I can apply the following skins: \n\n" +
                        "[Aten's Shield](" + artifact + " RPs)\n" +
                        "[Cyclop's Eye](" + artifact + "RPs)\n" +
                        "[Shield of Khaos](" + artifact + "RPs)\n" +
                        "[Oceanus Shield](" + toageneric + "RPs)\n" +
                        "[Aerus Shield](" + toageneric + "RPs)\n" +
                        "[Magma Shield](" + toageneric + "RPs)\n" +
                        "[Minotaur Shield](" + toageneric + "RPs)\n" +
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
                    switch ((eDamageType)item.Type_Damage)
                    {
                        case eDamageType.Thrust:
                            SendReply(t,
                                "[Golden Spear 2h](" + artifact + "RPs)\n" +
                                "Or, perhaps you'd just prefer a [hilt 2h] " + epic + " \n" +//alb 673/hib 674/mid 670
                                "");
                            break;

                        case eDamageType.Crush:
                            SendReply(t,
                                "[Battler Hammer 2h](" + artifact + " RPs)\n" +
                                "[Malice Hammer 2h](" + artifact + "RPs)\n" +
                                "[Bruiser Hammer 2h](" + artifact + "RPs)\n" +
                                "");
                            break;

                        case eDamageType.Slash:
                            SendReply(t,
                                "[Malice Axe 2h](" + artifact + "RPs)\n" +
                                "[Battler Sword 2h](" + artifact + "RPs)\n" +
                                "Or, perhaps you'd just prefer a [hilt 2h] (" + epic + "RPs) \n" +
                                "");
                            break;
                    }
                    SendReply(t, "Additionally, I can apply an [class epic 2h] (" + champion + "RPs) skin. \n");

                    break;

                case Slot.RANGED:
                    if ((eObjectType)item.Object_Type == eObjectType.Instrument)
                    {
                        SendReply(t, "This looks like it plays beautiful music. \n" +
                        "I can apply the following skins: \n\n" +
                        //"[Dragonslayer Harp](" + dragon + " RPs)\n" +
                        "[Class Epic Harp](" + epic + "RPs)\n" +
                        "[Labyrinth Harp](" + toageneric + "RPs)\n" +
                        "");
                    }
                    else
                    {
                        SendReply(t, "Nothing like bringing death from afar. \n" +
                        "I can apply the following skins: \n\n" +
                        //"[Dragonslayer Bow](" + dragon + " RPs)\n" +
                        "[Class Epic Bow](" + epic + "RPs)\n" +
                        "[Braggart's Bow](" + artifact + "RPs)\n" +
                        "[Fool's Bow](" + artifact + "RPs)\n" +
                        "[Labyrinth Bow](" + toageneric + "RPs)\n" +
                        // I am losing my mind
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
            if (rps is >= 2275 and < 3500) { player.RealmLevel = 6; }
            else
            if (rps is >= 3500 and < 5100) { player.RealmLevel = 7; }
            else
            if (rps is >= 5100 and < 7125) { player.RealmLevel = 8; }
            else
            if (rps is >= 7125 and < 9625) { player.RealmLevel = 9; }
            else
            if (rps is >= 9625 and < 12650) { player.RealmLevel = 10; }
            else
            if (rps is >= 16250 and < 20475) { player.RealmLevel = 11; }
            else
            if (rps is >= 20475 and < 25375) { player.RealmLevel = 12; }
            else
            if (rps is >= 25375 and < 31000) { player.RealmLevel = 13; }
            else
            if (rps is >= 31000 and < 37400) { player.RealmLevel = 14; }
            else
            if (rps is >= 37400 and < 44625) { player.RealmLevel = 15; }
            else
            if (rps is >= 44625 and < 52725) { player.RealmLevel = 16; }
            else
            if (rps is >= 52725 and < 61750) { player.RealmLevel = 17; }
            else
            if (rps is >= 61750 and < 71750) { player.RealmLevel = 18; }

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
