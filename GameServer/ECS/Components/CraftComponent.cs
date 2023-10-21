using System.Collections.Generic;
using Core.GS.Crafting;

namespace Core.GS.ECS;

public class CraftComponent : IManagedEntity
{
    public GamePlayer Owner { get; private set; }
    public CraftAction CraftAction { get; set; }
    public bool CraftState { get; set; }
    public EntityManagerId EntityManagerId { get; set; } = new(EEntityType.CraftComponent, false);
    public List<RecipeMgr> Recipes { get; private set; } = new();
    private object _recipesLock = new();

    public CraftComponent(GamePlayer owner)
    {
        Owner = owner;
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
            EntityMgr.Remove(this);
    }

    public void StartCraft(RecipeMgr recipe, ACraftingSkill skill, int craftingTime)
    {
        if (CraftAction == null)
        {
            CraftAction = new CraftAction(Owner, craftingTime, recipe, skill);
            EntityMgr.Add(this);
        }
    }

    public void StopCraft()
    {
        CraftAction?.CleanupCraftAction();
    }
}