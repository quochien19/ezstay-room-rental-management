namespace UtilityBillAPI.DTO
{
    public class BillSettingDTO
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public int GenerateDay { get; set; }
        public int DueAfterDays { get; set; }
        public bool IsAutoGenerateEnabled { get; set; }
    }
}
