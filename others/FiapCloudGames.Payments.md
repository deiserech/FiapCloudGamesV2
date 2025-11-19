# FiapCloudGames.Payments

## Visão Geral
Serviço responsável por processar pagamentos, armazenar transações/auditoria e orquestrar o fluxo de compra. Não é dono do `Library` nem das promoções; usa `PriceQuote` ou validação síncrona com `Games`.

## Responsabilidades
- Receber requisições de compra (`POST /api/payments/purchase`).
- Validar `PriceQuote` (ou consultar `Games` quando necessário).
- Processar autorização/capture (integração com gateway de pagamento externo).
- Persistir transação em `payments_db` e publicar eventos (`PurchaseCompleted`, `PurchaseFailed`).
- Garantir idempotência de pedidos.

Regras do MVP (decisões atualizadas):
- O `Payments` processará compras de forma **assíncrona** consumindo mensagens `PurchaseRequested` publicadas na fila pelo front-end/API.
- O sistema usa `PriceOffers` persistidos com **TTL = 1 hora**. `Payments` valida `PriceOffer` consultando a tabela `PriceOffers` antes de processar. Se o `PriceOffer` expirou, o `Payments` publicará `PurchaseRejected` e não processará o pagamento (sem reembolso no MVP).
- O endpoint síncrono `POST /api/payments/purchase` pode existir apenas como publisher da mensagem `PurchaseRequested` (gateway HTTP -> publica na fila) para o fluxo assíncrono.

## Modelos principais (resumo)
- PaymentTransaction
  - `PurchaseId: Guid`
  - `UserId: Guid`
  - `GameId: Guid`
  - `Amount: decimal`
  - `Currency: string`
  - `Status: enum { Requested, Authorized, Captured, Completed, Failed }`
  - `CreatedAt`, `UpdatedAt`

## Banco de Dados
- Banco: `payments_db`
- Tabelas: `Transactions`, `PaymentAttempts` (audit)

## Endpoint principal
- `POST /api/payments/purchase` — body mínimo:
```
{
  "userId": "guid",
  "gameId": "guid",
  "quote": { /* opcional: quote do Games */ },
  "paymentMethod": { "type": "card", "token": "..." },
  "idempotencyKey": "string"
}
```

### Comportamento do endpoint
1. Verifica `idempotencyKey` para evitar duplicação.
2. Se `quote` presente: valida assinatura e `validUntil`.
3. Se `quote` ausente ou inválido: opcionalmente chamar `GET /api/games/{id}` para recuperar preço atual (ou recusar).
4. Chama gateway de pagamento (autorizar/capturar conforme fluxo configurado).
5. Persiste transação com `Status` apropriado.
6. Publica evento `PurchaseCompleted` ou `PurchaseFailed`.

### Exemplo de resposta (sucesso)
```
{
  "purchaseId": "guid",
  "status": "Completed",
  "amount": 47.92,
  "currency": "BRL",
  "timestamp": "2025-11-17T11:00:00Z"
}
```

## Eventos publicados
- `PurchaseCompleted { purchaseId, userId, gameId, amount, currency, timestamp }`
- `PurchaseFailed { purchaseId, userId, gameId, reason, timestamp }`
- `PurchaseRequested` (opcional, para orquestração/saga)

Adicionais (MVP):
- `PurchaseRequested { purchaseId, quoteId, userId, gameId, offeredPrice, currency, idempotencyKey, requestedAt }` — publicada pelo front-end/API para a fila `purchases.requested`.
- `PurchaseRejected { purchaseId, userId, gameId, reason, processedAt, quoteId }` — publicada quando o `PriceOffer` expirou ou falhou validação.

## Segurança / Idempotência
- Requer `idempotencyKey` para requests de compra do cliente. Salvar mapping `idempotencyKey -> purchaseId`.
- Validar `PriceQuote.signature` com chave pública/HMAC.
- Proteção contra replay: quote tem `validUntil` e `quoteId`.

Observações de idempotência para mensageria (MVP):
- Mensageria normalmente entrega *at-least-once*. O consumer em `Payments` deve checar se já existe uma `Transaction` com o mesmo `purchaseId` ou `idempotencyKey` e ignorar mensagens duplicadas.
- Ao processar mensagem, marcar o `PriceOffer` como `Used` (ou `Using`) de forma transacional para evitar race conditions.

## Estratégias de consistência
- Pattern recomendado: `PriceQuote` assinado por `Games` + validação no `Payments`.
- Alternativa: replicar read-model em `Payments` mas validar sincronamente antes de capturar (fallback).
- Saga simples: `PurchaseRequested` -> `Payments` processa -> `PurchaseCompleted` -> `Users` consome.

## Tratamento de falhas e reconciliação
- Publicar eventos de tentativa e falha; manter logs/auditoria.
- Job de reconciliação que compara `payments_db` vs `users_db` (via events ou chamadas) para detectar compras não materializadas.

Comportamento MVP específico:
- TTL das ofertas é de 1 hora. Se um `PurchaseRequested` chegar após `offeredUntil`, o consumer deve publicar `PurchaseRejected` com `reason = "OfferExpired"` e não tentar estorno.
- Não haverá lógica de reembolso no MVP. Se a cobrança foi indevidamente feita por erro de infra, a correção ficará manual ou para versão futura.

## Integração com gateway (exemplo)
- Implementar provider pattern para suportar múltiplos gateways (Stripe, Adyen, PagSeguro).
- Usar ambiente `sandbox` para testes e `production` para produção.

## Observabilidade
- Métricas: `payments_requests_total`, `payments_success_total`, `payments_failed_total`, `payment_processing_latency_seconds`.
- Logs estruturados com `idempotencyKey`, `purchaseId`, `traceId`.

Adicional (MVP):
- Métricas: `purchase_requested_total`, `purchase_rejected_offer_expired_total`, `purchase_completed_total`.
- Endpoint de status: `GET /api/payments/{purchaseId}` — fornece estado atual (`Pending`, `Completed`, `Rejected`, `Failed`) para polling do cliente.

## Mecanismos de segurança e compliance
- Nunca armazenar dados sensíveis do cartão (use tokens/PCI compliant gateways).
- AES/HSM para segredos de gateway.

## Checklists de implementação
- [ ] Endpoint `POST /api/payments/purchase` com idempotência
- [ ] Validação de `PriceQuote` (HMAC/RSA)
- [ ] Integração com gateway de pagamento (provider pattern)
- [ ] Publicação de eventos `PurchaseCompleted`/`PurchaseFailed`
- [ ] Job de reconciliação e monitoramento

- [ ] Implementar tabela `PriceOffers` (TTL 1h) e endpoint `POST /api/payments/offer` ou `POST /api/games/{id}/offer` para emissão de ofertas persistidas.
- [ ] Implementar publisher HTTP que publica `PurchaseRequested` na fila `purchases.requested`.
- [ ] Implementar consumer em `Payments` que valida `PriceOffers`, verifica expiração e publica `PurchaseCompleted` ou `PurchaseRejected`.
- [ ] Implementar `GET /api/payments/{purchaseId}` para consulta de status (MVP: polling).

## Observações finais
- `Payments` deve ser tratado como um serviço de alta confiança/auditoria. Prefira padrões que evitem aceitar preços vindos do cliente sem assinatura ou validação servidor-a-servidor.
