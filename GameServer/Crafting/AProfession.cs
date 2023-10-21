using System;
using Core.Language;

namespace Core.GS
{
    /// <summary>
    /// Tradeskills that earn the crafter a title.
    /// </summary>
    public abstract class AProfession : ACraftingSkill
    {
        #region Title

        protected abstract String Profession { get; }

        public static String GetTitleFormat(int skillLevel)
        {
            if (skillLevel < 0)
                throw new ArgumentOutOfRangeException("skillLevel");


            switch (skillLevel / 100)
            {
                case 0: return "CraftersTitle.Helper";
                case 1: return "CraftersTitle.JuniorApprentice";
                case 2: return "CraftersTitle.Apprentice";
                case 3: return "CraftersTitle.Neophyte";
                case 4: return "CraftersTitle.Assistant";
                case 5: return "CraftersTitle.Junior";
                case 6: return "CraftersTitle.Journeyman";
                case 7: return "CraftersTitle.Senior";
                case 8: return "CraftersTitle.Master";
                case 9: return "CraftersTitle.Grandmaster";
                case 10: return "CraftersTitle.Legendary";
                default: return "CraftersTitle.LegendaryGrandmaster";
            }
        }

        public String GetTitle(GamePlayer player, int skillLevel)
        {
        	string profession = LanguageMgr.TryTranslateOrDefault(player, "!Profession!", Profession);
            try
            {
            	return LanguageMgr.TryTranslateOrDefault(player, "!None {0}!", GetTitleFormat(skillLevel), profession);
            }
            catch
            {
                return "<you may want to check your Crafting.txt language file>";
            }
        }

        #endregion
    }
}
