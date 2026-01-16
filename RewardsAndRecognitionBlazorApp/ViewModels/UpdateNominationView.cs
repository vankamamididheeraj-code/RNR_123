using System.ComponentModel.DataAnnotations;

namespace RewardsAndRecognitionBlazorApp.ViewModels
{
    public class UpdateNominationView
    {
        public Guid Id { get; set; }

        [Required] public Guid? CategoryId { get; set; }
        [Required] public Guid? YearQuarterId { get; set; }

        [Required, StringLength(2000)]
        public string? Description { get; set; }

        [Required, StringLength(2000)]
        public string? Achievements { get; set; }
    }
}
