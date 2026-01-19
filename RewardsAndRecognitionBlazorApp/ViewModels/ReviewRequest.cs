namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class ReviewRequest
    {
        public string Action { get; set; } = null!; // "Approved" or "Rejected"
        public string? Remarks { get; set; }
        public string UserId { get; set; } = null!; // Current user ID from session
    }
}
