//Dawn of Light Version 1.7.48
//12/13/2004
//Written by Gavinius
//based on Nardin and Zjovaz previous script
//08/18/2005
//by sirru
//completely rewrote SetEffect, no duel item things whatsoever left
//compatible with dol 1.7 and added some nice things like more 
//smalltalk, equip, emotes, changing of itemnames and spellcast at the end of process
//plus i added changing of speed and color
//what could be done is trimming the prefixes from the name instead of looking at the db, but i dont know how to do that :)

using System;
using DOL;
using DOL.GS;
using DOL.Events;
using DOL.Database;
using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Database.UniqueID;

namespace DOL.GS {
    [NPCGuildScript("Effect Master")]
    public class EffectNPC : GameNPC {
        private string EFFECTNPC_ITEM_WEAK = "DOL.GS.Scripts.EffectNPC_Item_Manipulation";//used to store the item in the player
        private ushort spell = 7215;//The spell which is casted
        private ushort duration = 3000;//3s, the duration the spell is cast
        private eEmote Emotes = eEmote.Raise;//The Emote the NPC does when Interacted
        private Queue m_timer = new Queue();//Gametimer for casting some spell at the end of the process
        private Queue castplayer = new Queue();//Used to hold the player who the spell gets cast on
        public string currencyName = "Orbs";
        private int effectPrice = 5000; //effects price
        private int dyePrice = 2000; //effects price
        private int removePrice = 0; //removal is free
        public string TempProperty = "ItemEffect";
        public string DisplayedItem = "EffectDisplay";
        public string TempEffectId = "TempEffectID";
        public string TempColorId = "TempColorID";
        private string _currencyID = ServerProperties.Properties.ALT_CURRENCY_ID;



        public override bool AddToWorld()
        {
            GuildName = "Effect Master";
            Level = 50;
            base.AddToWorld();
            return true;
        }
        
        public override bool Interact(GamePlayer player)
        {
            if (base.Interact(player))
            {
                TurnTo(player, 500);
                InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
                InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);

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

                if (displayItem != null)
                    DisplayReskinPreviewTo(player, (InventoryItem)displayItem.Clone());

                return true;
            }

            return false;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null || item.Id_nb == _currencyID) return false;
            if (source is GamePlayer p)
            {
                SendReply(p, "What service do you want to use ?\n" +
                             "I can add an [effect] to it or change its color with a [dye].\n\n" +
                             "Alternatively, I can [remove all effects] or [remove dye] from your weapon. "
                );
                p.TempProperties.setProperty(EFFECTNPC_ITEM_WEAK, item);

                SendReply(p, "When you are finished browsing, let me know and I will [confirm effect]."
                );
                var tmp = (InventoryItem) item.Clone();
                p.TempProperties.setProperty(TempProperty, item);
                p.TempProperties.setProperty(DisplayedItem, tmp);
            }

            return false;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;

            if (!(source is GamePlayer)) return false;

            GamePlayer player = source as GamePlayer;
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(EFFECTNPC_ITEM_WEAK);
            
            int cachedEffectID = player.TempProperties.getProperty<int>(TempEffectId);
            int cachedColorID = player.TempProperties.getProperty<int>(TempColorId);

            if (item == null) return false;

