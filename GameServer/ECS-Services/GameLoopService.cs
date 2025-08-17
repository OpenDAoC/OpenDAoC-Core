namespace DOL.GS
{
    public sealed class GameLoopService : GameServiceBase
    {
        public static new GameLoopService Instance { get; }

        static GameLoopService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
        }
    }
}
