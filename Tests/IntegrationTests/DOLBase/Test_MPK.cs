using DOL.MPK;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Text;
using System.IO;

namespace DOL.Tests.Integration.DOLBase
{
    [TestFixture]
    class Test_MPK
    {
        private string testDirectory => "testMPK";
        private string textFileLocation => "test.txt";
        private string mpkFileLocation => "test.mpk";
        private string extractPath => "extractDirectory";
        private string textFileContent => "1234";

        [OneTimeSetUp]
        public void CreateFilesToBeAddedToMPK()
        {
            Directory.CreateDirectory(testDirectory);
            Directory.SetCurrentDirectory(testDirectory);

            var testFile = File.Create(textFileLocation);
            var fileContent = Encoding.ASCII.GetBytes(textFileContent);
            testFile.Write(fileContent, 0, fileContent.Length);
            testFile.Close();
        }

        [Test, Order(1)]
        public void Save_TestTxtToMPK_CorrectCRC()
        {
            var newMPK = new MPK.MpkHandler(mpkFileLocation, true);
            var mpkFile = new MpkFile(textFileLocation);
            newMPK.AddFile(mpkFile);
            newMPK[textFileLocation].Header.TimeStamp = 0; //Make MPK creation deterministic

            newMPK.Save();
            ClassicAssert.AreEqual(1, newMPK.Count);

            var expectedCRCValue = 375344986;
            ClassicAssert.AreEqual(expectedCRCValue, newMPK.CRCValue);
        }

        [Test, Order(2)]
        public void Open_TestMPK_NoExceptions()
        {
            _ = new MPK.MpkHandler(mpkFileLocation, false);
        }

        [Test, Order(3)]
        public void Extract_TestMPK_SameTxtContent()
        {
            var mpk = new MPK.MpkHandler(mpkFileLocation, false);
            mpk.Extract(extractPath);

            var actualFileText = File.ReadAllText(Path.Combine(extractPath,  textFileLocation));
            var expectedFileText = textFileContent;
            ClassicAssert.AreEqual(expectedFileText, actualFileText);
        }

        [OneTimeTearDown]
        public void RemoveArtifacts()
        {
            Directory.SetCurrentDirectory("..");
            Directory.Delete(Path.Combine(testDirectory), true);
        }
    }
}
