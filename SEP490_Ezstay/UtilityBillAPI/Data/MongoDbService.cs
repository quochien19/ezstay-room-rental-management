using UtilityBillAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace UtilityBillAPI.Data;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    public MongoDbService( IOptions<MongoSettings> settings)
    {
        var clientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(settings.Value.DatabaseName);
      
    }

    public IMongoCollection<UtilityBill> UtilityBills => _database.GetCollection<UtilityBill>("UtilityBills");
    public IMongoCollection<UtilityBillDetail> UtilityBillDetails => _database.GetCollection<UtilityBillDetail>("UtilityBillDetails");
    public IMongoCollection<BillSetting> BillSettings => _database.GetCollection<BillSetting>("BillSettings");
}