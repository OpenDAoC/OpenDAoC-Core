using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Core.GS.Behaviour.Attributes;
using log4net;

namespace Core.GS.Behaviour
{				
	/// <summary>
	/// Declares the behaviours managed, all behaviourtypes instances
	/// must be registered here to be usable
	/// </summary>
    public sealed class BehaviorMgr
    {  
		#region Declaration

		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
        private static readonly IDictionary m_behaviourActionMap = new HybridDictionary();
        private static readonly IDictionary m_behaviourTriggerMap = new HybridDictionary();
        private static readonly IDictionary m_behaviourRequirementMap = new HybridDictionary();

		#endregion

        public static bool Init()
        {
            //We will search our assemblies for Quests by reflection so 
            //it is not neccessary anymore to register new quests with the 
            //server, it is done automatically!
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Walk through each type in the assembly
                foreach (Type type in assembly.GetTypes())
                {
                    // Pick up a class
                    if (type.IsClass != true)
                        continue;                                        
                    
                    if (typeof(IBehaviorAction).IsAssignableFrom(type))
                    {                        
                        ActionAttribute attr = GetActionAttribute(type);
                        if (attr != null)
                        {
                            if (log.IsInfoEnabled)
                                log.Info("Registering BehaviourAction: " + type.FullName);
                            RegisterBehaviourAction(attr.ActionType, type);
                        }
                    }

                    if (typeof(IBehaviorTrigger).IsAssignableFrom(type))
                    {
                        
                        TriggerAttribute attr = getTriggerAttribute(type);
                        if (attr != null)
                        {
                            if (log.IsInfoEnabled)
                                log.Info("Registering BehaviourTrigger: " + type.FullName);
                            RegisterBehaviourTrigger(attr.TriggerType, type);
                        }
                    }

                    if (typeof(IBehaviorRequirement).IsAssignableFrom(type))
                    {
                        
                        RequirementAttribute attr = getRequirementAttribute(type);
                        if (attr != null)
                        {
                            if (log.IsInfoEnabled)
                                log.Info("Registering BehaviourRequirement: " + type.FullName);
                            RegisterBehaviourRequirement(attr.RequirementType, type);
                        }
                    }
                }
            }
            return true;
        }

        public static ActionAttribute GetActionAttribute(Type type)
        {
            foreach (Attribute attr in type.GetCustomAttributes(typeof(ActionAttribute), false))
            {
                return (ActionAttribute)attr;
            }
            return null;
        }

        public static TriggerAttribute getTriggerAttribute(Type type)
        {
            foreach (Attribute attr in type.GetCustomAttributes(typeof(TriggerAttribute), false))
            {                
                return (TriggerAttribute)attr;
            }
            return null;
        }

        public static RequirementAttribute getRequirementAttribute(Type type)
        {
            foreach (Attribute attr in type.GetCustomAttributes(typeof(RequirementAttribute),false))
            {            
                return (RequirementAttribute)attr;
            }
            return null;
        }		
		
		/// <summary>
        /// Creates a new BehaviourBuilder
		/// </summary>		
        /// <returns>BehaviourBuilder</returns>
        public static BehaviorBuilder getBuilder() 
		{
            return new BehaviorBuilder();
        }

        public static void RegisterBehaviourAction(EActionType actionType, Type type)
        {
            if (m_behaviourActionMap.Contains(actionType))
            {
                if (log.IsErrorEnabled)
                    log.Error(actionType + " is already registered, only one Type can be declared for each ActionType. Duplicate declaration found in :" + m_behaviourActionMap[actionType] + " and " + type);
                return;
            }
            m_behaviourActionMap.Add(actionType, type);
        }

        public static Type GetTypeForActionType(EActionType actionType)
        {
            return (Type) m_behaviourActionMap[actionType];
        }

        public static void RegisterBehaviourTrigger(ETriggerType triggerType, Type type)
        {
            if (m_behaviourTriggerMap.Contains(triggerType))
            {
                if (log.IsErrorEnabled)
                    log.Error(triggerType + " is already registered, only one Type can be declared for each TriggerType. Duplicate declaration found in :" + m_behaviourTriggerMap[triggerType] + " and " + type);
                return;
            }
            m_behaviourTriggerMap.Add(triggerType, type);
        }

        public static Type GetTypeForTriggerType(ETriggerType triggerType)
        {
            return (Type)m_behaviourTriggerMap[triggerType];
        }

        public static void RegisterBehaviourRequirement(ERequirementType requirementType, Type type)
        {
            if (m_behaviourRequirementMap.Contains(requirementType))
            {
                if (log.IsErrorEnabled)
                    log.Error(requirementType + " is already registered, only one Type can be declared for each Requirement. Duplicate declaration found in :" + m_behaviourRequirementMap[requirementType] + " and " + type);
                return;
            }
            m_behaviourRequirementMap.Add(requirementType, type);
        }

        public static Type GetTypeForRequirementType(ERequirementType requirementType)
        {
            return (Type)m_behaviourRequirementMap[requirementType];
        }
		
    }

    /// <summary>
    /// Helper Class to Flag parameters in the generic Definition as unused
    /// </summary>
    public class Unused
    {
    }
}