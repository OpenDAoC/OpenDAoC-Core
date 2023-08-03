using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class OfRaRainOfFireHandler : OfRaRainOfBaseHandler
    {
        public OfRaRainOfFireHandler(DbAbilities ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Fire", 7125, 7125, 11, living);
        }
    }
}