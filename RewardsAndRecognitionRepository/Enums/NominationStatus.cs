using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardsAndRecognitionRepository.Enums
{
    public enum NominationStatus
    {
        Draft,
        PendingManager,
        PendingDirector,
        ManagerApproved,
        ManagerRejected,
        DirectorApproved,
        DirectorRejected
    }
}
