using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_RainOfFire : AtlasOF_RainOfBase
    {
        public AtlasOF_RainOfFire(DbAbility ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Fire", 7125, 7125, 11, living);
        }
    }
}
