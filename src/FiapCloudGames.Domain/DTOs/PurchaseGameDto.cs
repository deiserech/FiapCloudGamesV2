using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Domain.DTOs
{
    public class PurchaseGameDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int GameId { get; set; }

        [Required]
        [Range(0.01, 1000.00)]
        public decimal PurchasePrice { get; set; }
    }
}
