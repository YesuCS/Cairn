-- Cairn uninstall: remove event types built on the plugin's components and the
-- plugin's migration record. No tables ship with this plugin.
DELETE fet
FROM [FollowingEventType] fet
INNER JOIN [EntityType] et ON et.[Id] = fet.[EntityTypeId]
WHERE et.[Name] LIKE 'com.yesuchum.Cairn%';

DELETE FROM [PluginMigration] WHERE [PluginAssemblyName] LIKE 'com.yesuchum.Cairn%';
