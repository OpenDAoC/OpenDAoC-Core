using System.IO;
using System.Linq;
using DOL.Logging;
using NUnit.Framework;

namespace DOL.GS.Tests
{
    [TestFixture]
    public class UT_DOLScriptCompiler
    {
        // DOLScriptCompiler's static constructor reads the script assemblies from GameServer.Instance.Configuration.
        private sealed class ConfigurationOnlyServer : GameServer
        {
            public ConfigurationOnlyServer() : base(new GameServerConfiguration()) { }

            protected override void CheckAndInitDB() { }
        }

        private GameServer _previousInstance;
        private string _previousDirectory;
        private DirectoryInfo _workDirectory;

        [SetUp]
        public void SetUp()
        {
            LoggerManager.InitializeWithExplicitLibrary(null, LogLibrary.None);
            _previousInstance = GameServer.Instance;
            _previousDirectory = Directory.GetCurrentDirectory();
            GameServer.LoadTestDouble(new ConfigurationOnlyServer()); // Changes the current directory to the server root.
            _workDirectory = Directory.CreateTempSubdirectory("dol-script-compiler-");
        }

        [TearDown]
        public void TearDown()
        {
            GameServer.LoadTestDouble(_previousInstance);
            Directory.SetCurrentDirectory(_previousDirectory);
            _workDirectory.Delete(true);
        }

        private FileInfo WriteSource(string code)
        {
            var file = new FileInfo(Path.Combine(_workDirectory.FullName, "Script.cs"));
            File.WriteAllText(file.FullName, code);
            return file;
        }

        [Test]
        public void Compile_ValidSource_ReturnsAssembly()
        {
            var compiler = new DOLScriptCompiler();
            var output = new FileInfo(Path.Combine(_workDirectory.FullName, "Valid.dll"));

            var assembly = compiler.Compile(output, [WriteSource("namespace TestScripts { public class Valid { } }")]);

            Assert.That(compiler.HasErrors, Is.False);
            Assert.That(assembly, Is.Not.Null);
            Assert.That(assembly.GetType("TestScripts.Valid"), Is.Not.Null);
        }

        [Test]
        public void Compile_SourceWithErrors_ReturnsNullAndKeepsTheErrors()
        {
            var compiler = new DOLScriptCompiler();
            var output = new FileInfo(Path.Combine(_workDirectory.FullName, "Broken.dll"));

            var assembly = compiler.Compile(output, [WriteSource("namespace TestScripts { public class Broken { UndefinedType field; } }")]);

            Assert.That(compiler.HasErrors, Is.True);
            Assert.That(assembly, Is.Null);
            Assert.That(compiler.GetDetailedErrorMessages().Any(m => m.Contains("CS0246")), Is.True);
        }
    }
}
