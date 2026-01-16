using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionWebAPI.ViewModels;
using RewardsAndRecognitionRepository.Data;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YearQuarterController : ControllerBase
    {
        private readonly IYearQuarterRepo _yearQuarterRepo;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;

        public YearQuarterController(IYearQuarterRepo yearQuarterRepo, RewardsAndRecognitionRepository.Models.ApplicationDbContext context)
        {
            _yearQuarterRepo = yearQuarterRepo;
            _context = context;
        }

        // GET: api/yearquarter
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _yearQuarterRepo.GetAllAsync());
        }

        // GET: api/yearquarter/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            return Ok(await _yearQuarterRepo.GetActiveAsync());
        }

        // GET: api/yearquarter/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([
            FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeDeleted = false,
            CancellationToken ct = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            const int MaxPageSize = 200;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var baseQuery = _context.YearQuarters.AsQueryable();

            if (!includeDeleted)
                baseQuery = baseQuery.Where(yq => yq.IsDeleted == false);

            baseQuery = baseQuery.OrderByDescending(yq => yq.Year).ThenByDescending(yq => yq.Quarter);

            var paged = await baseQuery.ToPagedResultAsync<RewardsAndRecognitionRepository.Models.YearQuarter, YearQuarterView>(pageNumber, pageSize,
                q => q.Select(yq => new YearQuarterView
                {
                    Id = yq.Id,
                    Quarter = yq.Quarter,
                    Year = yq.Year,
                    IsActive = yq.IsActive,
                    StartDate = yq.StartDate,
                    EndDate = yq.EndDate,
                    IsDeleted = yq.IsDeleted
                }), ct);

            return Ok(paged);
        }

        // GET: api/yearquarter/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            return Ok(await _yearQuarterRepo.GetDeletedAsync());
        }

        // GET: api/yearquarter/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var yq = await _yearQuarterRepo.GetByIdAsync(id);
            return yq == null ? NotFound() : Ok(yq);
        }

        // POST: api/yearquarter
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]YearQuarter yq)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _yearQuarterRepo.AddAsync(yq);
            return Ok(yq);
        }

        // PUT: api/yearquarter/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, YearQuarter yq)
        {
            // if (id != yq.Id)
            //     return BadRequest("Mismatched ID");

            // var existing = await _yearQuarterRepo.GetByIdAsync(id);
            // if (existing == null)
            //     return NotFound();

            // await _yearQuarterRepo.UpdateAsync(yq);
            // return NoContent();
            if (id != yq.Id)
        return BadRequest("Mismatched ID");

    try
    {
        await _yearQuarterRepo.UpdateAsync(yq);
        return NoContent();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
    {
        return NotFound();
    }
        }

        // DELETE soft: api/yearquarter/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            await _yearQuarterRepo.SoftDeleteAsync(id);
            return NoContent();
        }

        // POST restore: api/yearquarter/{id}/restore
        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _yearQuarterRepo.RestoreAsync(id);
            return NoContent();
        }
    }
}
