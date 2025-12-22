using System.Reflection;
using System.Text;
using ReleaseContentBackport.DataGenerator.Globals;
using ReleaseContentBackport.DataGenerator.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace ReleaseContentBackport.DataGenerator;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public class ReleaseContentBackportDataGeneratorExtension(
    ModHelper modHelper,
    JsonUtil jsonUtil,
    DatabaseServer databaseServer,
    ISptLogger<ReleaseContentBackportDataGeneratorExtension> logger) : IOnLoad
{
    private string _modPath = null!;

    public Task OnLoad()
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        GenerateAndSaveNewItemDetails();
        // GenerateAndSaveItemsConfig();
        // GenerateAndSaveModulesTraderAssort();
        SaveNewItemsAssetsPaths();

        return Task.CompletedTask;
    }

    private void GenerateAndSaveNewItemDetails()
    {
        var items = GenerateNewItemDetails(
            GlobalValues.ModuleCategories.Concat(GlobalValues.WeaponCategories).ToList()
        );

        File.WriteAllText(
            Path.Combine(_modPath, "newItemDetails.json"),
            jsonUtil.Serialize(items),
            Encoding.UTF8
        );
    }

    private void GenerateAndSaveItemsConfig()
    {
        var itemsConfig = GenerateItemsConfig(
            GlobalValues.ModuleCategories.Concat(GlobalValues.WeaponCategories).ToList()
        );
        
        File.WriteAllText(
            Path.Combine(_modPath, "itemsConfig.json"),
            jsonUtil.Serialize(itemsConfig),
            Encoding.UTF8
        );
    }

    private void GenerateAndSaveModulesTraderAssort()
    {
        var traderConfigs = modHelper.GetJsonDataFromFile<TraderConfig[]>(
            _modPath, "data/trader_config.json"
        );

        var weaponModuleItems = GenerateNewItemDetails(
            GlobalValues.ModuleCategories
        ).Select(e => e.NewItem!.Id).ToList();

        var traderAssortItems = new List<TraderAssortItem>();
        foreach (var traderConfig in traderConfigs)
        {
            traderAssortItems.AddRange(
                from cashOffer in traderConfig.CashOffers
                where weaponModuleItems.Contains(cashOffer.Item.Id)
                select new TraderAssortItem
                {
                    TraderId = traderConfig.Id,
                    Item = new Item
                    {
                        Id = new MongoId(),
                        Template = cashOffer.Item.Id,
                        ParentId = "hideout",
                        SlotId = "hideout",
                        Upd = new Upd
                        {
                            UnlimitedCount = true, 
                            StackObjectsCount = 9999999, 
                            BuyRestrictionMax = cashOffer.BuyLimit,
                            BuyRestrictionCurrent = 0
                        }
                    },
                    BarterScheme = [new BarterScheme
                    {
                        Count = cashOffer.Price, 
                        Template = cashOffer.CurrencyItem.Id
                    }],
                    LoyaltyLevel = cashOffer.Level,
                    SubItems = []
                }
            );

            traderAssortItems.AddRange(
                from traderBarter in traderConfig.Barters
                where MongoId.IsValidMongoId(traderBarter.RewardItems.First().Item.Id)
                      && weaponModuleItems.Contains(traderBarter.RewardItems.First().Item.Id)
                select new TraderAssortItem
                {
                    TraderId = traderConfig.Id,
                    Item = new Item
                    {
                        Id = new MongoId(),
                        Template = traderBarter.RewardItems.First().Item.Id,
                        ParentId = "hideout",
                        SlotId = "hideout",
                        Upd = new Upd
                        {
                            UnlimitedCount = true,
                            StackObjectsCount = 9999999,
                            BuyRestrictionMax = traderBarter.BuyLimit, 
                            BuyRestrictionCurrent = 0
                        }
                    },
                    BarterScheme = traderBarter.RequiredItems
                        .Select(item => new BarterScheme
                        {
                            Count = Math.Round(item.Count), 
                            Template = item.Item.Id
                        })
                        .ToList(),
                    LoyaltyLevel = traderBarter.Level,
                    SubItems = []
                }
            );
        }
        
        File.WriteAllText(
            Path.Combine(_modPath, "traderAssortItems.json"),
            jsonUtil.Serialize(traderAssortItems),
            Encoding.UTF8
        );
    }

    private List<NewItemDetails> GenerateNewItemDetails(List<MongoId> categoriesWhitelist)
    {
        return (
            from releaseItem in GlobalValues.ReleaseItems
            where !databaseServer.GetTables().Templates.Items.ContainsKey(releaseItem.Key) &&
                  categoriesWhitelist.Contains(releaseItem.Value.Parent)
            let price = GlobalValues.ItemPrices[releaseItem.Value.Id]
            let handbookItem = databaseServer.GetTables()
                .Templates
                .Handbook
                .Items
                .FirstOrDefault(e => e.Id == new MongoId(releaseItem.Value.Prototype))
            select new NewItemDetails
            {
                NewItem = releaseItem.Value,
                FleaPriceRoubles = price,
                HandbookPriceRoubles = price,
                HandbookParentId = handbookItem != null ? handbookItem.ParentId : "",
                Locales = new Dictionary<string, LocaleDetails>
                {
                    ["en"] = new()
                    {
                        Name = GlobalValues.EnItemLocales[releaseItem.Value.Id].Name,
                        ShortName = GlobalValues.EnItemLocales[releaseItem.Value.Id].ShortName,
                        Description = GlobalValues.EnItemLocales[releaseItem.Value.Id].Description,
                    },
                    ["ru"] = new()
                    {
                        Name = GlobalValues.RuItemLocales[releaseItem.Value.Id].Name,
                        ShortName = GlobalValues.RuItemLocales[releaseItem.Value.Id].ShortName,
                        Description = GlobalValues.RuItemLocales[releaseItem.Value.Id].Description,
                    }
                }
            }
        ).ToList();
    }

    private List<ItemConfig> GenerateItemsConfig(List<MongoId> categoriesWhitelist)
    {
        return (from releaseItem in GlobalValues.ReleaseItems
            where databaseServer.GetTables().Templates.Items.ContainsKey(releaseItem.Key)
                  && categoriesWhitelist.Contains(releaseItem.Value.Parent)
            let conflictingItems =
                (releaseItem.Value.Properties?.ConflictingItems ?? Enumerable.Empty<MongoId>())
                .Where(conflictingItem => !databaseServer.GetTables().Templates.Items.ContainsKey(conflictingItem))
                .ToList()
            let compatibleItems = (releaseItem.Value.Properties?.Slots ?? [])
                .SelectMany(slot => (slot.Properties?.Filters?.FirstOrDefault()?.Filter ?? [])
                    .Where(compatibleItemId =>
                        !databaseServer.GetTables().Templates.Items.ContainsKey(compatibleItemId))
                    .Select(compatibleItemId => new { SlotName = slot.Name!, CompatibleItemId = compatibleItemId }))
                .GroupBy(x => x.SlotName, x => x.CompatibleItemId)
                .ToDictionary(g => g.Key, g => g.ToList())
            let itemConfig = new ItemConfig
            {
                Id = releaseItem.Key,
                Name = releaseItem.Value.Name!,
                ConflictingItems = conflictingItems,
                CompatibleItems = compatibleItems
            }
            where conflictingItems.Count != 0 || compatibleItems.Keys.Count != 0
            select itemConfig).ToList();
    }

    private void SaveNewItemsAssetsPaths()
    {
        var newItems = GenerateNewItemDetails(
            GlobalValues.ModuleCategories.Concat(GlobalValues.WeaponCategories).ToList());

        var assetsPaths = newItems
            .Select(e => e.NewItem!.Id)
            .Select(newItemId => GlobalValues.ReleaseItems[newItemId].Properties!.Prefab!.Path!)
            .ToList();

        File.WriteAllText(
            Path.Combine(_modPath, "assets_paths.json"), 
            jsonUtil.Serialize(assetsPaths), 
            Encoding.UTF8
        );
    }
}