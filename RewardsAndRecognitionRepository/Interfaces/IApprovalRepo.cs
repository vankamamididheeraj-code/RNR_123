using RewardsAndRecognitionRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardsAndRecognitionRepository.Interfaces
{
    public interface IApprovalRepo
    {
        Task<Approval?> GetApprovalByIdAsync(Guid approvalId);

        Task<List<Approval>> GetApprovalsByNominationIdAsync(Guid nominationId);

        Task<List<Approval>> GetApprovalsByApproverIdAsync(string approverId);

        Task<Approval> CreateApprovalAsync(Approval approval);

        Task<bool> UpdateApprovalAsync(Approval approval);

        Task<bool> DeleteApprovalAsync(Guid approvalId);

        Task<bool> ApproverHasAlreadyApprovedAsync(string approverId, Guid nominationId);
    }
}
