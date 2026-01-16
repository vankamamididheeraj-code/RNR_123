using System.ComponentModel.DataAnnotations;

namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class RegisterUserDto
    {
        [Required] public string Name { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        public Guid? TeamId { get; set; }
        public string? SelectedRole { get; set; }
    }
}
