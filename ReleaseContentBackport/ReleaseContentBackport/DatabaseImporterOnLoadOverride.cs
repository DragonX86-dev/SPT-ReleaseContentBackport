using System.Runtime.CompilerServices;
using ReleaseContentBackport.Globals;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace ReleaseContentBackport;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.Database)]
public class DatabaseImporterOnLoadOverride(
    ISptLogger<DatabaseImporter> logger,
    FileUtil fileUtil,
    ServerLocalisationService serverLocalisationService,
    DatabaseServer databaseServer,
    ImageRouter imageRouter,
    ImporterUtil importerUtil,
    JsonUtil jsonUtil)
    : DatabaseImporter(logger, fileUtil, serverLocalisationService, databaseServer, imageRouter, importerUtil, jsonUtil)
{
    private readonly DatabaseServer _databaseServer = databaseServer;
    private readonly ImporterUtil _importerUtil = importerUtil;

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_TableData")]
    private static extern void SetTableData(DatabaseServer @this, DatabaseTables tableData);

    public override async Task OnLoad()
    {
        var dataToImport = await _importerUtil.LoadRecursiveAsync<DatabaseTables>("./SPT_Data/database/");
        
        foreach (var traderAssort in GlobalValues.TraderAssort)
        {
            var itemId = traderAssort.Item.Id;
            var trader = dataToImport.Traders[traderAssort.TraderId];
            trader.Base.ItemsSell![$"{traderAssort.LoyaltyLevel}"].IdList.Add(itemId);
        }
        
        SetTableData(_databaseServer, dataToImport);
    }
}
