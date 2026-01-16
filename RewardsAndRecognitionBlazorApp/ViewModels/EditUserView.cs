using System.ComponentModel.DataAnnotations;

namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class EditUserView
    {
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = "";

        // Optional on edit
        public string? Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string SelectedRole { get; set; } = "";

        public Guid? TeamId { get; set; }
        public bool? IsActive { get; set; } = true;
    }
}
