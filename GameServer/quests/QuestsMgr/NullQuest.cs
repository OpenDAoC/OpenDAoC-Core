using System;
using DOL.Events;

namespace DOL.GS.Quests
{
    public class NullQuest : AbstractQuest
    {
        public override string Name => string.Empty;
        public override string Description => string.Empty;
        public override int Level { get => 0; set { } }
        public override int Step { get => -1; set { } }

        public override bool IsDoingQuest()
        {
            return false;
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            return false;
        }

        public override void FinishQuest() { }
        public override void AbortQuest() { }
        public override void Notify(DOLEvent e, object sender, EventArgs args) { }
        public override void OnQuestAssigned(GamePlayer player) { }
        public override int MaxQuestCount => 0;
        public override void SaveIntoDatabase() { }
        public override void DeleteFromDatabase() { }

        public override bool Command(GamePlayer player, eQuestCommand command, AbstractArea area = null)
        {
            return false;
        }

        public override void StartQuestActionTimer(GamePlayer player, eQuestCommand command, int seconds, string label = "") { }

        protected override int QuestActionCallback(ECSGameTimer timer)
        {
            return 0;
        }

        protected override void QuestCommandCompleted(eQuestCommand command, GamePlayer player) { }
    }
}
