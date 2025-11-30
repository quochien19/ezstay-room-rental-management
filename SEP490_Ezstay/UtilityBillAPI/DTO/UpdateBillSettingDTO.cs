using System.ComponentModel.DataAnnotations;

namespace UtilityBillAPI.DTO
{
    public class UpdateBillSettingDTO
    {
        [Range(1, 28)]
        public int GenerateDay { get; set; }

        [Range(0, 90)]
        public int DueAfterDays { get; set; }

        public bool IsAutoGenerateEnabled { get; set; }
    }
}
