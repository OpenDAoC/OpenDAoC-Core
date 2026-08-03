using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	public class CrusaderAntithesis : GameEpicDungeonNPC
	{
		private const ushort DISGUISED_MODEL = 667;
		private const ushort REVEALED_MODEL = 927;
		private const eFlags DISGUISE_FLAGS = eFlags.DONTSHOWNAME | eFlags.CANTTARGET;

		private bool _disguised;

		public CrusaderAntithesis() : base() { }

		public bool IsDisguised => _disguised;

		public override bool AddToWorld()
		{
			INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(50041);
			LoadTemplate(npcTemplate);

			SetOwnBrain(new CrusaderAntithesisBrain());
			return base.AddToWorld();
		}

		public void SetDisguised(bool disguised)
		{
			if (_disguised == disguised)
				return;

			_disguised = disguised;

			if (disguised)
			{
				Model = DISGUISED_MODEL;
				Flags |= DISGUISE_FLAGS;

				if (ObjectState == eObjectState.Active)
					Message.MessageToArea(this, "Crusader's Antithesis collapses into a gleaming sword, its blade sharp and biting!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			}
			else
			{
				Model = REVEALED_MODEL;
				Flags = (Flags & ~DISGUISE_FLAGS) | eFlags.GHOST;

				if (ObjectState == eObjectState.Active)
					Message.MessageToArea(this, "The sword twists apart in midair and regains its true form!", eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow, WorldMgr.VISIBILITY_DISTANCE);
			}

			BroadcastLivingEquipmentUpdate();
		}

		public override void OnAttackEnemy(AttackData ad)
		{
			if (ad != null && ad.Target != null && ad.Target.IsAlive && !IsCasting && Util.Chance(35))
				CastSpell(CrusaderDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

			base.OnAttackEnemy(ad);
		}

		private static Spell CrusaderDD => ScriptSpells.GetOrCreate("CrusaderAntithesisDD", 60, static db =>
		{
			db.CastTime = 0;
			db.Power = 0;
			db.RecastDelay = 3;
			db.ClientEffect = 14352;
			db.Icon = 14352;
			db.Damage = Util.Random(350, 450);
			db.DamageType = (int)eDamageType.Slash;
			db.Name = "Antithetical Strike";
			db.Range = 400;
			db.SpellID = 12016;
			db.Target = eSpellTarget.ENEMY.ToString();
			db.Type = eSpellType.DirectDamageNoVariance.ToString();
		});
	}
}

namespace DOL.AI.Brain
{
	public class CrusaderAntithesisBrain : StandardMobBrain
	{
		private long _swordFormUntil;

		public CrusaderAntithesisBrain() : base()
		{
			AggroLevel = 100;
			AggroRange = 600;
		}

		public override void Think()
		{
			if (Body is CrusaderAntithesis crusader)
			{
				if (!HasAggro)
					crusader.SetDisguised(false);
				else if (!crusader.IsDisguised)
				{
					if (Util.Chance(20) && Body.LastAttackedByEnemyTick < GameLoop.GameLoopTime - 4000)
					{
						crusader.SetDisguised(true);
						_swordFormUntil = GameLoop.GameLoopTime + Util.Random(3000, 6000);
					}
				}
				else if (GameLoop.GameLoopTime >= _swordFormUntil)
					crusader.SetDisguised(false);
			}

			base.Think();
		}
	}
}
