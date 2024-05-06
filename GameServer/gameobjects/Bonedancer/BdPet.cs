namespace DOL.GS
{
    public class BdPet : GameSummonedPet
    {
        private enum Procs
        {
            Cold = 32050,
            Disease = 32014,
            Heat = 32053,
            Poison = 32013,
            Stun = 2165
        };

        public BdPet(INpcTemplate npcTemplate) : base(npcTemplate) { }
    }
}
