using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FiapCloudGames.Domain.Entities
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.00, 100.00)]
        public decimal? DiscountPercentage { get; set; }

        [Range(0.00, 1000.00)]
        public decimal? DiscountAmount { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int GameId { get; set; }

        [JsonIgnore]
        public Game? Game { get; set; }

        public bool IsValidPromotion()
        {
            var now = DateTime.UtcNow;
            return IsActive && now >= StartDate && now <= EndDate;
        }

        public decimal CalculateDiscountedPrice()
        {
            if (!IsValidPromotion())
                return Game!.Price;

            if (DiscountAmount.HasValue)
            {
                return Math.Max(0, Game!.Price - DiscountAmount.Value);
            }

            var discountValue = Game!.Price * (DiscountPercentage / 100);
            return Math.Max(0, Game.Price - discountValue!.Value);
        }
    }
}
