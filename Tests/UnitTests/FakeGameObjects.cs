using Core.AI;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;

namespace Core.Tests.Unit.Gameserver
{
    public class FakePlayer : GamePlayer
    {
        public IPlayerClass FakePlayerClass = new DefaultPlayerClass();
        public int modifiedSpecLevel;
        public int modifiedIntelligence;
        public int modifiedToHitBonus;
        public int modifiedSpellLevel;
        public int modifiedEffectiveLevel;
        public int modifiedSpellDamage = 0;
        public int baseStat;
        private int totalConLostOnDeath;
        public int LastDamageDealt { get; private set; } = -1;
        public FakeRegion fakeRegion = new FakeRegion();

        public FakePlayer() : base(null, null)
        {
            this.ObjectState = eObjectState.Active;
            this.m_invulnerabilityTick = -1;
        }

        public override IPlayerClass PlayerClass { get { return FakePlayerClass; } }
        public override byte Level { get; set; }
        public override Region CurrentRegion { get { return fakeRegion; } set { } }
        public override IPacketLib Out => new FakePacketLib();
        public override GameClient Client => new GameClient(GameServer.Instance) { Account = new DbAccount() };
        public override int GetBaseStat(EStat stat) => baseStat;
        public override int GetModifiedSpecLevel(string keyName) => modifiedSpecLevel;

        public override int GetModified(EProperty property)
        {
            switch (property)
            {
                case EProperty.Intelligence:
                    return modifiedIntelligence;
                case EProperty.SpellLevel:
                    return modifiedSpellLevel;
                case EProperty.ToHitBonus:
                    return modifiedToHitBonus;
                case EProperty.LivingEffectiveLevel:
                    return modifiedEffectiveLevel;
                case EProperty.SpellDamage:
                    return modifiedSpellDamage;
                default:
                    return base.GetModified(property);
            }
        }

        public override void LoadFromDatabase(DataObject obj) { }

        public override void DealDamage(AttackData ad)
        {
            base.DealDamage(ad);
            LastDamageDealt = ad.Damage;
        }

        public override int TotalConstitutionLostAtDeath
        {
            get { return totalConLostOnDeath; }
            set { totalConLostOnDeath = value; }
        }
        public override void StartHealthRegeneration() { }
        public override void StartEnduranceRegeneration() { }
        public override void MessageToSelf(string message, EChatType chatType) { }
        protected override void ResetInCombatTimer() { }

        public override bool TargetInView { get; set; } = true;
    }

    public class FakeNPC : GameNpc
    {
        public int modifiedEffectiveLevel;

        public FakeNPC(ABrain defaultBrain) : base(defaultBrain)
        {
            this.ObjectState = eObjectState.Active;
        }

        public FakeNPC() : this(new FakeBrain()) { }

        public override Region CurrentRegion { get { return new FakeRegion(); } set { } }
        public override bool IsAlive => true;
        public override int GetModified(EProperty property)
        {
            switch (property)
            {
                case EProperty.LivingEffectiveLevel:
                    return modifiedEffectiveLevel;
                case EProperty.MaxHealth:
                    return 0;
                case EProperty.Intelligence:
                    return Intelligence;
                default:
                    return base.GetModified(property);
            }
        }
    }

    public class FakeLiving : GameLiving
    {
        public bool fakeIsAlive = true;
        public eObjectState fakeObjectState = eObjectState.Active;

        public override bool IsAlive => fakeIsAlive;
        public override eObjectState ObjectState => fakeObjectState;
        public override EGameObjectType GameObjectType => throw new System.NotImplementedException();
    }

    public class FakeControlledBrain : ABrain, IControlledBrain
    {
        public GameLiving fakeOwner;
        public bool receivedUpdatePetWindow = false;

        public GameLiving Owner => fakeOwner;
        public void UpdatePetWindow() { receivedUpdatePetWindow = true; }

        public EWalkState WalkState { get; }
        public EAggressionState AggressionState { get; set; }
        public bool IsMainPet { get; set; }
        public void Attack(GameObject target) { }
        public void Disengage() { }
        public void ComeHere() { }
        public void Follow(GameObject target) { }
        public void FollowOwner() { }
        public GameLiving GetLivingOwner() { return null; }
        public GameNpc GetNPCOwner() { return null; }
        public GamePlayer GetPlayerOwner() { return null; }
        public void Goto(GameObject target) { }
        public void SetAggressionState(EAggressionState state) { }
        public void Stay() { }
        public override void Think() { }

        public override void KillFSM() { }
    }

    public class FakeBrain : ABrain
    {
        public override void Think() { }
        public override void KillFSM() { }
    }
}
