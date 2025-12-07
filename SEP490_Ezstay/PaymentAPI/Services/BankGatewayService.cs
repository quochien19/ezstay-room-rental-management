using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using PaymentAPI.Model;
using PaymentAPI.Repository.Interface;
using PaymentAPI.Services.Interfaces;
using Shared.DTOs;
using Shared.DTOs.Payments.Responses;

namespace PaymentAPI.Services;

public class BankGatewayService: IBankGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly IBankGatewayRepository _bankGatewayRepository;
    private readonly IMapper _mapper;

    public BankGatewayService(HttpClient httpClient, IBankGatewayRepository bankGatewayRepository, IMapper mapper)
    {
        _httpClient = httpClient;
        _bankGatewayRepository = bankGatewayRepository;
        _mapper = mapper;
    }

    public async Task<List<BankGatewayResponse>> SyncFromVietQR()
    {
        var response = await _httpClient.GetStringAsync("https://api.vietqr.io/v2/banks");
        var json = JsonSerializer.Deserialize<JsonElement>(response);
        var list = json.GetProperty("data").EnumerateArray().Select(b => new BankGateway
        {
          //  BankCode = b.GetProperty("code").GetString(),
             FullName = b.GetProperty("name").GetString(),
             BankName = b.GetProperty("shortName").GetString(),
            Logo = b.GetProperty("logo").GetString(),
            IsActive =true
        }).ToList();
    
        var existing =  _bankGatewayRepository.GetAll();
        var newBanks = list.Where(b 
            => !existing.Any(e => e.BankName == b.BankName)).ToList();
    
        if (newBanks.Any())
            await _bankGatewayRepository.AddMany(newBanks);
        return  _mapper.Map<List<BankGatewayResponse>>(list);
    }
    public async Task<BankGatewayResponse> GetById(Guid id)
    {
        var result=   await _bankGatewayRepository.GetById(id); 
        return   _mapper.Map<BankGatewayResponse>(result);
    }

    public IQueryable<BankGatewayResponse>  GetAllBankGateway()
    {
        var bank = _bankGatewayRepository.GetAll();
        return bank.ProjectTo<BankGatewayResponse>(_mapper.ConfigurationProvider);
    }
    public IQueryable<BankGatewayResponse> GetAllActiveBankGateway()
    {
        return _bankGatewayRepository.GetAll()
            .Where(x => x.IsActive)
            .ProjectTo<BankGatewayResponse>(_mapper.ConfigurationProvider);
    }

    public async Task<ApiResponse<bool>> HiddenBankGateway(Guid id, bool isActive)
    {
        var bankGateway =await _bankGatewayRepository.GetById(id);
        if (bankGateway == null)
            throw new KeyNotFoundException("BankAccount not found");
      //  _mapper.Map(isActive, bankGateway);
        bankGateway.IsActive = isActive;
        bankGateway.UpdatedAt = DateTime.UtcNow;
       await _bankGatewayRepository.Update(bankGateway);
      return ApiResponse<bool>.Success(true , "Update bank gateway successfully ");
    }
}