# FiapCloudGames.Users

## Visão Geral
Serviço responsável por `User` (conta, perfil, credenciais de autenticação) e `Library` (coleção de jogos adquiridos pelo usuário). É o dono do estado de propriedade dos jogos.

## Responsabilidades
- Gerenciar contas de usuário (registro, login, atualização de perfil).
- Gerenciar `Library` (adicionar/consultar jogos do usuário).
- Consumir eventos de compra (`PurchaseCompleted`) para materializar propriedade.
- Expor endpoints para consulta de biblioteca e administração de usuários.

## Modelos principais (resumo)
- User
  - `Id: Guid`
  - `Email: string`
  - `PasswordHash: string`
  - `DisplayName: string`
  - `CreatedAt`, `LastLogin`
- LibraryEntry
  - `Id: Guid`
  - `UserId: Guid`
  - `GameId: Guid`
  - `PurchaseId: Guid` (referência ao pagamento/evento)
  - `AcquiredAt: DateTimeOffset`

## Banco de Dados
- Banco: `users_db`
- Tabelas: `Users`, `Library`, `UserSessions` (tokens/refresh, se aplicável)

## Endpoints REST (exemplos)
- `POST /api/users` — registrar usuário (body: `email`, `password`, `displayName`)
- `POST /api/users/login` — autenticar (retorna JWT)
- `GET /api/users/{id}` — obter perfil
- `GET /api/users/{id}/library` — listar jogos do usuário
- `POST /api/users/{id}/library` — adicionar jogo manualmente (admin)

### Exemplo: `GET /api/users/{id}/library` (response)
```
[
  {
    "gameId": "guid",
    "purchaseId": "guid",
    "acquiredAt": "2025-11-17T10:00:00Z"
  }
]
```

## Eventos consumidos
- `PurchaseCompleted { purchaseId, userId, gameId, amount, currency, timestamp, quoteId? }` — ao receber, o serviço deve:
  1. Verificar idempotency (se `purchaseId` já processado, ignorar).
  2. Criar `LibraryEntry` com `purchaseId` e `acquiredAt`.
  3. (Opcional) Publicar `LibraryUpdated` ou `UserOwnedGameAdded` para outros serviços.

### Regras do MVP relacionadas a ofertas e TTL
- O sistema opera com `PriceOffers` com TTL de **1 hora**. O `Payments` recebe requisições de compra de forma assíncrona via mensageria e valida o `PriceOffer` antes de capturar o pagamento.
- Política do MVP: se o `PriceOffer` expirou (agora > `offeredUntil`), o `Payments` publicará `PurchaseRejected` e **não** processará/estornará pagamentos — ou seja, o usuário deverá iniciar nova compra com preço atual.
- `Users` deve tratar mensagens `PurchaseRejected` (por exemplo, para informar o usuário ou registrar métricas). Mesmo que o `Users` não seja responsável por aceitar/refutar pagamento, é recomendável que ele registre notificações sobre tentativas de compra rejeitadas para suporte/analytics.

## Eventos publicados
- `UserCreated` (após registro)
- `LibraryUpdated { userId, gameId, purchaseId }` (opcional)

## Regras de negócio críticas
- Idempotência: armazenar `processedPurchaseIds` ou checar `Library` por `purchaseId` antes de inserir.
- Autorização: endpoints de biblioteca apenas para dono ou admin.
- Dados sensíveis: `PasswordHash` nunca exposto; use salted hashing (Argon2/Bcrypt) e práticas de segurança.

## Fluxo de compra (consumer)
1. `Payments` publica `PurchaseCompleted`.
2. `Users` consome e valida: garantir que `userId` existe.
3. `Users` grava `LibraryEntry` e confirma processamento.
4. Opcional: enviar notificação por email via serviço de mensagens.

## Segurança
- JWT para autenticação — `AuthController` local ao serviço.
- Proteção contra brute-force e rate-limiting em endpoints de login.

## Observabilidade
- Métricas: `users_registered_total`, `library_additions_total`, `purchase_events_processed_total`.
- Logs com `purchaseId` e `traceId` para rastreabilidade.

Adicional (MVP):
- Métricas: `purchase_rejected_offer_expired_total` — contar quantas compras foram rejeitadas por TTL expirado.

## Migração / Deploy
- Projeto: `FiapCloudGames.Users` contendo API, Domain, Persistence
- Migrations EF Core em `Migrations/Users`

## Testes / QA
- Testes unitários para o consumidor de `PurchaseCompleted` (idempotência, falhas temporárias).
- Testes de integração: simular evento `PurchaseCompleted` e validar `Library` atualizado.

## Checklists de implementação
- [ ] Implementar handler de evento `PurchaseCompleted` com idempotência
- [ ] Expor endpoint `GET /api/users/{id}/library`
- [ ] Criar testes de integração para fluxo de compra

## Observações finais
- `Users` é o dono do agregado `Library`, garantindo que a posse dos jogos seja consistente e local ao serviço. A separação melhora segurança e escalabilidade.
