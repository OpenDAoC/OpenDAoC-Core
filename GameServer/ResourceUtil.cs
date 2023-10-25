using System.Reflection;
using System.IO;

namespace Core.GS;

/// <summary>
/// Helps managing embedded resources
/// </summary>
public class ResourceUtil
{
	/// <summary>
	/// Searches for a specific resource and returns the stream
	/// </summary>
	/// <param name="fileName">the resource name</param>
	/// <returns>the resource stream</returns>
	public static Stream GetResourceStream(string fileName)
	{
		Assembly myAssembly = Assembly.GetAssembly(typeof(ResourceUtil));
		fileName = fileName.ToLower();
		foreach(string name in myAssembly.GetManifestResourceNames())
		{
			if(name.ToLower().EndsWith(fileName))
				return myAssembly.GetManifestResourceStream(name);
		}
		return null;
	}

	/// <summary>
	/// Extracts a given resource
	/// </summary>
	/// <param name="fileName">the resource name</param>
	public static void ExtractResource(string fileName)
	{
		ExtractResource(fileName, fileName);
	}
	
	/// <summary>
	/// Extracts a given resource
	/// </summary>
	/// <param name="resourceName">the resource name</param>
	/// <param name="fileName">the external file name</param>
	public static void ExtractResource(string resourceName, string fileName)
	{
		FileInfo finfo = new FileInfo(fileName);
		if(!finfo.Directory.Exists)
			finfo.Directory.Create();

		using(StreamReader reader = new StreamReader(GetResourceStream(resourceName)))
		{
			using(StreamWriter writer = new StreamWriter(File.Create(fileName)))
			{
				writer.Write(reader.ReadToEnd());
			}
		}
	}
}