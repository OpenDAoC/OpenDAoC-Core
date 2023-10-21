using System;
using System.Reflection;
using Core.Events;
using Core.GS.Behaviour;
using log4net;

namespace Core.GS.Quests
{
	/// <summary>
	/// BaseQuestParts are the core element of the new questsystem,
    /// you can add as many QuestAction to a quest as you want. 
    /// 
    /// A QuestAction contains basically 3 Things: Trigger, Requirements, Actions 
    ///
    /// Triggers: A trigger defines the circumstances under which a certain QuestAction is fired.
    /// This can be eTriggerAction.Interact, eTriggerAction.GiveItem, eTriggerAction.Attack, etc...
    /// Additional there are two variables to add the needed parameters for the triggertype (Item to give for GiveItem, NPC to interact for Interact, etc...). To fire a QuestAction at least one of the added triggers must be fulfilled. 
    ///
    /// Requirements: Requirements describe what must be true to allow a QuestAction to fire.
    /// Level of player, Step of Quest, Class of Player, etc... There are also some variables to add
    /// additional parameters. To fire a QuestAction ALL requirements must be fulfilled. 
    ///
    /// Actions: If one trigger and all requirements are fulfilled the corresponding actions of
    /// a QuestAction will we executed one after another. Actions can be more or less anything:
    /// at the moment there are: GiveItem, TakeItem, Talk, Give Quest, Increase Quest Step, FinishQuest,
    /// etc....
	/// </summary>
	public class QuestBehavior : BaseBehavior
    {

        public const string NUMBER_OF_EXECUTIONS = "quest.numberOfExecutions";

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Variables
                
        private Type questType;		
        
        private int maxNumberOfExecutions;                       

        #endregion

        #region Properties        
        

        /// <summary>
        /// Type of quest this questpart belnogs to
        /// </summary>
        public Type QuestType
        {
            get { return questType; }
            set { questType = value; }
        }        

        public int MaxNumberOfExecutions
        {
            get { return maxNumberOfExecutions; }
        }         

        #endregion
        

        /// <summary>
        /// Creates a QuestPart for the given questtype with the default npc.
        /// </summary>
        /// <param name="questType">type of Quest this QuestPart will belong to.</param>
        /// <param name="npc">NPC associated with his questpart typically NPC talking to or mob killing, etc...</param>        
        public QuestBehavior(Type questType, GameNpc npc) 
            : this (questType,npc,-1) { }

        /// <summary>
        /// Creates a QuestPart for the given questtype with the default npc.
        /// </summary>
        /// <param name="questType">type of Quest this QuestPart will belong to.</param>
        /// <param name="npc">NPC associated with his questpart typically NPC talking to or mob killing, etc...</param>        
        /// <param name="executions">Maximum number of executions the questpart should be execute during one quest for each player</param>
        public QuestBehavior(Type questType, GameNpc npc, int executions) : base (npc)
        {
            this.questType = questType;            
            this.maxNumberOfExecutions = executions;            
        }                

        /// <summary>
        /// This method is called by the BaseQuest whenever a event associated with the Quest accurs
        /// or a automatically added eventhandler for the trigers fires
        /// </summary>
        /// <param name="e">DolEvent of notify call</param>        
        /// <param name="sender">Sender of notify call</param>
        /// <param name="args">EventArgs of notify call</param>        
        public override void Notify(CoreEvent e, object sender, EventArgs args)
        {            
            GamePlayer player = BehaviorUtil.GuessGamePlayerFromNotify(e, sender, args);

            if (player == null)
            {
				//if (Log.IsDebugEnabled)
				//    Log.Debug("Couldn't guess player for EventArgs " + args + ". Triggers with this eventargs type won't work within quests.");
                return;
            }
            AQuest quest = player.IsDoingQuest(QuestType);
            
            int executions = 0;
            if (quest != null && quest.GetCustomProperty(this.ID + "_" + NUMBER_OF_EXECUTIONS) != null)
            {
                executions = Convert.ToInt32(quest.GetCustomProperty(ID + "_" + NUMBER_OF_EXECUTIONS));                
            }

            if (MaxNumberOfExecutions < 0 || executions < this.MaxNumberOfExecutions)
            {
                if (CheckTriggers(e, sender, args) && CheckRequirements(e, sender, args) && Actions != null)
                {
                    foreach (IBehaviorAction action in Actions)
                    {
                        action.Perform(e, sender, args);
                    }
                    if (quest != null)
                        quest.SetCustomProperty(this.ID + "_" + NUMBER_OF_EXECUTIONS, Convert.ToString(executions + 1));       
                }                
            }
		}
	}
}