-- Cotswold NPC role-correction pass.
--
-- Scope:
--   Cotswold Village, Albion / Salisbury Plains only.
--   Does not move, add, or delete NPC spawns.
--
-- Notes:
--   Most existing Cotswold merchant ItemListID values are shared with NPCs
--   outside Cotswold. This migration creates Cotswold-specific lists for
--   broadened inventories so the role pass does not leak into other towns.
--
-- Idempotency:
--   Re-running this script recreates only the Cotswold_* merchant lists below
--   and reapplies direct updates to the stable Cotswold Mob_IDs.

START TRANSACTION;

DELETE FROM merchantitem
WHERE ItemListID IN (
  'Cotswold_GeneralWeapons',
  'Cotswold_HeavyArmor',
  'Cotswold_LightArmor',
  'Cotswold_ClothOutfitter',
  'Cotswold_CasterImplements',
  'Cotswold_ArcaneOutfitter',
  'Cotswold_DyesAll',
  'Cotswold_MilitiaSupplies',
  'Cotswold_InnProvisions'
);

-- One-handed slashing and thrusting weapons.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_GeneralWeapons',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_general_weapons_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'a64434b4-2954-4803-9e4a-190564428325'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbThrusting'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Chain and plate armor.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_HeavyArmor',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_heavy_armor_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbChain'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbPlateAlloy'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Studded and leather armor.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_LightArmor',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_light_armor_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '00c1e711-8d1b-4b72-8012-932d940f2567'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '0539eb7d-fd02-49b7-afc7-cc6f635177c4'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Quilted armor, robes, and cloaks.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_ClothOutfitter',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_cloth_outfitter_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '5b250180-4fee-4105-af88-0fdf2ef6da1d'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbClothRobes'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Albion caster staffs plus basic staffs.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_CasterImplements',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_caster_implements_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbSorcererStaffs'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbTheurgistStaffs'
      UNION ALL
      SELECT 30 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbWizardStaffs'
      UNION ALL
      SELECT 40 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbStaffs'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Wizard-facing goods: wizard staffs, quilted armor, robes, and cloaks.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_ArcaneOutfitter',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_arcane_outfitter_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbWizardStaffs'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '5b250180-4fee-4105-af88-0fdf2ef6da1d'
      UNION ALL
      SELECT 30 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbClothRobes'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Full Cotswold dye access through one broad list.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_DyesAll',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_dyes_all_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'ClothDye1'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'ClothDye2'
      UNION ALL
      SELECT 30 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'LeatherDye'
      UNION ALL
      SELECT 40 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'LeatherDye2'
      UNION ALL
      SELECT 50 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'EnamelDye1'
      UNION ALL
      SELECT 60 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'EnamelDye2'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Quartermaster stock for militia and travelers.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_MilitiaSupplies',
  numbered.ItemTemplateID,
  FLOOR((numbered.rn - 1) / 30),
  MOD(numbered.rn - 1, 30),
  NOW(),
  CONCAT('cotswold_militia_supplies_', LPAD(numbered.rn, 3, '0'))
FROM (
  SELECT
    deduped.ItemTemplateID,
    ROW_NUMBER() OVER (ORDER BY deduped.source_order, deduped.source_slot, deduped.ItemTemplateID) AS rn
  FROM (
    SELECT ItemTemplateID, MIN(source_order) AS source_order, MIN(source_slot) AS source_slot
    FROM (
      SELECT 10 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = 'AlbShields'
      UNION ALL
      SELECT 20 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '77793001'
      UNION ALL
      SELECT 30 AS source_order, PageNumber * 30 + SlotPosition AS source_slot, ItemTemplateID
      FROM merchantitem WHERE ItemListID = '4e30837a-8fe3-4f62-beb1-ebfdb76af4dc'
    ) source_items
    GROUP BY ItemTemplateID
  ) deduped
) numbered;

-- Tavern and road provisions for Jonathan Lee.
INSERT INTO merchantitem (ItemListID, ItemTemplateID, PageNumber, SlotPosition, LastTimeRowUpdated, MerchantItem_ID)
SELECT
  'Cotswold_InnProvisions',
  provisions.ItemTemplateID,
  0,
  provisions.rn - 1,
  NOW(),
  CONCAT('cotswold_inn_provisions_', LPAD(provisions.rn, 3, '0'))
