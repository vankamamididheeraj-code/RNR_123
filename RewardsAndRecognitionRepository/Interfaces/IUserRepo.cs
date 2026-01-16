using RewardsAndRecognitionRepository.Models;

namespace RewardsAndRecognitionRepository.Interfaces
{
    public interface IUserRepo
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(string id);

        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByTeamAsync(Guid teamId);
        Task<IEnumerable<User>> GetUnassignedUsersAsync();
        Task<IEnumerable<User>> GetAllManagersAsync();
        Task<IEnumerable<User>> GetLeadsAsync(string? currentLeadId = null);
        Task<IEnumerable<User>> GetAllDirectorsAsync();
        Task<IEnumerable<User>> GetDeletedUsersAsync();
        Task<User?> GetUserWithTeamAsync(string userId);

        Task AddAsync(User user);
        Task UpdateAsync(User user);

        // Soft delete / restore
        Task SoftDeleteAsync(string userId);
        Task RestoreAsync(string userId);

        Task SaveAsync();
        

    }
}
