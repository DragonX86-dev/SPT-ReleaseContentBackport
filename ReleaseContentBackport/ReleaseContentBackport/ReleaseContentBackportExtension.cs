using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace ReleaseContentBackport;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ReleaseContentBackportExtension(
    ModHelper modHelper,
    JsonUtil jsonUtil,
    DatabaseServer databaseServer,
    CustomItemService customItemService,
    ISptLogger<ReleaseContentBackportExtension> logger) : IOnLoad
{
    private string _pathToMod = null!;
    
    public Task OnLoad()
    {
        _pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var items = jsonUtil.DeserializeFromFile<Dictionary<MongoId, TemplateItem>>(
            Path.Combine(_pathToMod, "data/items.json")
            )!;

        foreach (var item in items)
        {
            logger.LogWithColor(item.Key, LogTextColor.Magenta);
        }
        
        // AddItemsToDatabase(new List<string> {"data", "ammo.json"}.CombinePaths());
        // logger.LogWithColor("[ReleaseContentBackport] Loaded new ammunition.", LogTextColor.Green);
        //
        // AddItemsToDatabase(new List<string> {"data", "ammo_boxes.json"}.CombinePaths());
        // logger.LogWithColor("[ReleaseContentBackport] Loaded new ammunition boxes.", LogTextColor.Green);
        //
        // AddGlobalPresetsToDatabase(new List<string> {"data", "item_presets.json"}.CombinePaths());
        // logger.LogWithColor("[ReleaseContentBackport] Loaded item presets.", LogTextColor.Green);
        
        return Task.CompletedTask;
    }

    private void AddItemsToDatabase(string dataFilePath)
    {
        var items = modHelper.GetJsonDataFromFile<NewItemDetails[]>(_pathToMod, dataFilePath);

        foreach (var item in items)
        {
            customItemService.CreateItem(item);
        }
    }

    private void AddGlobalPresetsToDatabase(string dataFilePath)
    {
        var itemPresets = modHelper.GetJsonDataFromFile<Preset[]>(_pathToMod, dataFilePath);

        foreach (var preset in itemPresets)
        {
            databaseServer.GetTables().Globals.ItemPresets[preset.Id] = preset;
        }
    }
}