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
        private int effectPrice = 1800; //effects price
        private int dyePrice = 500; //effects price
        private int removePrice = 0; //removal is free


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
                TurnTo(player, 250);
                foreach (GamePlayer emoteplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    emoteplayer.Out.SendEmoteAnimation(this, Emotes);
                }
                SendReply(player, "Greetings " + player.Name + "!\n\n" +
                                    "I can either change the effect or the color of your weapons, armors...\n" +
                                    "Simply give me the item and i will start my work.\n\n" +
                                    "In exchange for my services, I will gladly take some of your Atlas Orbs.");
                //"On my countless journeys, i have mastered the art of"+ Didnt like the amount of talking
                //"focusing the etheral flows to a certain weapon.\n"+  so i slimmed it a  bit o.O
                //"Using this technique, i can make your weapon glow in"+
                //"every kind and color you can imagine.\n"+
                //"Just hand me the weapon and pay a small donation of "+PriceString+".");
                return true;
            }
            return false;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            GamePlayer t = source as GamePlayer;
            if (t == null || item == null || item.Template.Name.Equals("token_many")) return false;
            if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
            {
                t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            SendReply(t, "What service do you want to use ?\n" +
                         "I can add an [effect] to it or change its color with a [dye].\n\n" +
                         "Alternatively, I can [remove all effects] or [remove dye] from your weapon. "
                         );
            t.TempProperties.setProperty(EFFECTNPC_ITEM_WEAK, item);
            return false;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;

            if (!(source is GamePlayer)) return false;

            GamePlayer player = source as GamePlayer;
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(EFFECTNPC_ITEM_WEAK);

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
                                "[staff - faint red glow] (" + effectPrice + " " + currencyName + ")\n");
                            break;

                        default:
                            SendReply(player,
                                "Unfortunately I cannot work with this item.");
                            break;
                    }

                    break;

                //remove all effect
                case "remove all effects": SetEffect(player, 0, removePrice); break;
                //Longsword
                case "longsword - propane-style flame": SetEffect(player, 1, effectPrice); break;
                case "longsword - regular flame": SetEffect(player, 2, effectPrice); break;
                case "longsword - orange flame": SetEffect(player, 3, effectPrice); break;
                case "longsword - rising flame": SetEffect(player, 4, effectPrice); break;
                case "longsword - flame with smoke": SetEffect(player, 5, effectPrice); break;
                case "longsword - flame with sparks": SetEffect(player, 6, effectPrice); break;
                case "longsword - hot glow": SetEffect(player, 7, effectPrice); break;
                case "longsword - hot aura": SetEffect(player, 8, effectPrice); break;
                case "longsword - blue aura": SetEffect(player, 9, effectPrice); break;
                case "longsword - hot gold glow": SetEffect(player, 10, effectPrice); break;
                case "longsword - hot blue glow": SetEffect(player, 11, effectPrice); break;
                case "longsword - hot red glow": SetEffect(player, 12, effectPrice); break;
                case "longsword - red aura": SetEffect(player, 13, effectPrice); break;
                case "longsword - cold aura with sparkles": SetEffect(player, 14, effectPrice); break;
                case "longsword - cold aura with vapor": SetEffect(player, 15, effectPrice); break;
                case "longsword - hilt wavering blue beam": SetEffect(player, 16, effectPrice); break;
                case "longsword - hilt wavering green beam": SetEffect(player, 17, effectPrice); break;
                case "longsword - hilt wavering red beam": SetEffect(player, 18, effectPrice); break;
                case "longsword - hilt red/blue beam": SetEffect(player, 19, effectPrice); break;
                case "longsword - hilt purple beam": SetEffect(player, 20, effectPrice); break;
                //2hand sword
                case "gr sword - yellow flames": SetEffect(player, 21, effectPrice); break;
                case "gr sword - orange flames": SetEffect(player, 22, effectPrice); break;
                case "gr sword - fire with smoke": SetEffect(player, 23, effectPrice); break;
                case "gr sword - fire with sparks": SetEffect(player, 24, effectPrice); break;
                //2hand hammer
                case "gr - blue glow with sparkles": SetEffect(player, 25, effectPrice); break;
                case "gr - blue aura with cold vapor": SetEffect(player, 26, effectPrice); break;
                case "gr - icy blue glow": SetEffect(player, 27, effectPrice); break;
                case "gr - red aura": SetEffect(player, 28, effectPrice); break;
                case "gr - strong crimson glow": SetEffect(player, 29, effectPrice); break;
                case "gr - white core red glow": SetEffect(player, 30, effectPrice); break;
                case "gr - silvery/white glow": SetEffect(player, 31, effectPrice); break;
                case "gr - gold/yellow glow": SetEffect(player, 31, effectPrice); break;
                case "gr - hot green glow": SetEffect(player, 33, effectPrice); break;
                //hammer/blunt/crush
                case "hammer - red aura": SetEffect(player, 34, effectPrice); break;
                case "hammer - fiery glow": SetEffect(player, 35, effectPrice); break;
                case "hammer - more intense fiery glow": SetEffect(player, 36, effectPrice); break;
                case "hammer - flaming": SetEffect(player, 37, effectPrice); break;
                case "hammer - torchlike flaming": SetEffect(player, 38, effectPrice); break;
                case "hammer - silvery glow": SetEffect(player, 39, effectPrice); break;
                case "hammer - purple glow": SetEffect(player, 40, effectPrice); break;
                case "hammer - blue aura": SetEffect(player, 41, effectPrice); break;
                case "hammer - blue glow": SetEffect(player, 42, effectPrice); break;
                case "hammer - arcs from head to handle": SetEffect(player, 43, effectPrice); break;
                case "crush - arcing halo": SetEffect(player, 44, effectPrice); break;
                case "crush - center arcing": SetEffect(player, 45, effectPrice); break;
                case "crush - smaller arcing halo": SetEffect(player, 46, effectPrice); break;
                case "crush - hot orange core glow": SetEffect(player, 47, effectPrice); break;
                case "crush - orange aura": SetEffect(player, 48, effectPrice); break;
                case "crush - subtle aura with sparks": SetEffect(player, 49, effectPrice); break;
                case "crush - yellow flame": SetEffect(player, 50, effectPrice); break;
                case "crush - mana flame": SetEffect(player, 51, effectPrice); break;
                case "crush - hot green glow": SetEffect(player, 52, effectPrice); break;
                case "crush - hot red glow": SetEffect(player, 53, effectPrice); break;
                case "crush - hot purple glow": SetEffect(player, 54, effectPrice); break;
                case "crush - cold vapor": SetEffect(player, 55, effectPrice); break;
                //Axe
                case "axe - basic flame": SetEffect(player, 56, effectPrice); break;
                case "axe - orange flame": SetEffect(player, 57, effectPrice); break;
                case "axe - slow orange flame with sparks": SetEffect(player, 58, effectPrice); break;
                case "axe - fiery/trailing flame": SetEffect(player, 59, effectPrice); break;
                case "axe - cold vapor": SetEffect(player, 60, effectPrice); break;
                case "axe - blue aura with twinkles": SetEffect(player, 61, effectPrice); break;
                case "axe - hot green glow": SetEffect(player, 62, effectPrice); break;
                case "axe - hot blue glow": SetEffect(player, 63, effectPrice); break;
                case "axe - hot cyan glow": SetEffect(player, 64, effectPrice); break;
                case "axe - hot purple glow": SetEffect(player, 65, effectPrice); break;
                case "axe - blue->purple->orange glow": SetEffect(player, 66, effectPrice); break;
                //shortsword
                case "shortsword - propane flame": SetEffect(player, 67, effectPrice); break;
                case "shortsword - orange flame with sparks": SetEffect(player, 68, effectPrice); break;
                case "shortsword - blue aura with twinkles": SetEffect(player, 69, effectPrice); break;
                case "shortsword - green cloud with bubbles": SetEffect(player, 70, effectPrice); break;
                case "shortsword - red aura with blood bubbles": SetEffect(player, 71, effectPrice); break;
                case "shortsword - evil green glow": SetEffect(player, 72, effectPrice); break;
                case "shortsword - black glow": SetEffect(player, 73, effectPrice); break;
                //BattleSpear SetEffect
                case "battlespear - cold with twinkles": SetEffect(player, 74, effectPrice); break;
                case "battlespear - evil green aura": SetEffect(player, 75, effectPrice); break;
                case "battlespear - evil red aura": SetEffect(player, 76, effectPrice); break;
                case "battlespear - flaming": SetEffect(player, 77, effectPrice); break;
                case "battlespear - hot gold glow": SetEffect(player, 78, effectPrice); break;
                case "battlespear - hot fire glow": SetEffect(player, 79, effectPrice); break;
                case "battlespear - red aura": SetEffect(player, 80, effectPrice); break;
                //Spear SetEffect
                case "lugged spear - blue glow": SetEffect(player, 81, effectPrice); break;
                case "lugged spear - hot blue glow": SetEffect(player, 82, effectPrice); break;
                case "lugged spear - cold with twinkles": SetEffect(player, 83, effectPrice); break;
                case "lugged spear - flaming": SetEffect(player, 84, effectPrice); break;
                case "lugged spear - electric arcing": SetEffect(player, 85, effectPrice); break;
                case "lugged spear - hot yellow flame": SetEffect(player, 86, effectPrice); break;
                case "lugged spear - orange flame with sparks": SetEffect(player, 87, effectPrice); break;
                case "lugged spear - orange to purple flame": SetEffect(player, 88, effectPrice); break;
                case "lugged spear - hot purple flame": SetEffect(player, 89, effectPrice); break;
                case "lugged spear - silvery glow": SetEffect(player, 90, effectPrice); break;
                //Staff SetEffect
                case "staff - blue glow": SetEffect(player, 90, effectPrice); break;
                case "staff - blue glow with twinkles": SetEffect(player, 91, effectPrice); break;
                case "staff - gold glow": SetEffect(player, 92, effectPrice); break;
                case "staff - gold glow with twinkles": SetEffect(player, 93, effectPrice); break;
                case "staff - faint red glow": SetEffect(player, 94, effectPrice); break;
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
                    SetColor(player, 0, removePrice);
                    break;
                case "White":
                    SetColor(player, 0, dyePrice);
                    break;
                case "Old Red":
                    SetColor(player, 1, dyePrice);
                    break;
                case "Old Green":
                    SetColor(player, 2, dyePrice);
                    break;
                case "Old Blue":
                    SetColor(player, 3, dyePrice);
                    break;
                case "Old Yellow":
                    SetColor(player, 4, dyePrice);
                    break;
                case "Old Purple":
                    SetColor(player, 5, dyePrice);
                    break;
                case "Gray":
                    SetColor(player, 6, dyePrice);
                    break;
                case "Old Turquoise":
                    SetColor(player, 7, dyePrice);
                    break;
                case "Leather Yellow":
                    SetColor(player, 8, dyePrice);
                    break;
                case "Leather Red":
                    SetColor(player, 9, dyePrice);
                    break;
                case "Leather Green":
                    SetColor(player, 10, dyePrice);
                    break;
                case "Leather Orange":
                    SetColor(player, 11, dyePrice);
                    break;
                case "Leather Violet":
                    SetColor(player, 12, dyePrice);
                    break;
                case "Leather Forest Green":
                    SetColor(player, 13, dyePrice);
                    break;
                case "Leather Blue":
                    SetColor(player, 14, dyePrice);
                    break;
                case "Leather Purple":
                    SetColor(player, 15, dyePrice);
                    break;
                case "Bronze":
                    SetColor(player, 16, dyePrice);
                    break;
                case "Iron":
                    SetColor(player, 17, dyePrice);
                    break;
                case "Steel":
                    SetColor(player, 18, dyePrice);
                    break;
                case "Alloy":
                    SetColor(player, 19, dyePrice);
                    break;
                case "Fine Alloy":
                    SetColor(player, 20, dyePrice);
                    break;
                case "Mithril":
                    SetColor(player, 21, dyePrice);
                    break;
                case "Asterite":
                    SetColor(player, 22, dyePrice);
                    break;
                case "Eog":
                    SetColor(player, 23, dyePrice);
                    break;
                case "Xenium":
                    SetColor(player, 24, dyePrice);
                    break;
                case "Vaanum":
                    SetColor(player, 25, dyePrice);
                    break;
                case "Adamantium":
                    SetColor(player, 26, dyePrice);
                    break;
                case "Red Cloth":
                    SetColor(player, 27, dyePrice);
                    break;
                case "Orange Cloth":
                    SetColor(player, 28, dyePrice);
                    break;
                case "Yellow-Orange Cloth":
                    SetColor(player, 29, dyePrice);
                    break;
                case "Yellow Cloth":
                    SetColor(player, 30, dyePrice);
                    break;
                case "Yellow-Green Cloth":
                    SetColor(player, 31, dyePrice);
                    break;
                case "Green Cloth":
                    SetColor(player, 32, dyePrice);
                    break;
                case "Blue-Green Cloth":
                    SetColor(player, 33, dyePrice);
                    break;
                case "Turquoise Cloth":
                    SetColor(player, 34, dyePrice);
                    break;
                case "Light Blue Cloth":
                    SetColor(player, 35, dyePrice);
                    break;
                case "Blue Cloth":
                    SetColor(player, 36, dyePrice);
                    break;
                case "Blue-Violet Cloth":
                    SetColor(player, 37, dyePrice);
                    break;
                case "Violet Cloth":
                    SetColor(player, 38, dyePrice);
                    break;
                case "Bright Violet Cloth":
                    SetColor(player, 39, dyePrice);
                    break;
                case "Purple Cloth":
                    SetColor(player, 40, dyePrice);
                    break;
                case "Bright Purple Cloth":
                    SetColor(player, 41, dyePrice);
                    break;
                case "Purple-Red Cloth":
                    SetColor(player, 42, dyePrice);
                    break;
                case "Black Cloth":
                    SetColor(player, 43, dyePrice);
                    break;
                case "Brown Cloth":
                    SetColor(player, 44, dyePrice);
                    break;
                case "Blue Metal":
                    SetColor(player, 45, dyePrice);
                    break;
                case "Green Metal":
                    SetColor(player, 46, dyePrice);
                    break;
                case "Yellow Metal":
                    SetColor(player, 47, dyePrice);
                    break;
                case "Gold Metal":
                    SetColor(player, 48, dyePrice);
                    break;
                case "Red Metal":
                    SetColor(player, 49, dyePrice);
                    break;
                case "Purple Metal":
                    SetColor(player, 50, dyePrice);
                    break;
                case "Blue 1":
                    SetColor(player, 51, dyePrice);
                    break;
                case "Blue 2":
                    SetColor(player, 52, dyePrice);
                    break;
                case "Blue 3":
                    SetColor(player, 53, dyePrice);
                    break;
                case "Blue 4":
                    SetColor(player, 54, dyePrice);
                    break;
                case "Turquoise 1":
                    SetColor(player, 55, dyePrice);
                    break;
                case "Turquoise 2":
                    SetColor(player, 56, dyePrice);
                    break;
                case "Turquoise 3":
                    SetColor(player, 57, dyePrice);
                    break;
                case "Teal 1":
                    SetColor(player, 58, dyePrice);
                    break;
                case "Teal 2":
                    SetColor(player, 59, dyePrice);
                    break;
                case "Teal 3":
                    SetColor(player, 60, dyePrice);
                    break;
                case "Brown 1":
                    SetColor(player, 61, dyePrice);
                    break;
                case "Brown 2":
                    SetColor(player, 62, dyePrice);
                    break;
                case "Brown 3":
                    SetColor(player, 63, dyePrice);
                    break;
                case "Red 1":
                    SetColor(player, 64, dyePrice);
                    break;
                case "Red 2":
                    SetColor(player, 65, dyePrice);
                    break;
                case "Red 3":
                    SetColor(player, 66, dyePrice);
                    break;
                case "Red 4":
                    SetColor(player, 67, dyePrice);
                    break;
                case "Green 1":
                    SetColor(player, 68, dyePrice);
                    break;
                case "Green 2":
                    SetColor(player, 69, dyePrice);
                    break;
                case "Green 3":
                    SetColor(player, 70, dyePrice);
                    break;
                case "Green 4":
                    SetColor(player, 71, dyePrice);
                    break;
                case "Gray 1":
                    SetColor(player, 72, dyePrice);
                    break;
                case "Gray 2":
                    SetColor(player, 73, dyePrice);
                    break;
                case "Gray 3":
                    SetColor(player, 74, dyePrice);
                    break;
                case "Orange 1":
                    SetColor(player, 75, dyePrice);
                    break;
                case "Orange 2":
                    SetColor(player, 76, dyePrice);
                    break;
                case "Orange 3":
                    SetColor(player, 77, dyePrice);
                    break;
                case "Purple 1":
                    SetColor(player, 78, dyePrice);
                    break;
                case "Purple 2":
                    SetColor(player, 79, dyePrice);
                    break;
                case "Purple 3":
                    SetColor(player, 80, dyePrice);
                    break;
                case "Yellow 1":
                    SetColor(player, 81, dyePrice);
                    break;
                case "Yellow 2":
                    SetColor(player, 82, dyePrice);
                    break;
                case "Yellow 3":
                    SetColor(player, 83, dyePrice);
                    break;
                case "violet":
                    SetColor(player, 84, dyePrice);
                    break;
                case "Mauve":
                    SetColor(player, 85, dyePrice);
                    break;
                case "Blue 5":
                    SetColor(player, 86, dyePrice);
                    break;
                case "Purple 4":
                    SetColor(player, 87, dyePrice);
                    break;
                case "Ship Red":
                    SetColor(player, 100, dyePrice);
                    break;
                case "Ship Red 2":
                    SetColor(player, 101, dyePrice);
                    break;
                case "Ship Orange":
                    SetColor(player, 102, dyePrice);
                    break;
                case "Ship Orange 2":
                    SetColor(player, 103, dyePrice);
                    break;
                case "Orange 4":
                    SetColor(player, 104, dyePrice);
                    break;
                case "Ship Yellow":
                    SetColor(player, 105, dyePrice);
                    break;
                case "Ship Lime Green":
                    SetColor(player, 106, dyePrice);
                    break;
                case "Ship Green":
                    SetColor(player, 107, dyePrice);
                    break;
                case "Ship Green 2":
                    SetColor(player, 108, dyePrice);
                    break;
                case "Ship Turquoise":
                    SetColor(player, 109, dyePrice);
                    break;
                case "Ship Turquoise 2":
                    SetColor(player, 110, dyePrice);
                    break;
                case "Ship Blue":
                    SetColor(player, 111, dyePrice);
                    break;
                case "Ship Blue 2":
                    SetColor(player, 112, dyePrice);
                    break;
                case "Ship Blue 3":
                    SetColor(player, 113, dyePrice);
                    break;
                case "Ship Purple":
                    SetColor(player, 114, dyePrice);
                    break;
                case "Ship Purple 2":
                    SetColor(player, 115, dyePrice);
                    break;
                case "Ship Purple 3":
                    SetColor(player, 116, dyePrice);
                    break;
                case "Ship Pink":
                    SetColor(player, 117, dyePrice);
                    break;
                case "Ship Charcoal":
                    SetColor(player, 118, dyePrice);
                    break;
                case "Ship Charcoal 2":
                    SetColor(player, 119, dyePrice);
                    break;
                case "Red - crafter only":
                    SetColor(player, 120, dyePrice);
                    break;
                case "Plum - crafter only":
                    SetColor(player, 121, dyePrice);
                    break;
                case "Purple - crafter only":
                    SetColor(player, 122, dyePrice);
                    break;
                case "Dark Purple - crafter only":
                    SetColor(player, 123, dyePrice);
                    break;
                case "Dusky Purple - crafter only":
                    SetColor(player, 124, dyePrice);
                    break;
                case "Light Gold - crafter only":
                    SetColor(player, 125, dyePrice);
                    break;
                case "Dark Gold - crafter only":
                    SetColor(player, 126, dyePrice);
                    break;
                case "Dirty Orange - crafter only":
                    SetColor(player, 127, dyePrice);
                    break;
                case "Dark Tan - crafter only":
                    SetColor(player, 128, dyePrice);
                    break;
                case "Brown - crafter only":
                    SetColor(player, 129, dyePrice);
                    break;
                case "Light Green - crafter only":
                    SetColor(player, 130, dyePrice);
                    break;
                case "Olive Green - crafter only":
                    SetColor(player, 131, dyePrice);
                    break;
                case "Cornflower Blue - crafter only":
                    SetColor(player, 132, dyePrice);
                    break;
                case "Light Gray - crafter only":
                    SetColor(player, 133, dyePrice);
                    break;
                case "Hot Pink - crafter only":
                    SetColor(player, 134, dyePrice);
                    break;
                case "Dusky Rose - crafter only":
                    SetColor(player, 135, dyePrice);
                    break;
                case "Sage Green - crafter only":
                    SetColor(player, 136, dyePrice);
                    break;
                case "Lime Green - crafter only":
                    SetColor(player, 137, dyePrice);
                    break;
                case "Gray Teal - crafter only":
                    SetColor(player, 138, dyePrice);
                    break;
                case "Gray Blue - crafter only":
                    SetColor(player, 139, dyePrice);
                    break;
                case "Olive Gray - crafter only":
                    SetColor(player, 140, dyePrice);
                    break;
                case "Navy Blue - crafter only":
                    SetColor(player, 141, dyePrice);
                    break;
                case "Forest Green - crafter only":
                    SetColor(player, 142, dyePrice);
                    break;
                case "Burgundy - crafter only":
                    SetColor(player, 143, dyePrice);
                    break;

                    #endregion
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
            }

            int playerOrbs = player.Inventory.CountItemTemplate("token_many", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
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
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate("token_many", price, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            SendReply(player, "Thanks for your donation. The color has come out beautifully, wear it with pride.");

            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellCastAnimation(this, spell, 30);
            }
        }
        #endregion setcolor


        #region seteffect
        public void SetEffect(GamePlayer player, int effect, int price)
        {
            if (player == null)
                return;

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(EFFECTNPC_ITEM_WEAK);
            player.TempProperties.removeProperty(EFFECTNPC_ITEM_WEAK);

            if (item == null)
                return;

            if (item.Object_Type < 1 || item.Object_Type > 26)
            {
                SendReply(player, "I cannot work on anything else than weapons.");
                return;
            }

            if (item == null || item.SlotPosition == (int)eInventorySlot.Ground
                || item.OwnerID == null || item.OwnerID != player.InternalID)
            {
                player.Out.SendMessage("Invalid item.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            int playerOrbs = player.Inventory.CountItemTemplate("token_many", eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
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
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate("token_many", price, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);


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
    }
}

