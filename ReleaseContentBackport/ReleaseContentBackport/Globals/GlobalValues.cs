using System.Reflection;
using ReleaseContentBackport.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace ReleaseContentBackport.Globals;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class GlobalValues(ModHelper modHelper) : IOnLoad
{
    public static NewItemDetails[] NewItemDetails { get; private set; } = null!;
    
    public static CustomTraderAssort[] TraderAssort { get; private set; } = null!;
    
    public static Dictionary<MongoId, Preset> ItemPresets { get; private set; } = null!;
    
    public static ItemConfig[] ItemConfigs { get; private set; } = null!;

    public Task OnLoad()
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        NewItemDetails = modHelper.GetJsonDataFromFile<NewItemDetails[]>(modPath, "data/newItemDetails.json");
        TraderAssort = modHelper.GetJsonDataFromFile<CustomTraderAssort[]>(modPath, "data/traderAssort.json");
        ItemPresets = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Preset>>(modPath, "data/itemPresets.json");
        ItemConfigs = modHelper.GetJsonDataFromFile<ItemConfig[]>(modPath, "data/itemsConfig.json");

        return Task.CompletedTask;
    }
}