using System.ComponentModel.DataAnnotations;

namespace ECommerce517.ViewModels
{
    public class ResetPasswordVM
    {
        public int Id { get; set; }
        [Required]
        public string OTPNumber { get; set; } = string.Empty;
        public string ApplicationUserId { get; set; } = string.Empty;
    }
}
