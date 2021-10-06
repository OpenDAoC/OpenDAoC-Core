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

namespace DOL.GS
{
    [NPCGuildScript("Effect Master")]
    public class EffectNPC : GameNPC
    {
        private string EFFECTNPC_ITEM_WEAK = "DOL.GS.Scripts.EffectNPC_Item_Manipulation";//used to store the item in the player
        private ushort spell = 7215;//The spell which is casted
        private ushort duration = 3000;//3s, the duration the spell is cast
        private eEmote Emotes = eEmote.Raise;//The Emote the NPC does when Interacted
        private Queue m_timer = new Queue();//Gametimer for casting some spell at the end of the process
        private Queue castplayer = new Queue();//Used to hold the player who the spell gets cast on
        private int effectPrice = 2000; //effects price in RPs
        private int dyePrice = 500; //effects price in RPs


        public override bool AddToWorld()
        {
            GuildName = "NEW Effect Master";
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
                SendReply(player, "Greetings Traveller!\n\n" +
                                    "I can either change the effect, the speed or the color of your weapons, armors...\n" +
                                    "Simply give me the item and i will start my work.");
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
            if (t == null || item == null) return false;
            if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
            {
                t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }
            
            SendReply(t, "What service do you want to use ?\n" +
                         "I can add an [effect] to it or change its color with a [dye].\n\n" +
                         "Alternatively, I can [remove all effects]. "
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
            
            
            switch (str)
            {
                #region effects
                
                case "effect":
                    switch (item.Object_Type)
                    {
                        case (int)eObjectType.TwoHandedWeapon: case (int)eObjectType.LargeWeapons:
                            SendReply(player,
                                "Choose a weapon effect: \n" +
                                "[gr sword - yellow flames] (" + effectPrice + " RPs)\n" +
                                "[gr sword - orange flames] (" + effectPrice + " RPs)\n" +
                                "[gr sword - fire with smoke] (" + effectPrice + " RPs)\n" +
                                "[gr sword - fire with sparks] (" + effectPrice + " RPs)\n" +
                                "[gr sword - yellow flames] (" + effectPrice + " RPs)\n" +
                                "[gr sword - orange flames] (" + effectPrice + " RPs)\n" +
                                "[gr sword - fire with smoke] (" + effectPrice + " RPs)\n" +
                                "[gr sword - fire with sparks] (" + effectPrice + " RPs)\n" +
                                "[gr - blue glow with sparkles] (" + effectPrice + " RPs)\n" +
                                "[gr - blue aura with cold vapor] (" + effectPrice + " RPs)\n" +
                                "[gr - icy blue glow] (" + effectPrice + " RPs)\n" +
                                "[gr - red aura] (" + effectPrice + " RPs)\n" +
                                "[gr - strong crimson glow] (" + effectPrice + " RPs)\n" +
                                "[gr - white core red glow] (" + effectPrice + " RPs)\n" +
                                "[gr - silvery/white glow] (" + effectPrice + " RPs)\n" +
                                "[gr - gold/yellow glow] (" + effectPrice + " RPs)\n" +
                                "[gr - hot green glow] (" + effectPrice + " RPs)\n");
                            break;
                        
                        case (int)eObjectType.Blunt: case (int)eObjectType.CrushingWeapon: case (int)eObjectType.Hammer:
                            SendReply(player,
                                         "Choose a weapon effect: \n" +
                                         "[hammer - red aura] (" + effectPrice + " RPs)\n" +
                                         "[hammer - fiery glow] (" + effectPrice + " RPs)\n" +
                                         "[hammer - more intense fiery glow] (" + effectPrice + " RPs)\n" +
                                         "[hammer - flaming] (" + effectPrice + " RPs)\n" +
                                         "[hammer - torchlike flaming] (" + effectPrice + " RPs)\n" +
                                         "[hammer - silvery glow] (" + effectPrice + " RPs)\n" +
                                         "[hammer - purple glow] (" + effectPrice + " RPs)\n" +
                                         "[hammer - blue aura] (" + effectPrice + " RPs)\n" +
                                         "[hammer - blue glow] (" + effectPrice + " RPs)\n" +
                                         "[hammer - arcs from head to handle] (" + effectPrice + " RPs)\n" +
                                         "[crush - arcing halo] (" + effectPrice + " RPs)\n" +
                                         "[crush - center arcing] (" + effectPrice + " RPs)\n" +
                                         "[crush - smaller arcing halo] (" + effectPrice + " RPs)\n" +
                                         "[crush - hot orange core glow] (" + effectPrice + " RPs)\n" +
                                         "[crush - orange aura] (" + effectPrice + " RPs)\n" +
                                         "[crush - subtle aura with sparks] (" + effectPrice + " RPs)\n" +
                                         "[crush - yellow flame] (" + effectPrice + " RPs)\n" +
                                         "[crush - mana flame] (" + effectPrice + " RPs)\n" +
                                         "[crush - hot green glow] (" + effectPrice + " RPs)\n" +
                                         "[crush - hot red glow] (" + effectPrice + " RPs)\n" +
                                         "[crush - hot purple glow] (" + effectPrice + " RPs)\n" +
                                         "[crush - cold vapor] (" + effectPrice + " RPs)\n");
                            break;
                        
                        case (int)eObjectType.SlashingWeapon: case (int)eObjectType.Sword: case (int)eObjectType.Blades: case (int)eObjectType.Piercing: case (int)eObjectType.ThrustWeapon: case (int)eObjectType.LeftAxe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[longsword - propane-style flame] (" + effectPrice + " RPs)\n" +
                                        "[longsword - regular flame] (" + effectPrice + " RPs)\n" +
                                        "[longsword - orange flame] (" + effectPrice + " RPs)\n" +
                                        "[longsword - rising flame] (" + effectPrice + " RPs)\n" +
                                        "[longsword - flame with smoke] (" + effectPrice + " RPs)\n" +
                                        "[longsword - flame with sparks] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hot glow] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hot aura] (" + effectPrice + " RPs)\n" +
                                        "[longsword - blue aura] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hot blue glow] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hot gold glow] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hot red glow] (" + effectPrice + " RPs)\n" +
                                        "[longsword - red aura] (" + effectPrice + " RPs)\n" +
                                        "[longsword - cold aura with sparkles] (" + effectPrice + " RPs)\n" +
                                        "[longsword - cold aura with vapor] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hilt wavering blue beam] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hilt wavering green beam] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hilt wavering red beam] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hilt red/blue beam] (" + effectPrice + " RPs)\n" +
                                        "[longsword - hilt purple beam] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - propane flame] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - orange flame with sparks] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - blue aura with twinkles] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - green cloud with bubbles] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - red aura with blood bubbles] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - evil green glow] (" + effectPrice + " RPs)\n" +
                                        "[shortsword - black glow] (" + effectPrice + " RPs)\n");
                            break;
                            
                        case (int)eObjectType.Axe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[axe - basic flame] (" + effectPrice + " RPs)\n" +
                                        "[axe - orange flame] (" + effectPrice + " RPs)\n" +
                                        "[axe - slow orange flame with sparks] (" + effectPrice + " RPs)\n" +
                                        "[axe - fiery/trailing flame] (" + effectPrice + " RPs)\n" +
                                        "[axe - cold vapor] (" + effectPrice + " RPs)\n" +
                                        "[axe - blue aura with twinkles] (" + effectPrice + " RPs)\n" +
                                        "[axe - hot green glow] (" + effectPrice + " RPs)\n" +
                                        "[axe - hot blue glow] (" + effectPrice + " RPs)\n" +
                                        "[axe - hot cyan glow (" + effectPrice + " RPs)\n" +
                                        "[axe - hot purple glow] (" + effectPrice + " RPs)\n" +
                                        "[axe - blue->purple->orange glow] (" + effectPrice + " RPs)\n");
                            break;
                        case (int)eObjectType.Spear: case (int)eObjectType.CelticSpear: case (int)eObjectType.PolearmWeapon:
                            SendReply(player,
                                "Choose a weapon effect:  (" + effectPrice + " RPs)\n" +
                                "[battlespear - cold with twinkles] (" + effectPrice + " RPs)\n" +
                                "[battlespear - evil green aura] (" + effectPrice + " RPs)\n" +
                                "[battlespear - evil red aura] (" + effectPrice + " RPs)\n" +
                                "[battlespear - flaming] (" + effectPrice + " RPs)\n" +
                                "[battlespear - hot gold glow] (" + effectPrice + " RPs)\n" +
                                "[battlespear - hot fire glow] (" + effectPrice + " RPs)\n" +
                                "[battlespear - red aura] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - blue glow] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - hot blue glow] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - cold with twinkles] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - flaming] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - electric arcing] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - hot yellow flame] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - orange flame with sparks] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - orange to purple flame] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - hot purple flame] (" + effectPrice + " RPs)\n" +
                                "[lugged spear - silvery glow] (" + effectPrice + " RPs)\n");
                            break;
                        
                        case (int)eObjectType.Staff:
                            SendReply(player,
                                "Choose a weapon effect:  (" + effectPrice + " RPs)\n" +
                                "[staff - blue glow] (" + effectPrice + " RPs)\n" +
                                "[staff - blue glow with twinkles] (" + effectPrice + " RPs)\n" +
                                "[staff - gold glow] (" + effectPrice + " RPs)\n" +
                                "[staff - gold glow with twinkles] (" + effectPrice + " RPs)\n" +
                                "[staff - faint red glow] (" + effectPrice + " RPs)\n");
                            break;
                        
                        default:
                            SendReply(player,
                                "Unfortunately I cannot work with this item.");
                            break;
                    }

                    break;
                
                //remove all effect
                case "remove all effects": SetEffect(player, 0, effectPrice); break;
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
                    SendReply(player,"Please Choose Your Type Of Color" + 
                        "[Blues], [Greens], [Reds], [Yellows], [Purples], [Violets], [Oranges], [Blacks], or [Other]");
                    break;
                
                case "Blues":
                    SendReply(player,
                            "[Old Turquoise] (" + dyePrice + " RPs)\n" +
                            "[Leather Blue] (" + dyePrice + " RPs)\n" +
                            "[Blue-Green Cloth] (" + dyePrice + " RPs)\n" +
                            "[Turquoise Cloth] (" + dyePrice + " RPs)\n" +
                            "[Light Blue Cloth] (" + dyePrice + " RPs)\n" +
                            "[Blue Cloth] (" + dyePrice + " RPs)\n" +
                            "[Blue-Violet Cloth] (" + dyePrice + " RPs)\n" +
                            "[Blue Metal] (" + dyePrice + " RPs)\n" +
                            "[Blue 1] (" + dyePrice + " RPs)\n" +
                            "[Blue 2] (" + dyePrice + " RPs)\n" +
                            "[Blue 3] (" + dyePrice + " RPs)\n" +
                            "[Blue 4] (" + dyePrice + " RPs)\n" +
                            "[Turquoise 1] (" + dyePrice + " RPs)\n" +
                            "[Turquoise 2] (" + dyePrice + " RPs)\n" +
                            "[Turquoise 3] (" + dyePrice + " RPs)\n" +
                            "[Teal 1] (" + dyePrice + " RPs)\n" +
                            "[Teal 2] (" + dyePrice + " RPs)\n" +
                            "[Teal 3] (" + dyePrice + " RPs)\n");
                    break;
                case "Greens":
                  SendReply(player,
                      "[Old Green] (" + dyePrice + " RPs)\n" +
                      "[Leather Green] (" + dyePrice + " RPs)\n" +
                      "[Leather Forest Green] (" + dyePrice + " RPs)\n" +
                      "[Green Cloth] (" + dyePrice + " RPs)\n" +
                      "[Blue-Green Cloth] (" + dyePrice + " RPs)\n" +
                      "[Yellow-Green Cloth] (" + dyePrice + " RPs)\n" +
                      "[Green Metal] (" + dyePrice + " RPs)\n" +
                      "[Green 1] (" + dyePrice + " RPs)\n" +
                      "[Green 1] (" + dyePrice + " RPs)\n" +
                      "[Green 2] (" + dyePrice + " RPs)\n" +
                      "[Green 3] (" + dyePrice + " RPs)\n" +
                      "[Green 4] (" + dyePrice + " RPs)\n" +
                      "[Ship Lime Green] (" + dyePrice + " RPs)\n" +
                      "[Ship Green] (" + dyePrice + " RPs)\n" +
                      "[Ship Green 2] (" + dyePrice + " RPs)\n" +
                      "[Light green - crafter only] (" + dyePrice + " RPs)\n" +
                      "[Olive green - crafter only] (" + dyePrice + " RPs)\n" +
                      "[Sage green - crafter only] (" + dyePrice + " RPs)\n" +
                      "[Lime green - crafter only] (" + dyePrice + " RPs)\n" +
                      "[Forest Green - crafter only] (" + dyePrice + " RPs)\n");
                    break;
                case "Reds":
                    SendReply(player,
                        "[Old Red] (" + dyePrice + " RPs)\n" +
                        "[Leather Red] (" + dyePrice + " RPs)\n" +
                        "[Red Cloth] (" + dyePrice + " RPs)\n" +
                        "[Purple-Red Cloth] (" + dyePrice + " RPs)\n" +
                        "[Red Metal] (" + dyePrice + " RPs)\n" +
                        "[Red 1] (" + dyePrice + " RPs)\n" +
                        "[Red 2] (" + dyePrice + " RPs)\n" +
                        "[Red 3] (" + dyePrice + " RPs)\n" +
                        "[Red 4] (" + dyePrice + " RPs)\n" +
                        "[Ship Red] (" + dyePrice + " RPs)\n" +
                        "[Ship Red 2] (" + dyePrice + " RPs)\n" +
                        "[Red - crafter only] (" + dyePrice + " RPs)\n");
                        break;
                case "Yellows":
                    SendReply(player,
                        "[Old Yellow] (" + dyePrice + " RPs)\n" +
                        "[Leather Yellow] (" + dyePrice + " RPs)\n" +
                        "[Yellow-Orange Cloth] (" + dyePrice + " RPs)\n" +
                        "[Yellow Cloth] (" + dyePrice + " RPs)\n" +
                        "[Yellow- Cloth] (" + dyePrice + " RPs)\n" +
                        "[Yellow Metal] (" + dyePrice + " RPs)\n" +
                        "[Yellow 1] (" + dyePrice + " RPs)\n" +
                        "[Yellow 2] (" + dyePrice + " RPs)\n" +
                        "[Yellow 3] (" + dyePrice + " RPs)\n" +
                        "[Light gold - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Dark gold - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Gold Metal] (" + dyePrice + " RPs)\n" +
                        "[Ship Yellow] (" + dyePrice + " RPs)\n");
                        break;
                case "Purples":
                    SendReply(player,
                        "[Old Purple] (" + dyePrice + " RPs)\n" +
                        "[Leather Purple] (" + dyePrice + " RPs)\n" +
                        "[Purple Cloth] (" + dyePrice + " RPs)\n" +
                        "[Bright Purple Cloth] (" + dyePrice + " RPs)\n" +
                        "[Purple- Cloth] (" + dyePrice + " RPs)\n" +
                        "[Purple Metal] (" + dyePrice + " RPs)\n" +
                        "[Purple 1] (" + dyePrice + " RPs)\n" +
                        "[Purple 2] (" + dyePrice + " RPs)\n" +
                        "[Purple 3] (" + dyePrice + " RPs)\n" +
                        "[Purple 4] (" + dyePrice + " RPs)\n" +
                        "[Ship Purple] (" + dyePrice + " RPs)\n" +
                        "[Ship Purple 2] (" + dyePrice + " RPs)\n" +
                        "[Ship Purple 3] (" + dyePrice + " RPs)\n" +
                        "[Purple - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Dark purple - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Dusky purple - crafter only] (" + dyePrice + " RPs)\n");
                    break;
                case "Violets":
                    SendReply(player,
                        "[Leather Violet] (" + dyePrice + " RPs)\n" +
                        "[-Violet Cloth] (" + dyePrice + " RPs)\n" +
                        "[Violet Cloth] (" + dyePrice + " RPs)\n" +
                        "[Bright Violet Cloth] (" + dyePrice + " RPs)\n" +
                        "[Hot pink - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Dusky rose - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Ship Pink] (" + dyePrice + " RPs)\n" +
                        "[Violet] (" + dyePrice + " RPs)\n");
                        break;
                
                    case "Oranges":
                    SendReply(player,
                        "[Leather Orange] (" + dyePrice + " RPs)\n" +
                        "[Orange Cloth] (" + dyePrice + " RPs)\n" +
                        "[-Orange Cloth] (" + dyePrice + " RPs)\n" +
                        "[Orange 1] (" + dyePrice + " RPs)\n" +
                        "[Orange 2] (" + dyePrice + " RPs)\n" +
                        "[Orange 3] (" + dyePrice + " RPs)\n" +
                        "[Ship Orange] (" + dyePrice + " RPs)\n" +
                        "[Ship Orange 2] (" + dyePrice + " RPs)\n" +
                        "[Orange 3] (" + dyePrice + " RPs)\n" +
                        "[Dirty orange - crafter only] (" + dyePrice + " RPs)\n");
                    break;
                case "Blacks":
                    SendReply(player,
                        "[Black Cloth] (" + dyePrice + " RPs)\n" +
                        "[Brown Cloth] (" + dyePrice + " RPs)\n" +
                        "[Brown 1] (" + dyePrice + " RPs)\n" +
                        "[Brown 2] (" + dyePrice + " RPs)\n" +
                        "[Brown 3] (" + dyePrice + " RPs)\n" +
                        "[Brown - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Gray] (" + dyePrice + " RPs)\n" +
                        "[Gray 2] (" + dyePrice + " RPs)\n" +
                        "[Gray 3] (" + dyePrice + " RPs)\n" +
                        "[Light gray - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Gray  - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Olive gray - crafter only] (" + dyePrice + " RPs)\n");
                    break;
                case "Other":
                    SendReply(player,
                        "[Bronze] (" + dyePrice + " RPs)\n" +
                        "[Iron] (" + dyePrice + " RPs)\n" +
                        "[Steel] (" + dyePrice + " RPs)\n" +
                        "[Alloy] (" + dyePrice + " RPs)\n" +
                        "[Fine Alloy] (" + dyePrice + " RPs)\n" +
                        "[Mithril] (" + dyePrice + " RPs)\n" +
                        "[Asterite] (" + dyePrice + " RPs)\n" +
                        "[Eog] (" + dyePrice + " RPs)\n" +
                        "[Xenium] (" + dyePrice + " RPs)\n" +
                        "[Vaanum] (" + dyePrice + " RPs)\n" +
                        "[Adamantium] (" + dyePrice + " RPs)\n" +
                        "[Mauve] (" + dyePrice + " RPs)\n" +
                        "[Ship Charcoal] (" + dyePrice + " RPs)\n" +
                        "[Ship Charcoal 2] (" + dyePrice + " RPs)\n" +
                        "[Plum - crafter only] (" + dyePrice + " RPs)\n" +
                        "[Dark tan - crafter only] (" + dyePrice + " RPs)\n" +
                        "[White] (" + dyePrice + " RPs)\n");
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
                case "Yelow 3":
                    SetColor(player, 83, dyePrice);
                    break;
                case "violet":
                    SetColor(player, 84, dyePrice);
                    break;
                case "mauve":
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
                case "Ship Turquiose":
                    SetColor(player, 109, dyePrice);
                    break;
                case "Ship Turqiose 2":
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
                case "red - crafter only":
                    SetColor(player, 120, dyePrice);
                    break;
                case "plum - crafter only":
                    SetColor(player, 121, dyePrice);
                    break;
                case "purple - crafter only":
                    SetColor(player, 122, dyePrice);
                    break;
                case "dark purple - crafter only":
                    SetColor(player, 123, dyePrice);
                    break;
                case "dusky purple - crafter only":
                    SetColor(player, 124, dyePrice);
                    break;
                case "light gold - crafter only":
                    SetColor(player, 125, dyePrice);
                    break;
                case "dark gold - crafter only":
                    SetColor(player, 126, dyePrice);
                    break;
                case "dirty orange - crafter only":
                    SetColor(player, 127, dyePrice);
                    break;
                case "dark tan - crafter only":
                    SetColor(player, 128, dyePrice);
                    break;
                case "brown - crafter only":
                    SetColor(player, 129, dyePrice);
                    break;
                case "light green - crafter only":
                    SetColor(player, 130, dyePrice);
                    break;
                case "olive green - crafter only":
                    SetColor(player, 131, dyePrice);
                    break;
                case "cornflower blue - crafter only":
                    SetColor(player, 132, dyePrice);
                    break;
                case "light gray - crafter only":
                    SetColor(player, 133, dyePrice);
                    break;
                case "hot pink - crafter only":
                    SetColor(player, 134, dyePrice);
                    break;
                case "dusky rose - crafter only":
                    SetColor(player, 135, dyePrice);
                    break;
                case "sage green - crafter only":
                    SetColor(player, 136, dyePrice);
                    break;
                case "lime green - crafter only":
                    SetColor(player, 137, dyePrice);
                    break;
                case "gray teal - crafter only":
                    SetColor(player, 138, dyePrice);
                    break;
                case "gray blue - crafter only":
                    SetColor(player, 139, dyePrice);
                    break;
                case "olive gray - crafter only":
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

        #region setrealmlevel
        public void SetRealmLevel(GamePlayer player, int rps)
        {
            if (player == null)
                return;

            if (rps == 0) { player.RealmLevel = 1; } else
            if (rps is >= 25 and < 125) { player.RealmLevel = 2;} else
            if (rps is >= 125 and < 350) { player.RealmLevel = 3;} else 
            if (rps is >= 350 and < 750) { player.RealmLevel = 4;} else
            if (rps is >= 750 and < 1375) { player.RealmLevel = 5;} else
            if (rps is >= 2275 and < 3500) { player.RealmLevel = 6;} else
            if (rps is >= 3500 and < 5100) { player.RealmLevel = 7;} else
            if (rps is >= 5100 and < 7125) { player.RealmLevel = 8;} else 
            if (rps is >= 7125 and < 9625) { player.RealmLevel = 9;} else
            if (rps is >= 9625 and < 12650) { player.RealmLevel = 10;} else
            if (rps is >= 16250 and < 20475) { player.RealmLevel = 11;} else
            if (rps is >= 20475 and < 25375) { player.RealmLevel = 12;} else
            if (rps is >= 25375 and < 31000) { player.RealmLevel = 13;} else 
            if (rps is >= 31000 and < 37400) { player.RealmLevel = 14;} else
            if (rps is >= 37400 and < 44625) { player.RealmLevel = 15;} else
            if (rps is >= 44625 and < 52725) { player.RealmLevel = 16;} else
            if (rps is >= 52725 and < 61750) { player.RealmLevel = 17;} else
            if (rps is >= 61750 and < 71750) { player.RealmLevel = 18;}
            
            player.Out.SendUpdatePlayer();
            player.Out.SendCharStatsUpdate();
            player.Out.SendUpdatePoints();
            player.UpdatePlayerStatus();
        }
        #endregion
        
        
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
            
            if (player.RealmPoints < price)
            {
                SayTo(player, eChatLoc.CL_PopupWindow, "I need " + price + " RPs to dye that.");
                return;
            }
            
            m_timer.Enqueue(new RegionTimer(this, new RegionTimerCallback(Effect), duration));
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
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            player.RealmPoints -= price;
            player.RespecRealm();
            SetRealmLevel(player,(int)player.RealmPoints);
            
            SendReply(player, "Thanks for the donation. The color has come out beautifully, wear it with pride.");

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
            
            if (player.RealmPoints < price)
            {
               SayTo(player, eChatLoc.CL_PopupWindow, "I need " + price + " RPs to enchant that.");
               return;
            }
            
            m_timer.Enqueue(new RegionTimer(this, new RegionTimerCallback(Effect), duration));
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
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            player.RealmPoints -= price;
            player.RespecRealm();
            SetRealmLevel(player,(int)player.RealmPoints);

            SendReply(player, "Thanks for your donation. May the " + item.Name + " lead you to a bright future.");
            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellCastAnimation(this, spell, 30);
            }
        }
        #endregion seteffect

        public int Effect(RegionTimer timer)
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

