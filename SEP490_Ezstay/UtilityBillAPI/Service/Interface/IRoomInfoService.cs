using UtilityBillAPI.DTO;

namespace UtilityBillAPI.Service.Interface
{
    public interface IRoomInfoService
    {
        Task<RoomInfoDTO> GetRoomInfoAsync(Guid roomId);
    }
}
