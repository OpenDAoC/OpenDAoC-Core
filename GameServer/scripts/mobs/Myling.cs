using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
    public class Myling : GameNPC
    {
        protected const ushort mylingModel = 929;

        private bool _revealed;

        public Myling() : base() { }

        public override bool AddToWorld()
        {
            _revealed = false;
            Model = mylingModel;
            Flags &= ~eFlags.GHOST;
            return base.AddToWorld();
        }

        public override void StartAttack(GameObject attackTarget)
        {
            SetRevealed(true);
            attackComponent.RequestStartAttack(attackTarget);
        }

        public override void StopAttack()
        {
            SetRevealed(false);
            base.StopAttack();
        }

        protected void SetRevealed(bool revealed)
        {
            if (_revealed == revealed)
                return;

            _revealed = revealed;

            if (revealed)
            {
                switch (Util.Random(8))
                {
                    case 0:
                        Model = 138; // troll male
                        break;
                    case 1:
                        Model = 148; // troll female
                        break;
                    case 2:
                        Model = 169; // kobold male
                        break;
                    case 3:
                        Model = 180; // kobold female
                        break;
                    case 4:
                        Model = 160; // norse male
                        break;
                    case 5:
                        Model = 162; // norse female
                        break;
                    case 6:
                        Model = 185; // dwarf male
                        break;
                    case 7:
                        Model = 200; // dwarf female
                        break;
                    case 8:
                        Model = 24; // skeleton
                        break;
                }

                Flags |= eFlags.GHOST;
                Message.MessageToArea(this, "The myling's shape runs like wax and settles into a face you almost recognize.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
            }
            else
            {
                Model = mylingModel;
                Flags &= ~eFlags.GHOST;
                Message.MessageToArea(this, "The stolen face sloughs away, and the myling fades back into the murk.", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
            }

            BroadcastLivingEquipmentUpdate();
        }
    }
}
