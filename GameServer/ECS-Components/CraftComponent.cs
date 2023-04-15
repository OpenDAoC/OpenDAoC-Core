using System.Collections.Generic;

namespace DOL.GS
{
    public class CraftComponent
    {
        public CraftAction CraftAction { get; set; }
        public bool CraftState { get; set; }
        public int EntityManagerId { get; private set; } = EntityManager.UNSET_ID;
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
                EntityManagerId = EntityManager.Remove(EntityManager.EntityType.CraftComponent, EntityManagerId);
        }

        public void StartCraft(Recipe recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if (CraftAction == null)
            {
                CraftAction = new CraftAction(_owner, craftingTime, recipe, skill);

                if (EntityManagerId == EntityManager.UNSET_ID)
                    EntityManagerId = EntityManager.Add(EntityManager.EntityType.CraftComponent, this);
            }
        }

        public void StopCraft()
        {
            CraftAction?.CleanupCraftAction();
        }
    }
}
