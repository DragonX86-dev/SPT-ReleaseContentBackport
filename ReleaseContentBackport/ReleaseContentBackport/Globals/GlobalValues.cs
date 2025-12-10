using System.Reflection;
using ReleaseContentBackport.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace ReleaseContentBackport.Globals;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class GlobalValues(ModHelper modHelper) : IOnLoad
{
    public static NewItemDetails[] NewItemDetails { get; private set; } = null!;
    
    public static CustomTraderAssort[] TraderAssort { get; private set; } = null!;
    
    public static ItemConfig[] ItemConfigs { get; private set; } = null!;

    public Task OnLoad()
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        NewItemDetails = modHelper.GetJsonDataFromFile<NewItemDetails[]>(modPath, "data/modules.json");
        TraderAssort = modHelper.GetJsonDataFromFile<CustomTraderAssort[]>(modPath, "data/traderAssort.json");
        ItemConfigs = modHelper.GetJsonDataFromFile<ItemConfig[]>(modPath, "data/itemsConfig.json");

        return Task.CompletedTask;
    }
}