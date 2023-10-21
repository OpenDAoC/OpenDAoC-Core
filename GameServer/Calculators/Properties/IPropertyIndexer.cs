namespace Core.GS.Calculators;

/// <summary>
/// Interface for properties that are added to get final value
/// </summary>
public interface IPropertyIndexer
{
	int this[int index] { get; set; }
	int this[EProperty index] { get; set; }
}