            switch (str)
            {
                #region effects

                case "effect":
                    switch (item.Object_Type)
                    {
                        case (int)eObjectType.TwoHandedWeapon:
                        case (int)eObjectType.LargeWeapons:
                            SendReply(player,
                                "Choose a weapon effect: \n" +
                                "[gr sword - yellow flames] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - orange flames] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - fire with smoke] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - fire with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - yellow flames] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - orange flames] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - fire with smoke] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr sword - fire with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - blue glow with sparkles] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - blue aura with cold vapor] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - icy blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - red aura] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - strong crimson glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - white core red glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - silvery/white glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - gold/yellow glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[gr - hot green glow] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        case (int)eObjectType.Blunt:
                        case (int)eObjectType.CrushingWeapon:
                        case (int)eObjectType.Hammer:
                            SendReply(player,
                                         "Choose a weapon effect: \n" +
                                         "[hammer - red aura] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - fiery glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - more intense fiery glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - flaming] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - torchlike flaming] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - silvery glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - purple glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - blue aura] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[hammer - arcs from head to handle] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - center arcing] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - smaller arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - hot orange core glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - orange aura] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - subtle aura with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - yellow flame] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - mana flame] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - hot green glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - hot red glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - hot purple glow] (" + effectPrice + " " + currencyName + ")\n" +
                                         "[crush - cold vapor] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        case (int)eObjectType.SlashingWeapon:
                        case (int)eObjectType.Sword:
                        case (int)eObjectType.Blades:
                        case (int)eObjectType.Piercing:
                        case (int)eObjectType.ThrustWeapon:
                        case (int)eObjectType.LeftAxe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[longsword - propane-style flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - regular flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - orange flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - rising flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - flame with smoke] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - flame with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hot glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hot aura] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - blue aura] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hot blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hot gold glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hot red glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - red aura] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - cold aura with sparkles] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - cold aura with vapor] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hilt wavering blue beam] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hilt wavering green beam] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hilt wavering red beam] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hilt red/blue beam] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[longsword - hilt purple beam] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - propane flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - orange flame with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - blue aura with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - green cloud with bubbles] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - red aura with blood bubbles] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - evil green glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[shortsword - black glow] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        case (int)eObjectType.Axe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[axe - basic flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - orange flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - slow orange flame with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - fiery/trailing flame] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - cold vapor] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - blue aura with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - hot green glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - hot blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - hot cyan glow (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - hot purple glow] (" + effectPrice + " " + currencyName + ")\n" +
                                        "[axe - blue->purple->orange glow] (" + effectPrice + " " + currencyName + ")\n");
                            break;
                        case (int) eObjectType.Shield:
                            SendReply(player,"[crush - arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - center arcing] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - smaller arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - hot orange core glow] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - orange aura] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - subtle aura with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - yellow flame] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - mana flame] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - hot green glow] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - hot red glow] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - hot purple glow] (" + effectPrice + " " + currencyName + ")\n" +
                                             "[crush - cold vapor] (" + effectPrice + " " + currencyName + ")\n");
                            break;
                        case (int)eObjectType.Spear:
                        case (int)eObjectType.CelticSpear:
                        case (int)eObjectType.PolearmWeapon:
                            SendReply(player,
                                "Choose a weapon effect:  (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - cold with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - evil green aura] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - evil red aura] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - flaming] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - hot gold glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - hot fire glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[battlespear - red aura] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - hot blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - cold with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - flaming] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - electric arcing] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - hot yellow flame] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - orange flame with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - orange to purple flame] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - hot purple flame] (" + effectPrice + " " + currencyName + ")\n" +
                                "[lugged spear - silvery glow] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        case (int)eObjectType.Staff:
                            SendReply(player,
                                "Choose a weapon effect:  (" + effectPrice + " " + currencyName + ")\n" +
                                "[staff - blue glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[staff - blue glow with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                "[staff - gold glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[staff - gold glow with twinkles] (" + effectPrice + " " + currencyName + ")\n" +
                                "[staff - faint red glow] (" + effectPrice + " " + currencyName + ")\n" + 
                                "[crush - arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - center arcing] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - smaller arcing halo] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - hot orange core glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - orange aura] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - subtle aura with sparks] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - yellow flame] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - mana flame] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - hot green glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - hot red glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - hot purple glow] (" + effectPrice + " " + currencyName + ")\n" +
                                "[crush - cold vapor] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        default:
                            SendReply(player,
                                "Unfortunately I cannot work with this item.");
                            break;
                    }

                    break;

                //remove all effect
                case "remove all effects": PreviewEffect(player, 0); break;
                //Longsword
                case "longsword - propane-style flame": PreviewEffect(player, 1); break;
                case "longsword - regular flame": PreviewEffect(player, 2); break;
                case "longsword - orange flame": PreviewEffect(player, 3); break;
                case "longsword - rising flame": PreviewEffect(player, 4); break;
                case "longsword - flame with smoke": PreviewEffect(player, 5); break;
                case "longsword - flame with sparks": PreviewEffect(player, 6); break;
                case "longsword - hot glow": PreviewEffect(player, 7); break;
                case "longsword - hot aura": PreviewEffect(player, 8); break;
                case "longsword - blue aura": PreviewEffect(player, 9); break;
                case "longsword - hot gold glow": PreviewEffect(player, 10); break;
                case "longsword - hot blue glow": PreviewEffect(player, 11); break;
                case "longsword - hot red glow": PreviewEffect(player, 12); break;
                case "longsword - red aura": PreviewEffect(player, 13); break;
                case "longsword - cold aura with sparkles": PreviewEffect(player, 14); break;
                case "longsword - cold aura with vapor": PreviewEffect(player, 15); break;
                case "longsword - hilt wavering blue beam": PreviewEffect(player, 16); break;
                case "longsword - hilt wavering green beam": PreviewEffect(player, 17); break;
                case "longsword - hilt wavering red beam": PreviewEffect(player, 18); break;
                case "longsword - hilt red/blue beam": PreviewEffect(player, 19); break;
                case "longsword - hilt purple beam": PreviewEffect(player,20); break;
                //2hand sword
                case "gr sword - yellow flames": PreviewEffect(player,21); break;
                case "gr sword - orange flames": PreviewEffect(player,22); break;
                case "gr sword - fire with smoke": PreviewEffect(player,23); break;
                case "gr sword - fire with sparks": PreviewEffect(player,24); break;
                //2hand hammer
                case "gr - blue glow with sparkles": PreviewEffect(player,25); break;
                case "gr - blue aura with cold vapor": PreviewEffect(player,26); break;
                case "gr - icy blue glow": PreviewEffect(player,27); break;
                case "gr - red aura": PreviewEffect(player,28); break;
                case "gr - strong crimson glow": PreviewEffect(player,29); break;
                case "gr - white core red glow": PreviewEffect(player,30); break;
                case "gr - silvery/white glow": PreviewEffect(player,31); break;
                case "gr - gold/yellow glow": PreviewEffect(player,31); break;
                case "gr - hot green glow": PreviewEffect(player,33); break;
                //hammer/blunt/crush
                case "hammer - red aura": PreviewEffect(player,34); break;
                case "hammer - fiery glow": PreviewEffect(player,35); break;
                case "hammer - more intense fiery glow": PreviewEffect(player,36); break;
                case "hammer - flaming": PreviewEffect(player,37); break;
                case "hammer - torchlike flaming": PreviewEffect(player,38); break;
                case "hammer - silvery glow": PreviewEffect(player,39); break;
                case "hammer - purple glow": PreviewEffect(player,40); break;
                case "hammer - blue aura": PreviewEffect(player,41); break;
                case "hammer - blue glow": PreviewEffect(player,42); break;
                case "hammer - arcs from head to handle": PreviewEffect(player,43); break;
                case "crush - arcing halo": PreviewEffect(player,44); break;
                case "crush - center arcing": PreviewEffect(player,45); break;
                case "crush - smaller arcing halo": PreviewEffect(player,46); break;
                case "crush - hot orange core glow": PreviewEffect(player,47); break;
                case "crush - orange aura": PreviewEffect(player,48); break;
                case "crush - subtle aura with sparks": PreviewEffect(player,49); break;
                case "crush - yellow flame": PreviewEffect(player,50); break;
                case "crush - mana flame": PreviewEffect(player,51); break;
                case "crush - hot green glow": PreviewEffect(player,52); break;
                case "crush - hot red glow": PreviewEffect(player,53); break;
                case "crush - hot purple glow": PreviewEffect(player,54); break;
                case "crush - cold vapor": PreviewEffect(player,55); break;
                //Axe
                case "axe - basic flame": PreviewEffect(player,56); break;
                case "axe - orange flame": PreviewEffect(player,57); break;
                case "axe - slow orange flame with sparks": PreviewEffect(player,58); break;
                case "axe - fiery/trailing flame": PreviewEffect(player,59); break;
                case "axe - cold vapor": PreviewEffect(player,60); break;
                case "axe - blue aura with twinkles": PreviewEffect(player,61); break;
                case "axe - hot green glow": PreviewEffect(player,62); break;
                case "axe - hot blue glow": PreviewEffect(player,63); break;
                case "axe - hot cyan glow": PreviewEffect(player,64); break;
                case "axe - hot purple glow": PreviewEffect(player,65); break;
                case "axe - blue->purple->orange glow": PreviewEffect(player,66); break;
                //shortsword
                case "shortsword - propane flame": PreviewEffect(player,67); break;
                case "shortsword - orange flame with sparks": PreviewEffect(player,68); break;
                case "shortsword - blue aura with twinkles": PreviewEffect(player,69); break;
                case "shortsword - green cloud with bubbles": PreviewEffect(player,70); break;
                case "shortsword - red aura with blood bubbles": PreviewEffect(player,71); break;
                case "shortsword - evil green glow": PreviewEffect(player,72); break;
                case "shortsword - black glow": PreviewEffect(player,73); break;
                //BattleSpear SetEffect
                case "battlespear - cold with twinkles": PreviewEffect(player,74); break;
                case "battlespear - evil green aura": PreviewEffect(player,75); break;
                case "battlespear - evil red aura": PreviewEffect(player,76); break;
                case "battlespear - flaming": PreviewEffect(player,77); break;
                case "battlespear - hot gold glow": PreviewEffect(player,78); break;
                case "battlespear - hot fire glow": PreviewEffect(player,79); break;
                case "battlespear - red aura": PreviewEffect(player,80); break;
                //Spear SetEffect
                case "lugged spear - blue glow": PreviewEffect(player,81); break;
                case "lugged spear - hot blue glow": PreviewEffect(player,82); break;
                case "lugged spear - cold with twinkles": PreviewEffect(player,83); break;
                case "lugged spear - flaming": PreviewEffect(player,84); break;
                case "lugged spear - electric arcing": PreviewEffect(player,85); break;
                case "lugged spear - hot yellow flame": PreviewEffect(player,86); break;
                case "lugged spear - orange flame with sparks": PreviewEffect(player,87); break;
                case "lugged spear - orange to purple flame": PreviewEffect(player,88); break;
                case "lugged spear - hot purple flame": PreviewEffect(player,89); break;
                case "lugged spear - silvery glow": PreviewEffect(player,90); break;
                //Staff SetEffect
                case "staff - blue glow": PreviewEffect(player,90); break;
                case "staff - blue glow with twinkles": PreviewEffect(player,91); break;
                case "staff - gold glow": PreviewEffect(player,92); break;
                case "staff - gold glow with twinkles": PreviewEffect(player,93); break;
                case "staff - faint red glow": PreviewEffect(player,94); break;
                #endregion
                #region dye

                case "dye":
                    SendReply(player, "Please Choose Your Type Of Color" +
                        "[Blues], [Greens], [Reds], [Yellows], [Purples], [Violets], [Oranges], [Blacks], or [Other]");
                    break;

                case "Blues":
                    SendReply(player,
                            "[Old Turquoise] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Leather Blue] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue-Green Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Turquoise Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Light Blue Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue-Violet Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue Metal] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue 1] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue 2] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue 3] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Blue 4] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Turquoise 1] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Turquoise 2] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Turquoise 3] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Teal 1] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Teal 2] (" + dyePrice + " " + currencyName + ")\n" +
                            "[Teal 3] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Greens":
                    SendReply(player,
                        "[Old Green] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Leather Green] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Leather Forest Green] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Blue-Green Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow-Green Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green Metal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Green 4] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Lime Green] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Green] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Green 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Light Green - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Olive Green - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Sage Green - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Lime Green - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Forest Green - crafter only] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Reds":
                    SendReply(player,
                        "[Old Red] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Leather Red] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple-Red Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red Metal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red 4] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Red] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Red 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Red - crafter only] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Yellows":
                    SendReply(player,
                        "[Old Yellow] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Leather Yellow] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow-Orange Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow- Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow Metal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Yellow 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Light Gold - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dark Gold - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Gold Metal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Yellow] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Purples":
                    SendReply(player,
                        "[Old Purple] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Leather Purple] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Bright Purple Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple- Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple Metal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple 4] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Purple] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Purple 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Purple 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Purple - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dark Purple - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dusky Purple - crafter only] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Violets":
                    SendReply(player,
                        "[Leather Violet] (" + dyePrice + " " + currencyName + ")\n" +
                        "[-Violet Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Violet Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Bright Violet Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Hot Pink - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dusky Rose - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Pink] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Violet] (" + dyePrice + " " + currencyName + ")\n");
                    break;

                case "Oranges":
                    SendReply(player,
                        "[Leather Orange] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Orange Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[-Orange Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Orange 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Orange 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Orange 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Orange] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Orange 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Orange 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dirty Orange - crafter only] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Blacks":
                    SendReply(player,
                        "[Black Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Brown Cloth] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Brown 1] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Brown 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Brown 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Brown - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Gray] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Gray 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Gray 3] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Light Gray - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Gray  - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Olive Gray - crafter only] (" + dyePrice + " " + currencyName + ")\n");
                    break;
                case "Other":
                    SendReply(player,
                        "[Bronze] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Iron] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Steel] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Alloy] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Fine Alloy] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Mithril] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Asterite] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Eog] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Xenium] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Vaanum] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Adamantium] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Mauve] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Charcoal] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Ship Charcoal 2] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Plum - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[Dark Tan - crafter only] (" + dyePrice + " " + currencyName + ")\n" +
                        "[White] (" + dyePrice + " " + currencyName + ")\n");
                    break;

                case "remove dye":
                    SetColor(player,0, removePrice);
                    break;
                case "White":
                    PreviewColor(player,0);
                    break;
                case "Old Red":
                    PreviewColor(player,1);
                    break;
                case "Old Green":
                    PreviewColor(player,2);
                    break;
                case "Old Blue":
                    PreviewColor(player,3);
                    break;
                case "Old Yellow":
                    PreviewColor(player,4);
                    break;
                case "Old Purple":
                    PreviewColor(player,5);
                    break;
                case "Gray":
                    PreviewColor(player,6);
                    break;
                case "Old Turquoise":
                    PreviewColor(player,7);
                    break;
                case "Leather Yellow":
                    PreviewColor(player,8);
                    break;
                case "Leather Red":
                    PreviewColor(player,9);
                    break;
                case "Leather Green":
                    PreviewColor(player,10);
                    break;
                case "Leather Orange":
                    PreviewColor(player,11);
                    break;
                case "Leather Violet":
                    PreviewColor(player,12);
                    break;
                case "Leather Forest Green":
                    PreviewColor(player,13);
                    break;
                case "Leather Blue":
                    PreviewColor(player,14);
                    break;
                case "Leather Purple":
                    PreviewColor(player,15);
                    break;
                case "Bronze":
                    PreviewColor(player,16);
                    break;
                case "Iron":
                    PreviewColor(player,17);
                    break;
                case "Steel":
                    PreviewColor(player,18);
                    break;
                case "Alloy":
                    PreviewColor(player,19);
                    break;
                case "Fine Alloy":
                    PreviewColor(player,20);
                    break;
                case "Mithril":
                    PreviewColor(player,21);
                    break;
                case "Asterite":
                    PreviewColor(player,22);
                    break;
                case "Eog":
                    PreviewColor(player,23);
                    break;
                case "Xenium":
                    PreviewColor(player,24);
                    break;
                case "Vaanum":
                    PreviewColor(player,25);
                    break;
                case "Adamantium":
                    PreviewColor(player,26);
                    break;
                case "Red Cloth":
                    PreviewColor(player,27);
                    break;
                case "Orange Cloth":
                    PreviewColor(player,28);
                    break;
                case "Yellow-Orange Cloth":
                    PreviewColor(player,29);
                    break;
                case "Yellow Cloth":
                    PreviewColor(player,30);
                    break;
                case "Yellow-Green Cloth":
                    PreviewColor(player,31);
                    break;
                case "Green Cloth":
                    PreviewColor(player,32);
                    break;
                case "Blue-Green Cloth":
                    PreviewColor(player,33);
                    break;
                case "Turquoise Cloth":
                    PreviewColor(player,34);
                    break;
                case "Light Blue Cloth":
                    PreviewColor(player,35);
                    break;
                case "Blue Cloth":
                    PreviewColor(player,36);
                    break;
                case "Blue-Violet Cloth":
                    PreviewColor(player,37);
                    break;
                case "Violet Cloth":
                    PreviewColor(player,38);
                    break;
                case "Bright Violet Cloth":
                    PreviewColor(player,39);
                    break;
                case "Purple Cloth":
                    PreviewColor(player,40);
                    break;
                case "Bright Purple Cloth":
                    PreviewColor(player,41);
                    break;
                case "Purple-Red Cloth":
                    PreviewColor(player,42);
                    break;
                case "Black Cloth":
                    PreviewColor(player,43);
                    break;
                case "Brown Cloth":
                    PreviewColor(player,44);
                    break;
                case "Blue Metal":
                    PreviewColor(player,45);
                    break;
                case "Green Metal":
                    PreviewColor(player,46);
                    break;
                case "Yellow Metal":
                    PreviewColor(player,47);
                    break;
                case "Gold Metal":
                    PreviewColor(player,48);
                    break;
                case "Red Metal":
                    PreviewColor(player,49);
                    break;
                case "Purple Metal":
                    PreviewColor(player,50);
                    break;
                case "Blue 1":
                    PreviewColor(player,51);
                    break;
                case "Blue 2":
                    PreviewColor(player,52);
                    break;
                case "Blue 3":
                    PreviewColor(player,53);
                    break;
                case "Blue 4":
                    PreviewColor(player,54);
                    break;
                case "Turquoise 1":
                    PreviewColor(player,55);
                    break;
                case "Turquoise 2":
                    PreviewColor(player,56);
                    break;
                case "Turquoise 3":
                    PreviewColor(player,57);
                    break;
                case "Teal 1":
                    PreviewColor(player,58);
                    break;
                case "Teal 2":
                    PreviewColor(player,59);
                    break;
                case "Teal 3":
                    PreviewColor(player,60);
                    break;
                case "Brown 1":
                    PreviewColor(player,61);
                    break;
                case "Brown 2":
                    PreviewColor(player,62);
                    break;
                case "Brown 3":
                    PreviewColor(player,63);
                    break;
                case "Red 1":
                    PreviewColor(player,64);
                    break;
                case "Red 2":
                    PreviewColor(player,65);
                    break;
                case "Red 3":
                    PreviewColor(player,66);
                    break;
                case "Red 4":
                    PreviewColor(player,67);
                    break;
                case "Green 1":
                    PreviewColor(player,68);
                    break;
                case "Green 2":
                    PreviewColor(player,69);
                    break;
                case "Green 3":
                    PreviewColor(player,70);
                    break;
                case "Green 4":
                    PreviewColor(player,71);
                    break;
                case "Gray 1":
                    PreviewColor(player,72);
                    break;
                case "Gray 2":
                    PreviewColor(player,73);
                    break;
                case "Gray 3":
                    PreviewColor(player,74);
                    break;
                case "Orange 1":
                    PreviewColor(player,75);
                    break;
                case "Orange 2":
                    PreviewColor(player,76);
                    break;
                case "Orange 3":
                    PreviewColor(player,77);
                    break;
                case "Purple 1":
                    PreviewColor(player,78);
                    break;
                case "Purple 2":
                    PreviewColor(player,79);
                    break;
                case "Purple 3":
                    PreviewColor(player,80);
                    break;
                case "Yellow 1":
                    PreviewColor(player,81);
                    break;
                case "Yellow 2":
                    PreviewColor(player,82);
                    break;
                case "Yellow 3":
                    PreviewColor(player,83);
                    break;
                case "violet":
                    PreviewColor(player,84);
                    break;
                case "Mauve":
                    PreviewColor(player,85);
                    break;
                case "Blue 5":
                    PreviewColor(player,86);
                    break;
                case "Purple 4":
                    PreviewColor(player,87);
                    break;
                case "Ship Red":
                    PreviewColor(player,100);
                    break;
                case "Ship Red 2":
                    PreviewColor(player,101);
                    break;
                case "Ship Orange":
                    PreviewColor(player,102);
                    break;
                case "Ship Orange 2":
                    PreviewColor(player,103);
                    break;
                case "Orange 4":
                    PreviewColor(player,104);
                    break;
                case "Ship Yellow":
                    PreviewColor(player,105);
                    break;
                case "Ship Lime Green":
                    PreviewColor(player,106);
                    break;
                case "Ship Green":
                    PreviewColor(player,107);
                    break;
                case "Ship Green 2":
                    PreviewColor(player,108);
                    break;
                case "Ship Turquoise":
                    PreviewColor(player,109);
                    break;
                case "Ship Turquoise 2":
                    PreviewColor(player,110);
                    break;
                case "Ship Blue":
                    PreviewColor(player,111);
                    break;
                case "Ship Blue 2":
                    PreviewColor(player,112);
                    break;
                case "Ship Blue 3":
                    PreviewColor(player,113);
                    break;
                case "Ship Purple":
                    PreviewColor(player,114);
                    break;
                case "Ship Purple 2":
                    PreviewColor(player,115);
                    break;
                case "Ship Purple 3":
                    PreviewColor(player,116);
                    break;
                case "Ship Pink":
                    PreviewColor(player,117);
                    break;
                case "Ship Charcoal":
                    PreviewColor(player,118);
                    break;
                case "Ship Charcoal 2":
                    PreviewColor(player,119);
                    break;
                case "Red - crafter only":
                    PreviewColor(player,120);
                    break;
                case "Plum - crafter only":
                    PreviewColor(player,121);
                    break;
                case "Purple - crafter only":
                    PreviewColor(player,122);
                    break;
                case "Dark Purple - crafter only":
                    PreviewColor(player,123);
                    break;
                case "Dusky Purple - crafter only":
                    PreviewColor(player,124);
                    break;
                case "Light Gold - crafter only":
                    PreviewColor(player,125);
                    break;
                case "Dark Gold - crafter only":
                    PreviewColor(player,126);
                    break;
                case "Dirty Orange - crafter only":
                    PreviewColor(player,127);
                    break;
                case "Dark Tan - crafter only":
                    PreviewColor(player,128);
                    break;
                case "Brown - crafter only":
                    PreviewColor(player,129);
                    break;
                case "Light Green - crafter only":
                    PreviewColor(player,130);
                    break;
                case "Olive Green - crafter only":
                    PreviewColor(player,131);
                    break;
                case "Cornflower Blue - crafter only":
                    PreviewColor(player,132);
                    break;
                case "Light Gray - crafter only":
                    PreviewColor(player,133);
                    break;
                case "Hot Pink - crafter only":
                    PreviewColor(player,134);
                    break;
                case "Dusky Rose - crafter only":
                    PreviewColor(player,135);
                    break;
                case "Sage Green - crafter only":
                    PreviewColor(player,136);
                    break;
                case "Lime Green - crafter only":
                    PreviewColor(player,137);
                    break;
                case "Gray Teal - crafter only":
                    PreviewColor(player,138);
                    break;
                case "Gray Blue - crafter only":
                    PreviewColor(player,139);
                    break;
                case "Olive Gray - crafter only":
                    PreviewColor(player,140);
                    break;
                case "Navy Blue - crafter only":
                    PreviewColor(player,141);
                    break;
                case "Forest Green - crafter only":
                    PreviewColor(player,142);
                    break;
                case "Burgundy - crafter only":
                    PreviewColor(player,143);
                    break;

                    #endregion
                
                case "confirm effect":
                    SetEffect(player, cachedEffectID, effectPrice);
                    break;
                case "confirm color":
                    SetColor(player, cachedColorID, dyePrice);
                    break;
            }

            return true;
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }
        #region setcolor
        public void SetColor(GamePlayer player, int color, int price)
        {
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(EFFECTNPC_ITEM_WEAK);

            player.TempProperties.removeProperty(EFFECTNPC_ITEM_WEAK);

            if (item == null || item.SlotPosition == (int)eInventorySlot.Ground
                || item.OwnerID == null || item.OwnerID != player.InternalID)
            {
                player.Out.SendMessage("Invalid item.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            if (item.Object_Type == 41 || item.Object_Type == 43 || item.Object_Type == 44 ||
               item.Object_Type == 46)
            {
                SendReply(player, "You can't dye that.");
                return;
            }

            int playerOrbs = player.Inventory.CountItemTemplate(_currencyID, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SayTo(player, eChatLoc.CL_PopupWindow, "I need " + price + " " + currencyName + " to dye that.");
                return;
            }

            m_timer.Enqueue(new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Effect), duration));
            castplayer.Enqueue(player);

            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Color = color;
            unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            unique.Id_nb = "Unique" + System.Guid.NewGuid().ToString();
            if (GameServer.Database.ExecuteNonQuery("SELECT ItemUnique_ID FROM itemunique WHERE ItemUnique_ID = 'unique.ObjectId'"))
            {
                unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            }
            if (GameServer.Database.ExecuteNonQuery("SELECT Id_nb FROM itemunique WHERE Id_nb = 'unique.Id_nb'"))
            {
                unique.Id_nb = IDGenerator.GenerateID();
            }
            GameServer.Database.AddObject(unique);

            InventoryItem newInventoryItem = GameInventoryItem.Create<ItemUnique>(unique);
            if(item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if(item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate(_currencyID, price, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            player.TempProperties.removeProperty(TempProperty);
            player.TempProperties.removeProperty(DisplayedItem);
            player.TempProperties.removeProperty(TempEffectId);
            player.TempProperties.removeProperty(TempColorId);
            
            SendReply(player, "Thanks for your donation. The color has come out beautifully, wear it with pride.");

            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellCastAnimation(this, spell, 30);
            }
        }
        #endregion setcolor


        private void PreviewEffect(GamePlayer player, int effect)
        {
            InventoryItem item = (InventoryItem)player.TempProperties.getProperty<InventoryItem>(TempProperty).Clone();
            InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);
            item.Effect = effect;
            player.TempProperties.setProperty(TempEffectId, effect);
            DisplayReskinPreviewTo(player, item);
            SendReply(player, "When you are finished browsing, let me know and I will [confirm effect]."
            );
        }
        
        private void PreviewColor(GamePlayer player, int color)
        {
            InventoryItem item = (InventoryItem)player.TempProperties.getProperty<InventoryItem>(TempProperty).Clone();
            InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);
            item.Color = color;
            player.TempProperties.setProperty(TempColorId, color);
            DisplayReskinPreviewTo(player, item);
            SendReply(player, "When you are finished browsing, let me know and I will [confirm color]."
            );
        }

        #region seteffect
        public void SetEffect(GamePlayer player, int effect, int price)
        {
            if (player == null)
                return;

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(EFFECTNPC_ITEM_WEAK);
            player.TempProperties.removeProperty(EFFECTNPC_ITEM_WEAK);

            if (item == null)
                return;

            if ((item.Object_Type < 1 || item.Object_Type > 26) || item.Object_Type == 42)
            {
                SendReply(player, "I cannot work on anything other than weapons and shields.");
                return;
            }

            if (item == null || item.SlotPosition == (int)eInventorySlot.Ground
                || item.OwnerID == null || item.OwnerID != player.InternalID)
            {
                player.Out.SendMessage("Invalid item.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            int playerOrbs = player.Inventory.CountItemTemplate(_currencyID, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SayTo(player, eChatLoc.CL_PopupWindow, "I need " + price + " " + currencyName + " to enchant that.");
                return;
            }

            m_timer.Enqueue(new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Effect), duration));
            castplayer.Enqueue(player);


            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Effect = effect;
            unique.Id_nb = IDGenerator.GenerateID();
            unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            if (GameServer.Database.ExecuteNonQuery("SELECT ItemUnique_ID FROM itemunique WHERE ItemUnique_ID = 'unique.ObjectId'"))
            {
                unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            }
            if (GameServer.Database.ExecuteNonQuery("SELECT Id_nb FROM itemunique WHERE Id_nb = 'unique.Id_nb'"))
            {
                unique.Id_nb = IDGenerator.GenerateID();
            }
            GameServer.Database.AddObject(unique);

            InventoryItem newInventoryItem = GameInventoryItem.Create<ItemUnique>(unique);
            if(item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if(item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate(_currencyID, price, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            player.TempProperties.removeProperty(TempProperty);
            player.TempProperties.removeProperty(DisplayedItem);
            player.TempProperties.removeProperty(TempEffectId);
            player.TempProperties.removeProperty(TempColorId);
            
            SendReply(player, "Thanks for your donation. May the " + item.Name + " lead you to a bright future.");
            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellCastAnimation(this, spell, 30);
            }
        }
        #endregion seteffect

        public int Effect(ECSGameTimer timer)
        {
            m_timer.Dequeue();
            GamePlayer player = (GamePlayer)castplayer.Dequeue();
            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellEffectAnimation(this, player, spell, 0, false, 1);
            }
            return 0;
        }
        
        private void DisplayReskinPreviewTo(GamePlayer player, InventoryItem item)
        {
            GameNPC display = CreateDisplayNPC(player, item);
            display.AddToWorld();

            var tempAd = new AttackData();
            tempAd.Attacker = display;
            tempAd.Target = display;
            if (item.Hand == 1)
            {
                tempAd.AttackType = AttackData.eAttackType.MeleeTwoHand;
                display.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
            }
            else
            {
                tempAd.AttackType = AttackData.eAttackType.MeleeOneHand;
                display.SwitchWeapon(eActiveWeaponSlot.Standard);
            }

            tempAd.AttackResult = eAttackResult.HitUnstyled;
            display.AttackState = true;
            display.TargetObject = display;
            display.ObjectState = eObjectState.Active;
            display.attackComponent.AttackState = true;
            display.BroadcastLivingEquipmentUpdate();
            player.Out.SendObjectUpdate(display);

            //Uncomment this if you want animations
            // var animationThread = new Thread(() => LoopAnimation(player,item, display,tempAd));
            // animationThread.IsBackground = true;
            // animationThread.Start();
        }
        
        private GameNPC CreateDisplayNPC(GamePlayer player, InventoryItem item)
        {
            var mob = new DisplayModel(player, item);

            //player model contains 5 bits of extra data that causes issues if used
            //for an NPC model. we do this to drop the first 5 bits and fill w/ 0s
            ushort tmpModel = (ushort)(player.Model << 5);
            tmpModel = (ushort)(tmpModel >> 5);

            //Fill the object variables
            mob.X = this.X + 50;
            mob.Y = this.Y;
            mob.Z = this.Z;
            mob.CurrentRegion = this.CurrentRegion;

            return mob;

            /*
            mob.Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
            //Console.WriteLine($"item: {item} slot: {item.Item_Type}");
            //mob.Inventory.AddItem((eInventorySlot) item.Item_Type, item);
            //Console.WriteLine($"mob inventory: {mob.Inventory.ToString()}");
            player.Out.SendNPCCreate(mob);
            //mob.AddToWorld();*/
        }
    }
}

