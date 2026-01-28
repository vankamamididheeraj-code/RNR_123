using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using RewardsAndRecognitionBlazorApp.ViewModels;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Data;
using RewardsAndRecognitionRepository.Enums;
using RewardsAndRecognitionRepository.Service;
using RewardsAndRecognitionWebAPI.ViewModels;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NominationController : ControllerBase
    {
        private readonly INominationRepo _nominationRepo;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public NominationController(
            INominationRepo nominationRepo, 
            RewardsAndRecognitionRepository.Models.ApplicationDbContext context,
            UserManager<User> userManager,
            IEmailService emailService)
        {
            _nominationRepo = nominationRepo;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: api/nomination
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false)
        {
            var results = await _nominationRepo.GetAllNominationsAsync(includeDeleted);
            return Ok(results);
        }

        // GET: api/nomination/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([
            FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] string? q = null,
            [FromQuery] bool includeDeleted = false,
            CancellationToken ct = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            const int MaxPageSize = 200;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _context.Nominations
                .Include(n => n.Nominator)
                .Include(n => n.Nominee!)
                    .ThenInclude(nominee => nominee.Team)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Include(n => n.Approvals)
                .AsQueryable();

            // Apply role-based filtering
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();

                // If Director, filter to only show nominations from teams under this director
                if (role == "Director")
                {
                    baseQuery = baseQuery.Where(n => 
                        n.Nominee != null && 
                        n.Nominee.Team != null &&
                        n.Nominee.Team.DirectorId == user.Id &&
                        !n.Nominee.Team.IsDeleted);
                }
                // If Manager, filter to only show nominations from teams under this manager
                else if (role == "Manager")
                {
                    baseQuery = baseQuery.Where(n => 
                        n.Nominee != null && 
                        n.Nominee.Team != null &&
                        n.Nominee.Team.ManagerId == user.Id &&
                        !n.Nominee.Team.IsDeleted);
                }
            }

            if (!includeDeleted)
                baseQuery = baseQuery.Where(n => !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status))
            {
                // simple status filtering by textual match
                baseQuery = baseQuery.Where(n => n.Status.ToString().ToLower().Contains(status.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.ToLower();
                baseQuery = baseQuery.Where(n =>
                    (n.Nominee != null && n.Nominee.Email.ToLower().Contains(qLower)) ||
                    (n.Nominator != null && n.Nominator.Email.ToLower().Contains(qLower)) ||
                    (n.Category != null && n.Category.Name.ToLower().Contains(qLower)) ||
                    (n.YearQuarter != null && (n.YearQuarter.Year.ToString() + " q" + n.YearQuarter.Quarter).ToLower().Contains(qLower))
                );
            }

            baseQuery = baseQuery.OrderByDescending(n => n.CreatedAt);

            // Fetch the paginated nominations with all includes
            var totalCount = await baseQuery.CountAsync(ct);
            var nominations = await baseQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // Map to view models
            var nominationViews = nominations.Select(n => new RewardsAndRecognitionWebAPI.ViewModels.NominationView
            {
                Id = n.Id,
                CategoryId = n.CategoryId,
                Category = n.Category == null ? null : new RewardsAndRecognitionWebAPI.ViewModels.CategoryView { Id = n.Category.Id, Name = n.Category.Name },
                NominatorId = n.NominatorId,
                Nominator = n.Nominator == null ? null : new RewardsAndRecognitionWebAPI.ViewModels.UserView { Id = n.Nominator.Id, Email = n.Nominator.Email },
                NomineeId = n.NomineeId,
                Nominee = n.Nominee == null ? null : new RewardsAndRecognitionWebAPI.ViewModels.UserView { Id = n.Nominee.Id, Email = n.Nominee.Email },
                YearQuarterId = n.YearQuarterId,
                YearQuarter = n.YearQuarter == null ? null : new RewardsAndRecognitionWebAPI.ViewModels.YearQuarterView { Id = n.YearQuarter.Id, Year = n.YearQuarter.Year, Quarter = n.YearQuarter.Quarter, StartDate = n.YearQuarter.StartDate, EndDate = n.YearQuarter.EndDate, IsActive = n.YearQuarter.IsActive },
                Description = n.Description,
                Achievements = n.Achievements,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                IsDeleted = n.IsDeleted,
                Approvals = n.Approvals?.Select(a => new RewardsAndRecognitionWebAPI.ViewModels.ApprovalView
                {
                    Id = a.Id,
                    NominationId = a.NominationId,
                    ApproverId = a.ApproverId,
                    Action = a.Action,
                    Level = a.Level,
                    ActionAt = a.ActionAt,
                    Remarks = a.Remarks
                }).ToList() ?? new List<RewardsAndRecognitionWebAPI.ViewModels.ApprovalView>()
            }).ToList();

            var paged = new PagedResult<RewardsAndRecognitionWebAPI.ViewModels.NominationView>
            {
                Items = nominationViews.ToArray(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(paged);
        }

        // GET: api/nomination/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var nomination = await _nominationRepo.GetNominationByIdAsync(id);
            return nomination == null ? NotFound() : Ok(nomination);
        }

        // GET: api/nomination/nominator/{userId}
        [HttpGet("nominator/{userId}")]
        public async Task<IActionResult> GetByNominator(string userId)
        {
            var results = await _nominationRepo.GetNominationsByNominatorAsync(userId);
            return Ok(results);
        }

        // GET: api/nomination/nominee/{userId}
        [HttpGet("nominee/{userId}")]
        public async Task<IActionResult> GetByNominee(string userId)
        {
            var results = await _nominationRepo.GetNominationsForNomineeAsync(userId);
            return Ok(results);
        }

        // GET: api/nomination/pending/{approverId}
        [HttpGet("pending/{approverId}")]
        public async Task<IActionResult> GetPending(string approverId)
        {
            var results = await _nominationRepo.GetPendingNominationsForApproverAsync(approverId);
            return Ok(results);
        }

        // POST: api/nomination
        [HttpPost]
        public async Task<IActionResult> Create(Nomination nomination)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // If no status is provided, default to PendingManager (not Draft)
            if (nomination.Status == default)
            {
                nomination.Status = RewardsAndRecognitionRepository.Enums.NominationStatus.PendingManager;
            }

            nomination.CreatedAt = DateTime.UtcNow;

            await _nominationRepo.AddNominationAsync(nomination);

            return Ok(nomination);
        }

        // POST: api/nomination/draft
        [HttpPost("draft")]
        public async Task<IActionResult> SaveDraft(Nomination nomination)
        {
            // Minimal validation for drafts - only require nominator
            if (string.IsNullOrWhiteSpace(nomination.NominatorId))
                return BadRequest("Nominator is required");

            nomination.Status = RewardsAndRecognitionRepository.Enums.NominationStatus.Draft;
            nomination.CreatedAt = DateTime.UtcNow;

            await _nominationRepo.AddNominationAsync(nomination);

            return Ok(nomination);
        }

        // PUT: api/nomination/draft/{id}
        [HttpPut("draft/{id:guid}")]
        public async Task<IActionResult> UpdateDraft(Guid id, Nomination nomination)
        {
            if (id != nomination.Id)
                return BadRequest("ID mismatch");

            var existing = await _nominationRepo.GetNominationByIdAsync(id);
            if (existing == null)
                return NotFound();

            if (existing.Status != RewardsAndRecognitionRepository.Enums.NominationStatus.Draft)
                return BadRequest("Can only update nominations with Draft status");

            // Update draft fields
            existing.NomineeId = nomination.NomineeId;
            existing.CategoryId = nomination.CategoryId;
            existing.YearQuarterId = nomination.YearQuarterId;
            existing.Description = nomination.Description;
            existing.Achievements = nomination.Achievements;
            existing.Status = RewardsAndRecognitionRepository.Enums.NominationStatus.Draft;

            await _nominationRepo.UpdateNominationAsync(existing);

            return Ok(existing);
        }

        // GET: api/nomination/drafts/{nominatorId}
        [HttpGet("drafts/{nominatorId}")]
        public async Task<IActionResult> GetDrafts(string nominatorId)
        {
            if (string.IsNullOrWhiteSpace(nominatorId))
                return BadRequest("Nominator ID is required");

            var drafts = await _context.Nominations
                .Include(n => n.Nominee)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Where(n => n.NominatorId == nominatorId && 
                           n.Status == RewardsAndRecognitionRepository.Enums.NominationStatus.Draft && 
                           !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(drafts);
        }

        // GET: api/nomination/latest-draft/{nominatorId}
        [HttpGet("latest-draft/{nominatorId}")]
        public async Task<IActionResult> GetLatestDraft(string nominatorId)
        {
            if (string.IsNullOrWhiteSpace(nominatorId))
                return BadRequest("Nominator ID is required");

            var draft = await _context.Nominations
                .Include(n => n.Nominee)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .Where(n => n.NominatorId == nominatorId && 
                           n.Status == RewardsAndRecognitionRepository.Enums.NominationStatus.Draft && 
                           !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (draft == null)
                return NotFound();

            return Ok(draft);
        }

        // PUT: api/nomination/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateNominationView dto)
        {
            if (id != dto.Id) return BadRequest("Mismatched ID");

            var existing = await _nominationRepo.GetNominationByIdAsync(id);
            if (existing == null) return NotFound();

            existing.CategoryId = dto.CategoryId!.Value;
            existing.YearQuarterId = dto.YearQuarterId!.Value;
            existing.Description = dto.Description;
            existing.Achievements = dto.Achievements;

            await _nominationRepo.UpdateNominationAsync(existing);
            return NoContent();
        }

        // DELETE soft: api/nomination/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _nominationRepo.SoftDeleteNominationAsync(id);
            return NoContent();
        }

        // POST: api/nomination/{id}/review
        [HttpPost("{id:guid}/review")]
        public async Task<IActionResult> Review(Guid id, [FromBody] ReviewRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new { error = "Request body is null" });

                if (string.IsNullOrEmpty(request.Action))
                    return BadRequest(new { error = "Action is required" });

                if (string.IsNullOrEmpty(request.UserId))
                    return BadRequest(new { error = "UserId is required" });

                var nomination = await _context.Nominations
                    .Include(n => n.Nominee)
                    .Include(n => n.Nominator)
                    .Include(n => n.Category)
                    .Include(n => n.YearQuarter)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (nomination == null)
                    return NotFound("Nomination not found");

                var currentUser = await _userManager.FindByIdAsync(request.UserId);
                if (currentUser == null)
                    return NotFound("User not found");

            var userRoles = await _userManager.GetRolesAsync(currentUser);

            // TeamLead cannot approve/reject nominations
            if (userRoles.Contains("TeamLead"))
            {
                return BadRequest("TeamLead users cannot approve or reject nominations. Only Managers and Directors can review nominations.");
            }

            if (!Enum.TryParse<ApprovalAction>(request.Action, ignoreCase: true, out var parsedAction))
            {
                return BadRequest("Invalid action. Must be 'Approved' or 'Rejected'");
            }

            // Determine the appropriate status based on role and action
            if (userRoles.Contains("Manager"))
            {
                // Manager approval or rejection both go to Director for final decision
                nomination.Status = parsedAction == ApprovalAction.Approved
                    ? NominationStatus.ManagerApproved
                    : NominationStatus.ManagerRejected;
            }
            else if (userRoles.Contains("Director"))
            {
                // Director makes the final approval or rejection decision
                nomination.Status = parsedAction == ApprovalAction.Approved
                    ? NominationStatus.DirectorApproved
                    : NominationStatus.DirectorRejected;
            }
            else
            {
                return BadRequest("User does not have appropriate role for approval");
            }

            await _nominationRepo.UpdateNominationAsync(nomination);

            // Create approval record
            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                NominationId = id,
                ApproverId = currentUser.Id,
                Action = parsedAction,
                Level = userRoles.Contains("Manager") 
                    ? ApprovalLevel.Manager 
                    : ApprovalLevel.Director,
                ActionAt = DateTime.UtcNow,
                Remarks = request.Remarks
            };

            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();

            // Send emails if Director approved
            if (nomination.Status == NominationStatus.DirectorApproved)
            {
                var nominator = nomination.Nominator ?? await _userManager.FindByIdAsync(nomination.NominatorId);
                var nominee = nomination.Nominee ?? await _userManager.FindByIdAsync(nomination.NomineeId);

                if (nominator != null && !string.IsNullOrEmpty(nominator.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            subject: "Nomination Approved",
                            isHtml: true,
                            body: $@"
                            <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #ffffff;"">
                              <div style=""background-color: #ffffff; padding: 10px 20px; max-width: 600px; margin: auto; color: #000;"">
                                <img src=""cid:bannerImage"" alt=""Zelis Banner"" style=""width: 100%; max-width: 600px;"">
                                <h2 style='color: green;'>Congratulations!</h2>
                                <p>Your nomination for <strong>{nominee?.Name}</strong> has been <strong>approved by the Director</strong>.</p>
                                <p>Thank you for recognizing great work on our Rewards and Recognition platform.</p>
                                <p style='color: gray;'>Regards,<br/>Rewards & Recognition Team</p>
                              </div>
                            </body>",
                            to: nominator.Email
                        );
                    }
                    catch (Exception)
                    {
                        // Log email error but don't fail the request
                    }
                }

                if (nominee != null && !string.IsNullOrEmpty(nominee.Email))
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            subject: "You've Been Selected for an Award!",
                            isHtml: true,
                            body: $@"
                            <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #ffffff;"">
                              <div style=""background-color: #ffffff; padding: 10px 20px; max-width: 600px; margin: auto; color: #000;"">
                                <img src=""cid:bannerImage"" alt=""Zelis Banner"" style=""width: 100%; max-width: 600px;"">
                                <h2 style='color: navy;'>Congratulations {nominee.Name}!</h2>
                                <p>You have been selected for an <strong>award</strong> in the category of <strong>{nomination.Category?.Name}</strong> for <strong>{nomination.YearQuarter?.Quarter}</strong>.</p>
                                <p>This recognition comes as part of our Rewards & Recognition initiative. Keep up the amazing work!</p>
                                <p style='color: gray;'>Cheers,<br/>Rewards & Recognition Team</p>
                              </div>
                            </body>",
                            to: nominee.Email
                        );
                    }
                    catch (Exception)
                    {
                        // Log email error but don't fail the request
                    }
                }
            }

            return Ok(new 
            { 
                message = parsedAction == ApprovalAction.Approved 
                    ? "Nomination approved successfully" 
                    : "Nomination rejected successfully",
                status = nomination.Status.ToString()
            });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "An error occurred", details = ex.Message });
            }
        }
    }

    // DTO for review request
    public class ReviewRequest
    {
        public string Action { get; set; } = null!; // "Approved" or "Rejected"
        public string? Remarks { get; set; }
        public string UserId { get; set; } = null!; // Current user ID from session
    }
}
