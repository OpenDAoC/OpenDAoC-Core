using System.Collections.Generic;

namespace DOL.GS
{
    public class CraftComponent
    {
        public GameLiving owner;
        public GamePlayer ownerPlayer;
        public CraftAction craftAction;
        public int EntityManagerId { get; set; } = EntityManager.UNSET_ID;

        /// <summary>
        /// The objects currently being crafted by this living
        /// </summary>
        protected List<Recipe> m_recipes;

        /// <summary>
        /// Returns the list of recipes
        /// </summary>
        public List<Recipe> Recipes
        {
            get { return m_recipes; }
        }

        public void AddRecipe(Recipe recipe)
        {
            lock (Recipes)
            {
                if (recipe == null) return;
                if (m_recipes.Contains(recipe)) return;
                m_recipes.Add(recipe);

                if (m_recipes.Count > 0 && EntityManagerId == -1)
                    EntityManagerId = EntityManager.Add(EntityManager.EntityType.CraftComponent, this);
            }
        }

        public void RemoveRecipe(Recipe recipe)
        {
            //			log.Warn(Name + ": RemoveAttacker "+attacker.Name);
            //			log.Error(Environment.StackTrace);
            lock (Recipes)
            {
                if (m_recipes.Contains(recipe)) m_recipes.Remove(recipe);
            }
        }

        public CraftComponent(GameLiving owner)
        {
            this.owner = owner;
            ownerPlayer = owner as GamePlayer;
            m_recipes = new List<Recipe>();

            EntityManagerId = EntityManager.Add(EntityManager.EntityType.CraftComponent, this);
        }

        public void Tick(long time)
        {
            craftAction?.Tick(time);
            // if (craftAction is null && !owner.InCombat)
            // {
            //     if (EntityManager.GetLivingByComponent(typeof(AttackComponent)).ToArray().Contains(owner))
            //         EntityManager.RemoveComponent(typeof(AttackComponent), owner);
            // }
        }

        public void StartCraft(Recipe recipe, AbstractCraftingSkill skill, int craftingTime)
        {
            if(craftAction == null)
                craftAction = new CraftAction(owner, craftingTime, recipe, skill);
        }

        public void StopCraft()
        {
            if (craftAction != null)
            {
                craftAction.CleanupCraftAction();
            }
                
        }
        
        

        /// <summary>
        /// Gets the crafting-state of this living
        /// </summary>
        public virtual bool CraftState { get; set; }
        
    }
}