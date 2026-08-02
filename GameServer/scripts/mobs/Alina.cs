using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class Alina : GameNPC
    {
        private bool _werewolf;

        public override bool AddToWorld()
        {
            AlinaModelBrain sBrain = new AlinaModelBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }

        public void SetWerewolfForm(bool werewolf)
        {
            if (_werewolf == werewolf)
                return;

            _werewolf = werewolf;

            if (werewolf)
            {
                Model = 395;
                Name = "Noble Werewolf Alina";
                Level = 22;
                Realm = eRealm.None;
                EquipmentTemplateID = null;
                Inventory = null;
                Message.MessageToArea(this, "Alina doubles over with a strangled cry, 'No, not again! Get away from me!'", eChatType.CT_Say, eChatLoc.CL_ChatWindow, WorldMgr.VISIBILITY_DISTANCE);
            }
            else
            {
                Model = 220;
                Name = "Alina";
                Level = 19;
                Realm = eRealm.Midgard;
                LoadEquipmentTemplateFromDatabase("Alina");
                Message.MessageToArea(this, "The werewolf's snarl softens into a woman's ragged breathing. Alina is herself again.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
            }

            BroadcastLivingEquipmentUpdate();
        }
    }
}

namespace DOL.AI.Brain
{
    public class AlinaModelBrain : StandardMobBrain
    {
        public override void Think()
        {
            if (!Body.InCombat)
                ((Alina)Body).SetWerewolfForm(Body.CurrentRegion.IsNightTime);

            base.Think();
        }
    }
}