FROM (
  SELECT 1 AS rn, 'spring_water' AS ItemTemplateID
  UNION ALL SELECT 2, 'stein_of_ale2'
  UNION ALL SELECT 3, 'goblet_of_wine2'
  UNION ALL SELECT 4, 'light_beer'
  UNION ALL SELECT 5, 'apple_cider'
  UNION ALL SELECT 6, 'honey_mead'
  UNION ALL SELECT 7, 'buttered_biscuit'
  UNION ALL SELECT 8, 'candied_apple'
  UNION ALL SELECT 9, 'roast_leg_of_lamb'
  UNION ALL SELECT 10, 'mixed_spring_greens'
  UNION ALL SELECT 11, 'stewed_cabbage'
  UNION ALL SELECT 12, 'Turkey_Leg'
) provisions
JOIN itemtemplate existing_items ON existing_items.Id_nb = provisions.ItemTemplateID;

-- Civic, service, trainer, and ambient roles.
UPDATE mob SET Guild = 'Villager', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '5df36a92-8007-4bf4-bab1-e4ef3036b79d'; -- Aery

UPDATE mob SET Guild = 'Villager', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'c6b0e92c-7c43-4a0e-beee-aea89002d2dd'; -- Andrew Wyatt

UPDATE mob SET Guild = 'Villager', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'a3235730-c658-436c-85ce-1ce03248efa9'; -- Cullen Smyth

UPDATE mob SET Guild = 'Traveler', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'f10b2af3-707f-48c4-8373-7d0d9b034cde'; -- Daniel Edwards

UPDATE mob SET Guild = 'Tavern Patron', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '5c49335f-c01b-4ba3-b61e-b1d905f213ed'; -- Drunk Man

UPDATE mob SET Guild = 'Villager', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '8c5ed29b-fe7b-4e50-b3fc-9dc9f982b5d0'; -- Godelava Dowden

UPDATE mob SET Guild = 'Shopper', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '0e4e95a7-ed86-4a91-8742-2c6021aca728'; -- Odelia Wyman

UPDATE mob SET Guild = 'Fae Researcher', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7aac31ac-318c-4d24-a0a9-e4db5a53afc6'; -- Master Kless

UPDATE mob SET Guild = 'Town Crier', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '0678529b-aaae-4ccd-ab39-42c5325fdb6f'; -- Pompin the Crier

UPDATE mob SET Guild = 'Stonemason', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'f2874e1b-a614-4114-9686-d79dfe812879'; -- Stonemason Glover

UPDATE mob SET Guild = 'Guard', LastTimeRowUpdated = NOW()
WHERE Mob_ID IN (
  '58d5404f-440a-406e-b81b-5dc766c84cf3',
  '92465ac6-e484-4c4c-a7fd-1939a8df536b'
); -- Veteran Guardsmen

-- Ranged craft.
UPDATE mob SET Guild = 'Bowyer', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7bba2d9e-1ce8-4956-95f9-bd68118f321c'; -- Grum Bowman

UPDATE mob SET Guild = 'Fletcher', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '70f38415-9242-4a50-86c5-3f9bfd7b51ac'; -- Braenwyn Fletcher

UPDATE mob SET Guild = 'Crossbowyer', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '8ad04224-b98c-43da-b3f5-f680010a0abd'; -- Pellam

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Fletcher''s Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '56e5734a-cf24-4cf1-94f7-c0b69603192f'; -- Yetta Fletcher

-- Arms, armor, and militia supply.
UPDATE mob SET
  Guild = 'General Weapons',
  ItemsListTemplateID = 'Cotswold_GeneralWeapons',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '2286d8df-c6e1-4da2-a137-3defc743b6d7'; -- John Weyland

UPDATE mob SET Guild = 'Heavy Weapons', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'c6810dec-d456-482e-ae17-7169c38e4449'; -- Bedamor Routh

UPDATE mob SET Guild = 'Polearms Merchant', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '2564d8b4-8e2a-4968-aaa6-aaae3ccb08d2'; -- Rayn Olwyc

