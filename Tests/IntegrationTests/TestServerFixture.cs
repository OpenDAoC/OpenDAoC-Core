using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Language;
using DOL.Logging;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [SetUpFixture]
    public class TestServerFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LoggerManager.InitializeWithExplicitLibrary(string.Empty, LogLibrary.Console);
            Integration.TestGameServer.Install();
            Properties.InitProperties();
            LanguageMgr.Init();
            GameLiving.LoadCalculators();
            SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells, true);
            WorldMgr.Init([]);

            GameLoopThreadPool threadPool = new GameLoopThreadPoolSingleThreaded();
            threadPool.Init();
            typeof(GameLoop).GetField("_threadPool", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, threadPool);
            ClientService.Instance.BeginTick();
        }
    }
}
