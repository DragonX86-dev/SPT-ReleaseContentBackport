using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;

namespace ReleaseContentBackport;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ReleaseContentBackportExtension(
    JsonUtil jsonUtil,
    ModHelper modHelper,
    DatabaseServer databaseServer,
    CustomItemService customItemService,
    ISptLogger<ReleaseContentBackportExtension> logger) : IOnLoad
{
    private string _pathToMod = null!;
    
    public Task OnLoad()
    {
        _pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
        AddItemsToDatabase(new List<string> {"db", "ammo.json"}.CombinePaths());
        AddItemsToDatabase(new List<string> {"db", "ammoboxes.json"}.CombinePaths());
        
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
}