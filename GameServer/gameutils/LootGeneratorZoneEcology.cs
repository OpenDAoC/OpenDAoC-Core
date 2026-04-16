using System;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// DB-driven zone ecology loot gate.
	/// Mapped mobs use zone-local ecology loot and optional coin; unmapped mobs
	/// retain the default global loot behavior.
	/// </summary>
	public class LootGeneratorZoneEcology : LootGeneratorBase, IConditionalLootGenerator
	{
		private readonly LootGeneratorMoney m_money = new LootGeneratorMoney();
		private readonly LootGeneratorOneTimeDrop m_oneTimeDrop = new LootGeneratorOneTimeDrop();
		private readonly ROGMobGenerator m_rog = new ROGMobGenerator();
		private readonly LootGeneratorTemplate m_template = new LootGeneratorTemplate();

		public override void Refresh(GameNPC mob)
		{
			ZoneEcologyLootRules.Refresh();
			m_oneTimeDrop.Refresh(mob);
			m_template.Refresh(mob);
		}

		public override LootList GenerateLoot(GameNPC mob, GameObject killer)
		{
			if (!ZoneEcologyLootRules.TryGetMapping(mob, out DbZoneEcologyLoot mapping))
				return GenerateDefaultLoot(mob, killer);

			LootList loot = base.GenerateLoot(mob, killer);
			loot.AddAll(m_oneTimeDrop.GenerateLoot(mob, killer));
			loot.AddAll(GenerateTemplateLoot(mapping.LootTemplateName, mapping.DropCount, killer));
			loot.AddAll(GenerateMaterialLoot(mapping, killer));
			loot.AddAll(GenerateNamedRogLoot(mapping, mob, killer));

			if (mapping.DropsCoin)
				loot.AddAll(m_money.GenerateLoot(mob, killer));

			return loot;
		}

		public bool IsActiveFor(GameNPC mob)
		{
			return ZoneEcologyLootRules.TryGetMapping(mob, out _);
		}

		private LootList GenerateDefaultLoot(GameNPC mob, GameObject killer)
		{
			LootList loot = base.GenerateLoot(mob, killer);
			loot.AddAll(m_rog.GenerateLoot(mob, killer));
			loot.AddAll(m_money.GenerateLoot(mob, killer));
			loot.AddAll(m_oneTimeDrop.GenerateLoot(mob, killer));
			loot.AddAll(m_template.GenerateLoot(mob, killer));
			return loot;
		}

		private static LootList GenerateTemplateLoot(string templateName, int dropCount, GameObject killer)
		{
			GamePlayer player = ResolveLootPlayer(killer);
			if (player == null || string.IsNullOrEmpty(templateName))
				return new LootList();

			if (!ZoneEcologyLootRules.TryGetTemplateRows(templateName, out List<DbLootTemplate> rows))
				return new LootList();

			LootList loot = new LootList(dropCount);
			foreach (DbLootTemplate row in rows)
			{
				DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(row.ItemTemplateID);
				if (item == null)
					continue;

				if (item.Realm != 0 && item.Realm != (int)player.Realm && !player.CanUseCrossRealmItems)
					continue;

				if (row.Chance == 100)
					loot.AddFixed(item, row.Count);
				else if (row.Chance > 0)
					loot.AddRandom(row.Chance, item, row.Count);
			}

			return loot;
		}

		private static LootList GenerateMaterialLoot(DbZoneEcologyLoot mapping, GameObject killer)
		{
			if (mapping == null || mapping.MaterialDropChance <= 0 || string.IsNullOrEmpty(mapping.MaterialLootTemplateName))
				return new LootList();

			if (!Util.Chance(mapping.MaterialDropChance))
				return new LootList();

			return GenerateTemplateLoot(mapping.MaterialLootTemplateName, mapping.MaterialDropCount, killer);
		}

		private static LootList GenerateNamedRogLoot(DbZoneEcologyLoot mapping, GameNPC mob, GameObject killer)
		{
			LootList loot = new LootList();
			if (mapping == null || !mapping.IsNamed || mapping.NamedRogChance <= 0)
				return loot;

			GamePlayer killerPlayer = ResolveKillerPlayer(killer);
			if (killerPlayer == null)
				return loot;

			int killedCon = killerPlayer.GetConLevel(mob);
			if (killedCon <= -3 || !Util.Chance(mapping.NamedRogChance))
				return loot;

			GamePlayer lootPlayer = killerPlayer.Group?.Leader ?? killerPlayer;
			eCharacterClass classForLoot = GetClassForLoot(lootPlayer);
			byte lootLevel = (byte)Math.Max(1, Math.Min(byte.MaxValue, mob.Level + 1));
			GeneratedUniqueItem item = AtlasROGManager.GenerateMonsterLootROG(lootPlayer.Realm, classForLoot, lootLevel, lootPlayer.CurrentZone?.IsOF ?? false);
			item.GenerateItemQuality(killedCon);
			item.MaxCount = 1;
			loot.AddFixed(item, 1);
			return loot;
		}

		private static eCharacterClass GetClassForLoot(GamePlayer player)
		{
			if (player == null)
				return eCharacterClass.Unknown;

			BattleGroup battlegroup = player.TempProperties.GetProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY);
			if (battlegroup != null)
			{
				List<eCharacterClass> battlegroupClasses = new List<eCharacterClass>();
				foreach (GamePlayer member in battlegroup.Members.Keys)
				{
					if (member == null || member.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
						continue;

					battlegroupClasses.Add((eCharacterClass)member.CharacterClass.ID);
				}

				if (battlegroupClasses.Count > 0)
					return battlegroupClasses[Util.Random(battlegroupClasses.Count - 1)];
			}

			if (player.Group != null)
			{
				List<eCharacterClass> groupClasses = new List<eCharacterClass>();
				foreach (GamePlayer member in player.Group.GetMembersInTheGroup())
				{
					if (member == null || member.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
						continue;

					groupClasses.Add((eCharacterClass)member.CharacterClass.ID);
				}

				if (groupClasses.Count > 0)
					return groupClasses[Util.Random(groupClasses.Count - 1)];
			}

			return (eCharacterClass)player.CharacterClass.ID;
		}

		private static GamePlayer ResolveLootPlayer(GameObject killer)
		{
			GamePlayer player = ResolveKillerPlayer(killer);
			if (player != null && player.Group != null)
				player = player.Group.Leader;

			return player;
		}

		private static GamePlayer ResolveKillerPlayer(GameObject killer)
		{
			GamePlayer player = killer as GamePlayer;
			if (player == null)
			{
				GameNPC npc = killer as GameNPC;
				IControlledBrain controlledBrain = npc != null ? npc.Brain as IControlledBrain : null;
				if (controlledBrain != null)
					player = controlledBrain.GetPlayerOwner();
			}

			return player;
		}
	}
}
