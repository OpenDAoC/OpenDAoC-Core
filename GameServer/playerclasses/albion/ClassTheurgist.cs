using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
    [CharacterClass((int)eCharacterClass.Theurgist, "Theurgist", "Elementalist")]
    public class ClassTheurgist : ClassElementalist
    {
        public ClassTheurgist() : base()
        {
            m_profession = "PlayerClass.Profession.DefendersofAlbion";
            m_specializationMultiplier = 10;
            m_primaryStat = eStat.INT;
            m_secondaryStat = eStat.DEX;
            m_tertiaryStat = eStat.QUI;
            m_manaStat = eStat.INT;
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }

        public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
        {
            PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre
        };
    }
}
