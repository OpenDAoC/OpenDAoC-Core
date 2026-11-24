using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class SiegeCrafting : AbstractProfession
    {
        public SiegeCrafting() : base()
        {
            Icon = 0x03;
            Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Siegecraft");
            eSkill = eCraftingSkill.SiegeCrafting;
        }

        protected override string Profession => "CraftersProfession.Siegecrafter";

        public override void GainCraftingSkillPoints(GamePlayer player, Recipe recipe)
        {
            if (Util.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
            {
                player.GainCraftingSkill(eCraftingSkill.SiegeCrafting, 1);
                player.Out.SendUpdateCraftingSkills();
            }
        }

        public override void BuildCraftedItem(GamePlayer player, Recipe recipe)
        {
            DbItemTemplate product = recipe.Product;
            GameSiegeWeapon siegeWeapon;

            switch ((eObjectType) product.Object_Type)
            {
                case eObjectType.SiegeBalista:
                    siegeWeapon = new GameSiegeBallista();
                    break;
                case eObjectType.SiegeCatapult:
                    siegeWeapon = new GameSiegeCatapult();
                    break;
                case eObjectType.SiegeCauldron:
                    siegeWeapon = new GameSiegeCauldron();
                    break;
                case eObjectType.SiegeRam:
                    siegeWeapon = new GameSiegeRam();
                    break;
                case eObjectType.SiegeTrebuchet:
                    siegeWeapon = new GameSiegeTrebuchet();
                    break;
                default:
                    base.BuildCraftedItem(player, recipe);
                    return;
            }

            // Actually stores the Id_nb of the siege weapon.
            siegeWeapon.ItemId = product.Id_nb;
            siegeWeapon.LoadFromDatabase(product);
            siegeWeapon.CurrentRegion = player.CurrentRegion;
            siegeWeapon.Heading = player.Heading;
            siegeWeapon.X = player.X;
            siegeWeapon.Y = player.Y;
            siegeWeapon.Z = player.Z;
            siegeWeapon.Realm = player.Realm;
            siegeWeapon.AddToWorld();
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "SiegeCrafting.BuildCraftedItem.Successfully", product.Name), eChatType.CT_Action, eChatLoc.CL_SystemWindow);
        }
    }
}
