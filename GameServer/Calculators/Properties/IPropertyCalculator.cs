namespace Core.GS.Calculators;

/// <summary>
/// Purpose of a property calculator is to serve
/// as a formula plugin that calcs the correct property value
/// ready for further calculations considering all bonuses/buffs 
/// and possible caps on it
/// it is a capsulation of the calculation logic behind each property
/// 
/// to reach that goal it makes use of the itembonus and buff category fields
/// on the living that will be filled through equip actions and 
/// buff/debuff effects
/// 
/// further it has access to all other calculators and properties
/// on a living to fulfil its task
/// </summary>
public interface IPropertyCalculator
{
	/// <summary>
	/// Calculates the final property value.
	/// </summary>
	/// <param name="living"></param>
	/// <param name="property"></param>
	/// <returns></returns>
	int CalcValue(GameLiving living, EProperty property);
	int CalcValueBase(GameLiving living, EProperty property);

    /// <summary>
    /// Calculates the modified value from buff bonuses only.
    /// </summary>
    /// <param name="living"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    int CalcValueFromBuffs(GameLiving living, EProperty property);

    /// <summary>
    /// Calculates the modified value from item bonuses only.
    /// </summary>
    /// <param name="living"></param>
    /// <param name="property"></param>
    /// <returns></returns>
    int CalcValueFromItems(GameLiving living, EProperty property);
}