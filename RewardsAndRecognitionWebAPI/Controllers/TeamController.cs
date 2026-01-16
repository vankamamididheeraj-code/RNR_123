using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Data;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly ITeamRepo _teamRepo;
        private readonly IUserRepo _userRepo;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;

        public TeamController(ITeamRepo teamRepo, IUserRepo userRepo, RewardsAndRecognitionRepository.Models.ApplicationDbContext context)
        {
            _teamRepo = teamRepo;
            _userRepo = userRepo;
            _context = context;
        }

        // GET: api/team
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false)
        {
            var teams = await _teamRepo.GetAllAsync(includeDeleted);
            return Ok(teams);
        }

        // GET: api/team/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            return Ok(await _teamRepo.GetDeletedAsync());
        }

        // GET: api/team/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([
            FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] bool includeDeleted = false,
            CancellationToken ct = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            const int MaxPageSize = 200;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _context.Teams.AsQueryable();

            if (!includeDeleted)
                baseQuery = baseQuery.Where(t => t.IsDeleted == false);

            // stable order: Name desc then Id desc
            baseQuery = baseQuery.OrderByDescending(t => t.Name).ThenByDescending(t => t.Id);

            var paged = await baseQuery.ToPagedResultAsync<RewardsAndRecognitionRepository.Models.Team, Team>(pageNumber, pageSize,
                q => q.Select(t => new Team
                {
                    Id = t.Id,
                    Name = t.Name,
                    TeamLeadId = t.TeamLeadId,
                    ManagerId = t.ManagerId,
                    DirectorId = t.DirectorId,
                    IsDeleted = t.IsDeleted
                }), ct);

            return Ok(paged);
        }

        // GET: api/team/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var team = await _teamRepo.GetByIdAsync(id);
            if (team == null)
                return NotFound();

            return Ok(team);
        }

        // POST: api/team
        [HttpPost]
        public async Task<IActionResult> Create(Team team)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate FK Users
            if (await _userRepo.GetByIdAsync(team.TeamLeadId) == null)
                return BadRequest("Invalid TeamLeadId.");

            if (await _userRepo.GetByIdAsync(team.ManagerId) == null)
                return BadRequest("Invalid ManagerId.");

            if (await _userRepo.GetByIdAsync(team.DirectorId) == null)
                return BadRequest("Invalid DirectorId.");

            await _teamRepo.AddAsync(team);
            return Ok(team);
        }

        // PUT: api/team/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, Team updatedTeam)
        {
            if (id != updatedTeam.Id)
                return BadRequest("Team ID mismatch.");

            var existing = await _teamRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            // Validate FK Users
            if (await _userRepo.GetByIdAsync(updatedTeam.TeamLeadId) == null)
                return BadRequest("Invalid TeamLeadId.");

            if (await _userRepo.GetByIdAsync(updatedTeam.ManagerId) == null)
                return BadRequest("Invalid ManagerId.");

            if (await _userRepo.GetByIdAsync(updatedTeam.DirectorId) == null)
                return BadRequest("Invalid DirectorId.");

            // ✅ update the tracked entity (no duplicate tracking)
            existing.Name = updatedTeam.Name;
            existing.TeamLeadId = updatedTeam.TeamLeadId;
            existing.ManagerId = updatedTeam.ManagerId;
            existing.DirectorId = updatedTeam.DirectorId;

            await _teamRepo.UpdateAsync(existing);
            return NoContent();
        }

            // DELETE soft: api/team/{id}
            [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var team = await _teamRepo.GetByIdAsync(id);
            if (team == null)
                return NotFound();

            await _teamRepo.SoftDeleteAsync(id);
            return NoContent();
        }

        // DELETE hard: api/team/{id}/hard
        [HttpDelete("{id:guid}/hard")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var team = await _teamRepo.GetByIdAsync(id);
            if (team == null)
                return NotFound();

            await _teamRepo.DeleteAsync(team);
            return NoContent();
        }
    }
}
