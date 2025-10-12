using FiapCloudGames.Domain.Entities;
using FiapCloudGames.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FiapCloudGames.Api.Controllers
{
    /// <summary>
    /// Controller responsável pelo gerenciamento de promoções de jogos
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        /// <summary>
        /// Obtém todas as promoções ativas
        /// </summary>
        /// <returns>Lista de promoções ativas</returns>
        /// <response code="200">Retorna a lista de promoções ativas</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("active")]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(IEnumerable<Promotion>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Promotion>>> GetActivePromotions()
        {
            try
            {
                var promotions = await _promotionService.GetActivePromotionsAsync();
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtém uma promoção específica pelo ID
        /// </summary>
        /// <param name="id">ID da promoção</param>
        /// <returns>Dados da promoção</returns>
        /// <response code="200">Retorna a promoção encontrada</response>
        /// <response code="404">Promoção não encontrada</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(Promotion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Promotion>> GetPromotion(int id)
        {
            try
            {
                var promotion = await _promotionService.GetPromotionByIdAsync(id);
                if (promotion == null)
                {
                    return NotFound();
                }
                return Ok(promotion);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtém todas as promoções ativas de um jogo específico
        /// </summary>
        /// <param name="gameId">ID do jogo</param>
        /// <returns>Lista de promoções ativas do jogo</returns>
        /// <response code="200">Retorna a lista de promoções ativas do jogo</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("game/{gameId}/active")]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(IEnumerable<Promotion>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Promotion>>> GetActivePromotionsByGame(int gameId)
        {
            try
            {
                var promotions = await _promotionService.GetActivePromotionsByGameIdAsync(gameId);
                return Ok(promotions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Calcula o preço com desconto para um jogo específico
        /// </summary>
        /// <param name="gameId">ID do jogo</param>
        /// <returns>Preço original e preço com desconto aplicado</returns>
        /// <response code="200">Retorna o preço com desconto calculado</response>
        /// <response code="400">Erro na solicitação</response>
        [HttpGet("game/{gameId}/discounted-price")]
        [Authorize(Roles = "Admin, User")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<decimal>> GetDiscountedPrice(int gameId)
        {
            try
            {
                var discountedPrice = await _promotionService.GetDiscountedPriceAsync(gameId);
                return Ok(new {DiscountedPrice = discountedPrice });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cria uma nova promoção
        /// </summary>
        /// <param name="promotion">Dados da nova promoção</param>
        /// <returns>Promoção criada</returns>
        /// <response code="201">Promoção criada com sucesso</response>
        /// <response code="400">Dados inválidos ou erro na validação</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Promotion), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Promotion>> CreatePromotion([FromBody] Promotion promotion)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdPromotion = await _promotionService.CreatePromotionAsync(promotion);
                return CreatedAtAction(nameof(GetPromotion), new { id = createdPromotion.Id }, createdPromotion);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Atualiza uma promoção existente
        /// </summary>
        /// <param name="id">ID da promoção a ser atualizada</param>
        /// <param name="promotion">Dados atualizados da promoção</param>
        /// <returns>Promoção atualizada</returns>
        /// <response code="200">Promoção atualizada com sucesso</response>
        /// <response code="400">Dados inválidos ou ID não confere</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(Promotion), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Promotion>> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {
            try
            {
                if (id != promotion.Id)
                {
                    return BadRequest("ID da promoção não confere.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedPromotion = await _promotionService.UpdatePromotionAsync(promotion);
                return Ok(updatedPromotion);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Remove uma promoção
        /// </summary>
        /// <param name="id">ID da promoção a ser removida</param>
        /// <returns>Resposta vazia em caso de sucesso</returns>
        /// <response code="204">Promoção removida com sucesso</response>
        /// <response code="404">Promoção não encontrada</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeletePromotion(int id)
        {
            try
            {
                await _promotionService.DeletePromotionAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
