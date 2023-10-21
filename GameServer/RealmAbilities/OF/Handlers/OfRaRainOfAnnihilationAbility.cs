using Core.Database;

namespace Core.GS.RealmAbilities
{
    public class OfRaRainOfAnnihilationAbility : OfRaRainOfBaseAbility
    {
        public OfRaRainOfAnnihilationAbility(DbAbility ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Annihilation", 7127, 7127, 15, living);
        }
    }
}
