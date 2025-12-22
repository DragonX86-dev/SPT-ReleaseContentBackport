using System.Reflection;
using ReleaseContentBackport.Globals;
using ReleaseContentBackport.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Mod;

namespace ReleaseContentBackport;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public class ReleaseContentBackportExtension(
    ModHelper modHelper,
    DatabaseServer databaseServer,
    CustomItemService customItemService,
    ISptLogger<ReleaseContentBackportExtension> logger) : IOnLoad
{
    private string _pathToMod = null!;
    
    public Task OnLoad()
    {
        _pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var config = modHelper.GetJsonDataFromFile<ModConfig>(_pathToMod, "config.json");

        if (config.RefSellsGpCoinEnable)
        {
            AddGpCoinToRefAssortment();
        }

        AddNewWeaponModulesToDatabase();
        LoadTraderAssort();
        LoadItemsPresets();
        LoadItemsConfig();
        
        logger.LogWithColor("[ReleaseContentBackport] The mod is loaded");
        
        return Task.CompletedTask;
    }

    private void AddGpCoinToRefAssortment()
    {
        var itemId = new MongoId();
        var refTrader = databaseServer.GetTables().Traders[Traders.REF];
        
        refTrader.Assort.Items.Add(new Item
        {
            Id = itemId,
            Template = ItemTpl.MONEY_GP_COIN,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999
            }
        });

        refTrader.Assort.LoyalLevelItems[itemId] = 1;
        refTrader.Assort.BarterScheme[itemId] = [
            [
                new BarterScheme
                {
                    Count = 7500,
                    Template = ItemTpl.MONEY_ROUBLES
                }
            ]
        ];
    }

    private void AddNewWeaponModulesToDatabase()
    {
        foreach (var item in GlobalValues.NewItemDetails)
        {
            customItemService.CreateItem(item);
        }
    }

    private void LoadTraderAssort()
    {
        foreach (var traderAssort in GlobalValues.TraderAssort)
        {
            var itemId = traderAssort.Item.Id;
            var trader = databaseServer.GetTables().Traders[traderAssort.TraderId];
            trader.Base.ItemsSell![$"{traderAssort.LoyaltyLevel}"].IdList.Add(traderAssort.Item.Template);

            trader.Assort.Items.Add(traderAssort.Item);
            trader.Assort.LoyalLevelItems[itemId] = traderAssort.LoyaltyLevel;
            trader.Assort.BarterScheme[itemId] = [traderAssort.BarterScheme];
        }
    }

    private void LoadItemsPresets()
    {
        foreach (var (key, value) in GlobalValues.ItemPresets)
        {
            databaseServer.GetTables().Globals.ItemPresets[key] = value;
        }
    }

    private void LoadItemsConfig()
    {
        foreach (var itemConfig in GlobalValues.ItemConfigs)
        {
            var existItem = databaseServer.GetTables().Templates.Items[itemConfig.Id];
            
            itemConfig.ConflictingItems.ForEach(conflictingItemId =>
            {
                existItem.Properties?.ConflictingItems?.Add(conflictingItemId);
            });

            foreach (var (modName, newItemIds) in itemConfig.CompatibleItems)
            {
                newItemIds.ForEach(newItemId =>
                {
                    var item = databaseServer.GetTables().Templates.Items[itemConfig.Id];
                    var slot = item.Properties?.Slots?.FirstOrDefault(e => e.Name == modName);
                    
                    if (slot != null)
                    {
                        slot.Properties?.Filters?.First().Filter?.Add(newItemId);
                    }
                });
            }
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