﻿using System.Collections.Generic;

namespace DOL.GS
{
    public class CraftComponent : IManagedEntity
    {
        public CraftAction CraftAction { get; set; }
        public bool CraftState { get; set; }
        public EntityManagerId EntityManagerId { get; set; } = new();
        public List<RecipeMgr> Recipes { get; private set; } = new();
        private GamePlayer _owner;
        private object _recipesLock = new();

        public CraftComponent(GamePlayer owner)
        {
            _owner = owner;
        }

        public void AddRecipe(RecipeMgr recipe)
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

        public void RemoveRecipe(RecipeMgr recipe)
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
                EntityMgr.Remove(EntityMgr.EntityType.CraftComponent, this);
        }

        public void StartCraft(RecipeMgr recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if (CraftAction == null)
            {
                CraftAction = new CraftAction(_owner, craftingTime, recipe, skill);
                EntityMgr.Add(EntityMgr.EntityType.CraftComponent, this);
            }
        }

        public void StopCraft()
        {
            CraftAction?.CleanupCraftAction();
        }
    }
}