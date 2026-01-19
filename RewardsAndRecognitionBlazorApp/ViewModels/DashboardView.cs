using System.Collections.Generic;

namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class DashboardView
    {
        public int TotalNominations { get; set; }
        public int PendingApprovals { get; set; }
        public int ApprovedNominations { get; set; }
        public int RejectedNominations { get; set; }
        public List<object> RecentNominations { get; set; } = new();
        public List<object> PendingApprovalNominations { get; set; } = new();
    }
}