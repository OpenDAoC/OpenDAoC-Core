using System.Collections.Generic;

namespace DOL.GS
{
    public class CraftComponent : IManagedEntity
    {
        public GamePlayer Owner { get; private set; }
        public CraftAction CraftAction { get; set; }
        public bool CraftState { get; set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.CraftComponent, false);
        public List<Recipe> Recipes { get; private set; } = new();
        private object _recipesLock = new();

        public CraftComponent(GamePlayer owner)
        {
            Owner = owner;
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
                EntityManager.Remove(this);
        }

        public void StartCraft(Recipe recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if (CraftAction == null)
            {
                CraftAction = new CraftAction(Owner, craftingTime, recipe, skill);
                EntityManager.Add(this);
            }
        }

        public void StopCraft()
        {
            CraftAction?.CleanupCraftAction();
        }
    }
}
