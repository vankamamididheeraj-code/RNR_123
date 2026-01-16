using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RewardsAndRecognitionRepository.Enums;
using RewardsAndRecognitionRepository.Interfaces;
using RewardsAndRecognitionRepository.Models;

namespace RewardsAndRecognitionWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApprovalController : ControllerBase
    {
        //private readonly IApprovalRepo _approvalRepo;
        //private readonly INominationRepo _nominationRepo;

        //public ApprovalController(IApprovalRepo approvalRepo, INominationRepo nominationRepo)
        //{
        //    _approvalRepo = approvalRepo;
        //    _nominationRepo = nominationRepo;
        //}

        //// GET: api/approval/nomination/{nominationId}
        //[HttpGet("nomination/{nominationId:guid}")]
        //public async Task<IActionResult> GetByNomination(Guid nominationId)
        //{
        //    var results = await _approvalRepo.GetApprovalsByNominationIdAsync(nominationId);
        //    return Ok(results);
        //}

        //// GET: api/approval/approver/{approverId}
        //[HttpGet("approver/{approverId}")]
        //public async Task<IActionResult> GetByApprover(string approverId)
        //{
        //    var results = await _approvalRepo.GetApprovalsByApproverIdAsync(approverId);
        //    return Ok(results);
        //}

        //// POST: api/approval
        //[HttpPost]
        //public async Task<IActionResult> Approve(Approval approval)
        //{
        //    // Prevent duplicate approvals
        //    if (await _approvalRepo.ApproverHasAlreadyApprovedAsync(approval.ApproverId, approval.NominationId))
        //        return BadRequest("Approver already approved this nomination.");

        //    approval.ActionAt = DateTime.UtcNow;

        //    var saved = await _approvalRepo.CreateApprovalAsync(approval);

        //    // Update nomination status
        //    var nomination = await _nominationRepo.GetNominationByIdAsync(approval.NominationId);

        //    if (nomination == null)
        //        return NotFound("Nomination not found.");

        //    if (approval.Level == ApprovalLevel.Manager)
        //    {
        //        nomination.Status =
        //            approval.Action == ApprovalAction.Approved ?
        //            NominationStatus.PendingDirector :
        //            NominationStatus.ManagerRejected;
        //    }
        //    else if (approval.Level == ApprovalLevel.Director)
        //    {
        //        nomination.Status =
        //            approval.Action == ApprovalAction.Approved ?
        //            NominationStatus.DirectorApproved :
        //            NominationStatus.DirectorRejected;
        //    }

        //    await _nominationRepo.UpdateNominationAsync(nomination);

        //    return Ok(saved);
        //}
    }
}
