namespace DOL.GS
{
    public class EffectService
    {
        public static void Tick(long tick)
        {
            
            //Needs to be logic for each effect?
            foreach (var e in EntityManager.GetAllEffects())
            {

            }
        }


        //Parrellel Thread does this
        private static void HandleTick(long tick)
        {
            
        }
    }
}