# CatalogAPI

**Game Purchase Management System (API de Catálogo e Compras)**

API RESTful construída com **.NET 10** e arquitetura limpa (*Clean Architecture*) para gerenciar catálogo de jogos, compras via eventos e biblioteca de usuários. Central no fluxo de compra da plataforma, expõe endpoints para navegação, compras e eventos integrados a serviços externos (PaymentsAPI, AuthService). 

## Índice

1. Visão Geral
2. Responsabilidades no Sistema
3. Tecnologias e Dependências
4. Arquitetura de Software
5. Fluxos de Requisição e Eventos
6. Endpoints Disponíveis
7. Pré-Requisitos e Variáveis de Ambiente
8. Execução Local, Docker e Kubernetes
9. Testes Automatizados
10. Observações para Certificação Fase 2

---

## 1. Visão Geral

O **CatalogAPI** permite:

* Listar e consultar jogos com paginação.
* Realizar pedidos de compra de jogos.
* Consultar jogos na biblioteca de um usuário autenticado.
* Gerenciar criação/atualização/deleção de jogos (*Admin*).
* Orquestrar eventos de compra de forma transacional e resiliente. 

O serviço integra um *AuthService* externo para validação de tokens e um Message Broker (*RabbitMQ*) para comunicação assíncrona com outros microsserviços. 

---

## 2. Responsabilidades no Sistema

| Serviço                   | Responsabilidade                                                                |
| ------------------------- | ------------------------------------------------------------------------------- |
| **CatalogAPI**            | Gerenciar catálogo, aceitar pedidos de compra e processar eventos de pagamento. |
| **AuthService (externo)** | Validar Bearer Token e retornar dados de usuário/roles.                         |
| **PaymentsAPI**           | Processar pagamento de pedidos (eventos).                                       |

*Observação*: A API publica eventos de compra e consome eventos de pagamento aprovado/rejeitado.

---

## 3. Tecnologias e Dependências

**Plataforma**

* .NET 10 com ASP.NET Core
* C# 13
* Entity Framework Core 10 + Npgsql (PostgreSQL)
* MassTransit + RabbitMQ

**Outros componentes**

* Serilog para logging estruturado
* Swagger/OpenAPI para documentação
* Health Checks para PostgreSQL e RabbitMQ
* Testcontainers para testes de integração
* xUnit e FluentAssertions para testes automatizados 

---

## 4. Arquitetura de Software

Aplicação estruturada em **Clean Architecture** com separação clara entre:

* API Layer (Controllers, Middlewares)
* Application (CQRS/Handlers, DTOs, Commands/Queries)
* Domain (Entidades, Interfaces)
* Infrastructure (EF Core, Repositórios, Outbox, Event Publisher) 

Padrões adicionais:

* **Outbox Pattern** para garantir publicação transacional de eventos.
* **Mediator / CQRS** para separar comandos e queries. 

---

## 5. Fluxos de Requisição e Eventos

**Compra de jogo (simplificado)**

1. `POST /api/games/{gameId}/purchase` → Cria pedido.
2. Pedido persistido com evento `OrderPlacedEvent` na Outbox.
3. Processador de Outbox publica evento no *RabbitMQ*.
4. **PaymentsAPI** consome e processa pagamento, publica `PaymentProcessedEvent`.
5. **CatalogAPI** consome resultado:

   * Se aprovado → adiciona jogo à biblioteca do usuário.
   * Se rejeitado → ignora/gera log.
6. **NotificationsAPI** consome `PaymentProcessedEvent` para envio de confirmação. 

---

## 6. Endpoints Disponíveis

| Verbo  | Endpoint                       | Autenticação | Descrição                        |               |
| ------ | ------------------------------ | ------------ | -------------------------------- | ------------- |
| GET    | `/api/games`                   | Não          | Lista jogos (paginação).         |               |
| POST   | `/api/games`                   | Sim (Admin)  | Cria jogo novo.                  |               |
| PUT    | `/api/games/{id}`              | Sim (Admin)  | Atualiza jogo.                   |               |
| DELETE | `/api/games/{id}`              | Sim (Admin)  | Remove jogo.                     |               |
| POST   | `/api/games/{gameId}/purchase` | Sim          | Realiza pedido de compra.        |               |
| GET    | `/api/user-games`              | Sim          | Lista jogos do usuário.          |               |
| GET    | `/api/user-games/{gameId}`     | Sim          | Consulta jogo da biblioteca.     |               |
| GET    | `/health`                      | Não          | Health check (Banco + RabbitMQ). |  |

---

## 7. Pré-Requisitos e Variáveis de Ambiente

### Pré-Requisitos

* .NET 10 SDK instalado
* Docker + Docker Compose
* PostgreSQL
* RabbitMQ
* Serviço de Auth (Mock ou real)

### Variáveis de Ambiente

| Variável                             | Descrição                               |               |
| ------------------------------------ | --------------------------------------- | ------------- |
| `ConnectionStrings__CatalogDatabase` | URL de conexão PostgreSQL               |               |
| `RabbitMQ__Host`                     | Host do RabbitMQ                        |               |
| `RabbitMQ__Username`                 | Usuário RabbitMQ                        |               |
| `RabbitMQ__Password`                 | Senha RabbitMQ                          |               |
| `AuthService__BaseUrl`               | URL do serviço de autenticação          |               |
| `ASPNETCORE_ENVIRONMENT`             | Ambiente (*Development* / *Production*) |  |

> *Certifique-se de configurar Secrets para credenciais sensíveis quando em produção.*

---

## 8. Execução

### Local

1. Clone o repositório.
2. Configure PostgreSQL e RabbitMQ.
3. Ajuste variáveis de ambiente.
4. Execute:

```bash
dotnet restore
dotnet build
dotnet run --project src/CatalogAPI.API
```

### Docker

1. Ajuste `.env` com valores necessários.
2. Execute:

```bash
docker compose up --build
```

A API será exposta no `http://localhost:8080` conforme configuração no `docker-compose.yml`. 

### Kubernetes *(opcional para certificação)*

1. Gere manifests em `k8s/`.
2. Aplique:

```bash
kubectl apply -f k8s/
```

Configure *Secrets* e *ConfigMaps* para variáveis sensíveis nos manifests.

---

## 9. Testes Automatizados

Testes de unidade e integração estão localizados em `tests/CatalogAPI.Tests`.
Executar todos os testes com:

```bash
dotnet test
```

---

## 10. Observações para Certificação Fase 2

* **Consistência transacional de eventos** por Outbox Pattern está implementada.
* **Mensageria assíncrona** via RabbitMQ com MassTransit.
* **Autenticação externa** delegada ao AuthService.
* **Estrutura de documentação** é baseada em Swagger/OpenAPI.
* **Monitoramento/HealthChecks** prontos para prontidão e liveness. 

---
