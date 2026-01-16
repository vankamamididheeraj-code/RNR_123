using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RewardsAndRecognitionRepository.Repos
{
    public class ApprovalRepo : IApprovalRepo
    {
        private readonly ApplicationDbContext _context;

        public ApprovalRepo(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Approval?> GetApprovalByIdAsync(Guid approvalId)
        {
            return await _context.Approvals
                .Include(a => a.Nomination)
                    .ThenInclude(n => n.Nominee)
                .Include(a => a.Approver)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == approvalId);
        }

        public async Task<List<Approval>> GetApprovalsByNominationIdAsync(Guid nominationId)
        {
            return await _context.Approvals
                .Include(a => a.Approver)
                .Where(a => a.NominationId == nominationId)
                .OrderBy(a => a.ActionAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Approval>> GetApprovalsByApproverIdAsync(string approverId)
        {
            return await _context.Approvals
                .Include(a => a.Nomination)
                    .ThenInclude(n => n.Nominee)
                .Where(a => a.ApproverId == approverId)
                .OrderByDescending(a => a.ActionAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Approval> CreateApprovalAsync(Approval approval)
        {
            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();
            return approval;
        }

        public async Task<bool> UpdateApprovalAsync(Approval approval)
        {
            _context.Approvals.Update(approval);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _context.Approvals.AnyAsync(a => a.Id == approval.Id);
                if (!exists)
                    return false;

                throw;
            }
        }

        public async Task<bool> DeleteApprovalAsync(Guid approvalId)
        {
            var approval = await _context.Approvals.FindAsync(approvalId);
            if (approval == null)
                return false;

            _context.Approvals.Remove(approval);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproverHasAlreadyApprovedAsync(string approverId, Guid nominationId)
        {
            return await _context.Approvals
                .AnyAsync(a => a.ApproverId == approverId && a.NominationId == nominationId);
        }
    }
}
