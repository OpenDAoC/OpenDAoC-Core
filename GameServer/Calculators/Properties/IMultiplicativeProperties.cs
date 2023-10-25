namespace Core.GS.Calculators;

/// <summary>
/// Interface for properties that are multiplied to get final value (like max speed)
/// </summary>
public interface IMultiplicativeProperties
{
	/// <summary>
	/// Adds new value, if key exists value will be overwriten
	/// </summary>
	/// <param name="index">The property index</param>
	/// <param name="key">The key used to remove value later</param>
	/// <param name="value">The value added</param>
	void Set(int index, object key, double value);

	/// <summary>
	/// Removes stored value
	/// </summary>
	/// <param name="index">The property index</param>
	/// <param name="key">The key use to add the value</param>
	void Remove(int index, object key);

	/// <summary>
	/// Gets the property value
	/// </summary>
	/// <param name="index">The property index</param>
	/// <returns>The property value (1.0 = 100%)</returns>
	double Get(int index);
}