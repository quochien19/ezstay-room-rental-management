using MongoDB.Driver;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;

namespace PaymentAPI.Repository;

public class BankGatewayRepository : IBankGatewayRepository
{
    private readonly IMongoCollection<BankGateway> _bankGateways;

    public BankGatewayRepository(IMongoDatabase database)
    {
        _bankGateways = database.GetCollection<BankGateway>("BankGateways");
    }
    public async Task<BankGateway> GetById(Guid id)
    {
        return await _bankGateways.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task<BankGateway?> GetByIdAsync(Guid id)
    {
        return await _bankGateways.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task AddMany(IEnumerable<BankGateway> gateways)
    {
        await _bankGateways.InsertManyAsync(gateways);
    }
    public async Task Update(BankGateway bankGateway)
    {
        await _bankGateways.ReplaceOneAsync(a => a.Id == bankGateway.Id, bankGateway);
    }
    public IQueryable<BankGateway> GetAllActiveBankGateway()
    {
        return _bankGateways.AsQueryable()
            .Where(x => x.IsActive);
    }
    
    public IQueryable<BankGateway> GetAll()
    {
        return _bankGateways.AsQueryable();
    }
}