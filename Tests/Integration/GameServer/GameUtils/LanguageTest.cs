using System;
using Core.GS.Languages;
using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class LanguageTest: ServerTests
{
	public LanguageTest()
	{
	}
	
	[Test]
	public void TestGetString()
	{
		Console.WriteLine("TestGetString();");
		Console.WriteLine(LanguageMgr.GetTranslation ("test","fail default string"));
		Assert.IsTrue(true, "ok");
	}
}