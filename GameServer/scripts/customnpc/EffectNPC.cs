using System.Collections;
using DOL.Database;
using DOL.Database.UniqueID;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    [NPCGuildScript("Effect Master")]
    public class EffectNPC : GameNPC
    {
        private string EFFECTNPC_ITEM_WEAK = "DOL.GS.Scripts.EffectNPC_Item_Manipulation";//used to store the item in the player
        private ushort spell = 7215;//The spell which is casted
        private ushort duration = 3000;//3s, the duration the spell is cast
        private Queue m_timer = new Queue();//Gametimer for casting some spell at the end of the process
        private Queue castplayer = new Queue();//Used to hold the player who the spell gets cast on
        public string TempProperty = "ItemEffect";
        public string DisplayedItem = "EffectDisplay";
        public string TempEffectId = "TempEffectID";
        public string TempColorId = "TempColorID";

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
                DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(TempProperty);
                DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);

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
                    DisplayReskinPreviewTo(player, (DbInventoryItem)displayItem.Clone());

                return true;
            }

            return false;
        }

        public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
        {
            if (source == null || item == null)
                return false;

            if (source is GamePlayer p)
            {
                SendReply(p, "What service do you want to use ?\n" +
                             "I can add an [effect] to it or change its color with a [dye].\n\n" +
                             "Alternatively, I can [remove all effects] or [remove dye] from your weapon. "
                );
                p.TempProperties.SetProperty(EFFECTNPC_ITEM_WEAK, item);

                SendReply(p, "When you are finished browsing, let me know and I will [confirm effect]."
                );
                var tmp = (DbInventoryItem) item.Clone();
                p.TempProperties.SetProperty(TempProperty, item);
                p.TempProperties.SetProperty(DisplayedItem, tmp);
            }

            return false;
        }

        public override bool WhisperReceive(GameLiving source, string str)
        {
            if (!base.WhisperReceive(source, str)) return false;

            if (!(source is GamePlayer)) return false;

            GamePlayer player = source as GamePlayer;
            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(EFFECTNPC_ITEM_WEAK);
            
            int cachedEffectID = player.TempProperties.GetProperty<int>(TempEffectId);
            int cachedColorID = player.TempProperties.GetProperty<int>(TempColorId);

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
                                "[gr sword - yellow flames]\n" +
                                "[gr sword - orange flames])\n" +
                                "[gr sword - fire with smoke])\n" +
                                "[gr sword - fire with sparks])\n" +
                                "[gr sword - yellow flames])\n" +
                                "[gr sword - orange flames])\n" +
                                "[gr sword - fire with smoke])\n" +
                                "[gr sword - fire with sparks])\n" +
                                "[gr - blue glow with sparkles])\n" +
                                "[gr - blue aura with cold vapor])\n" +
                                "[gr - icy blue glow])\n" +
                                "[gr - red aura])\n" +
                                "[gr - strong crimson glow])\n" +
                                "[gr - white core red glow])\n" +
                                "[gr - silvery/white glow])\n" +
                                "[gr - gold/yellow glow])\n" +
                                "[gr - hot green glow])\n");
                            break;

                        case (int)eObjectType.Blunt:
                        case (int)eObjectType.CrushingWeapon:
                        case (int)eObjectType.Hammer:
                            SendReply(player,
                                         "Choose a weapon effect: \n" +
                                         "[hammer - red aura])\n" +
                                         "[hammer - fiery glow])\n" +
                                         "[hammer - more intense fiery glow])\n" +
                                         "[hammer - flaming])\n" +
                                         "[hammer - torchlike flaming])\n" +
                                         "[hammer - silvery glow])\n" +
                                         "[hammer - purple glow])\n" +
                                         "[hammer - blue aura])\n" +
                                         "[hammer - blue glow])\n" +
                                         "[hammer - arcs from head to handle])\n" +
                                         "[crush - arcing halo])\n" +
                                         "[crush - center arcing])\n" +
                                         "[crush - smaller arcing halo])\n" +
                                         "[crush - hot orange core glow])\n" +
                                         "[crush - orange aura])\n" +
                                         "[crush - subtle aura with sparks])\n" +
                                         "[crush - yellow flame])\n" +
                                         "[crush - mana flame])\n" +
                                         "[crush - hot green glow])\n" +
                                         "[crush - hot red glow])\n" +
                                         "[crush - hot purple glow])\n" +
                                         "[crush - cold vapor])\n");
                            break;

                        case (int)eObjectType.SlashingWeapon:
                        case (int)eObjectType.Sword:
                        case (int)eObjectType.Blades:
                        case (int)eObjectType.Piercing:
                        case (int)eObjectType.ThrustWeapon:
                        case (int)eObjectType.LeftAxe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[longsword - propane-style flame])\n" +
                                        "[longsword - regular flame])\n" +
                                        "[longsword - orange flame])\n" +
                                        "[longsword - rising flame])\n" +
                                        "[longsword - flame with smoke])\n" +
                                        "[longsword - flame with sparks])\n" +
                                        "[longsword - hot glow])\n" +
                                        "[longsword - hot aura])\n" +
                                        "[longsword - blue aura])\n" +
                                        "[longsword - hot blue glow])\n" +
                                        "[longsword - hot gold glow])\n" +
                                        "[longsword - hot red glow])\n" +
                                        "[longsword - red aura])\n" +
                                        "[longsword - cold aura with sparkles])\n" +
                                        "[longsword - cold aura with vapor])\n" +
                                        "[longsword - hilt wavering blue beam])\n" +
                                        "[longsword - hilt wavering green beam])\n" +
                                        "[longsword - hilt wavering red beam])\n" +
                                        "[longsword - hilt red/blue beam])\n" +
                                        "[longsword - hilt purple beam])\n" +
                                        "[shortsword - propane flame])\n" +
                                        "[shortsword - orange flame with sparks])\n" +
                                        "[shortsword - blue aura with twinkles])\n" +
                                        "[shortsword - green cloud with bubbles])\n" +
                                        "[shortsword - red aura with blood bubbles])\n" +
                                        "[shortsword - evil green glow])\n" +
                                        "[shortsword - black glow])\n");
                            break;

                        case (int)eObjectType.Axe:
                            SendReply(player,
                                        "Choose a weapon effect: \n" +
                                        "[axe - basic flame])\n" +
                                        "[axe - orange flame])\n" +
                                        "[axe - slow orange flame with sparks])\n" +
                                        "[axe - fiery/trailing flame])\n" +
                                        "[axe - cold vapor])\n" +
                                        "[axe - blue aura with twinkles])\n" +
                                        "[axe - hot green glow])\n" +
                                        "[axe - hot blue glow])\n" +
                                        "[axe - hot cyan glow)\n" +
                                        "[axe - hot purple glow])\n" +
                                        "[axe - blue->purple->orange glow])\n");
                            break;
                        case (int) eObjectType.Shield:
                            SendReply(player,"[crush - arcing halo])\n" +
                                             "[crush - center arcing])\n" +
                                             "[crush - smaller arcing halo])\n" +
                                             "[crush - hot orange core glow])\n" +
                                             "[crush - orange aura])\n" +
                                             "[crush - subtle aura with sparks])\n" +
                                             "[crush - yellow flame])\n" +
                                             "[crush - mana flame])\n" +
                                             "[crush - hot green glow])\n" +
                                             "[crush - hot red glow])\n" +
                                             "[crush - hot purple glow])\n" +
                                             "[crush - cold vapor])\n");
                            break;
                        case (int)eObjectType.Spear:
                        case (int)eObjectType.CelticSpear:
                        case (int)eObjectType.PolearmWeapon:
                            SendReply(player,
                                "Choose a weapon effect: )\n" +
                                "[battlespear - cold with twinkles])\n" +
                                "[battlespear - evil green aura])\n" +
                                "[battlespear - evil red aura])\n" +
                                "[battlespear - flaming])\n" +
                                "[battlespear - hot gold glow])\n" +
                                "[battlespear - hot fire glow])\n" +
                                "[battlespear - red aura])\n" +
                                "[lugged spear - blue glow])\n" +
                                "[lugged spear - hot blue glow])\n" +
                                "[lugged spear - cold with twinkles])\n" +
                                "[lugged spear - flaming])\n" +
                                "[lugged spear - electric arcing])\n" +
                                "[lugged spear - hot yellow flame])\n" +
                                "[lugged spear - orange flame with sparks])\n" +
                                "[lugged spear - orange to purple flame])\n" +
                                "[lugged spear - hot purple flame])\n" +
                                "[lugged spear - silvery glow])\n");
                            break;

                        case (int)eObjectType.Staff:
                            SendReply(player,
                                "Choose a weapon effect: )\n" +
                                "[staff - blue glow])\n" +
                                "[staff - blue glow with twinkles])\n" +
                                "[staff - gold glow])\n" +
                                "[staff - gold glow with twinkles])\n" +
                                "[staff - faint red glow])\n" + 
                                "[crush - arcing halo])\n" +
                                "[crush - center arcing])\n" +
                                "[crush - smaller arcing halo])\n" +
                                "[crush - hot orange core glow])\n" +
                                "[crush - orange aura])\n" +
                                "[crush - subtle aura with sparks])\n" +
                                "[crush - yellow flame])\n" +
                                "[crush - mana flame])\n" +
                                "[crush - hot green glow])\n" +
                                "[crush - hot red glow])\n" +
                                "[crush - hot purple glow])\n" +
                                "[crush - cold vapor])\n");
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
                            "[Old Turquoise]\n" +
                            "[Leather Blue]\n" +
                            "[Blue-Green Cloth]\n" +
                            "[Turquoise Cloth]\n" +
                            "[Light Blue Cloth]\n" +
                            "[Blue Cloth]\n" +
                            "[Blue-Violet Cloth]\n" +
                            "[Blue Metal]\n" +
                            "[Blue 1]\n" +
                            "[Blue 2]\n" +
                            "[Blue 3]\n" +
                            "[Blue 4]\n" +
                            "[Turquoise 1]\n" +
                            "[Turquoise 2]\n" +
                            "[Turquoise 3]\n" +
                            "[Teal 1]\n" +
                            "[Teal 2]\n" +
                            "[Teal 3]\n");
                    break;
                case "Greens":
                    SendReply(player,
                        "[Old Green]\n" +
                        "[Leather Green]\n" +
                        "[Leather Forest Green]\n" +
                        "[Green Cloth]\n" +
                        "[Blue-Green Cloth]\n" +
                        "[Yellow-Green Cloth]\n" +
                        "[Green Metal]\n" +
                        "[Green 1]\n" +
                        "[Green 1]\n" +
                        "[Green 2]\n" +
                        "[Green 3]\n" +
                        "[Green 4]\n" +
                        "[Ship Lime Green]\n" +
                        "[Ship Green]\n" +
                        "[Ship Green 2]\n" +
                        "[Light Green - crafter only]\n" +
                        "[Olive Green - crafter only]\n" +
                        "[Sage Green - crafter only]\n" +
                        "[Lime Green - crafter only]\n" +
                        "[Forest Green - crafter only]\n");
                    break;
                case "Reds":
                    SendReply(player,
                        "[Old Red]\n" +
                        "[Leather Red]\n" +
                        "[Red Cloth]\n" +
                        "[Purple-Red Cloth]\n" +
                        "[Red Metal]\n" +
                        "[Red 1]\n" +
                        "[Red 2]\n" +
                        "[Red 3]\n" +
                        "[Red 4]\n" +
                        "[Ship Red]\n" +
                        "[Ship Red 2]\n" +
                        "[Red - crafter only]\n");
                    break;
                case "Yellows":
                    SendReply(player,
                        "[Old Yellow]\n" +
                        "[Leather Yellow]\n" +
                        "[Yellow-Orange Cloth]\n" +
                        "[Yellow Cloth]\n" +
                        "[Yellow- Cloth]\n" +
                        "[Yellow Metal]\n" +
                        "[Yellow 1]\n" +
                        "[Yellow 2]\n" +
                        "[Yellow 3]\n" +
                        "[Light Gold - crafter only]\n" +
                        "[Dark Gold - crafter only]\n" +
                        "[Gold Metal]\n" +
                        "[Ship Yellow]\n");
                    break;
                case "Purples":
                    SendReply(player,
                        "[Old Purple]\n" +
                        "[Leather Purple]\n" +
                        "[Purple Cloth]\n" +
                        "[Bright Purple Cloth]\n" +
                        "[Purple- Cloth]\n" +
                        "[Purple Metal]\n" +
                        "[Purple 1]\n" +
                        "[Purple 2]\n" +
                        "[Purple 3]\n" +
                        "[Purple 4]\n" +
                        "[Ship Purple]\n" +
                        "[Ship Purple 2]\n" +
                        "[Ship Purple 3]\n" +
                        "[Purple - crafter only]\n" +
                        "[Dark Purple - crafter only]\n" +
                        "[Dusky Purple - crafter only]\n");
                    break;
                case "Violets":
                    SendReply(player,
                        "[Leather Violet]\n" +
                        "[-Violet Cloth]\n" +
                        "[Violet Cloth]\n" +
                        "[Bright Violet Cloth]\n" +
                        "[Hot Pink - crafter only]\n" +
                        "[Dusky Rose - crafter only]\n" +
                        "[Ship Pink]\n" +
                        "[Violet]\n");
                    break;

                case "Oranges":
                    SendReply(player,
                        "[Leather Orange]\n" +
                        "[Orange Cloth]\n" +
                        "[-Orange Cloth]\n" +
                        "[Orange 1]\n" +
                        "[Orange 2]\n" +
                        "[Orange 3]\n" +
                        "[Ship Orange]\n" +
                        "[Ship Orange 2]\n" +
                        "[Orange 3]\n" +
                        "[Dirty Orange - crafter only]\n");
                    break;
                case "Blacks":
                    SendReply(player,
                        "[Black Cloth]\n" +
                        "[Brown Cloth]\n" +
                        "[Brown 1]\n" +
                        "[Brown 2]\n" +
                        "[Brown 3]\n" +
                        "[Brown - crafter only]\n" +
                        "[Gray]\n" +
                        "[Gray 2]\n" +
                        "[Gray 3]\n" +
                        "[Light Gray - crafter only]\n" +
                        "[Gray  - crafter only]\n" +
                        "[Olive Gray - crafter only]\n");
                    break;
                case "Other":
                    SendReply(player,
                        "[Bronze]\n" +
                        "[Iron]\n" +
                        "[Steel]\n" +
                        "[Alloy]\n" +
                        "[Fine Alloy]\n" +
                        "[Mithril]\n" +
                        "[Asterite]\n" +
                        "[Eog]\n" +
                        "[Xenium]\n" +
                        "[Vaanum]\n" +
                        "[Adamantium]\n" +
                        "[Mauve]\n" +
                        "[Ship Charcoal]\n" +
                        "[Ship Charcoal 2]\n" +
                        "[Plum - crafter only]\n" +
                        "[Dark Tan - crafter only]\n" +
                        "[White]\n");
                    break;

                case "remove dye":
                    SetColor(player,0);
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
                    SetEffect(player, cachedEffectID);
                    break;
                case "confirm color":
                    SetColor(player, cachedColorID);
                    break;
            }

            return true;
        }

        public void SendReply(GamePlayer player, string msg)
        {
            player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }
        #region setcolor
        public void SetColor(GamePlayer player, int color)
        {
            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(EFFECTNPC_ITEM_WEAK);

            player.TempProperties.RemoveProperty(EFFECTNPC_ITEM_WEAK);

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

            m_timer.Enqueue(new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Effect), duration));
            castplayer.Enqueue(player);

            player.Inventory.RemoveItem(item);
            DbItemUnique unique = new DbItemUnique(item.Template);
            unique.Color = color;
            unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            unique.Id_nb = "Unique" + System.Guid.NewGuid().ToString();
            if (GameServer.Database.ExecuteNonQuery("SELECT ItemUnique_ID FROM itemunique WHERE ItemUnique_ID = 'unique.ObjectId'"))
            {
                unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            }
            if (GameServer.Database.ExecuteNonQuery("SELECT Id_nb FROM itemunique WHERE Id_nb = 'unique.Id_nb'"))
            {
                unique.Id_nb = IdGenerator.GenerateID();
            }

            DbInventoryItem newInventoryItem = GameInventoryItem.Create<DbItemUnique>(unique);
            if(item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if(item.Creator != string.Empty)
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);

            player.TempProperties.RemoveProperty(TempProperty);
            player.TempProperties.RemoveProperty(DisplayedItem);
            player.TempProperties.RemoveProperty(TempEffectId);
            player.TempProperties.RemoveProperty(TempColorId);
            
            SendReply(player, "Thanks for your donation. The color has come out beautifully, wear it with pride.");

            foreach (GamePlayer visplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                visplayer.Out.SendSpellCastAnimation(this, spell, 30);
            }
        }
        #endregion setcolor


        private void PreviewEffect(GamePlayer player, int effect)
        {
            DbInventoryItem item = (DbInventoryItem)player.TempProperties.GetProperty<DbInventoryItem>(TempProperty).Clone();
            DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);
            item.Effect = effect;
            player.TempProperties.SetProperty(TempEffectId, effect);
            DisplayReskinPreviewTo(player, item);
            SendReply(player, "When you are finished browsing, let me know and I will [confirm effect]."
            );
        }
        
        private void PreviewColor(GamePlayer player, int color)
        {
            DbInventoryItem item = (DbInventoryItem)player.TempProperties.GetProperty<DbInventoryItem>(TempProperty).Clone();
            DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);
            item.Color = color;
            player.TempProperties.SetProperty(TempColorId, color);
            DisplayReskinPreviewTo(player, item);
            SendReply(player, "When you are finished browsing, let me know and I will [confirm color]."
            );
        }

        #region seteffect
        public void SetEffect(GamePlayer player, int effect)
        {
            if (player == null)
                return;

            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(EFFECTNPC_ITEM_WEAK);
            player.TempProperties.RemoveProperty(EFFECTNPC_ITEM_WEAK);

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

            m_timer.Enqueue(new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Effect), duration));
            castplayer.Enqueue(player);


            player.Inventory.RemoveItem(item);
            DbItemUnique unique = new DbItemUnique(item.Template);
            unique.Effect = effect;
            unique.Id_nb = IdGenerator.GenerateID();
            unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            if (GameServer.Database.ExecuteNonQuery("SELECT ItemUnique_ID FROM itemunique WHERE ItemUnique_ID = 'unique.ObjectId'"))
            {
                unique.ObjectId = "Unique" + System.Guid.NewGuid().ToString();
            }
            if (GameServer.Database.ExecuteNonQuery("SELECT Id_nb FROM itemunique WHERE Id_nb = 'unique.Id_nb'"))
            {
                unique.Id_nb = IdGenerator.GenerateID();
            }

            DbInventoryItem newInventoryItem = GameInventoryItem.Create<DbItemUnique>(unique);
            if(item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if(item.Creator != string.Empty)
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { newInventoryItem });
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);

            player.TempProperties.RemoveProperty(TempProperty);
            player.TempProperties.RemoveProperty(DisplayedItem);
            player.TempProperties.RemoveProperty(TempEffectId);
            player.TempProperties.RemoveProperty(TempColorId);
            
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
        
        private void DisplayReskinPreviewTo(GamePlayer player, DbInventoryItem item)
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
            display.TargetObject = display;
            display.ObjectState = eObjectState.Active;
            display.attackComponent.AttackState = true;
            display.BroadcastLivingEquipmentUpdate();
            ClientService.UpdateNpcForPlayer(player, display);

            //Uncomment this if you want animations
            // var animationThread = new Thread(() => LoopAnimation(player,item, display,tempAd));
            // animationThread.IsBackground = true;
            // animationThread.Start();
        }
        
        private GameNPC CreateDisplayNPC(GamePlayer player, DbInventoryItem item)
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
            //mob.Inventory.AddItem((eInventorySlot) item.Item_Type, item);
            player.Out.SendNPCCreate(mob);
            //mob.AddToWorld();*/
        }
    }
}
