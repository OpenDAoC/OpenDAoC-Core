using System.Collections.Generic;

namespace DOL.GS
{
    public class CraftComponent : IManagedEntity
    {
        public CraftAction CraftAction { get; set; }
        public bool CraftState { get; set; }
        public EntityManagerId EntityManagerId { get; set; } = new();
        public bool AllowReuseByEntityManager => false;
        public List<Recipe> Recipes { get; private set; } = new();
        private GamePlayer _owner;
        private object _recipesLock = new();

        public CraftComponent(GamePlayer owner)
        {
            _owner = owner;
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

        public void Tick(long time)
        {
            CraftAction?.Tick(time);

            if (CraftAction == null)
                EntityManager.Remove(EntityManager.EntityType.CraftComponent, this);
        }

        public void StartCraft(Recipe recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if (CraftAction == null)
            {
                CraftAction = new CraftAction(_owner, craftingTime, recipe, skill);
                EntityManager.Add(EntityManager.EntityType.CraftComponent, this);
            }
        }

        public void StopCraft()
        {
            CraftAction?.CleanupCraftAction();
        }
    }
}
