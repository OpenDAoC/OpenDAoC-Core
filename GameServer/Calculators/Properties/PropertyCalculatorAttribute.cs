using System;

namespace Core.GS.Calculators;

/// <summary>
/// Denotes a class as a property calculator. Must also implement IPropertyCalculator.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
public class PropertyCalculatorAttribute : Attribute
{
	/// <summary>
	/// Defines lowest property of calculator properties range
	/// </summary>
	private readonly EProperty m_min;
	/// <summary>
	/// Defines highest property of calculator properties range
	/// </summary>
	private readonly EProperty m_max;

	/// <summary>
	/// Gets the lowest property of calculator properties range
	/// </summary>
	public EProperty Min
	{
		get { return m_min; }
	}

	/// <summary>
	/// Gets the highest property of calculator properties range
	/// </summary>
	public EProperty Max
	{
		get { return m_max; }
	}

	/// <summary>
	/// Constructs a new calculator attribute for just one property
	/// </summary>
	/// <param name="prop">The property calculator is assigned to</param>
	public PropertyCalculatorAttribute(EProperty prop) : this(prop, prop)
	{
	}

	/// <summary>
	/// Constructs a new calculator attribute for range of properties
	/// </summary>
	/// <param name="min">The lowest property in range</param>
	/// <param name="max">The highest property in range</param>
	public PropertyCalculatorAttribute(EProperty min, EProperty max)
	{
		if (min > max)
			throw new ArgumentException("min property is higher than max (min=" + (int)min + " max=" + (int)max + ")");
		if (min < 0 || max > EProperty.MaxProperty)
			throw new ArgumentOutOfRangeException("max", (int)max, "property must be in 0 .. eProperty.MaxProperty range");
		m_min = min;
		m_max = max;
	}
}