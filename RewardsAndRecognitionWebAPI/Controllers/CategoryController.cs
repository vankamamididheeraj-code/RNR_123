using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Service;
using RewardsAndRecognitionRepository.Data;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepo _categoryRepo;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;

        public CategoryController(ICategoryRepo categoryRepo, RewardsAndRecognitionRepository.Models.ApplicationDbContext context)
        {
            _categoryRepo = categoryRepo;
            _context = context;
        }

        // GET: api/category
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeDeleted = false)
        {
            var categories = await _categoryRepo.GetAllAsync(includeDeleted);
            return Ok(categories);
        }

        // GET: api/category/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeleted()
        {
            var deleted = await _categoryRepo.GetDeletedAsync();
            return Ok(deleted);
        }

        // GET: api/category/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        // POST: api/category
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _categoryRepo.AddAsync(model);
            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/category/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Category model)
        {
            // if (id != model.Id)
            //     return BadRequest("Id mismatch.");

            // if (!ModelState.IsValid)
            //     return BadRequest(ModelState);

            // var existing = await _categoryRepo.GetByIdAsync(id);
            // if (existing == null)
            //     return NotFound();

            // await _categoryRepo.UpdateAsync(model);
            // return NoContent();
            if (id != model.Id)
         return BadRequest("Id mismatch.");
 
     if (!ModelState.IsValid)
         return BadRequest(ModelState);
 
     var existing = await _categoryRepo.GetByIdAsync(id);
     if (existing == null)
         return NotFound();
 
     // Update the tracked entity
     existing.Name = model.Name;
     existing.Description = model.Description;
     existing.isActive = model.isActive;
     existing.CreatedAt = model.CreatedAt;
 
     await _categoryRepo.UpdateAsync(existing);   // ✅ no conflict
     return NoContent();
        }

        // DELETE (soft): api/category/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            var exists = await _categoryRepo.ExistsAsync(id);
            if (!exists)
                return NotFound();

            await _categoryRepo.SoftDeleteAsync(id);
            return NoContent();
        }

        // DELETE hard: api/category/{id}/hard
        [HttpDelete("{id:guid}/hard")]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            var exists = await _categoryRepo.ExistsAsync(id);
            if (!exists)
                return NotFound();

            await _categoryRepo.DeleteAsync(id);
            return NoContent();
        }

        // GET: api/category/paged?pageNumber=&pageSize=&includeDeleted=
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            var baseQuery = _context.Categories.AsQueryable();

            if (!includeDeleted)
                baseQuery = baseQuery.Where(c => !c.IsDeleted);

            // stable ordering by Name then Id
            baseQuery = baseQuery.OrderBy(c => c.Name).ThenBy(c => c.Id);

            var paged = await baseQuery.ToPagedResultAsync<RewardsAndRecognitionRepository.Models.Category, RewardsAndRecognitionWebAPI.ViewModels.CategoryView>(
                pageNumber,
                pageSize,
                q => q.Select(c => new RewardsAndRecognitionWebAPI.ViewModels.CategoryView
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    isActive = c.isActive,
                    IsDeleted = c.IsDeleted,
                    CreatedAt = c.CreatedAt
                }),
                ct
            );

            return Ok(paged);
        }
    }
}
