using Core.GS.Quests;

namespace Core.GS.Behaviour
{
    public class BehaviorBuilder
    {
        public BehaviorBuilder()
        {            
            //this.addActionMethod = questType.GetMethod("AddBehaviour", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);            
        }                

        public void AddBehaviour(QuestBehavior questPart)
        {            
            //addActionMethod.Invoke(null, new object[] { questPart });
        }        

        public BaseBehavior CreateBehaviour(GameNpc npc)
        {
            BaseBehavior behavior =  new BaseBehavior(npc);            
            return behavior;
        }
        
    }
}