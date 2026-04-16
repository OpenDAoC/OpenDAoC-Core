param(
    [string]$Container = "opendaoc-db",
    [string]$Database = "opendaoc",
    [string]$User = "root",
    [string]$Password = "my-secret-pw"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..\..")
$cleanupSql = Join-Path $scriptDir "cotswold_loot_economy_20260415_cleanup.sql"
$roleSeedSql = Join-Path $repoRoot "zz_cotswold_role_correction_20260414.sql"

function Invoke-MysqlFile {
    param([string]$Path)

    Get-Content -Raw -Path $Path | docker exec -i $Container mysql "-u$User" "-p$Password" -D $Database
}

Invoke-MysqlFile $cleanupSql
Invoke-MysqlFile $roleSeedSql

$restoreSharedMerchantAssignments = @"
START TRANSACTION;

UPDATE mob SET ItemsListTemplateID = '7a411b91-e85e-42e7-be42-412fcfd272f5', LastTimeRowUpdated = NOW()
WHERE Mob_ID = 'c6810dec-d456-482e-ae17-7169c38e4449';

UPDATE mob SET ItemsListTemplateID = '93cd2d04-d5db-4754-b0b8-c976e0b65960', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '2564d8b4-8e2a-4968-aaa6-aaae3ccb08d2';

UPDATE mob SET ItemsListTemplateID = 'AlbBows', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7bba2d9e-1ce8-4956-95f9-bd68118f321c';

UPDATE mob SET ItemsListTemplateID = '77793001', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '70f38415-9242-4a50-86c5-3f9bfd7b51ac';

UPDATE mob SET ItemsListTemplateID = 'AlbXBows', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '8ad04224-b98c-43da-b3f5-f680010a0abd';

UPDATE mob SET ItemsListTemplateID = 'AlbShields', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '884f90c0-557b-4aee-afdb-fadfe96b7000';

UPDATE mob SET ItemsListTemplateID = '9704f11f-6082-47b6-bd9e-99fc72f27237', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '7190c839-f976-4b71-9425-b960f8f2334f';

UPDATE mob SET ItemsListTemplateID = '54b8e407-8ceb-4246-90f2-6c668cb7dfb8', LastTimeRowUpdated = NOW()
WHERE Mob_ID = '666be9c5-71c0-495a-a83c-cbb604c5d0b4';

DELETE FROM merchantitem
WHERE ItemListID IN (
  'Cotswold_HeavyWeapons',
  'Cotswold_Polearms',
  'Cotswold_Bows',
  'Cotswold_Arrows',
  'Cotswold_Crossbows',
  'Cotswold_Shields',
  'Cotswold_RogueSupplies',
  'Cotswold_Instruments'
);

COMMIT;
"@

$restoreSharedMerchantAssignments | docker exec -i $Container mysql "-u$User" "-p$Password" -D $Database

Write-Host "Rolled back Cotswold economy and Camelot Hills ecology DB changes, then replayed the prior Cotswold role seed."
Write-Host "Code-level rollback is a normal VCS revert of DbZoneEcologyLoot.cs, LootGeneratorZoneEcology.cs, ILootGenerator.cs, LootMgr.cs, Dockerfile, and the 20260415 seed files."
