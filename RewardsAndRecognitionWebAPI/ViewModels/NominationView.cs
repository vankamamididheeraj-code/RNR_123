using RewardsAndRecognitionRepository.Enums;
namespace RewardsAndRecognitionWebAPI.ViewModels
{
public class NominationView
    {
 
        public Guid Id { get; set; }
 
        public string NominatorId { get; set; }
        public UserView? Nominator { get; set; }
 
        public string NomineeId { get; set; }
        public UserView? Nominee { get; set; }
 
        public Guid CategoryId { get; set; }
        public CategoryView? Category { get; set; }
 
        public string Description { get; set; }
        public string Achievements { get; set; }
 
        public NominationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
 
        public Guid YearQuarterId { get; set; }
        public YearQuarterView? YearQuarter { get; set; }
 
        public ICollection<ApprovalView>? Approvals { get; set; }
        public bool IsDeleted { get; internal set; }
 
 
    }
}