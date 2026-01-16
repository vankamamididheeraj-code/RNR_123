namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class UserView
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;

        public string? TeamName { get; set; }
        public string? ManagerName { get; set; }
        public string? Role { get; set; }

        public bool? IsActive { get; set; }
        public Guid? TeamId { get; set; }

        public bool IsDeleted { get; set; }
    }
}
