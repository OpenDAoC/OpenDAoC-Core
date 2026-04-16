-- Cleanup portion of the Cotswold economy and Camelot Hills ecology rollback.
-- Use rollback_cotswold_loot_economy_20260415.ps1 from this directory so
-- the prior Cotswold role seed is replayed after this cleanup.

START TRANSACTION;

DELETE FROM lootgenerator
WHERE LootGenerator_ID IN ('cotswold_ecology_20260415', 'zone_ecology_20260415');

DELETE FROM zoneecologyloot
WHERE RegionID = 1 AND ZoneID = 0;

DELETE FROM loottemplate
WHERE TemplateName LIKE 'cotswold_loot_%'
   OR LootTemplate_ID LIKE 'cotswold_loot_20260415_%'
   OR TemplateName LIKE 'camelot_hills_loot_%'
   OR LootTemplate_ID LIKE 'camelot_hills_loot_20260415_%';

DELETE FROM itemtemplate
WHERE Id_nb LIKE 'camelot_hills_%';

DELETE FROM merchantitem
WHERE ItemListID IN (
  'Cotswold_GeneralWeapons',
  'Cotswold_HeavyWeapons',
  'Cotswold_Polearms',
  'Cotswold_Bows',
  'Cotswold_Arrows',
  'Cotswold_Crossbows',
  'Cotswold_Shields',
  'Cotswold_HeavyArmor',
  'Cotswold_LightArmor',
  'Cotswold_ClothOutfitter',
  'Cotswold_CasterImplements',
  'Cotswold_ArcaneOutfitter',
  'Cotswold_MilitiaSupplies',
  'Cotswold_RogueSupplies',
  'Cotswold_Instruments'
);

COMMIT;
