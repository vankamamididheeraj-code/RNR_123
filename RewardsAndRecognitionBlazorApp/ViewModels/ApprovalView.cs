namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class ApprovalView
    {
        public Guid Id { get; set; }
 
        public Guid NominationId { get; set; }
        public NominationView Nomination { get; set; }
 
        public string ApproverId { get; set; }
        public UserView Approver { get; set; }
        
        public ApprovalView Action { get; set; }
        public ApprovalView Level { get; set; }
 
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
 
 
    }
}