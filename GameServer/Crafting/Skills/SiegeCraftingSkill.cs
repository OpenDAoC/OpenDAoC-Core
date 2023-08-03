using System;
using DOL.Language;

namespace DOL.GS
{
	public class SiegeCraftingSkill : AbstractProfession
	{
		public SiegeCraftingSkill()
			: base()
		{
			Icon = 0x03;
			Name = LanguageMgr.GetTranslation(ServerProperties.ServerProperties.SERV_LANGUAGE, "Crafting.Name.Siegecraft");
			eSkill = eCraftingSkill.SiegeCrafting;
		}
		public override string CRAFTER_TITLE_PREFIX
		{
			get
			{
				return "Siegecrafter";
			}
		}

        protected override String Profession
        {
            get
            {
                return "CraftersProfession.Siegecrafter";
            }
        }

		public override void GainCraftingSkillPoints(GamePlayer player, RecipeMgr recipe)
		{
			if (UtilCollection.Chance(CalculateChanceToGainPoint(player, recipe.Level)))
			{
				player.GainCraftingSkill(eCraftingSkill.SiegeCrafting, 1);
				player.Out.SendUpdateCraftingSkills();
			}
		}

		public override void BuildCraftedItem(GamePlayer player, RecipeMgr recipe)
		{
			var product = recipe.Product;
			GameSiegeWeapon siegeweapon;
			switch ((EObjectType)product.Object_Type)
			{
				case EObjectType.SiegeBalista:
					{
						siegeweapon = new GameSiegeBallista();
					}
					break;
				case EObjectType.SiegeCatapult:
					{
						siegeweapon = new GameSiegeCatapult();
					}
					break;
				case EObjectType.SiegeCauldron:
					{
						siegeweapon = new GameSiegeCauldron();
					}
					break;
				case EObjectType.SiegeRam:
					{
						siegeweapon = new GameSiegeRam();
					}
					break;
				case EObjectType.SiegeTrebuchet:
					{
						siegeweapon = new GameSiegeTrebuchet();
					}
					break;
				default:
					{
						base.BuildCraftedItem(player, recipe);
						return;
					}
			}

			//actually stores the Id_nb of the siegeweapon
			siegeweapon.ItemId = product.Id_nb;

			siegeweapon.LoadFromDatabase(product);
			siegeweapon.CurrentRegion = player.CurrentRegion;
			siegeweapon.Heading = player.Heading;
			siegeweapon.X = player.X;
			siegeweapon.Y = player.Y;
			siegeweapon.Z = player.Z;
			siegeweapon.Realm = player.Realm;
			siegeweapon.AddToWorld();
		}
	}
}