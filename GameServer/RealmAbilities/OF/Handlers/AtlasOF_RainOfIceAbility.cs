using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_RainOfIce : AtlasOF_RainOfBase
    {
        public AtlasOF_RainOfIce(DbAbility ability, int level) : base(ability, level) { }

        public override void Execute(GameLiving living)
        {
            Execute("Rain Of Ice", 7126, 7126, 13, living);
        }
    }
}
