using System;
using DOL.Events;
using DOL.GS.Behaviour.Attributes;
using log4net;
using System.Reflection;

namespace DOL.GS.Behaviour
{
    /// <summary>
    /// Requirements describe what must be true to allow a QuestAction to fire.
    /// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
    /// additional parameters. To fire a QuestAction ALL requirements must be fulfilled.         
    /// </summary>
    public abstract class AbstractRequirement<TypeN,TypeV> : IBehaviorRequirement
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private eRequirementType type;
        private TypeN n;
        private TypeV v;
        private eComparator comparator;
		private GameNpc defaultNPC;		

        /// <summary>
        /// R: RequirmentType
        /// </summary>
        public eRequirementType RequirementType
        {
            get { return type; }
            set { type = value; }
        }
        /// <summary>
        /// N: first Requirment Variable
        /// </summary>
        public TypeN N
        {
            get { return n; }
			set { n = value; }
        }
        /// <summary>
        /// V: Secoond Requirmenet Variable
        /// </summary>
        public TypeV V
        {
            get { return v; }
			set { v = value; }
        }
        /// <summary>
        /// C: Requirement Comparator
        /// </summary>
        public eComparator Comparator
        {
            get { return comparator; }
            set { comparator = value; }
        }

        /// <summary>
        /// returns the NPC of the requirement
        /// </summary>
        public GameNpc NPC
        {
            get { return defaultNPC; }
            set { defaultNPC = value; }
        }		
        

        public AbstractRequirement(GameNpc npc, eRequirementType type, eComparator comp)
        {
            this.defaultNPC = npc;
            this.type = type;
            this.comparator = comp;
        }

		/// <summary>
        /// Creates a new QuestRequirement and does some basich compativilite checks for the parameters
		/// </summary>
		/// <param name="defaultNPC"></param>
		/// <param name="type"></param>
		/// <param name="n"></param>
		/// <param name="v"></param>
		/// <param name="comp"></param>
        public AbstractRequirement(GameNpc defaultNPC, eRequirementType type, Object n, Object v, eComparator comp) : this(defaultNPC,type,comp)
        {            			            

            RequirementAttribute attr = BehaviorMgr.getRequirementAttribute(this.GetType());
            // handle parameter N
            object defaultValueN = GetDefaultValue(attr.DefaultValueN);            
            this.N = (TypeN)BehaviorUtils.ConvertObject(n, defaultValueN, typeof(TypeN));
            CheckParameter(this.N, attr.IsNullableN, typeof(TypeN));
            
            // handle parameter V
            object defaultValueV = GetDefaultValue(attr.DefaultValueV);            
            this.v = (TypeV)BehaviorUtils.ConvertObject(v, defaultValueV, typeof(TypeV));
            CheckParameter(this.V, attr.IsNullableV, typeof(TypeV));
            
        }

        protected virtual object GetDefaultValue(Object defaultValue)
        {
            if (defaultValue != null)
            {
                if (defaultValue is EDefaultValueConstants)
                {
                    switch ((EDefaultValueConstants)defaultValue)
                    {                        
                        case EDefaultValueConstants.NPC:
                            defaultValue = NPC;
                            break;
                    }
                }
            }
            return defaultValue;
        }

        protected virtual bool CheckParameter(object value, Boolean isNullable, Type destinationType)
        {
            if (destinationType == typeof(Unused))
            {
                if (value != null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn("Parameter is not used for =" + this.GetType().Name + ".\n The recieved parameter " + value + " will not be used for anthing. Check your quest code for inproper usage of parameters!");
                        return false;
                    }
                }
            }
            else
            {
                if (!isNullable && value == null)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Not nullable parameter was null, expected type is " + destinationType.Name + "for =" + this.GetType().Name + ".\nRecived parameter was " + value);
                        return false;
                    }
                }
                if (value != null && !(destinationType.IsInstanceOfType(value)))
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Parameter was not of expected type, expected type is " + destinationType.Name + "for " + this.GetType().Name + ".\nRecived parameter was " + value);
                        return false;
                    }
                }
            }

            return true;
        }

        public abstract bool Check(CoreEvent e, object sender, EventArgs args);

        /// <summary>
        /// Compares value1 with value2 
        /// Allowed Comparators: Less,Greater,Equal, NotEqual, None
        /// </summary>
        /// <param name="value1">Value1 one to compare</param>
        /// <param name="value2">Value2 to cmopare</param>
        /// <param name="comp">Comparator to use for Comparison</param>
        /// <returns>result of comparison</returns>
        protected static bool compare(long value1, long value2, eComparator comp)
        {
            switch (comp)
            {
                case eComparator.Less:
                    return (value1 < value2);
                case eComparator.Greater:
                    return (value1 > value2);
                case eComparator.Equal:
                    return (value1 == value2);
                case eComparator.NotEqual:
                    return (value1 != value2);
                case eComparator.None:
                    return true;
                default:
                    throw new ArgumentException("Comparator not supported:" + comp, "comp");
            }
        }

        /// <summary>
        /// Compares value1 with value2 
        /// Allowed Comparators: Less,Greater,Equal, NotEqual, None
        /// </summary>
        /// <param name="value1">Value1 one to compare</param>
        /// <param name="value2">Value2 to cmopare</param>
        /// <param name="comp">Comparator to use for Comparison</param>
        /// <returns>result of comparison</returns>
        protected static bool compare(int value1, int value2, eComparator comp)
        {
            return compare((long)value1, (long)value2, comp);
        }
    }
}