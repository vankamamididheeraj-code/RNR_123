using RewardsAndRecognitionRepository.Enums;
using RewardsAndRecognitionRepository.Models;
 
namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class ApprovalView
    {
 
        public Guid Id { get; set; }
 
        public Guid NominationId { get; set; }
        public NominationView Nomination { get; set; }
 
        public string ApproverId { get; set; }
        public User Approver { get; set; }
 
        public ApprovalAction Action { get; set; }
        public ApprovalLevel Level { get; set; }
 
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
    }
}