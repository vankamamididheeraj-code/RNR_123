using RewardsAndRecognitionRepository.Enums;

namespace RewardsAndRecognitionRepository.Helpers
{
    /// <summary>
    /// Helper class for NominationStatus business logic
    /// </summary>
    public static class NominationStatusHelper
    {
        /// <summary>
        /// Final states are statuses where Director has made a final decision
        /// Note: For Employee Dashboard, only DirectorApproved is shown (rejected nominations are hidden)
        /// </summary>
        public static readonly NominationStatus[] FinalStates = new[]
        {
            NominationStatus.DirectorApproved,
            NominationStatus.DirectorRejected
        };

        /// <summary>
        /// Checks if a nomination status is in a final state (Director decision made)
        /// </summary>
        public static bool IsFinalState(NominationStatus status)
        {
            return status == NominationStatus.DirectorApproved || 
                   status == NominationStatus.DirectorRejected;
        }
    }
}
