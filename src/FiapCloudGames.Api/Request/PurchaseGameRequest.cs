using System.ComponentModel.DataAnnotations;

namespace FiapCloudGames.Api.Request
{
    /// <summary>
    /// Modelo de requisição para compra de jogos
    /// </summary>
    public class PurchaseGameRequest
    {
        /// <summary>
        /// ID do usuário que está comprando o jogo
        /// </summary>
        [Required]
        public int UserId { get; set; }
        
        /// <summary>
        /// ID do jogo a ser comprado
        /// </summary>
        [Required]
        public int GameId { get; set; }
        
        /// <summary>
        /// Preço pago pelo jogo
        /// </summary>
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero")]
        public decimal PurchasePrice { get; set; }

    }
    
}
