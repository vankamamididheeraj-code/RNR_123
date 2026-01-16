namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class TeamView
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";

        public string TeamLeadId { get; set; } = "";
        public string TeamLeadName { get; set; } = "";
        public string ManagerId { get; set; } = "";
        public string ManagerName { get; set; } = "";
        public string DirectorId { get; set; } = "";
        public string DirectorName { get; set; } = "";
        public bool IsDeleted { get; set; }
    }
}
