using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FiapCloudGames.Domain.Entities
{
    public class Game
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 1000.00)]
        public decimal Price { get; set; }

        [JsonIgnore]
        public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

        [JsonIgnore]
        public ICollection<Library> LibraryEntries { get; set; } = new List<Library>();
    }
}
