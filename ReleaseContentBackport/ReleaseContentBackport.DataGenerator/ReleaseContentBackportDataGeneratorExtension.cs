using System.Reflection;
using System.Text;
using ReleaseContentBackport.DataGenerator.Globals;
using ReleaseContentBackport.DataGenerator.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace ReleaseContentBackport.DataGenerator;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ReleaseContentBackportDataGeneratorExtension(
    ModHelper modHelper,
    JsonUtil jsonUtil,
    DatabaseServer databaseServer,
    ISptLogger<ReleaseContentBackportDataGeneratorExtension> logger) : IOnLoad
{
    private string _modPath = null!;
    
    private readonly List<ItemConfig> _items = [];

    public Task OnLoad()
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        GenerateItemsConfig();
        SaveWeaponModulesNewItemDetails();
        SaveModulesCompabilityConfig();
        // SaveAllItems();

        return Task.CompletedTask;
    }

    private void GenerateItemsConfig()
    {
        var categoriesWhiteList = modHelper.GetJsonDataFromFile<List<MongoId>>(
            _modPath, "data/categoriesWhiteList.json"
        );

        foreach (var releaseItem in GlobalValues.ReleaseItems)
        {
            if (!categoriesWhiteList.Contains(releaseItem.Value.Parent))
            {
                continue;
            }

            var conflictingItems = (releaseItem.Value.Properties?.ConflictingItems ?? Enumerable.Empty<MongoId>())
                .Where(conflictingItem => !databaseServer.GetTables().Templates.Items.ContainsKey(conflictingItem))
                .ToList();

            var compatibleItems = (releaseItem.Value.Properties?.Slots ?? [])
                .SelectMany(slot =>
                    (slot.Properties?.Filters?.FirstOrDefault()?.Filter ?? [])
                    .Where(compatibleItemId =>
                        !databaseServer.GetTables().Templates.Items.ContainsKey(compatibleItemId))
                    .Select(compatibleItemId => new { SlotName = slot.Name!, CompatibleItemId = compatibleItemId })
                )
                .GroupBy(x => x.SlotName, x => x.CompatibleItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var newModuleItem = new ItemConfig
            {
                Id = releaseItem.Key,
                Name = releaseItem.Value.Name!,
                ConflictingItems = conflictingItems,
                CompatibleItems = compatibleItems
            };

            if (databaseServer.GetTables().Templates.Items.ContainsKey(releaseItem.Key))
            {
                if (conflictingItems.Count != 0 || compatibleItems.Keys.Count != 0)
                {
                    _items.Add(newModuleItem with { IsNew = false });
                }
            }
            else
            {
                _items.Add(newModuleItem);
            }
        }
    }

    private void SaveWeaponModulesNewItemDetails()
    {
        var result = new List<NewItemDetails>();
        
        var weaponModules = _items.Where(e => e.IsNew && !e.Name.StartsWith("weapon_"));
        
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var weaponModule in weaponModules)
        {
            var item = GlobalValues.ReleaseItems[weaponModule.Id];
            if (item.Name != weaponModule.Name)
            {
                continue;
            }
    
            var price = GlobalValues.ItemPrices[weaponModule.Id];

            var handbookItem = databaseServer
                .GetTables()
                .Templates
                .Handbook
                .Items
                .FirstOrDefault(e => e.Id == new MongoId(item.Prototype));
            
            result.Add(new NewItemDetails
            {
                NewItem = item,
                FleaPriceRoubles = price,
                HandbookPriceRoubles = price,
                HandbookParentId = handbookItem != null ? handbookItem.ParentId : "",
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new()
                    {
                        Name = GlobalValues.EnItemLocales[weaponModule.Id].Name,
                        ShortName = GlobalValues.EnItemLocales[weaponModule.Id].ShortName,
                        Description = GlobalValues.EnItemLocales[weaponModule.Id].Description,
                    },
                    ["ru"] = new()
                    {
                        Name = GlobalValues.RuItemLocales[weaponModule.Id].Name,
                        ShortName = GlobalValues.RuItemLocales[weaponModule.Id].ShortName,
                        Description = GlobalValues.RuItemLocales[weaponModule.Id].Description,
                    }
                }
            });
        }
        
        var modulesJsonStr = jsonUtil.Serialize(result);
        File.WriteAllText(Path.Combine(_modPath, "modules.json"), modulesJsonStr, Encoding.UTF8);
    }

    private void SaveModulesCompabilityConfig()
    {
        var itemsConfig = jsonUtil.Serialize(_items.Where(e => !e.IsNew));
        File.WriteAllText(Path.Combine(_modPath, "itemsConfig.json"), itemsConfig, Encoding.UTF8);
    }
    
    private void SaveAllItems()
    {
        var itemsConfig = jsonUtil.Serialize(_items);
        File.WriteAllText(Path.Combine(_modPath, "allItems.json"), itemsConfig, Encoding.UTF8);
    }

    private void SaveItemsCategories()
    {
        var itemsCategoriesJsonStr = jsonUtil.Serialize(
            GlobalValues.ReleaseItems.Select(e => e.Value.Parent).Distinct()
        );
        File.WriteAllText(Path.Combine(_modPath, "itemsCategories.json"), itemsCategoriesJsonStr, Encoding.UTF8);
    }
}