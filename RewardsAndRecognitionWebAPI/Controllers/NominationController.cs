using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionBlazorApp.ViewModels;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Data;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NominationController : ControllerBase
    {
        private readonly INominationRepo _nominationRepo;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;

        public NominationController(INominationRepo nominationRepo, RewardsAndRecognitionRepository.Models.ApplicationDbContext context)
        {
            _nominationRepo = nominationRepo;
            _context = context;
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
                .Include(n => n.Nominee)
                .Include(n => n.Category)
                .Include(n => n.YearQuarter)
                .AsQueryable();

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

            var paged = await baseQuery.ToPagedResultAsync<RewardsAndRecognitionRepository.Models.Nomination, RewardsAndRecognitionWebAPI.ViewModels.NominationView>(pageNumber, pageSize,
                qry => qry.Select(n => new RewardsAndRecognitionWebAPI.ViewModels.NominationView
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
                    IsDeleted = n.IsDeleted
                }), ct);

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

            nomination.Status = RewardsAndRecognitionRepository.Enums.NominationStatus.PendingManager;
            nomination.CreatedAt = DateTime.UtcNow;

            await _nominationRepo.AddNominationAsync(nomination);

            return Ok(nomination);
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
    }
}
