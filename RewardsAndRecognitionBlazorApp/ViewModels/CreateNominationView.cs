using System.ComponentModel.DataAnnotations;
 
namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class CreateNominationView

    {

        public string? NominatorId { get; set; }

        public Guid YearQuarterId { get; set; }
 
        // Dropdown binding

        [Required(ErrorMessage = "Nominee is required")]

        public string? NomineeId { get; set; }
 
        [Required(ErrorMessage = "Category is required")]

        public string? CategoryId { get; set; }
 
        [Required(ErrorMessage = "Description is required")]

        public string? Description { get; set; }
 
        [Required(ErrorMessage = "Achievements is required")]

        public string? Achievements { get; set; }

    }

}

 