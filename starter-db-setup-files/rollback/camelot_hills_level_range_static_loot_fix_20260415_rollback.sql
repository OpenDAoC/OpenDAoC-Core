-- Roll back the Camelot Hills level-range static loot fix.
--
-- This restores the pre-pass mapping behavior while leaving the broader
-- zone ecology table and material/named-mob systems in place.

START TRANSACTION;

DELETE FROM zoneecologyloot
WHERE RegionID = 1
  AND ZoneID = 0
  AND ZoneEcologyLoot_ID IN (
    'camelot_hills_zone0_large_skeleton_001_255',
    'camelot_hills_zone0_skeleton_004_255',
    'camelot_hills_zone0_bandit_005_255',
    'camelot_hills_zone0_cutpurse_006_255',
    'camelot_hills_zone0_young_cutpurse_006_255',
    'camelot_hills_zone0_poacher_006_255',
    'camelot_hills_zone0_young_poacher_006_255',
    'camelot_hills_zone0_puny_skeleton_006_255',
    'camelot_hills_zone0_moldy_skeleton_006_255'
  );

UPDATE zoneecologyloot
SET MinLevel = 0,
    MaxLevel = 255,
    LootTemplateName = CASE MobName
      WHEN 'convert guard' THEN 'camelot_hills_loot_humanoid_guard'
      WHEN 'Daffyd Difwych' THEN 'camelot_hills_loot_humanoid_guard'
      WHEN 'large skeleton' THEN 'camelot_hills_loot_large_skeleton'
      ELSE LootTemplateName
    END,
    LastTimeRowUpdated = NOW()
WHERE RegionID = 1 AND ZoneID = 0;

DELETE FROM loottemplate
WHERE TemplateName IN (
  'camelot_hills_loot_puny_skeleton_weathered',
  'camelot_hills_loot_skeleton_weathered',
  'camelot_hills_loot_large_skeleton_low',
  'camelot_hills_loot_moldy_skeleton',
  'camelot_hills_loot_cutpurse_adept',
  'camelot_hills_loot_poacher_adept',
  'camelot_hills_loot_bandit_veteran',
  'camelot_hills_loot_humanoid_guard_low',
  'camelot_hills_loot_humanoid_guard_captain'
);

UPDATE loottemplate
SET ItemTemplateID = 'bronze_hand_axe',
    LastTimeRowUpdated = NOW()
WHERE TemplateName = 'camelot_hills_loot_zombie_farmer'
  AND ItemTemplateID = 'iron_hand_axe';

UPDATE loottemplate
SET ItemTemplateID = CASE ItemTemplateID
    WHEN 'iron_short_sword2' THEN 'bronze_short_sword'
    WHEN 'iron_broadsword2' THEN 'bronze_broadsword'
    ELSE ItemTemplateID
  END,
  LastTimeRowUpdated = NOW()
WHERE TemplateName = 'camelot_hills_loot_bandit_henchman'
  AND ItemTemplateID IN ('iron_short_sword2', 'iron_broadsword2');

UPDATE loottemplate
SET ItemTemplateID = CASE ItemTemplateID
    WHEN 'iron_longsword' THEN 'bronze_broadsword'
    WHEN 'steel_round_shield' THEN 'medium_round_shield'
    ELSE ItemTemplateID
  END,
  LastTimeRowUpdated = NOW()
WHERE TemplateName = 'camelot_hills_loot_bandit_leader'
  AND ItemTemplateID IN ('iron_longsword', 'steel_round_shield');

UPDATE loottemplate
SET ItemTemplateID = 'bronze_short_sword',
    LastTimeRowUpdated = NOW()
WHERE TemplateName = 'camelot_hills_loot_named_humanoid'
  AND ItemTemplateID = 'iron_longsword';

UPDATE loottemplate
SET ItemTemplateID = 'medium_round_shield',
    LastTimeRowUpdated = NOW()
WHERE TemplateName = 'camelot_hills_loot_humanoid_guard'
  AND ItemTemplateID = 'steel_round_shield';

COMMIT;
