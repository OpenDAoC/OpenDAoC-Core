using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Spells;
using DOL.GS.Effects;
using DOL.AI.Brain;
using DOL.GS.Housing;

using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class OFAssistant : GameNPC
    {
        public override bool AddToWorld()
        {
            if (!(base.AddToWorld()))
                return false;

            Level = 100;
            Flags |= eFlags.PEACE;

            if (Realm == eRealm.None)
                Realm = eRealm.Albion;

            switch (Realm)
            {
                case eRealm.Albion:
                {
                    Name = "Master Elementalist";
                    Model = 61;
                    LoadEquipmentTemplateFromDatabase("master_elementalist");
                }
                    break;
                case eRealm.Hibernia:
                {
                    Name = "Seoltoir";
                    Model = 342;
                    LoadEquipmentTemplateFromDatabase("seoltoir");
                }
                    break;
                case eRealm.Midgard:
                {
                    Name = "Gothi of Odin";
                    Model = 153;
                    LoadEquipmentTemplateFromDatabase("master_runemaster");
                }
                    break;
            }

            SetOwnBrain(new AssistantTeleporterBrain());

            return true;
        }

        public void CastEffect()
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellCastAnimation(this, 4468, 50);
            }
        }
    }

    public class OFTeleporter : GameNPC
    {
        //Re-Port every 45 seconds.
        private const int ReportInterval = 45;

        //RvR medallions
        private const string HadrianID = "hadrian_necklace";
        private const string EmainID = "emain_necklace";
        private const string OdinID = "odin_necklace";
        private const string VindsaulID = "vindsaul_necklace";
        private const string CainID = "druimcain_necklace";
        private const string SnowdoniaID = "snowdonia_necklace";
        private const string HomeID = "home_necklace";

        //QoL medallions
        private const string BindID = "bind_necklace";
        private const string CityID = "city_necklace";
        
        //Beta medallions
        private const string KeepID = "keep_necklace";

        //housing medallions
        private const string AlbionHousingEntID = "housing_entrance_necklace";
        private const string HiberniaHousingEntID = "housing_entrance_necklace";
        private const string MidgardHousingEntID = "housing_entrance_necklace";
        private const string GuildHouseID = "guild_house_necklace";
        private const string HearthBindID = "hearth_bind_necklace";
        private const string PersonalHouseID = "personal_house_necklace";

        //BG medallions
        private const string BattlegroundsID = "battlegrounds_necklace";

        //Other medallions
        private const string DarknessFallsID = "df_necklace";

        private IList<OFAssistant> m_ofAssistants;

        private IList<OFAssistant> Assistants
        {
            get { return m_ofAssistants; }
            set { m_ofAssistants = value; }
        }

        private DBSpell m_buffSpell;
        private Spell m_portSpell;

        private ECSGameTimer castTimer;

        private Spell PortSpell
        {
            get
            {
                m_buffSpell = new DBSpell();
                m_buffSpell.ClientEffect = 4468;
                m_buffSpell.CastTime = 5;
                m_buffSpell.Icon = 4468;
                m_buffSpell.Duration = ReportInterval;
                m_buffSpell.Target = "Self";
                m_buffSpell.Type = "ArmorFactorBuff";
                m_buffSpell.Name = "TELEPORTER_EFFECT";
                m_buffSpell.RecastDelay = ReportInterval;
                m_portSpell = new Spell(m_buffSpell, 0);
                return m_portSpell;
            }
            set { m_portSpell = value; }
        }

        public void StartTeleporting()
        {
            if (castTimer is null)
                castTimer = new ECSGameTimer(this);

            bool cast = CastSpell(PortSpell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
            if (GetSkillDisabledDuration(PortSpell) > 0)
                cast = false;

            if (Assistants == null)
            {
                Assistants = new List<OFAssistant>();
            }

            if (Assistants.Count < 5)
            {
                //cache our assistants on first run
                foreach (GameNPC assistant in GetNPCsInRadius(5000))
                {
                    if (assistant is OFAssistant)
                    {
                        Assistants.Add(assistant as OFAssistant);
                        Console.WriteLine($"Adding assistant {assistant}");
                    }
                }

                //Console.WriteLine(Assistants.ToString());
            }

            if (cast)
            {
                string portMessage = "";

                switch (Realm)
                {
                    case eRealm.Albion:
                    {
                        portMessage =
                            "From sodden ground to the glow of the moon, let each vessel in this circle depart to lands now lost from the light of our fair Camelot!";
                        break;
                    }
                    case eRealm.Midgard:
                    {
                        portMessage = "Huginn and Munnin guide you all and return with news of your journeys.";
                        break;
                    }
                    case eRealm.Hibernia:
                    {
                        portMessage =
                            "Go forth and rid Hibernia of the threat of foreign barbarians and fools forever.";
                        break;
                    }
                }

                foreach (GamePlayer player in GetPlayersInRadius(500))
                {
                    player.Out.SendMessage(this.Name + " says, \"" + portMessage + "\"", eChatType.CT_Say,
                        eChatLoc.CL_ChatWindow);
                }

                castTimer.Interval = PortSpell.CastTime;
                castTimer.Callback += new ECSGameTimer.ECSTimerCallback(CastTimerCallback);
                castTimer.Start(PortSpell.CastTime);
                foreach (OFAssistant assi in Assistants)
                {
                    assi.CastEffect();
                }
            }
        }

        private int CastTimerCallback(ECSGameTimer selfRegenerationTimer)
        {
            castTimer.Callback -= new ECSGameTimer.ECSTimerCallback(CastTimerCallback);
            OnAfterSpellCastSequence(null);
            return 10;
        }

        public override void OnAfterSpellCastSequence(ISpellHandler handler)
        {
            castTimer.Stop();
            //base.OnAfterSpellCastSequence(handler);

            InventoryItem medallion = null;

            foreach (GamePlayer player in GetPlayersInRadius(300))
            {
                GameLocation PortLocation = null;
                medallion = player.Inventory.GetItem(eInventorySlot.Mythical);

                switch (player.Realm)
                {
                    case eRealm.Albion:
                    {
                        if (medallion != null)
                        {
                            switch (medallion.Id_nb)
                            {
                                case OdinID:
                                    PortLocation = new GameLocation("Odin Alb", 100, 596364, 631509, 5971);
                                    break;
                                case EmainID:
                                    PortLocation = new GameLocation("Emain Alb", 200, 475835, 343661, 4080);
                                    break;
                                case SnowdoniaID:
                                    PortLocation = new GameLocation("Snowdonia Alb", 1, 527608, 358918, 3083);
                                    break;
                                case HomeID:
                                    PortLocation = new GameLocation("Home Alb", 1, 584285, 477200, 2600);
                                    break;
                                case CityID:
                                    PortLocation = new GameLocation("City Alb", 10, 36226, 29820, 7971);
                                    break;
                                case KeepID:
                                    if (!IsAllowedToKeepJump(player))
                                    {
                                        break;
                                    }
                                    PortLocation = new GameLocation("Caer Berkstead", 1, 584271, 390681,  5848, 2160);
                                    break;
                                case BattlegroundsID:
                                {
                                    // if (player.Level is >= 15 and <= 19)
                                    // {
                                    //     if (player.RealmPoints >= 125)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Abermenai Alb", 253, 38113, 53507, 4160, 3268);
                                    // }
                                    // else 
                                    if (player.Level is >= 20 and <= 24)
                                    {
                                        if (player.RealmPoints >= 7125)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Thidranki Alb", 252, 38113, 53507, 4160, 3268);
                                    }
                                    // else if (player.Level is >= 25 and <= 29)
                                    // {
                                    //     if (player.RealmPoints >= 1375)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Murdaigean Alb", 251, 38113, 53507, 4160,
                                    //         3268);
                                    // }
                                    else if (player.Level is >= 30 and <= 34)
                                    {
                                        if (player.RealmPoints >= 122500)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Caledonia Alb", 250, 38113, 53507, 4160, 3268);
                                    }

                                    break;
                                }
                                case DarknessFallsID:
                                    PortLocation = new GameLocation("DF Alb", 249, 31670, 27908, 22893);
                                    break;
                                case AlbionHousingEntID:
                                    PortLocation = new GameLocation("Housing Entrance Alb", 2, 584736, 561341, 3576, 2268);
                                    
                                    break;
                                case PersonalHouseID:
                                    House house = HouseMgr.GetHouseByPlayer(player);
                                    if (house == null)
                                    {
                                        goto case AlbionHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = house.OutdoorJumpPoint;
                                    }
                                    break;
                                case GuildHouseID:
                                    House guildHouse = HouseMgr.GetGuildHouseByPlayer(player);
                                    if (guildHouse == null)
                                    {
                                        goto case AlbionHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = guildHouse.OutdoorJumpPoint;
                                    }
                                    break;
                                case HearthBindID:
                                    // Check if player has set a house bind
                                    if (!(player.BindHouseRegion > 0))
                                    {
                                        SayTo(player, "Sorry, you haven't set any house bind point yet.");
                                        goto case AlbionHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location still exists
                                    ArrayList houses = (ArrayList) HouseMgr.GetHousesCloseToSpot(
                                        (ushort) player.BindHouseRegion,
                                        player.BindHouseXpos, player.BindHouseYpos, 700);
                                    if (houses.Count == 0)
                                    {
                                        SayTo(player,
                                            "I'm afraid I can't teleport you to your hearth since the house at your " +
                                            "house bind location has been torn down.");
                                        goto case AlbionHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location contains a bind stone
                                    House targetHouse = (House) houses[0];
                                    IDictionary<uint, DBHouseHookpointItem>
                                        hookpointItems = targetHouse.HousepointItems;
                                    Boolean hasBindstone = false;

                                    foreach (KeyValuePair<uint, DBHouseHookpointItem> targetHouseItem in hookpointItems)
                                    {
                                        if (((GameObject) targetHouseItem.Value.GameObject).GetName(0, false).ToLower()
                                            .EndsWith("bindstone"))
                                        {
                                            hasBindstone = true;
                                            break;
                                        }
                                    }

                                    if (!hasBindstone)
                                    {
                                        SayTo(player,
                                            "I'm sorry to tell that the bindstone of your current house bind location " +
                                            "has been removed, so I'm not able to teleport you there.");
                                        goto case AlbionHousingEntID;
                                    }

                                    // Check if the player has the permission to bind at the house bind stone
                                    if (!targetHouse.CanBindInHouse(player))
                                    {
                                        SayTo(player,
                                            "You're no longer allowed to bind at the house bindstone you've previously " +
                                            "chosen, hence I'm not allowed to teleport you there.");
                                        goto case AlbionHousingEntID;
                                    }

                                    PortLocation = targetHouse.OutdoorJumpPoint;
                                    break;
                            }
                        }
                    }
                        break;
                    case eRealm.Midgard:
                    {
                        if (medallion != null)
                        {
                            switch (medallion.Id_nb)
                            {
                                case HadrianID:
                                    PortLocation = new GameLocation("Hadrian Mid", 1, 655200, 293217, 4879);
                                    break;
                                case EmainID:
                                    PortLocation = new GameLocation("Emain Mid", 200, 474107, 295199, 3871);
                                    break;
                                case VindsaulID:
                                    PortLocation = new GameLocation("Vindsaul Faste Mid", 100, 704916, 738544, 5704);
                                    break;
                                case HomeID:
                                    PortLocation = new GameLocation("Home Mid", 100, 766811, 669605, 5736);
                                    break;
                                case CityID:
                                    PortLocation = new GameLocation("City Mid", 101, 31746, 27429, 8792);
                                    break;
                                case KeepID:
                                    if (!IsAllowedToKeepJump(player))
                                    {
                                        break;
                                    }
                                    PortLocation = new GameLocation("Glenlock Faste", 100, 707024, 657565, 5184, 2050);
                                    break;
                                case BattlegroundsID:
                                {
                                    // if (player.Level >= 15 && player.Level <= 19)
                                    // {
                                    //     if (player.RealmPoints >= 125)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Abermenai Mid", 253, 53568, 23643, 4530);
                                    // }
                                    // else 
                                    if (player.Level >= 20 && player.Level <= 24)
                                    {
                                        if (player.RealmPoints >= 350)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Thidranki Mid", 252, 53568, 23643, 4530);
                                    }
                                    // else if (player.Level >= 25 && player.Level <= 29)
                                    // {
                                    //     if (player.RealmPoints >= 1375)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Murdaigean Mid", 251, 53568, 23643, 4530);
                                    // }
                                    else if (player.Level >= 30 && player.Level <= 34)
                                    {
                                        if (player.RealmPoints >= 7125)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Caledonia Mid", 250, 53568, 23643, 4530);
                                    }

                                    break;
                                }
                                case DarknessFallsID:
                                    PortLocation = new GameLocation("DF Mid", 249, 18584, 18887, 22892);
                                    break;
                                case MidgardHousingEntID:
                                    PortLocation = new GameLocation("Housing Entrance Mid", 102, 526776, 561737, 3634, 3971);
                                    break;
                                case PersonalHouseID:
                                    House house = HouseMgr.GetHouseByPlayer(player);
                                    if (house == null)
                                    {
                                        goto case MidgardHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = house.OutdoorJumpPoint;
                                    }
                                    break;
                                case GuildHouseID:
                                    House guildHouse = HouseMgr.GetGuildHouseByPlayer(player);
                                    if (guildHouse == null)
                                    {
                                        goto case MidgardHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = guildHouse.OutdoorJumpPoint;
                                    }
                                    break;
                                case HearthBindID:
                                    // Check if player has set a house bind
                                    if (!(player.BindHouseRegion > 0))
                                    {
                                        SayTo(player, "Sorry, you haven't set any house bind point yet.");
                                        goto case MidgardHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location still exists
                                    ArrayList houses = (ArrayList) HouseMgr.GetHousesCloseToSpot(
                                        (ushort) player.BindHouseRegion,
                                        player.BindHouseXpos, player.BindHouseYpos, 700);
                                    if (houses.Count == 0)
                                    {
                                        SayTo(player,
                                            "I'm afraid I can't teleport you to your hearth since the house at your " +
                                            "house bind location has been torn down.");
                                        goto case MidgardHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location contains a bind stone
                                    House targetHouse = (House) houses[0];
                                    IDictionary<uint, DBHouseHookpointItem>
                                        hookpointItems = targetHouse.HousepointItems;
                                    Boolean hasBindstone = false;

                                    foreach (KeyValuePair<uint, DBHouseHookpointItem> targetHouseItem in hookpointItems)
                                    {
                                        if (((GameObject) targetHouseItem.Value.GameObject).GetName(0, false).ToLower()
                                            .EndsWith("bindstone"))
                                        {
                                            hasBindstone = true;
                                            break;
                                        }
                                    }

                                    if (!hasBindstone)
                                    {
                                        SayTo(player,
                                            "I'm sorry to tell that the bindstone of your current house bind location " +
                                            "has been removed, so I'm not able to teleport you there.");
                                        goto case MidgardHousingEntID;
                                    }

                                    // Check if the player has the permission to bind at the house bind stone
                                    if (!targetHouse.CanBindInHouse(player))
                                    {
                                        SayTo(player,
                                            "You're no longer allowed to bind at the house bindstone you've previously " +
                                            "chosen, hence I'm not allowed to teleport you there.");
                                        goto case MidgardHousingEntID;
                                    }

                                    PortLocation = targetHouse.OutdoorJumpPoint;
                                    break;
                            }
                        }
                    }
                        break;
                    case eRealm.Hibernia:
                    {
                        if (medallion != null)
                        {
                            switch (medallion.Id_nb)
                            {
                                case OdinID:
                                    PortLocation = new GameLocation("Odin Hib", 100, 596055, 581400, 6031);
                                    break;
                                case HadrianID:
                                    PortLocation = new GameLocation("Hadrian Hib", 1, 605743, 293676, 4839);
                                    break;
                                case CainID:
                                    PortLocation = new GameLocation("Druim Cain Hib", 200, 421788, 486493, 1824);
                                    break;
                                case HomeID:
                                    PortLocation = new GameLocation("Home Hib", 200, 334386, 420071, 5184);
                                    break;
                                case CityID:
                                    PortLocation = new GameLocation("City Hib", 201, 34140, 32058, 8047);
                                    break;
                                case KeepID:
                                    if (!IsAllowedToKeepJump(player))
                                    {
                                        break;
                                    }
                                    PortLocation = new GameLocation("Dun nGed", 200, 397316, 399496, 4328,3030);
                                    break;
                                case BattlegroundsID:
                                {
                                    // if (player.Level >= 15 && player.Level <= 19)
                                    // {
                                    //     if (player.RealmPoints >= 125)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Abermenai Hib", 253, 17367, 18248, 4320);
                                    // }
                                    // else 
                                    if (player.Level >= 20 && player.Level <= 24)
                                    {
                                        if (player.RealmPoints >= 350)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Thidranki Hib", 252, 17367, 18248, 4320);
                                    }
                                    // else if (player.Level >= 25 && player.Level <= 29)
                                    // {
                                    //     if (player.RealmPoints >= 1375)
                                    //     {
                                    //         break;
                                    //     }
                                    //
                                    //     PortLocation = new GameLocation("Murdaigean Hib", 251, 17367, 18248, 4320);
                                    // }
                                    else if (player.Level >= 30 && player.Level <= 34)
                                    {
                                        if (player.RealmPoints >= 7125)
                                        {
                                            break;
                                        }

                                        PortLocation = new GameLocation("Caledonia Hib", 250, 17367, 18248, 4320);
                                    }

                                    break;
                                }
                                case DarknessFallsID:
                                    PortLocation = new GameLocation("DF Hib", 249, 46385, 40298, 21357);
                                    break;
                                case HiberniaHousingEntID:
                                    PortLocation = new GameLocation("Housing Entrance Hib", 202, 555117, 526463, 3012, 1045);
                                    break;
                                case PersonalHouseID:
                                    House house = HouseMgr.GetHouseByPlayer(player);
                                    if (house == null)
                                    {
                                        goto case HiberniaHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = house.OutdoorJumpPoint;
                                    }
                                    break;
                                case GuildHouseID:
                                    House guildHouse = HouseMgr.GetGuildHouseByPlayer(player);
                                    if (guildHouse == null)
                                    {
                                        goto case HiberniaHousingEntID; // Fall through, port to housing entrance.
                                    }
                                    else
                                    {
                                        PortLocation = guildHouse.OutdoorJumpPoint;
                                    }
                                    break;
                                case HearthBindID:
                                    // Check if player has set a house bind
                                    if (!(player.BindHouseRegion > 0))
                                    {
                                        SayTo(player, "Sorry, you haven't set any house bind point yet.");
                                        goto case HiberniaHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location still exists
                                    ArrayList houses = (ArrayList) HouseMgr.GetHousesCloseToSpot(
                                        (ushort) player.BindHouseRegion,
                                        player.BindHouseXpos, player.BindHouseYpos, 700);
                                    if (houses.Count == 0)
                                    {
                                        SayTo(player,
                                            "I'm afraid I can't teleport you to your hearth since the house at your " +
                                            "house bind location has been torn down.");
                                        goto case HiberniaHousingEntID;
                                    }

                                    // Check if the house at the player's house bind location contains a bind stone
                                    House targetHouse = (House) houses[0];
                                    IDictionary<uint, DBHouseHookpointItem>
                                        hookpointItems = targetHouse.HousepointItems;
                                    Boolean hasBindstone = false;

                                    foreach (KeyValuePair<uint, DBHouseHookpointItem> targetHouseItem in hookpointItems)
                                    {
                                        if (((GameObject) targetHouseItem.Value.GameObject).GetName(0, false).ToLower()
                                            .EndsWith("bindstone"))
                                        {
                                            hasBindstone = true;
                                            break;
                                        }
                                    }

                                    if (!hasBindstone)
                                    {
                                        SayTo(player,
                                            "I'm sorry to tell that the bindstone of your current house bind location " +
                                            "has been removed, so I'm not able to teleport you there.");
                                        goto case HiberniaHousingEntID;
                                    }

                                    // Check if the player has the permission to bind at the house bind stone
                                    if (!targetHouse.CanBindInHouse(player))
                                    {
                                        SayTo(player,
                                            "You're no longer allowed to bind at the house bindstone you've previously " +
                                            "chosen, hence I'm not allowed to teleport you there.");
                                        goto case HiberniaHousingEntID;
                                    }

                                    PortLocation = targetHouse.OutdoorJumpPoint;
                                    break;
                            }
                        }
                    }
                        break;
                }

                //Move the player to the designated port location.
                if (PortLocation != null)
                {
                    //Remove the Necklace.
                    player.Inventory.RemoveItem(medallion);
                    player.MoveTo(PortLocation);
                }
            }
        }

        public override bool AddToWorld()
        {
            if (!(base.AddToWorld()))
                return false;

            if (Realm == eRealm.None)
                Realm = eRealm.Albion;

            Level = 100;

            switch (Realm)
            {
                case eRealm.Albion:
                {
                    Name = "Master Visur";
                    Model = 63;
                    LoadEquipmentTemplateFromDatabase("visur");
                }
                    break;
                case eRealm.Hibernia:
                {
                    Name = "Glasny";
                    Model = 342;
                    LoadEquipmentTemplateFromDatabase("glasny");
                }
                    break;
                case eRealm.Midgard:
                {
                    Name = "Stor Gothi Annark";
                    Model = 153;
                    LoadEquipmentTemplateFromDatabase("stor_gothi");
                }
                    break;
            }

            SetOwnBrain(new MainTeleporterBrain());

            return true;
        }

        private bool IsAllowedToKeepJump(GamePlayer player)
        {
            var keeps = new List<int>();
            switch (player.Realm)
            {
                case eRealm.Albion:
                    keeps.Add(55); // Caer Hurbury
                    keeps.Add(56); // Caer Renaris
                    keeps.Add(51); // Caer Berkstead
                    break;
                case eRealm.Midgard:
                    keeps.Add(81); // Arvakr Faste
                    keeps.Add(80); // Fensalir Faste
                    keeps.Add(79); // Glenlock Faste
                    break;
                case eRealm.Hibernia:
                    keeps.Add(105); // Dun Scathaig
                    keeps.Add(106); // Dun Ailinne
                    keeps.Add(103); // Dun nGed
                    break;
            }

            var allowed = true;
            foreach (var keep in keeps)
            {
                var keepToCheck = GameServer.KeepManager.GetKeepByID(keep);
                if (keepToCheck.Realm == player.Realm && !keepToCheck.InCombat) continue;
                allowed = false;
                break;
            }

            return allowed;
        }
    }

    public class MainTeleporterBrain : StandardMobBrain
    {
        public override void Think()
        {
            OFTeleporter teleporter = Body as OFTeleporter;

            GameSpellEffect effect = null;

            foreach (GameSpellEffect activeEffect in teleporter.EffectList)
            {
                if (activeEffect.Name == "TELEPORTER_EFFECT")
                {
                    effect = activeEffect;
                }
            }

            if (effect != null || teleporter.IsCasting)
                return;

            teleporter.StartTeleporting();
        }
    }

    public class AssistantTeleporterBrain : StandardMobBrain
    {
        public override void Think()
        {
            //do nothing
        }
    }
}