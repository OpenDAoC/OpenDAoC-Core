using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
    public class OfRaRainOfIceAbility : OfRaRainOfBaseAbility
    {
        public OfRaRainOfIceAbility(DbAbility ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Ice", 7126, 7126, 13, living);
        }
    }
}
