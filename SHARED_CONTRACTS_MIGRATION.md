# Migração para Shared.Contracts - Padronização de Eventos

## Status: ✅ CONCLUÍDA

Todos os eventos foram padronizados usando `Shared.Contracts` em todos os serviços, removendo `SetMessageType` customizados e simplificando a topologia RabbitMQ/MassTransit.

## Mudanças Implementadas

### 1. Criação de Shared.Contracts

Criado projeto `Shared.Contracts` em cada serviço com os seguintes eventos padronizados:

- **OrderPlacedEvent**: Evento publicado pelo CatalogAPI quando um pedido é criado
- **PaymentProcessedEvent**: Evento publicado pelo PaymentsAPI após processar pagamento
- **UserCreatedIntegrationEvent**: Evento publicado pelo UsersAPI quando um novo usuário é criado

Todos os eventos são `record` types com:
- Propriedades imutáveis (`init`)
- Construtores sem parâmetros para compatibilidade com MassTransit
- Tipagem explícita (sem `any`)
- Namespace comum: `Shared.Contracts.Events`

### 2. CatalogAPI

**Mudanças:**
- ✅ Criado `Shared.Contracts` com `OrderPlacedEvent` e `PaymentProcessedEvent`
- ✅ Removido `SetMessageType` customizado
- ✅ `ProcessPaymentEventConsumer` agora usa `Shared.Contracts.Events.PaymentProcessedEvent`
- ✅ `PurchaseGameCommandHandler` e `CreateOrderCommandHandler` usam `Shared.Contracts.Events.OrderPlacedEvent`
- ✅ `OutboxProcessorService` atualizado para usar contratos compartilhados
- ✅ Configuração MassTransit simplificada (apenas `SetEntityName`)

**Topologia:**
- **Exchange:** `fcg.order-placed-event` (fanout)
- **Queue:** `fcg.catalog.payment-processed` (consome PaymentProcessedEvent)
- **Bind:** Sem routing key (fanout exchange)

### 3. PaymentsAPI

**Mudanças:**
- ✅ Criado `Shared.Contracts` com `OrderPlacedEvent` e `PaymentProcessedEvent`
- ✅ Removido `SetMessageType` customizado
- ✅ `OrderPlacedConsumer` usa `Shared.Contracts.Events.OrderPlacedEvent`
- ✅ Publica `Shared.Contracts.Events.PaymentProcessedEvent`
- ✅ Configuração MassTransit simplificada

**Topologia:**
- **Exchange:** `fcg.order-placed-event` (fanout) - consome
- **Exchange:** `fcg.payment-processed-event` (fanout) - publica
- **Queue:** `fcg.payments.order-placed` (consome OrderPlacedEvent)
- **Bind:** Sem routing key (fanout exchange)

### 4. NotificationsAPI

**Mudanças:**
- ✅ Criado `Shared.Contracts` com `PaymentProcessedEvent` e `UserCreatedIntegrationEvent`
- ✅ Removido `SetMessageType` customizado
- ✅ `PaymentProcessedConsumer` usa `Shared.Contracts.Events.PaymentProcessedEvent`
- ✅ `UserCreatedIntegrationEventConsumer` já usava `Shared.Contracts.Events.UserCreatedIntegrationEvent`
- ✅ Configuração MassTransit simplificada

**Topologia:**
- **Exchange:** `fcg.payment-processed-event` (fanout) - consome
- **Exchange:** `fcg.user-created-event` (topic) - consome
- **Queue:** `fcg.notifications.payment-processed` (consome PaymentProcessedEvent)
- **Queue:** `fcg.notifications.user-created` (consome UserCreatedIntegrationEvent)
- **Bind:** Sem routing key para payment-processed (fanout), com routing key para user-created (topic)

## Topologia Final RabbitMQ

### Exchanges

| Exchange | Tipo | Publicado por | Consumido por |
|----------|------|---------------|---------------|
| `fcg.order-placed-event` | Fanout | CatalogAPI | PaymentsAPI |
| `fcg.payment-processed-event` | Fanout | PaymentsAPI | CatalogAPI, NotificationsAPI |
| `fcg.user-created-event` | Topic | UsersAPI | NotificationsAPI |

### Queues

| Queue | Consumido por | Evento |
|-------|---------------|--------|
| `fcg.payments.order-placed` | PaymentsAPI | OrderPlacedEvent |
| `fcg.catalog.payment-processed` | CatalogAPI | PaymentProcessedEvent |
| `fcg.notifications.payment-processed` | NotificationsAPI | PaymentProcessedEvent |
| `fcg.notifications.user-created` | NotificationsAPI | UserCreatedIntegrationEvent |

## Benefícios da Migração

✅ **Consistência**: Todos os serviços usam os mesmos contratos de eventos  
✅ **Simplicidade**: Removido `SetMessageType` customizado - apenas `SetEntityName`  
✅ **Manutenibilidade**: Contratos centralizados em `Shared.Contracts`  
✅ **Compatibilidade**: Tipos imutáveis (`record`) compatíveis com MassTransit  
✅ **Tipagem Forte**: Sem `any`, tipagem explícita em todos os eventos  
✅ **Resolução de Problemas**: Mensagens não vão mais para `*_skipped` queues

## Como Testar

1. **Rebuild todos os serviços:**
   ```bash
   cd Fase2-CatalogAPI && dotnet build
   cd Fase2-PaymentsAPI && dotnet build
   cd Fase2-NotificationsAPI && dotnet build
   ```

2. **Restart dos serviços no Kubernetes:**
   ```bash
   kubectl rollout restart deployment/catalog-api -n fiap-gamestore
   kubectl rollout restart deployment/payments-api -n fiap-gamestore
   kubectl rollout restart deployment/notifications-api -n fiap-gamestore
   ```

3. **Verificar queues no RabbitMQ:**
   - Acessar http://localhost:31672 (ou porta do NodePort)
   - Verificar que não há mensagens em `*_skipped` queues
   - Verificar que mensagens estão sendo consumidas corretamente

4. **Testar fluxo completo:**
   - Criar pedido no CatalogAPI
   - Verificar que OrderPlacedEvent é publicado
   - Verificar que PaymentsAPI consome e publica PaymentProcessedEvent
   - Verificar que CatalogAPI e NotificationsAPI consomem PaymentProcessedEvent

## Notas Técnicas

- Todos os eventos são `record` types para imutabilidade
- `OrderPlacedEvent` inclui `DecimalStringConverter` para compatibilidade com serialização JSON
- Exchanges são do tipo **fanout** para `order-placed` e `payment-processed` (sem routing key)
- Exchange `user-created` é do tipo **topic** (com routing key)
- `ConfigureConsumeTopology = false` em todos os consumers para evitar criação automática de exchanges
