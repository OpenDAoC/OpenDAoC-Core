using System.Collections.Generic;

namespace Core.GS
{
	/// <summary>
	/// This interface allow to add extension methods to Object that handle Custom Parmeters in string form.
	/// You can implement this in any way to take advantage of Generic Queries on typed data on imported Database custom values tables
	/// </summary>
	public interface ICustomParamsValuable
	{
		Dictionary<string, List<string>> CustomParamsDictionary { get; set; }
	}
}
