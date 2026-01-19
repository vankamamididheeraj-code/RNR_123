using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RewardsAndRecognitionRepository.Models;
 
namespace RewardsAndRecognitionRepository.Interfaces
{
    public interface INominationRepo
    {
 
        Task<IEnumerable<Nomination>> GetAllNominationsAsync(bool includeDeleted = false);
 
        /// Get a single nomination by Id.
 
        Task<Nomination?> GetNominationByIdAsync(Guid id);
 
        /// Create a new nomination.
 
 
        Task AddNominationAsync(Nomination nomination);
 
        /// Update nomination details (description, achievements, status).
 
 
        Task UpdateNominationAsync(Nomination nomination);
 
        /// Hard delete a nomination from DB.
 
        Task DeleteNominationAsync(Guid id);
 
        /// Soft delete a nomination (mark IsDeleted = true).
        Task SoftDeleteNominationAsync(Guid id);
 
        /// Returns list of unique categories ever used in nominations.
 
        Task<List<Category>> GetUniqueCategoriesAsync();
 
        /// Returns IDs of categories used at least once in any nomination.
 
        Task<List<Guid>> GetUsedCategoryIdsAsync();
 
        /// Get nominations created by a specific user (Nominator).
 
        Task<IEnumerable<Nomination>> GetNominationsByNominatorAsync(string userId);
 
        /// Get nominations received by a specific user (Nominee).
 
        Task<IEnumerable<Nomination>> GetNominationsForNomineeAsync(string userId);
 
        /// Get nominations pending approval for a specific approver.
        Task<IEnumerable<Nomination>> GetPendingNominationsForApproverAsync(string approverId);
 
        /// Get nominations for teams managed by the user (Manager or Director).
        Task<List<Nomination>> GetNominationsForTeamsManagedByUserAsync(string userId);
 
        /// Get nominations by team.
        Task<List<Nomination>> GetNominationsByTeamAsync(Guid teamId);
 
        /// Get nominations pending manager approval for the user.
        Task<List<Nomination>> GetNominationsPendingManagerApprovalForUserAsync(string userId);
 
        /// Get nominations pending director approval for the user.
        Task<List<Nomination>> GetNominationsPendingDirectorApprovalForUserAsync(string userId);
    }
}