using Shared.DTOs.Contracts.Responses;

namespace UtilityBillAPI.Service.Interface
{
    public interface IContractService
    {
        Task<ContractResponse?> GetContractAsync(Guid contractId);
    }
}
