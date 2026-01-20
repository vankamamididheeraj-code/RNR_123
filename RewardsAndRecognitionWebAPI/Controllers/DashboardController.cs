using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Enums;
using RewardsAndRecognitionWebAPI.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly INominationRepo _nominationRepo;
        private readonly IApprovalRepo _approvalRepo;
        private readonly ITeamRepo _teamRepo;
        private readonly IYearQuarterRepo _yearQuarterRepo;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            UserManager<User> userManager,
            INominationRepo nominationRepo,
            IApprovalRepo approvalRepo,
            ITeamRepo teamRepo,
            IYearQuarterRepo yearQuarterRepo,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _nominationRepo = nominationRepo;
            _approvalRepo = approvalRepo;
            _teamRepo = teamRepo;
            _yearQuarterRepo = yearQuarterRepo;
            _context = context;
        }

        // GET: api/dashboard
        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            var visibleNominations = await GetVisibleNominationsAsync(user, role);
            var pendingApprovalNominations = await GetPendingApprovalNominationsAsync(user.Id, role);

            var dashboard = new DashboardView
            {
                TotalNominations = visibleNominations.Count,
                ApprovedNominations = visibleNominations.Count(n => n.Status == NominationStatus.ManagerApproved || n.Status == NominationStatus.DirectorApproved),
                RejectedNominations = visibleNominations.Count(n => n.Status == NominationStatus.ManagerRejected || n.Status == NominationStatus.DirectorRejected),
                PendingApprovals = pendingApprovalNominations.Count,
                RecentNominations = visibleNominations
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .Select(MapToNominationView)
                    .ToList(),
                PendingApprovalNominations = pendingApprovalNominations
                    .Take(10)
                    .Select(MapToNominationView)
                    .ToList()
            };

            return Ok(dashboard);
        }

        // GET: api/dashboard/teamlead?yearQuarterId={yearQuarterId}
        [HttpGet("TeamLead")]
        [Authorize(Roles = "TeamLead")]
        public async Task<IActionResult> GetTeamLeadDashboard([FromQuery] string yearQuarterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(yearQuarterId))
            {
                return BadRequest("YearQuarterId is required");
            }

            Guid quarterId;
            if (!Guid.TryParse(yearQuarterId, out quarterId))
            {
                return BadRequest("Invalid YearQuarterId");
            }

            // Get all teams where the logged-in user is the team lead
            var allTeams = await _teamRepo.GetAllAsync();
            var teamLeadTeamIds = allTeams
                .Where(t => t.TeamLeadId == user.Id && !t.IsDeleted)
                .Select(t => t.Id)
                .ToList();

            // Get all users in those teams
            var usersInTeamLeadTeams = await _context.Users
                .Where(u => u.TeamId.HasValue && teamLeadTeamIds.Contains(u.TeamId.Value))
                .Select(u => u.Id)
                .ToListAsync();

            // Include the team lead themselves
            usersInTeamLeadTeams.Add(user.Id);

            // Get all nominations for the specified quarter created by team lead or users under team lead
            var allNominations = await _nominationRepo.GetAllNominationsAsync();
            var quarterNominations = allNominations
                .Where(n => n.YearQuarterId == quarterId && 
                           usersInTeamLeadTeams.Contains(n.NominatorId))
                .ToList();

            // Calculate statistics
            var totalNominations = quarterNominations.Count;
            // Pending reviews includes all nominations until Director makes final decision
            var pendingNominations = quarterNominations.Count(n => 
                n.Status != NominationStatus.DirectorApproved && 
                n.Status != NominationStatus.DirectorRejected);
            var finalApprovedNominations = quarterNominations.Count(n => 
                n.Status == NominationStatus.DirectorApproved);
            var rejectedNominations = quarterNominations.Count(n => 
                n.Status == NominationStatus.DirectorRejected);

            var dashboardData = new
            {
                TotalNominations = totalNominations,
                PendingNominations = pendingNominations,
                FinalApprovedNominations = finalApprovedNominations,
                RejectedNominations = rejectedNominations
            };

            return Ok(dashboardData);
        }

        // GET: api/dashboard/manager?yearQuarterId={yearQuarterId}
        [HttpGet("Manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagerDashboard([FromQuery] string yearQuarterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(yearQuarterId))
            {
                return BadRequest("YearQuarterId is required");
            }

            Guid quarterId;
            if (!Guid.TryParse(yearQuarterId, out quarterId))
            {
                return BadRequest("Invalid YearQuarterId");
            }

            // Get all teams under the manager
            var allTeams = await _teamRepo.GetAllAsync();
            var managerTeamIds = allTeams
                .Where(t => t.ManagerId == user.Id && !t.IsDeleted)
                .Select(t => t.Id)
                .ToList();

            // Get all nominations for the specified quarter where nominee is in manager's teams
            var allNominations = await _nominationRepo.GetAllNominationsAsync();
            var quarterNominations = allNominations
                .Where(n => n.YearQuarterId == quarterId && 
                           n.Nominee != null && 
                           n.Nominee.TeamId.HasValue && 
                           managerTeamIds.Contains(n.Nominee.TeamId.Value))
                .ToList();

            // Get nomination IDs for this quarter
            var nominationIds = quarterNominations.Select(n => n.Id).ToList();

            // Get all manager-level approvals from Approvals table
            var managerApprovals = await _context.Approvals
                .Where(a => nominationIds.Contains(a.NominationId) 
                    && a.Level == ApprovalLevel.Manager 
                    && a.Action == ApprovalAction.Approved)
                .Select(a => a.NominationId)
                .Distinct()
                .ToListAsync();

            // Get all manager-level rejections from Approvals table
            var managerRejections = await _context.Approvals
                .Where(a => nominationIds.Contains(a.NominationId) 
                    && a.Level == ApprovalLevel.Manager 
                    && a.Action == ApprovalAction.Rejected)
                .Select(a => a.NominationId)
                .Distinct()
                .ToListAsync();

            // Calculate statistics
            var totalNominations = quarterNominations.Count;
            var pendingManagerApproval = quarterNominations.Count(n => 
                n.Status == NominationStatus.PendingManager);
            // Count all nominations that have Manager-level approval in Approvals table
            var managerApproved = managerApprovals.Count;
            // Count all nominations that have Manager-level rejection in Approvals table
            var managerRejected = managerRejections.Count;

            var dashboardData = new
            {
                TotalNominations = totalNominations,
                PendingManagerApproval = pendingManagerApproval,
                ManagerApproved = managerApproved,
                ManagerRejected = managerRejected
            };

            return Ok(dashboardData);
        }

        // GET: api/dashboard/director?yearQuarterId={yearQuarterId}
        [HttpGet("Director")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> GetDirectorDashboard([FromQuery] string yearQuarterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(yearQuarterId))
            {
                return BadRequest("YearQuarterId is required");
            }

            Guid quarterId;
            if (!Guid.TryParse(yearQuarterId, out quarterId))
            {
                return BadRequest("Invalid YearQuarterId");
            }

            // Get all teams under the director
            var allTeams = await _teamRepo.GetAllAsync();
            var directorTeamIds = allTeams
                .Where(t => t.DirectorId == user.Id && !t.IsDeleted)
                .Select(t => t.Id)
                .ToList();

            // Get all nominations for the specified quarter where nominee is in director's teams
            var allNominations = await _nominationRepo.GetAllNominationsAsync();
            var quarterNominations = allNominations
                .Where(n => n.YearQuarterId == quarterId && 
                           n.Nominee != null && 
                           n.Nominee.TeamId.HasValue && 
                           directorTeamIds.Contains(n.Nominee.TeamId.Value))
                .ToList();

            // Get nomination IDs for this quarter
            var nominationIds = quarterNominations.Select(n => n.Id).ToList();

            // Get all director-level approvals from Approvals table
            var directorApprovals = await _context.Approvals
                .Where(a => nominationIds.Contains(a.NominationId) 
                    && a.Level == ApprovalLevel.Director 
                    && a.Action == ApprovalAction.Approved)
                .Select(a => a.NominationId)
                .Distinct()
                .ToListAsync();

            // Get all director-level rejections from Approvals table
            var directorRejections = await _context.Approvals
                .Where(a => nominationIds.Contains(a.NominationId) 
                    && a.Level == ApprovalLevel.Director 
                    && a.Action == ApprovalAction.Rejected)
                .Select(a => a.NominationId)
                .Distinct()
                .ToListAsync();

            // Get nominations with director action (approved or rejected)
            var nominationsWithDirectorAction = directorApprovals.Concat(directorRejections).Distinct().ToList();

            // Calculate statistics
            var totalNominations = quarterNominations.Count;
            // Pending Reviews = nominations that haven't been reviewed by director yet (no director-level action in Approvals table)
            var pendingReviews = quarterNominations.Count(n => 
                !nominationsWithDirectorAction.Contains(n.Id));
            // Count all nominations that have Director-level approval in Approvals table
            var finalApproved = directorApprovals.Count;
            // Count all nominations that have Director-level rejection in Approvals table
            var finalRejected = directorRejections.Count;

            var dashboardData = new
            {
                TotalNominations = totalNominations,
                PendingReviews = pendingReviews,
                FinalApproved = finalApproved,
                FinalRejected = finalRejected
            };

            return Ok(dashboardData);
        }

        // GET: api/dashboard/employee
        [HttpGet("Employee")]
        [Authorize]
        public async Task<IActionResult> GetEmployeeDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Get active quarter
            var activeQuarters = await _yearQuarterRepo.GetActiveAsync();
            var activeQuarter = activeQuarters.FirstOrDefault();

            // Get all nominations where the user is either nominator or nominee
            var allNominations = await _nominationRepo.GetAllNominationsAsync();
            var userNominations = allNominations
                .Where(n => n.NominatorId == user.Id || n.NomineeId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    NomineeName = n.Nominee?.Name ?? "N/A",
                    CategoryName = n.Category?.Name ?? "N/A",
                    Status = n.Status.ToString()
                })
                .ToList();

            var dashboardData = new
            {
                Nominations = userNominations,
                ActiveQuarterName = activeQuarter != null ? $"Q{(int)activeQuarter.Quarter}" : "",
                ActiveYearName = activeQuarter?.Year.ToString() ?? "",
                ActiveQuarterCloseDate = activeQuarter?.EndDate?.ToString("MMM dd, yyyy") ?? ""
            };

            return Ok(dashboardData);
        }

        // GET: api/dashboard/manager/teams
        [HttpGet("Manager/Teams")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagerTeams()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var allTeams = await _teamRepo.GetAllAsync();
            var managerTeams = allTeams
                .Where(t => t.ManagerId == user.Id && !t.IsDeleted)
                .Select(t => new
                {
                    TeamId = t.Id.ToString(),
                    TeamName = t.Name
                })
                .ToList();

            return Ok(managerTeams);
        }

        // GET: api/dashboard/director/teams
        [HttpGet("Director/Teams")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> GetDirectorTeams()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var allTeams = await _teamRepo.GetAllAsync();
            var directorTeams = allTeams
                .Where(t => t.DirectorId == user.Id && !t.IsDeleted)
                .Select(t => new
                {
                    TeamId = t.Id.ToString(),
                    TeamName = t.Name
                })
                .ToList();

            return Ok(directorTeams);
        }

        // GET: api/dashboard/director/managers
        [HttpGet("Director/Managers")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> GetDirectorManagers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Get all teams where the director is assigned
            var allTeams = await _teamRepo.GetAllAsync();
            var directorTeams = allTeams.Where(t => t.DirectorId == user.Id && !t.IsDeleted).ToList();

            // Get unique managers from these teams
            var managerIds = directorTeams
                .Where(t => !string.IsNullOrEmpty(t.ManagerId))
                .Select(t => t.ManagerId)
                .Distinct()
                .ToList();

            var managers = new List<object>();
            foreach (var managerId in managerIds)
            {
                var manager = await _userManager.FindByIdAsync(managerId);
                if (manager != null && !manager.IsDeleted)
                {
                    managers.Add(new
                    {
                        ManagerId = manager.Id,
                        ManagerName = manager.Name
                    });
                }
            }

            return Ok(managers);
        }

        // GET: api/dashboard/manager/teams-by-manager?managerId={managerId}
        [HttpGet("Manager/TeamsByManager")]
        [Authorize(Roles = "Director")]
        public async Task<IActionResult> GetTeamsByManager([FromQuery] string managerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(managerId))
            {
                return BadRequest("ManagerId is required");
            }

            var allTeams = await _teamRepo.GetAllAsync();
            var managerTeams = allTeams
                .Where(t => t.ManagerId == managerId && t.DirectorId == user.Id && !t.IsDeleted)
                .Select(t => new
                {
                    TeamId = t.Id.ToString(),
                    TeamName = t.Name
                })
                .ToList();

            return Ok(managerTeams);
        }

        // GET: api/dashboard/team/nominations?teamId={teamId}&yearQuarterId={yearQuarterId}
        [HttpGet("Team/Nominations")]
        [Authorize(Roles = "Manager,Director")]
        public async Task<IActionResult> GetTeamNominations([FromQuery] string teamId, [FromQuery] string yearQuarterId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(yearQuarterId))
            {
                return BadRequest("TeamId and YearQuarterId are required");
            }

            Guid teamGuid, quarterGuid;
            if (!Guid.TryParse(teamId, out teamGuid) || !Guid.TryParse(yearQuarterId, out quarterGuid))
            {
                return BadRequest("Invalid TeamId or YearQuarterId");
            }

            var allNominations = await _nominationRepo.GetAllNominationsAsync();
            var teamNominations = allNominations
                .Where(n => n.Nominee != null && n.Nominee.TeamId == teamGuid && n.YearQuarterId == quarterGuid)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    NomineeName = n.Nominee?.Name ?? "N/A",
                    CategoryName = n.Category?.Name ?? "N/A",
                    Description = n.Description ?? "",
                    Achievements = n.Achievements ?? "",
                    Status = n.Status.ToString(),
                    CreatedAt = n.CreatedAt.ToString("M/d/yyyy")
                })
                .ToList();

            return Ok(teamNominations);
        }

        private async Task<List<Nomination>> GetVisibleNominationsAsync(User user, string? role)
        {
            // Implement role-based filtering
            if (role == "Admin")
            {
                return (await _nominationRepo.GetAllNominationsAsync()).ToList();
            }
            else if (role == "Director" || role == "Manager")
            {
                // Assume method exists: GetNominationsForTeamsManagedByUserAsync
                // This should return nominations where Nominee.Team.ManagerId or DirectorId == userId
                return await _nominationRepo.GetNominationsForTeamsManagedByUserAsync(user.Id);
            }
            else // Employee, TeamLead
            {
                // Personal nominations + team nominations
                var personal = await _nominationRepo.GetNominationsByNominatorAsync(user.Id);
                var received = await _nominationRepo.GetNominationsForNomineeAsync(user.Id);
                var teamNoms = user.TeamId.HasValue ? await _nominationRepo.GetNominationsByTeamAsync(user.TeamId.Value) : new List<Nomination>();
                return personal.Concat(received).Concat(teamNoms).Distinct().ToList();
            }
        }

        private async Task<List<Nomination>> GetPendingApprovalNominationsAsync(string userId, string? role)
        {
            if (role == "Manager")
            {
                // Assume method exists: GetNominationsPendingManagerApprovalForUserAsync
                return await _nominationRepo.GetNominationsPendingManagerApprovalForUserAsync(userId);
            }
            else if (role == "Director")
            {
                // Assume method exists: GetNominationsPendingDirectorApprovalForUserAsync
                return await _nominationRepo.GetNominationsPendingDirectorApprovalForUserAsync(userId);
            }
            return new List<Nomination>();
        }

        private NominationView MapToNominationView(Nomination n)
        {
            // Map Nomination to NominationView (simplified; expand as needed)
            return new NominationView
            {
                Id = n.Id,
                NominatorId = n.NominatorId,
                Nominator = n.Nominator != null ? new UserView { Id = n.Nominator.Id, Name = n.Nominator.Name } : null,
                NomineeId = n.NomineeId,
                Nominee = n.Nominee != null ? new UserView { Id = n.Nominee.Id, Name = n.Nominee.Name } : null,
                CategoryId = n.CategoryId,
                Category = n.Category != null ? new CategoryView { Id = n.Category.Id, Name = n.Category.Name } : null,
                Description = n.Description,
                Achievements = n.Achievements,
                Status = n.Status,
                CreatedAt = n.CreatedAt,
                YearQuarterId = n.YearQuarterId,
                YearQuarter = n.YearQuarter != null ? new YearQuarterView { Id = n.YearQuarter.Id, Year = n.YearQuarter.Year, Quarter = n.YearQuarter.Quarter } : null,
                Approvals = n.Approvals?.Select(a => new ApprovalView { Id = a.Id, Action = a.Action, Level = a.Level }).ToList(),
                IsDeleted = n.IsDeleted
            };
        }
    }
}