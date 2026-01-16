using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;
using RewardsAndRecognitionRepository.Data;
using Microsoft.EntityFrameworkCore;
using RewardsAndRecognitionWebAPI.ViewModels;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepo _userRepo;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly RewardsAndRecognitionRepository.Models.ApplicationDbContext _context;

        public UserController(
            IUserRepo userRepo,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            RewardsAndRecognitionRepository.Models.ApplicationDbContext context)
        {
            _userRepo = userRepo;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: api/user/roles
        [HttpGet("roles")]
        [AllowAnonymous]
        public IActionResult GetRoles()
        {
            var roles = new List<string> { "Admin", "TeamLead", "Manager", "Director", "Employee" };
            return Ok(roles);
        }

        // POST: api/user/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newUser = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                TeamId = model.TeamId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            if (!string.IsNullOrWhiteSpace(model.SelectedRole))
            {
                if (await _roleManager.RoleExistsAsync(model.SelectedRole))
                {
                    await _userManager.AddToRoleAsync(newUser, model.SelectedRole);
                }
            }

            return Ok(new
            {
                newUser.Id,
                newUser.UserName,
                newUser.Email,
                newUser.TeamId,
                RoleAssigned = model.SelectedRole
            });
        }

        // GET: api/user  (includes deleted)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepo.GetAllAsync();
            var userViews = new List<UserView>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                userViews.Add(new UserView
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email ?? "",
                    TeamId = u.TeamId,
                    TeamName = u.Team?.Name ?? "Not Assigned",
                    ManagerName = u.Team?.Manager?.Name ?? "No Manager",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = u.IsActive,
                    IsDeleted = u.IsDeleted
                });
            }

            return Ok(userViews);
        }

        // GET: api/user/active  (non-deleted only)
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var users = await _userRepo.GetActiveUsersAsync();
            var userViews = new List<UserView>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                userViews.Add(new UserView
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email ?? "",
                    TeamId = u.TeamId,
                    TeamName = u.Team?.Name ?? "Not Assigned",
                    ManagerName = u.Team?.Manager?.Name ?? "No Manager",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = u.IsActive,
                    IsDeleted = u.IsDeleted
                });
            }

            return Ok(userViews);
        }

        // GET: api/user/{id}  (return UserView to match Blazor Edit)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            if (u == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(u);

            return Ok(new UserView
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                TeamId = u.TeamId,
                TeamName = u.Team?.Name ?? "Not Assigned",
                ManagerName = u.Team?.Manager?.Name ?? "No Manager",
                Role = roles.FirstOrDefault() ?? "No Role",
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted
            });
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto model)
        {
            if (id != model.Id)
                return BadRequest("User ID mismatch.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.TeamId = model.TeamId;
            user.IsActive = model.IsActive;

            // Role update (single-role assumption)
            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(model.SelectedRole) && model.SelectedRole != currentRole)
            {
                if (currentRole != null)
                    await _userManager.RemoveFromRoleAsync(user, currentRole);

                if (await _roleManager.RoleExistsAsync(model.SelectedRole))
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            // Password update only if provided
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passRes = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passRes.Succeeded)
                    return BadRequest(passRes.Errors);
            }

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
                return BadRequest(updateRes.Errors);

            return NoContent();
        }

        // DELETE soft: api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _userRepo.SoftDeleteAsync(id);
            return NoContent();
        }

        // POST: api/user/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(string id)
        {
            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _userRepo.RestoreAsync(id);
            return NoContent();
        }

        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassigned()
        {
            return Ok(await _userRepo.GetUnassignedUsersAsync());
        }

        // GET: api/user/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([
            FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 5,
            [FromQuery] bool showDeleted = false,
            CancellationToken ct = default)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 1;
            const int MaxPageSize = 200;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            // base query includes navigation
            var baseQuery = _context.Users
                .Include(u => u.Team)
                    .ThenInclude(t => t.Manager)
                .AsQueryable();

            if (!showDeleted)
            {
                baseQuery = baseQuery.Where(u => u.IsDeleted == false && u.IsActive == true);
            }

            // stable ordering: CreatedAt desc, then Id desc
            baseQuery = baseQuery.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id);

            // project and page
            var paged = await baseQuery.ToPagedResultAsync<User, UserView>(pageNumber, pageSize,
                q => q.Select(u => new UserView
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email ?? string.Empty,
                    TeamId = u.TeamId,
                    TeamName = u.Team != null ? u.Team.Name : "Not Assigned",
                    ManagerName = u.Team != null && u.Team.Manager != null ? u.Team.Manager.Name : "No Manager",
                    Role = null, // will fill roles per-item below
                    IsActive = u.IsActive,
                    IsDeleted = u.IsDeleted
                }), ct);

            // fill roles for returned items (small N)
            var items = paged.Items;
            foreach (var item in items)
            {
                var user = await _userManager.FindByIdAsync(item.Id);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    item.Role = roles.FirstOrDefault() ?? "No Role";
                }
            }

            return Ok(paged);
        }

        [HttpGet("team/{teamId:guid}")]
        public async Task<IActionResult> GetUsersByTeam(Guid teamId)
        {
            return Ok(await _userRepo.GetUsersByTeamAsync(teamId));
        }

        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers()
        {
            return Ok(await _userRepo.GetAllManagersAsync());
        }

        [HttpGet("directors")]
        public async Task<IActionResult> GetDirectors()
        {
            return Ok(await _userRepo.GetAllDirectorsAsync());
        }

        [HttpGet("leads")]
        public async Task<IActionResult> GetTeamLeads([FromQuery] string? currentLeadId = null)
        {
            return Ok(await _userRepo.GetLeadsAsync(currentLeadId));
        }

        // GET: api/user/deleted
        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedUsers()
        {
            var users = await _userRepo.GetDeletedUsersAsync();
            var userViews = new List<UserView>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                userViews.Add(new UserView
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email ?? "",
                    TeamId = u.TeamId,
                    TeamName = u.Team?.Name ?? "Not Assigned",
                    ManagerName = u.Team?.Manager?.Name ?? "No Manager",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = u.IsActive,
                    IsDeleted = u.IsDeleted
                });
            }

            return Ok(userViews);
        }
    }
}
