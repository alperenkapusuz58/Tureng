using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace ClockworkUmbraco.Models.ViewModels
{
    [DataContract]
    public class ContactFormModel
    {
        [Required(ErrorMessage = "Ad Soyad alanı zorunludur")]
        public string NameSurname { get; set; }

        [Required(ErrorMessage = "Email zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Konu zorunludur")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Mesaj zorunludur")]
        public string Message { get; set; }

        [Required(ErrorMessage = "Doğrulama zorunludur")]
        public string Agreement { get; set; }

    }
}

