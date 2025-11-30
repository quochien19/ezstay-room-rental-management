using UtilityBillAPI.Service.Interface;
using UtilityBillAPI.DTO;
using System.Text.Json;

namespace UtilityBillAPI.Service
{
    public class RoomInfoService : IRoomInfoService
    {
        private readonly HttpClient _httpRoom;
        private readonly HttpClient _httpBoardingHouse;
        public RoomInfoService(IHttpClientFactory httpClientFactory)
        {
            _httpRoom = httpClientFactory.CreateClient("RoomAPI");
            _httpBoardingHouse = httpClientFactory.CreateClient("BoardingHouseAPI");
        }

        public async Task<RoomInfoDTO> GetRoomInfoAsync(Guid roomId)
        {
            // Fetch Room
            var roomResponse = await _httpRoom.GetAsync($"/api/Rooms/{roomId}");
            if (!roomResponse.IsSuccessStatusCode)
                throw new Exception("Room not found.");
            var roomJson = await roomResponse.Content.ReadFromJsonAsync<JsonElement>();

            string roomName = roomJson.GetProperty("roomName").GetString() ?? "";
            Guid boardingHouseId = roomJson.GetProperty("houseId").GetGuid();

            // Fetch Boarding House
            var houseResponse = await _httpBoardingHouse.GetAsync($"/api/BoardingHouses/{boardingHouseId}");
            if (!houseResponse.IsSuccessStatusCode)
                throw new Exception("Boarding House not found.");
            var houseJson = await houseResponse.Content.ReadFromJsonAsync<JsonElement>();
            string houseName = houseJson.GetProperty("houseName").GetString() ?? "";

            return new RoomInfoDTO
            {
                RoomName = roomName,
                HouseName = houseName
            };
        }
    }
}
