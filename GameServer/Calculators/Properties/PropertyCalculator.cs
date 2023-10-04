using log4net;

namespace DOL.GS.PropertyCalc
{
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
	public class PropertyCalculator : IPropertyCalculator
	{
		protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public PropertyCalculator()
		{
		}

		/// <summary>
		/// calculates the final property value
		/// </summary>
		/// <param name="living"></param>
		/// <param name="property"></param>
		/// <returns></returns>
		public virtual int CalcValue(GameLiving living, eProperty property) 
		{
			return 0;
		}

		public virtual int CalcValueBase(GameLiving living, eProperty property) 
		{
			return 0;
		}

        /// <summary>
        /// Calculates the property value for this living's buff bonuses only.
        /// </summary>
        /// <param name="living"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual int CalcValueFromBuffs(GameLiving living, eProperty property)
        {
            return 0;
        }

        /// <summary>
        /// Calculates the property value for this living's item bonuses only.
        /// </summary>
        /// <param name="living"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual int CalcValueFromItems(GameLiving living, eProperty property)
        {
            return 0;
        }
	}
}
