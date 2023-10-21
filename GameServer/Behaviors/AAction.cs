using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour;
using log4net;

namespace Core.GS.Behaviors;

/// <summary>
/// If one trigger and all requirements are fulfilled the corresponding actions of
/// a QuestAction will we executed one after another. Actions can be more or less anything:
/// at the moment there are: GiveItem, TakeItem, Talk, Give Quest, Increase Quest Step, FinishQuest,
/// etc....
/// </summary>
public abstract class AAction<TypeP,TypeQ> : IBehaviorAction
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private EActionType actionType;
    private TypeQ q;
    private TypeP p;
	private GameNpc defaultNPC;		
    
    /// <summary>
    /// The action type
    /// </summary>
    public EActionType ActionType
    {
        get { return actionType; }
        set { actionType = value; }
    }
    /// <summary>
    /// First Action Variable
    /// </summary>
    public TypeP P
    {
        get { return p; }
		set { p = value; }
    }
    /// <summary>
    /// Second Action Variable
    /// </summary>
    public TypeQ Q
    {
        get { return q; }
		set {q = value;}
    }

	/// <summary>
	/// returns the NPC of the action
	/// </summary>
	public GameNpc NPC
	{
		get { return defaultNPC; }
        set { defaultNPC = value; }
	}

    public AAction(GameNpc npc, EActionType actionType)
    {
        this.defaultNPC = npc;
        this.actionType = actionType;
    }

	/// <summary>
    /// Initializes a new instance of the AbstractAction class.
	/// </summary>
	/// <param name="npc"></param>
	/// <param name="actionType"></param>
	/// <param name="p"></param>
	/// <param name="q"></param>
	public AAction(GameNpc npc, EActionType actionType, Object p, Object q) : this (npc,actionType)
	{		
        ActionAttribute attr = BehaviorMgr.GetActionAttribute(this.GetType());

        // handle parameter P
        object defaultValueP = GetDefaultValue(attr.DefaultValueP);                                        
        this.p = (TypeP)BehaviorUtil.ConvertObject(p, defaultValueP, typeof(TypeP));
        CheckParameter(this.p, attr.IsNullableP, typeof(TypeP));            

        // handle parameter Q
        object defaultValueQ = GetDefaultValue(attr.DefaultValueQ);                            
        this.q = (TypeQ)BehaviorUtil.ConvertObject(q, defaultValueQ, typeof(TypeQ));
        CheckParameter(this.q, attr.IsNullableQ, typeof(TypeQ));
	}

    protected virtual object GetDefaultValue(Object defaultValue) {
                                
        if (defaultValue != null )
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

    /// <summary>
    /// Action performed 
    /// Can be used in subclasses to define special behaviour of actions
    /// </summary>
    /// <param name="e"></param>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public abstract void Perform(CoreEvent e, object sender, EventArgs args);
    
}