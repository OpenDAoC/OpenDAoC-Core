using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    public class CraftAction
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public const string CRAFT_QUEUE_LENGTH_PROPERTY = "CraftQueueLength";
        public const string CRAFT_QUEUE_REMAINING_PROPERTY = "CraftQueueRemaining";
        public const string RECIPE_TO_CRAFT_PROPERTY = "RecipeToCraft";

        private GamePlayer _owner;
        private Recipe _recipe;
        private AbstractCraftingSkill _skill;
        private int _craftTime;
        private long _startTick;
        private bool _finishedCraft;

        private long EndTime => _startTick + _craftTime;

        public CraftAction(GamePlayer owner, int CraftingTime, Recipe recipe, AbstractCraftingSkill skill)
        {
            _owner = owner;
            _startTick = GameLoop.GameLoopTime;
            _craftTime = CraftingTime * 1000;
            _recipe = recipe;
            _skill = skill;
            owner.craftComponent.CraftState = true;
        }

        public void Tick()
        {
            if (_owner.IsMezzed || _owner.IsStunned)
            {
                CleanupCraftAction();
                return;
            }

            if (_owner.CurrentSpellHandler?.Spell.Uninterruptible == false)
            {
                CleanupCraftAction();
                return;
            }

            if (_owner.IsMoving)
            {
                CleanupCraftAction();
                _owner.Out.SendMessage(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "AbstractCraftingSkill.CraftItem.MoveAndInterrupt"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (ServiceUtils.ShouldTick(EndTime))
                MakeItem();

            if (_finishedCraft)
                CleanupCraftAction();
        }

        public void CleanupCraftAction()
        {
            _owner.craftComponent.CraftAction = null;
            _owner.craftComponent.CraftState = false;
            _finishedCraft = false;
            _owner.Out.SendCloseTimerWindow();
            _owner.TempProperties.RemoveProperty(CRAFT_QUEUE_REMAINING_PROPERTY);
        }

        protected virtual void MakeItem()
        {
            Recipe recipe = _recipe;
            AbstractCraftingSkill skill = _skill;
            int queue = _owner.TempProperties.GetProperty<int>(CRAFT_QUEUE_LENGTH_PROPERTY);
            int remainingToCraft = _owner.TempProperties.GetProperty<int>(CRAFT_QUEUE_REMAINING_PROPERTY);

            if (_owner == null || recipe == null || skill == null)
            {
                _owner?.Out.SendMessage("Could not find recipe or item to craft!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                log.Error("Crafting.MakeItem: Could not retrieve player, recipe, or raw materials to craft from CraftTimer.");
                return;
            }

            if (queue > 1 && remainingToCraft == 0)
                remainingToCraft = queue;

            _owner.Out.SendCloseTimerWindow();

            if (Util.Chance(skill.CalculateChanceToMakeItem(_owner, recipe.Level)))
            {
                if (!skill.RemoveUsedMaterials(_owner, recipe))
                {
                    _owner.Out.SendMessage(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "AbstractCraftingSkill.MakeItem.NotAllMaterials"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    if (_owner.Client.Account.PrivLevel == 1)
                    {
                        CleanupCraftAction();
                        return;
                    }
                }

                _owner.TempProperties.SetProperty(CRAFT_QUEUE_REMAINING_PROPERTY, --remainingToCraft);
                skill.BuildCraftedItem(_owner, recipe);
                skill.GainCraftingSkillPoints(_owner, recipe);
            }
            else
            {
                _owner.Out.SendMessage(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "AbstractCraftingSkill.MakeItem.LoseNoMaterials", recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                _owner.Out.SendPlaySound(eSoundType.Craft, 0x02);
            }

            if (remainingToCraft > 0)
            {
                if (skill.CheckRawMaterials(_owner, recipe))
                {
                    _startTick = GameLoop.GameLoopTime;
                    _owner.Out.SendTimerWindow(LanguageMgr.GetTranslation(_owner.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CurrentlyMaking", recipe.Product.Name), skill.GetCraftingTime(_owner, recipe));
                    _finishedCraft = false;
                    _owner.craftComponent.AddRecipe(recipe);
                }
                else
                    _finishedCraft = true;
            }
            else
            {
                _owner.TempProperties.RemoveProperty(CRAFT_QUEUE_REMAINING_PROPERTY);
                _finishedCraft = true;
            }
        }
    }
}
