using System.ComponentModel.DataAnnotations;

namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class UpdateUserDto
    {
        [Required] public string Id { get; set; } = "";
        [Required] public string Name { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";

        public string? Password { get; set; } // optional
        [Required] public string SelectedRole { get; set; } = "";

        public Guid? TeamId { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
