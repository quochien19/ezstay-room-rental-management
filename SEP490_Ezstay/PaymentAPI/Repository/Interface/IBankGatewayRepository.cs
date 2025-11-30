using PaymentAPI.Model;

namespace PaymentAPI.Repository.Interface;

public interface IBankGatewayRepository
{
    Task<BankGateway> GetById(Guid id);
    Task<BankGateway?> GetByIdAsync(Guid id);
    IQueryable<BankGateway> GetAll();
    Task Update(BankGateway bankGateway);
    Task AddMany(IEnumerable<BankGateway> bankGateway);
    IQueryable<BankGateway> GetAllActiveBankGateway();
}