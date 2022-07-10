using System;
using System.Collections.Generic;
using System.Linq;


namespace DOL.GS
{
    public class CraftComponent
    {
        public GameLiving owner;
        public CraftAction craftAction;

        public GamePlayer ownerPlayer;


        /// <summary>
        /// The objects currently being crafted by this living
        /// 
        /// </summary>
        protected List<Recipe> m_recipes;


        /// <summary>
        /// Returns the list of recipes
        /// </summary>
        public List<Recipe> Recipes
        {
            get { return m_recipes; }
        }

        /// <summary>
        /// Adds an attacker to the attackerlist
        /// </summary>
        /// <param name="attacker">the attacker to add</param>
        public void AddRecipe(Recipe recipe)
        {
            lock (Recipes)
            {
                if (recipe == null) return;
                if (m_recipes.Contains(recipe)) return;
                m_recipes.Add(recipe);

                if (m_recipes.Count() > 0 &&
                    !EntityManager.GetLivingByComponent(typeof(CraftComponent)).Contains(owner))
                    EntityManager.AddComponent(typeof(CraftComponent), owner);
            }
        }

        /// <summary>
        /// Removes an attacker from the list
        /// </summary>
        /// <param name="attacker">the attacker to remove</param>
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
            
            if (!EntityManager.GetLivingByComponent(typeof(CraftComponent)).ToArray().Contains(owner))
                EntityManager.AddComponent(typeof(CraftComponent), owner);
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