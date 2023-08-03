using System;
using System.Text;
using System.Reflection;
using DOL.Database;

namespace DOL.GS.Quests
{
    public class QuestBuilder
    {
        private Type questType;

        private MethodInfo addActionMethod;        

        public Type QuestType
        {
            get { return questType; }
            set { questType = value; }
        }

        public QuestBuilder(Type questType)
        {
            this.questType = questType;
            this.addActionMethod = questType.GetMethod("AddBehaviour", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);            
        }                

        public void AddBehaviour(QuestBehavior questPart)
        {            
            addActionMethod.Invoke(null, new object[] { questPart });
        }        

        public QuestBehavior CreateBehaviour(GameNpc npc)
        {
            QuestBehavior questPart =  new QuestBehavior(questType, npc);            
            return questPart;
        }

        public QuestBehavior CreateBehaviour(GameNpc npc, int maxExecutions)
        {
            QuestBehavior questPart = new QuestBehavior(questType, npc,maxExecutions);
            return questPart;
        }
    }
}