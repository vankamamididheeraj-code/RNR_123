namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class TeamView
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeamLeadId { get; set; } = string.Empty;
        public string TeamLeadName { get; set; } = string.Empty;
        public string ManagerId { get; set; } = string.Empty;
        public string ManagerName { get; set; } = string.Empty;
        public string DirectorId { get; set; } = string.Empty;
        public string DirectorName { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}