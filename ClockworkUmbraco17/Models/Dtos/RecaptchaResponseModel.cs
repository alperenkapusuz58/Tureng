namespace ClockworkUmbraco.Models.Dtos;

public class RecaptchaResponseModel
{
    public bool Success { get; set; }
    public List<string> ErrorCodes { get; set; }
    public decimal Score { get; set; }
}

