using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class CraftComponent : IServiceObject<CraftComponent>
    {
        public GamePlayer Owner { get; }
        public CraftAction CraftAction { get; set; }
        public bool CraftState { get; set; }
        public ServiceObjectId ServiceObjectId { get; }
        public List<Recipe> Recipes { get; } = new();
        private readonly Lock _recipesLock = new();

        public CraftComponent(GamePlayer owner)
        {
            Owner = owner;
            ServiceObjectId = ServiceObjectFactory.CreateId(this);
        }

        public void AddRecipe(Recipe recipe)
        {
            lock (_recipesLock)
            {
                if (recipe == null)
                    return;

                if (Recipes.Contains(recipe))
                    return;

                Recipes.Add(recipe);
            }
        }

        public void RemoveRecipe(Recipe recipe)
        {
            lock (_recipesLock)
            {
                if (Recipes.Contains(recipe))
                    Recipes.Remove(recipe);
            }
        }

        public void Tick()
        {
            CraftAction?.Tick();

            if (CraftAction == null)
                ServiceObjectId.Unregister();
        }

        public void StartCraft(Recipe recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if (CraftAction == null)
            {
                CraftAction = new CraftAction(Owner, craftingTime, recipe, skill);
                ServiceObjectId.Register();
            }
        }

        public void StopCraft()
        {
            CraftAction?.CleanupCraftAction();
        }
    }
}
