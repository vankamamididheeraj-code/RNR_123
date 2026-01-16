namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class UserView
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;

        public string? TeamName { get; set; }        // "Not Assigned" if null
        public string? ManagerName { get; set; }     // "No Manager" if null
        public string? Role { get; set; }            // "No Role" if null

        public bool? IsActive { get; set; }
        public Guid? TeamId { get; set; }

        public bool IsDeleted { get; set; }
        //public string Id { get; set; }
        //public string Name { get; set; }
        //public string Email { get; set; }
        //public string PasswordHash { get; set; }
        //public string SelectedRole { get; set; }
        //public Guid? TeamId { get; set; }
        //public Team? Team { get; set; }
        //public bool IsDeleted { get; set; }
        //public dynamic TargetField { get; internal set; }
    }
}
