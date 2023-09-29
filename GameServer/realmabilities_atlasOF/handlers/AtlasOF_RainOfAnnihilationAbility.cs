using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_RainOfAnnihilation : AtlasOF_RainOfBase
    {
        public AtlasOF_RainOfAnnihilation(DbAbility ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Annihilation", 7127, 7127, 15, living);
        }
    }
}
