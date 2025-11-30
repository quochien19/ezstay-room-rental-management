using AutoMapper;
using UtilityBillAPI.DTO;
using UtilityBillAPI.Models;

namespace UtilityBillAPI.Profiles
{
    public class UtilityBillProfile : Profile
    {
        public UtilityBillProfile()
        {
            CreateMap<UtilityBill, UtilityBillDTO>().ReverseMap();
            CreateMap<UtilityBillDetail, UtilityBillDetailDTO>().ReverseMap();
        }
    }
}
