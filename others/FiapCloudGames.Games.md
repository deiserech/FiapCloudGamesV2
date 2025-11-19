# FiapCloudGames.Games

## Visão Geral
Serviço responsável pelo catálogo de jogos (`Game`) e pelas promoções (`Promotion`). Mantém o preço canônico, regras de promoção e fornece cotações de preço quando necessário.

## Responsabilidades
- Gerenciar entidade `Game` (metadados, preço base, estoque lógico se houver).
- Gerenciar `Promotion` (regras, validade, percentuais/descontos fixos).
- Fornecer `PriceQuote` para checkout (opcionalmente assinado).
- Publicar eventos relacionados a promoções e catálogo.

## Modelos principais (resumo)
- Game
  - `Id: Guid`
  - `Title: string`
  - `Description: string`
  - `Price: decimal`
  - `Currency: string`
  - `CreatedAt`, `UpdatedAt`
- Promotion
  - `Id: Guid`
  - `GameId: Guid`
  - `DiscountPercent: int` (ou `DiscountAmount: decimal`)
  - `ValidFrom: DateTimeOffset`
  - `ValidTo: DateTimeOffset`
  - `Version: int`

## Banco de Dados
- Banco: `games_db` (isolado por serviço)
- Tabelas mínimas: `Games`, `Promotions`, `GameStatistics` (opcional)

## Endpoints REST (exemplos)
- `GET /api/games` — lista de jogos (filtros: category, search, page)
- `GET /api/games/{id}` — detalhe do jogo, inclui preço atual e promoções ativas
- `POST /api/games` — criar jogo (admin)
- `PUT /api/games/{id}` — atualizar jogo (admin)
- `POST /api/games/{id}/promotions` — criar promoção para jogo (admin)
- `GET /api/games/{id}/promotions` — listar promoções ativas
- `POST /api/games/{id}/quote` — gerar `PriceQuote` (ver seção Quote abaixo)

### Exemplo: `GET /api/games/{id}` (response)
```
{
  "id": "00000000-0000-0000-0000-000000000001",
  "title": "Super Game",
  "price": 59.90,
  "currency": "BRL",
  "activePromotion": {
    "id": "...",
    "discountPercent": 20,
    "validFrom": "2025-11-01T00:00:00Z",
    "validTo": "2025-11-30T23:59:59Z"
  }
}
```

## PriceQuote (pattern recomendado)
- Objetivo: permitir `Payments` (ou o cliente) obter um preço autoritativo com validade curta sem exigir uma chamada síncrona a cada checkout.
- Response do `POST /api/games/{id}/quote`:
```
{
  "quoteId": "guid",
  "gameId": "guid",
  "price": 47.92,
  "currency": "BRL",
  "validUntil": "2025-11-17T12:34:56Z",
  "signature": "BASE64(HMAC_SHA256(payload, secret))"
}
```
- `signature` deve ser validada por `Payments`. Implementar rotação/armazenamento seguro de chaves e expiração curta (ex.: 2-5 minutos).

### Regras do MVP (decisões atuais)
- `Payments` opera de forma assíncrona via mensageria; o fluxo de compra não espera resposta síncrona do `Games`.
- Para suportar o processamento assíncrono, o serviço pode emitir uma `PriceOffer` (quote persistido) com `offeredUntil` maior (TTL configurável). Por padrão do MVP o TTL acordado é de **1 hora**.
- `Games` deve expor endpoint para gerar `PriceOffer`/`PriceQuote` e persistir o registro em banco (campo `OfferedUntil`). Esse registro serve como garantia de preço para `Payments` consumir de forma assíncrona.
- Política do MVP: se o `Payments` processar a compra após `OfferedUntil`, o pagamento será recusado (sem tentativa de estorno pelo MVP). Portanto o `Games` deve comunicar claramente ao cliente o tempo de garantia do preço.

## Eventos publicados
- `PromotionCreated { promotionId, gameId, version, discountPercent, validFrom, validTo }`
- `PromotionUpdated { promotionId, gameId, version, ... }`
- `PromotionDeleted { promotionId, gameId }`
- `PriceQuoteIssued { quoteId, gameId, price, currency, validUntil }` (opcional)
- `GameUpdated` / `GameCreated` (opcional, para read-models)

Adicional (MVP):
- `PriceOfferCreated { quoteId, gameId, userId, offeredPrice, offeredUntil }` — publicado quando é criada a oferta persistida que o `Payments` irá validar de forma assíncrona.

Formato de evento: JSON (incluir `traceId`, `timestamp`, `sourceService`). Use schema versioning.

## Consumo de eventos
- Opcionalmente consumir `PurchaseCompleted` para atualizar estatísticas de venda.

## Regras de negócio críticas
- Promoções têm `version` e `validUntil` — consumidores devem aplicar em ordem e ignorar versões antigas.
- `PriceQuote` vencido não deve ser aceito; cobrança somente com quote válido ou validação síncrona.

## Segurança
- Endpoints administrativos atrás de autenticação/authorization (JWT com roles/claims).
- Assinatura HMAC/RSA para `PriceQuote`.
- TLS obrigatório.

## Observabilidade
- Expor métricas Prometheus: `requests_total`, `quotes_issued_total`, `promotions_active`.
- Logs estruturados com `traceId` distribuído.

## Migração / Deploy
- Projetos: `FiapCloudGames.Games` (API + Domain + Persistence)
- Migration scripts EF Core em `Migrations/Games`.

## Checklists de implementação
- [ ] Endpoints CRUD para `Game` e `Promotion`
- [ ] Endpoint `POST /api/games/{id}/quote` com assinatura
- [ ] Publicação de eventos ao criar/atualizar/deletar promoções
- [ ] Testes unitários do cálculo de preço e aplicação de promoção

- [ ] Endpoint `POST /api/games/{id}/offer` (MVP) que cria `PriceOffer` persistido com `OfferedUntil = now + 1h` e publica `PriceOfferCreated`.

## Observações finais
- Mantém a autoridade do preço e regras de promoção; fundamental para evitar inconsistências no fluxo de pagamentos.
