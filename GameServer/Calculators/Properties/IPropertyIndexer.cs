namespace DOL.GS.PropertyCalc;

/// <summary>
/// Interface for properties that are added to get final value
/// </summary>
public interface IPropertyIndexer
{
	int this[int index] { get; set; }
	int this[eProperty index] { get; set; }
}