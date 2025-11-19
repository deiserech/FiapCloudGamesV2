using FiapCloudGames.Users.Application.Interfaces.Services;
using FiapCloudGames.Users.Domain.Entities;
using FiapCloudGames.Users.Application.Interfaces.Services;
using FiapCloudGames.Users.Domain.Events;
using Microsoft.Extensions.Logging;

namespace FiapCloudGames.Users.Application.Services;

public class PurchaseService(ILibraryService libraryService, ILogger<PurchaseService> logger) : IPurchaseService
{

    public async Task ProcessAsync(PurchaseCompletedEvent message, CancellationToken cancellationToken = default)
    {
        var library = Library.FromEvent(message);

        await libraryService.CreateAsync(library);

        logger.LogInformation("Library created for UserId: {UserId}, GameId: {GameId}, PurchaseId: {PurchaseId}",
            message.UserId, message.GameId, message.PurchaseId);
    }
}
