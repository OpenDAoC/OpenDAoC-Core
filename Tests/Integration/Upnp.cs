using NUnit.Framework;

namespace Core.Tests.Integration;

[TestFixture]
public class Upnp
{
	UpnpNat upnp;
	
	public Upnp(){}
	
	[OneTimeSetUp]
	public void init()
	{
		upnp = new UpnpNat();
	}
	
	[Test, Explicit]
	public void Discover()
	{
		Assert.IsTrue(upnp.Discover());
	}
}