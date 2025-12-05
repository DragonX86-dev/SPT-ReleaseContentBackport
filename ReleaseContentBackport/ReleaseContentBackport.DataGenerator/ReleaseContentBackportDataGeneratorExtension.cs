using System.Reflection;
using System.Text;
using ReleaseContentBackport.DataGenerator.Globals;
using ReleaseContentBackport.DataGenerator.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
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
    // private readonly Dictionary<string, NewItemDetails> _modules = [];

    private readonly List<ModuleItem> _newItems = [];

    private readonly List<MongoId> _itemsCategoriesWhiteList =
    [
        "5a74651486f7744e73386dd1",
        "55818afb4bdc2dde698b456d",
        "55818b084bdc2d5b648b4571",
        "56ea9461d2720b67698b456f",
        "55818af64bdc2d5b648b4570",
        "550aa4bf4bdc2dd6348b456b",
        "550aa4dd4bdc2dc9348b4569",
        "550aa4cd4bdc2dd8348b456c",
        "55818add4bdc2d5b648b456f",
        "55818ad54bdc2ddc698b4569",
        "55818acf4bdc2dde698b456b",
        "55818ac54bdc2d5b648b456e",
        "55818ae44bdc2dde698b456c",
        "55818b164bdc2ddc698b456c",
        "55818a6f4bdc2db9688b456b",
        "55818b014bdc2ddc698b456b",
        "5448bc234bdc2d3c308b4569",
        "55818b224bdc2dde698b456f",
        "55818a594bdc2db9688b456a",
        "555ef6e44bdc2de9068b457e",
        "55818a104bdc2db9688b4569",
        "55818a684bdc2ddd698b456d",
        "55818a304bdc2db5418b457d",
        "5447b5fc4bdc2d87278b4567",
        "5447b5f14bdc2d61278b4567",
        "5447bedf4bdc2d87278b4568",
        "5447b6194bdc2d67278b4567",
        "5447bed64bdc2d97278b4568",
        "5447b5cf4bdc2d65278b4567",
        "5447b6094bdc2dc3278b4567",
        "617f1ef5e8b54b0998387733",
        "5447b5e04bdc2d62278b4567",
        "5447b6254bdc2dc3278b4568",
    ];

    // public Task OnLoad()
    // {
    //     var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
    //     _items = jsonUtil.DeserializeFromFile<Dictionary<MongoId, TemplateItem>>(
    //         Path.Combine(modPath, "data/items.json")
    //     )!;
    //
    //     var ak308WeaponModulesList = jsonUtil.DeserializeFromFile<ModuleItem[]>(
    //         Path.Combine(modPath, "data/ak308_modules_list.json"))!;
    //
    //     FindWeaponModulesRecursively(ak308WeaponModulesList);
    //

    //
    //     return Task.CompletedTask;
    // }

    public Task OnLoad()
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        foreach (var releaseItem in GlobalValues.ReleaseItems.ToArray())
        {
            if (!_itemsCategoriesWhiteList.Contains(releaseItem.Value.Parent))
            {
                continue;
            }

            var conflictingItems = (releaseItem.Value.Properties?.ConflictingItems ?? Enumerable.Empty<MongoId>())
                .Where(conflictingItem => !databaseServer.GetTables().Templates.Items.ContainsKey(conflictingItem))
                .ToList();

            var compatibleItems = (releaseItem.Value.Properties?.Slots ?? [])
                .SelectMany(slot => 
                    (slot.Properties?.Filters?.FirstOrDefault()?.Filter ?? [])
                    .Where(compatibleItemId => !databaseServer.GetTables().Templates.Items.ContainsKey(compatibleItemId))
                    .Select(compatibleItemId => new { SlotName = slot.Name!, CompatibleItemId = compatibleItemId })
                )
                .GroupBy(x => x.SlotName, x => x.CompatibleItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var newModuleItem = new ModuleItem
            {
                Id = releaseItem.Key,
                Name = releaseItem.Value.Name!,
                IsNew = true,
                ConflictingItems = conflictingItems.ToArray(),
                CompatibleItems = compatibleItems
            };

            if (!databaseServer.GetTables().Templates.Items.ContainsKey(releaseItem.Key))
            {
                _newItems.Add(newModuleItem);
            }
            else
            {
                if (conflictingItems.Count != 0 || compatibleItems.Keys.Count != 0)
                {
                    _newItems.Add(newModuleItem with { IsNew = false });
                }
            }
        }

        var modulesString = jsonUtil.Serialize(_newItems);
        File.WriteAllText(Path.Combine(modPath, "weapons_and_modules.json"), modulesString, Encoding.UTF8);

        return Task.CompletedTask;
    }

    // private void FindWeaponModulesRecursively(ModuleItem[] weaponModules)
    // {
    //     foreach (var weaponModule in weaponModules)
    //     {
    //         var item = _items[weaponModule.Id];
    //
    //         if (item.Name != weaponModule.Name)
    //         {
    //             continue;
    //         }
    //
    //         if (weaponModule.Childs.Length != 0)
    //         {
    //             FindWeaponModulesRecursively(weaponModule.Childs);
    //         }
    //
    //         if (_modules.ContainsKey(weaponModule.Id)) continue;
    //
    //         var price = GlobalValues.ItemPrices[weaponModule.Id];
    //         
    //         var handbookItem = databaseServer
    //             .GetTables()
    //             .Templates
    //             .Handbook
    //             .Items
    //             .FirstOrDefault(e => e.Id == new MongoId(item.Prototype));
    //
    //         if (handbookItem == null) continue;
    //
    //         _modules[weaponModule.Id] = new NewItemDetails
    //         {
    //             NewItem = item,
    //             FleaPriceRoubles = price,
    //             HandbookPriceRoubles = price,
    //             HandbookParentId = handbookItem.ParentId,
    //             Locales = new Dictionary<string, LocaleDetails>
    //             {
    //                 ["en"] = new()
    //                 {
    //                     Name = GlobalValues.EnItemLocales[weaponModule.Id].Name,
    //                     ShortName = GlobalValues.EnItemLocales[weaponModule.Id].ShortName,
    //                     Description = GlobalValues.EnItemLocales[weaponModule.Id].Description,
    //                 },
    //                 ["ru"] = new()
    //                 {
    //                     Name = GlobalValues.RuItemLocales[weaponModule.Id].Name,
    //                     ShortName = GlobalValues.RuItemLocales[weaponModule.Id].ShortName,
    //                     Description = GlobalValues.RuItemLocales[weaponModule.Id].Description,
    //                 }
    //             }
    //         };
    //     }
    // }
}