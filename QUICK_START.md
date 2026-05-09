# CatalogAPI - Quick Start Guide

## 🚀 Start the Application

### Option 1: Docker Compose (Recommended)
```bash
cd f:\FIAP\FaseII\Fase2-CatalogAPI
docker-compose up --build
```

Services available:
- **API:** http://localhost:8080
- **Health Check:** http://localhost:8080/health
- **PostgreSQL:** localhost:5432
- **RabbitMQ Management:** http://localhost:15672 (guest/guest)
- **Database Browser (Adminer):** http://localhost:8081

### Option 2: Local Development
```bash
# 1. Start PostgreSQL
docker run -d --name postgres-catalog -p 5432:5432 \
  -e POSTGRES_PASSWORD=admin123 \
  postgres:16

# 2. Start RabbitMQ
docker run -d --name rabbitmq-catalog -p 5672:5672 -p 15672:15672 \
  rabbitmq:4.0-management

# 3. Apply migrations
dotnet ef database update -p src/CatalogAPI.Infrastructure -s src/CatalogAPI.API

# 4. Start mock auth service
cd src/CatalogAPI.API/auth-service
npm install
npm start

# 5. Run API (in new terminal)
cd f:\FIAP\FaseII\Fase2-CatalogAPI
dotnet run --project src/CatalogAPI.API
```

API available at: http://localhost:5000

## 📝 Test API Endpoints

### 1. Health Check
```bash
curl http://localhost:8080/health
```

### 2. Get Games (Paginated)
```bash
curl http://localhost:8080/api/v1/games?pageNumber=1&pageSize=20
```

### 3. Purchase a Game
```bash
curl -X POST http://localhost:8080/api/v1/games/550e8400-e29b-41d4-a716-446655440000/purchase \
  -H "Authorization: Bearer any-token-here" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: f47ac10b-58cc-4372-a567-0e02b2c3d479"
```

Expected Response (201 Created):
```json
{
  "userGameId": "uuid",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "gameId": "550e8400-e29b-41d4-a716-446655440000",
  "purchaseDate": "2026-01-08T14:28:54Z"
}
```

## 🎮 Seeded Games

The database comes with 10 pre-loaded games:

1. **God of War** ($59.99)
2. **Elden Ring** ($59.99)
3. **FIFA 25** ($69.99)
4. **Minecraft** ($26.95)
5. **Cyberpunk 2077** ($39.99)
6. **The Witcher 3** ($29.99)
7. **GTA VI** ($69.99)
8. **Stardew Valley** ($14.99)
9. **Hades II** ($29.99)
10. **Baldur's Gate 3** ($59.99)

To purchase a game, use one of the real UUIDs from the database.

## 📊 Monitor Services

### Check Docker Services
```bash
docker-compose ps
```

### View API Logs
```bash
docker-compose logs -f catalogapi
```

### View Database
- Open http://localhost:8081 (Adminer)
- Server: postgres
- User: admin
- Password: admin123
- Database: catalogdb

### View RabbitMQ
- Open http://localhost:15672
- Username: guest
- Password: guest

## 🔧 Troubleshooting

### Port already in use?
```bash
# Find and stop conflicting service
docker ps
docker kill <container_id>
```

### Database connection failed?
```bash
# Check PostgreSQL is running
docker exec postgres /bin/bash -c "pg_isready"

# Check connection string in appsettings.json
```

### RabbitMQ connection failed?
```bash
# Check RabbitMQ is running
docker-compose logs rabbitmq
```

### OutboxMessages not processing?
```bash
# Check logs for OutboxProcessorService
docker-compose logs catalogapi | grep OutboxProcessor
```

## 📚 Documentation Files

- **README.md** - Comprehensive documentation
- **IMPLEMENTATION_SUMMARY.md** - Completion checklist and architecture
- **QUICK_START.md** - This file

## 🧪 Run Tests

```bash
# Unit tests
dotnet test tests/CatalogAPI.Tests

# With code coverage (requires coverage tool)
dotnet test tests/CatalogAPI.Tests --collect:"XPlat Code Coverage"
```

## 🛑 Stop Services

```bash
docker-compose down

# Keep volumes for next run
# OR remove everything including data
docker-compose down -v
```

## ✅ Verification

After starting, verify:
- ✅ Health check returns "Healthy"
- ✅ GET /games returns list of 10 games
- ✅ POST /purchase with valid Bearer token succeeds
- ✅ RabbitMQ management UI accessible
- ✅ Adminer database browser accessible
- ✅ Logs appear in console

## 🎯 Architecture at a Glance

```
Request → Correlation ID → Auth → CQRS Handler → UnitOfWork Transaction
   ↓        ↓                ↓        ↓             ↓
  API    Middleware     HTTPAuth   Mediator    UserGame+OutboxMessage
                        Service     Dispatch      ↓
                      (Polly)                  SaveChanges
                      (CircuitBr)              MassTransit.Publish
                                               Commit/Rollback
                                                    ↓
                                         OutboxProcessorService
                                         (5s interval, batch 100)
                                                    ↓
                                            RabbitMQ
```

## 💡 Key Features

- ✅ **Outbox Pattern** for reliable event publishing
- ✅ **Transactional consistency** with UnitOfWork
- ✅ **Correlation ID tracking** for debugging
- ✅ **Polly retry + Circuit Breaker** for resilience
- ✅ **Structured logging** to file and console
- ✅ **Health checks** for dependencies
- ✅ **Pagination** (20 items per page)
- ✅ **Bearer token validation** with external service
- ✅ **Compensating transactions** on failure

## 📞 Need Help?

1. Check logs: `docker-compose logs -f catalogapi`
2. Review README.md for detailed documentation
3. Check docker-compose.yml for configuration
4. Verify ports: `netstat -ano | findstr :8080`

---

**Status:** ✅ Ready to Deploy
**Version:** 1.0
**Last Updated:** January 8, 2026