UPDATE mob SET Guild = 'Shields Merchant', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '884f90c0-557b-4aee-afdb-fadfe96b7000'; -- Lar Rodor

UPDATE mob SET
  Guild = 'Heavy Armor',
  ItemsListTemplateID = 'Cotswold_HeavyArmor',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '711b0dd8-4420-4a06-9fd2-8ed018b1acbf'; -- Gill Hoxley

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Militia Armorer',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '0c5aaf1c-dcb0-47d6-b39d-ff83375c4fed'; -- Col Aldar

UPDATE mob SET
  Guild = 'Light Armor',
  ItemsListTemplateID = 'Cotswold_LightArmor',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'f7483247-e1f8-4dcf-825a-f1d7f282571c'; -- Ellyn Weyland

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Leatherworker',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'c7f97a25-d411-4fe5-9485-5c03a2fc1dc3'; -- Lundeg Tranyth

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Outfitter''s Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'acff5460-6474-4cee-80af-271ec0a09742'; -- Farma Hornly

UPDATE mob SET
  Guild = 'Quartermaster',
  ItemsListTemplateID = 'Cotswold_MilitiaSupplies',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '4cfafc13-ac56-4fa0-a435-cb804435fa84'; -- Samwell Hornly

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Quartermaster''s Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '96df1737-2512-479b-b75a-254715483a0f'; -- Grannis Ynos

-- Arcane, cloth, and dye domain.
UPDATE mob SET
  Guild = 'Arcane Outfitter',
  ItemsListTemplateID = 'Cotswold_ArcaneOutfitter',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7c3ec2f1-61c6-4ae4-8dc7-efc3d0ee9c28'; -- Doreen Egesa

UPDATE mob SET
  Guild = 'Arcane Implements',
  ItemsListTemplateID = 'Cotswold_CasterImplements',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'e74efd8b-9353-4a78-b2e6-ef71059734ed'; -- Cauldir Edyn

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Arcane Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'd67c333a-1a3e-4f86-9d95-acbecec9b61a'; -- Cudbert Dalston

UPDATE mob SET
  Guild = 'Clothier',
  ItemsListTemplateID = 'Cotswold_ClothOutfitter',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '04554437-22a9-40e0-bb93-cb8bdb711b36'; -- Jon Smythe

UPDATE mob SET
  Guild = 'Dyemaster',
  ItemsListTemplateID = 'Cotswold_DyesAll',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '8c6ac9e2-0b11-4d2a-a310-b67275c81cb8'; -- Dyemaster Alwin

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Dyer''s Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '73dbbcff-0427-407f-a9da-3ffd6cb9dc87'; -- Dyemaster Edra

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Leather Dyer',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'cf93968c-37db-42b1-a312-21af0082812e'; -- Dyemaster Leax

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Enamel Stockkeeper',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '785b4e1d-0ff3-48e0-8ffd-ef7a603fa3d0'; -- Dyemaster Octe

UPDATE mob SET
  ClassType = 'DOL.GS.GameNPC',
  Guild = 'Dyer''s Assistant',
  ItemsListTemplateID = NULL,
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '2fe077d1-7f4f-478f-a0bf-5fb948ecfa21'; -- Dyemaster Wanetta

UPDATE mob SET
  Guild = 'Dyer',
  ItemsListTemplateID = 'Cotswold_DyesAll',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '8d3826b3-e8ea-4708-ba76-fc2eeabedf91'; -- Eowyln Astos

-- Village support, discreet support, and music.
UPDATE mob SET
  Guild = 'Innkeeper',
  ItemsListTemplateID = 'Cotswold_InnProvisions',
  LastTimeRowUpdated = NOW()
WHERE Mob_ID = '413c2ada-9fc7-4b8a-b061-993d249a6860'; -- Jonathan Lee

UPDATE mob SET Guild = 'Rogue Supplier', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7190c839-f976-4b71-9425-b960f8f2334f'; -- Unendaldan

UPDATE mob SET Guild = 'Instrument Merchant', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '666be9c5-71c0-495a-a83c-cbb604c5d0b4'; -- Ydenia Philpott

COMMIT;
