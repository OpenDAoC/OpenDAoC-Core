START TRANSACTION;

DELETE FROM loottemplate
WHERE TemplateName LIKE 'classic_ecology_%'
   OR LootTemplate_ID LIKE 'classic_ecology_%';

DELETE FROM itemtemplate
WHERE Id_nb LIKE 'classic_ecology_%'
   OR ItemTemplate_ID LIKE 'classic_ecology_%';

COMMIT;
