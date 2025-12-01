using AutoMapper;
using PaymentAPI.DTOs.Responses;
using PaymentAPI.Model;
using PaymentAPI.Services.Interfaces;

namespace PaymentAPI.Mapping;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, PaymentResponse>();
        // CreateMap<PaymentHistory, PaymentHistoryResponse>();
    }
}
