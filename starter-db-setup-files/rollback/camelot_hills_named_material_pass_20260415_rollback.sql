-- Roll back the Camelot Hills named-mob and crafting-material pass.
--
-- This leaves the broader Camelot Hills ecology loot pass in place, but disables
-- the new material drops and named-mob reward modifiers added on 2026-04-15.

START TRANSACTION;

UPDATE zoneecologyloot
SET Archetype = '',
    MaterialLootTemplateName = '',
    MaterialDropChance = 0,
    MaterialDropCount = 1,
    IsNamed = 0,
    NamedXpMultiplier = 1,
    NamedXpCapMultiplier = 1,
    NamedRogChance = 0,
    LastTimeRowUpdated = NOW()
WHERE RegionID = 1 AND ZoneID = 0;

DELETE FROM loottemplate
WHERE TemplateName LIKE 'camelot_hills_material_%'
   OR LootTemplate_ID LIKE 'camelot_hills_material_20260415_%';

COMMIT;
