using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Domain.DTOs
{
    public class CreatePromotionDto
    {
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 100.00)]
        public decimal DiscountPercentage { get; set; }

        [Range(0.01, 1000.00)]
        public decimal? DiscountAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public int GameId { get; set; }
    }
}
