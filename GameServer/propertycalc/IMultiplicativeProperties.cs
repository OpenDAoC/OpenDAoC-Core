using DOL.GS.Effects;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// Interface for properties that are multiplied to get final value (like max speed)
    /// </summary>
    public interface IMultiplicativeProperties
    {
        /// <summary>
        /// Adds new value, if key exists value will be overwritten
        /// </summary>
        /// <param name="index">The property index</param>
        /// <param name="effect">The key used to remove value later</param>
        /// <param name="value">The value added</param>
        void Set(int index, ECSGameEffect effect, double value);

        /// <summary>
        /// Adds new value, if key exists value will be overwritten
        /// </summary>
        /// <param name="index">The property index</param>
        /// <param name="effect">The key used to remove value later</param>
        /// <param name="value">The value added</param>
        void Set(int index, IGameEffect effect, double value);

        /// <summary>
        /// Removes stored value
        /// </summary>
        /// <param name="index">The property index</param>
        /// <param name="effect">The key use to add the value</param>
        void Remove(int index, ECSGameEffect effect);

        /// <summary>
        /// Removes stored value
        /// </summary>
        /// <param name="index">The property index</param>
        /// <param name="effect">The key use to add the value</param>
        void Remove(int index, IGameEffect effect);

        /// <summary>
        /// Gets the property value
        /// </summary>
        /// <param name="index">The property index</param>
        /// <returns>The property value (1.0 = 100%)</returns>
        double Get(int index);
    }
}
