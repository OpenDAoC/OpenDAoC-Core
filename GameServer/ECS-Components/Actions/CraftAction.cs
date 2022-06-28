using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    /// <summary>
    /// The attack action of this living
    /// </summary>
    public class CraftAction
    {
	    protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
	    
        private GameLiving owner;
        private GamePlayer ownerPlayer;
        private int CraftTime;
        private long m_startTick;
        public long EndTime { get { return m_startTick + CraftTime; } }

        public bool finishedCraft = false;

        public Recipe m_recipe;
        public AbstractCraftingSkill m_skill;

        /// <summary>
        /// Constructs a new craft action
        /// </summary>
        /// <param name="owner">The action source</param>
        public CraftAction(GameLiving owner, int CraftingTime, Recipe recipe, AbstractCraftingSkill skill)
        {
            this.owner = owner;
            ownerPlayer = owner as GamePlayer;
            m_startTick = GameLoop.GameLoopTime;
            CraftTime = CraftingTime * 1000;
            m_recipe = recipe;
            m_skill = skill;
            owner.craftComponent.CraftState = true;
        }

        /// <summary>
        /// Called on every timer tick
        /// </summary>
        public void Tick(long time)
        {

            if (time > m_startTick)
            {
                //GameLiving owner = (GameLiving)m_actionSource;

                if (owner.IsMezzed || owner.IsStunned)
                {
                    //CraftTime = 100;
                    CleanupCraftAction();
                    return;
                }

                if (owner.IsCasting && !owner.CurrentSpellHandler.Spell.Uninterruptible)
                {
                    //Interval = 100;
                    CleanupCraftAction();
                    return;
                }

                if (owner.IsMoving)
                {
	                CleanupCraftAction();
	                ownerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(ownerPlayer.Client.Account.Language, "AbstractCraftingSkill.CraftItem.MoveAndInterrupt"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
	                return;
                }

                /*
                if (!owner.craftComponent.CraftState)
                {
                    // AttackData ad = owner.TempProperties.getProperty<object>(LAST_ATTACK_DATA, null) as AttackData;
                    // owner.TempProperties.removeProperty(LAST_ATTACK_DATA);
                    // if (ad != null && ad.Target != null)
                    //     ad.Target.attackComponent.RemoveAttacker(owner);
                    //Stop();
                    CleanupCraftAction();
                    return;
                }*/

                if(time > EndTime)
	                MakeItem();

                
                if(finishedCraft)
	                CleanupCraftAction();
            }
        }

        public void CleanupCraftAction()
        {
            owner.craftComponent.craftAction = null;
            owner.craftComponent.CraftState = false;
            finishedCraft = false;
            ownerPlayer.Out.SendCloseTimerWindow();
            owner.TempProperties.removeProperty("CraftQueueRemaining");
        }
        
        
        protected virtual void MakeItem()
        {
	        GamePlayer player = this.owner as GamePlayer;
	        Recipe recipe = m_recipe;
			AbstractCraftingSkill skill = m_skill;
			var queue = player.TempProperties.getProperty<int>("CraftQueueLength");
			var remainingToCraft = player.TempProperties.getProperty<int>("CraftQueueRemaining");

			if (player == null || recipe == null || skill == null)
			{
				if (player != null) player.Out.SendMessage("Could not find recipe or item to craft!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				log.Error("Crafting.MakeItem: Could not retrieve player, recipe, or raw materials to craft from CraftTimer.");
				return;
			}

			if (queue > 1 && remainingToCraft == 0)
			{
				remainingToCraft = queue;
			}

			//player.CraftTimer?.Stop();
			player.Out.SendCloseTimerWindow();

			if (Util.Chance(skill.CalculateChanceToMakeItem(player, recipe.Level)))
			{
				if (!skill.RemoveUsedMaterials(player, recipe))
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.MakeItem.NotAllMaterials"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

					if (player.Client.Account.PrivLevel == 1)
						return;
				}
				skill.BuildCraftedItem(player, recipe);
				skill.GainCraftingSkillPoints(player, recipe);
			}
			else
			{
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.MakeItem.LoseNoMaterials", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
				player.Out.SendPlaySound(eSoundType.Craft, 0x02);
			}

			if (remainingToCraft > 1)
			{
				if (skill.CheckRawMaterials(player, recipe))
				{
					player.TempProperties.setProperty("CraftQueueRemaining", --remainingToCraft);
					//StartCraftingTimerAndSetCallBackMethod(player, recipe, GetCraftingTime(player, recipe));
					this.m_startTick = GameLoop.GameLoopTime + 1;
					player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CurrentlyMaking", recipe.Product.Name), skill.GetCraftingTime(player, recipe));
					finishedCraft = false;
					player.craftComponent.AddRecipe(recipe);
				}
				else
				{ 
					finishedCraft = true;
				}
			}
			else
			{
				player.TempProperties.removeProperty("CraftQueueRemaining");
				finishedCraft = true;
			}
        }
    }
}
