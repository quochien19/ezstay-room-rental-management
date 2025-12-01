namespace PaymentAPI.DTOs.Requests;

public class CreatePayment
{
    public string Gateway { get; set; }
    
    public string AccountNumber { get; set; }
    
    public string Content { get; set; } 
    public decimal TransferAmount { get; set; }
    public string TransferType { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public long IdNumber { get; set; } 

    [System.Text.Json.Serialization.JsonIgnore]
    public string TransactionId => IdNumber.ToString(); 
}