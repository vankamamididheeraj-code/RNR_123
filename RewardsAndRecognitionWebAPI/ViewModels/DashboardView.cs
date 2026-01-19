using System.Collections.Generic;

namespace RewardsAndRecognitionWebAPI.ViewModels
{
    public class DashboardView
    {
        public int TotalNominations { get; set; }
        public int PendingApprovals { get; set; }
        public int ApprovedNominations { get; set; }
        public int RejectedNominations { get; set; }
        public List<NominationView> RecentNominations { get; set; } = new List<NominationView>();
        public List<NominationView> PendingApprovalNominations { get; set; } = new List<NominationView>();
    }
}