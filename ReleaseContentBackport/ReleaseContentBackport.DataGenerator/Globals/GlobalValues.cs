using System.Reflection;
using ReleaseContentBackport.DataGenerator.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace ReleaseContentBackport.DataGenerator.Globals;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader)]
public class GlobalValues(ModHelper modHelper) : IOnLoad
{
    public static Dictionary<MongoId, int> ItemPrices { get; private set; } = null!;
    
    public static Dictionary<MongoId, TemplateItem> ReleaseItems { get; private set; } = null!;
    
    public static List<MongoId> ModuleCategories { get; private set; } = null!;
    
    public static List<MongoId> WeaponCategories { get; private set; } = null!;

    public static Dictionary<MongoId, ItemLocale> EnItemLocales { get; private set; } = null!;
   
    public static Dictionary<MongoId, ItemLocale> RuItemLocales { get; private set; } = null!;

    public Task OnLoad()
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        ItemPrices = modHelper.GetJsonDataFromFile<Dictionary<MongoId, int>>(modPath, "data/items-prices.json");
        ReleaseItems = modHelper.GetJsonDataFromFile<Dictionary<MongoId, TemplateItem>>(modPath, "data/items.json");

        ModuleCategories = modHelper.GetJsonDataFromFile<List<MongoId>>(modPath, "data/categories/moduleCategories.json");
        WeaponCategories = modHelper.GetJsonDataFromFile<List<MongoId>>(modPath, "data/categories/weaponCategories.json");
        
        EnItemLocales = modHelper.GetJsonDataFromFile<Dictionary<MongoId, ItemLocale>>(modPath, "data/locales/en.json");
        RuItemLocales = modHelper.GetJsonDataFromFile<Dictionary<MongoId, ItemLocale>>(modPath, "data/locales/ru.json");   

        return Task.CompletedTask;
    }
}