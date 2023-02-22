using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS
{
    public class CraftAction
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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

        public void Tick(long time)
        {
            if (time <= _startTick)
                return;

            if (_owner.IsMezzed || _owner.IsStunned)
            {
                CleanupCraftAction();
                return;
            }

            if (_owner.IsCasting && !_owner.CurrentSpellHandler.Spell.Uninterruptible)
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

            if (time > EndTime)
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
            _owner.TempProperties.removeProperty("CraftQueueRemaining");
        }

        protected virtual void MakeItem()
        {
            GamePlayer player = _owner as GamePlayer;
            Recipe recipe = _recipe;
            AbstractCraftingSkill skill = _skill;
            int queue = player.TempProperties.getProperty<int>("CraftQueueLength");
            int remainingToCraft = player.TempProperties.getProperty<int>("CraftQueueRemaining");

            if (player == null || recipe == null || skill == null)
            {
                player?.Out.SendMessage("Could not find recipe or item to craft!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                log.Error("Crafting.MakeItem: Could not retrieve player, recipe, or raw materials to craft from CraftTimer.");
                return;
            }

            if (queue > 1 && remainingToCraft == 0)
                remainingToCraft = queue;

            player.Out.SendCloseTimerWindow();

            if (Util.Chance(skill.CalculateChanceToMakeItem(player, recipe.Level)))
            {
                if (!skill.RemoveUsedMaterials(player, recipe))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.MakeItem.NotAllMaterials"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    if (player.Client.Account.PrivLevel == 1)
                    {
                        CleanupCraftAction();
                        return;
                    }
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
                    _startTick = GameLoop.GameLoopTime + 1;
                    player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CurrentlyMaking", recipe.Product.Name), skill.GetCraftingTime(player, recipe));
                    _finishedCraft = false;
                    player.craftComponent.AddRecipe(recipe);
                }
                else
                    _finishedCraft = true;
            }
            else
            {
                player.TempProperties.removeProperty("CraftQueueRemaining");
                _finishedCraft = true;
            }
        }
    }
}
