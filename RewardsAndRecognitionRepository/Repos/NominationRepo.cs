using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
 
 
namespace RewardsAndRecognitionRepository.Repos
{
    public class NominationRepo : INominationRepo
    {
        private readonly ApplicationDbContext _context;
 
        public NominationRepo(ApplicationDbContext context)
 
        {
 
            _context = context;
 
        }
 
        public async Task<IEnumerable<Nomination>> GetAllNominationsAsync(bool includeDeleted = false)
 
        {
 
            var query = _context.Nominations
 
                .Include(n => n.Nominator)
 
                .Include(n => n.Nominee)
 
                .Include(n => n.Category)
 
                .Include(n => n.YearQuarter)
 
                .Include(n => n.Approvals)
 
                .AsQueryable();
 
            if (!includeDeleted)
 
            {
 
                query = query.Where(n => !n.IsDeleted);
 
            }
 
            return await query
 
                .AsNoTracking()
 
                .ToListAsync();
 
        }
 
        public async Task<Nomination?> GetNominationByIdAsync(Guid id)
 
        {
 
            return await _context.Nominations
 
                .Include(n => n.Nominator)
 
                .Include(n => n.Nominee)
 
                    .ThenInclude(u => u.Team)
 
                .Include(n => n.Category)
 
                .Include(n => n.YearQuarter)
 
                .Include(n => n.Approvals)
 
                .AsNoTracking()
 
                .FirstOrDefaultAsync(n => n.Id == id);
 
        }
 
        public async Task AddNominationAsync(Nomination nomination)
 
        {
 
            _context.Nominations.Add(nomination);
 
            await _context.SaveChangesAsync();
 
        }
 
        public async Task UpdateNominationAsync(Nomination nomination)
 
        {
 
            _context.Nominations.Update(nomination);
 
            await _context.SaveChangesAsync();
 
        }
 
        public async Task DeleteNominationAsync(Guid id)
 
        {
 
            var nomination = await _context.Nominations.FindAsync(id);
 
            if (nomination == null)
 
                return;
 
            _context.Nominations.Remove(nomination);
 
            await _context.SaveChangesAsync();
 
        }
 
        /// <summary>
 
        /// Get all unique categories that have been used in nominations (entity list).
 
        /// </summary>
 
        public async Task<List<Category>> GetUniqueCategoriesAsync()
 
        {
 
            var categoryIds = await _context.Nominations
 
                .Where(n => !n.IsDeleted)
 
                .Select(n => n.CategoryId)
 
                .Distinct()
 
                .ToListAsync();
 
            return await _context.Categories
 
                .Where(c => categoryIds.Contains(c.Id) && !c.IsDeleted)
 
                .AsNoTracking()
 
                .ToListAsync();
 
        }
 
        public async Task<List<Guid>> GetUsedCategoryIdsAsync()
 
        {
 
            return await _context.Nominations
 
                .Where(n => !n.IsDeleted)
 
                .Select(n => n.CategoryId)
 
                .Distinct()
 
                .ToListAsync();
 
        }
 
        public async Task SoftDeleteNominationAsync(Guid id)
 
        {
 
            var nomination = await _context.Nominations.FindAsync(id);
 
            if (nomination == null || nomination.IsDeleted)
 
                return;
 
            nomination.IsDeleted = true;
 
            _context.Nominations.Update(nomination);
 
            await _context.SaveChangesAsync();
 
        }
 
        // -----------------------------------------
 
        // NEW: BY NOMINATOR (created by user)
 
        // -----------------------------------------
 
        public async Task<IEnumerable<Nomination>> GetNominationsByNominatorAsync(string userId)
 
        {
 
            return await _context.Nominations
 
                .Include(n => n.Nominator)
 
                .Include(n => n.Nominee)
 
                .Include(n => n.Category)
 
                .Include(n => n.YearQuarter)
 
                .Where(n => n.NominatorId == userId && !n.IsDeleted)
 
                .AsNoTracking()
 
                .ToListAsync();
 
        }
 
        // -----------------------------------------
 
        // NEW: FOR NOMINEE (received by user)
 
        // -----------------------------------------
 
        public async Task<IEnumerable<Nomination>> GetNominationsForNomineeAsync(string userId)
 
        {
 
            return await _context.Nominations
 
                .Include(n => n.Nominator)
 
                .Include(n => n.Category)
 
                .Include(n => n.YearQuarter)
 
                .Where(n => n.NomineeId == userId && !n.IsDeleted)
 
                .AsNoTracking()
 
                .ToListAsync();
 
        }
 
        // -----------------------------------------
 
        // NEW: PENDING FOR APPROVER (manager/director)
 
        // -----------------------------------------
 
        public async Task<IEnumerable<Nomination>> GetPendingNominationsForApproverAsync(string approverId)
 
        {
 
            return await _context.Nominations
 
                .Include(n => n.Nominator)
 
                .Include(n => n.Nominee)
 
                    .ThenInclude(u => u.Team)
 
                .Include(n => n.Category)
 
                .Include(n => n.YearQuarter)
 
                .Where(n => !n.IsDeleted &&
 
                            (
 
                                // Manager-level pending
 
                                (n.Status == Enums.NominationStatus.PendingManager &&
 
                                 n.Nominee.Team != null &&
 
                                 n.Nominee.Team.ManagerId == approverId)
 
                                ||
 
                                // Director-level pending
 
                                (n.Status == Enums.NominationStatus.PendingDirector &&
 
                                 n.Nominee.Team != null &&
 
                                 n.Nominee.Team.DirectorId == approverId)
 
                            ))
 
                .AsNoTracking()
 
                .ToListAsync();
 
        }
 
        public async Task<List<Nomination>> GetNominationsForTeamsManagedByUserAsync(string userId)
        {
            // Return nominations where Nominee.Team.ManagerId == userId or Nominee.Team.DirectorId == userId
            return await _context.Nominations
                .Include(n => n.Nominator)
                .Include(n => n.Nominee).ThenInclude(u => u.Team)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Include(n => n.Approvals)
                .Where(n => !n.IsDeleted && (n.Nominee.Team.ManagerId == userId || n.Nominee.Team.DirectorId == userId))
                .AsNoTracking()
                .ToListAsync();
        }
 
        public async Task<List<Nomination>> GetNominationsByTeamAsync(Guid teamId)
        {
            return await _context.Nominations
                .Include(n => n.Nominator)
                .Include(n => n.Nominee).ThenInclude(u => u.Team)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Include(n => n.Approvals)
                .Where(n => !n.IsDeleted && n.Nominee.TeamId == teamId)
                .AsNoTracking()
                .ToListAsync();
        }
 
        public async Task<List<Nomination>> GetNominationsPendingManagerApprovalForUserAsync(string userId)
        {
            return await _context.Nominations
                .Include(n => n.Nominator)
                .Include(n => n.Nominee).ThenInclude(u => u.Team)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Include(n => n.Approvals)
                .Where(n => !n.IsDeleted && n.Status == Enums.NominationStatus.PendingManager && n.Nominee.Team.ManagerId == userId)
                .AsNoTracking()
                .ToListAsync();
        }
 
        public async Task<List<Nomination>> GetNominationsPendingDirectorApprovalForUserAsync(string userId)
        {
            return await _context.Nominations
                .Include(n => n.Nominator)
                .Include(n => n.Nominee).ThenInclude(u => u.Team)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Include(n => n.Approvals)
                .Where(n => !n.IsDeleted && n.Status == Enums.NominationStatus.PendingDirector && n.Nominee.Team.DirectorId == userId)
                .AsNoTracking()
                .ToListAsync();
        }
 
    }
}