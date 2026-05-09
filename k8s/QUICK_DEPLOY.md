# Deploy Rápido - CatalogAPI Kubernetes

## Pré-requisitos Rápidos

1. **Cluster Kubernetes configurado**
   - Verifique com: `.\k8s\check-cluster.ps1`
   - Se não tiver cluster, consulte `k8s/SETUP_CLUSTER.md`
   - Opções: Docker Desktop, Minikube, Kind, ou cluster remoto

2. **kubectl conectado ao cluster**
   ```powershell
   kubectl cluster-info
   kubectl get nodes
   ```

3. **Imagens Docker construídas** (veja README.md)

## Deploy em 3 Passos

### 1. Build das Imagens (se ainda não fez)

```bash
# CatalogAPI
docker build -t catalogapi:latest -f Dockerfile .

# Auth Service
docker build -t auth-service:latest -f auth-service/Dockerfile ./auth-service
```

### 2. Verificar Cluster (se necessário)

```powershell
# Verificar configuração
.\k8s\check-cluster.ps1

# Se não houver cluster configurado, consulte:
# k8s/SETUP_CLUSTER.md
```

### 3. Deploy

**Opção A - Script Automático:**
```bash
# Linux/Mac
./k8s/deploy.sh

# Windows PowerShell
.\k8s\deploy.ps1
```

**Opção B - Manual:**
```bash
kubectl apply -f k8s/
```

### 4. Verificar

```bash
kubectl get all -n catalogapi
```

## Acessar a API

```bash
# Port-forward
kubectl port-forward svc/catalogapi-service 8080:8080 -n catalogapi

# Testar
curl http://localhost:8080/health
curl http://localhost:8080/swagger
```

## Comandos Úteis

```bash
# Ver logs
kubectl logs -f deployment/catalogapi -n catalogapi

# Ver status do HPA
kubectl get hpa -n catalogapi

# Escalar manualmente
kubectl scale deployment catalogapi --replicas=5 -n catalogapi

# Deletar tudo
kubectl delete -f k8s/
```

## Troubleshooting Rápido

```bash
# Pods não iniciam?
kubectl describe pod <pod-name> -n catalogapi

# Ver eventos
kubectl get events -n catalogapi --sort-by='.lastTimestamp'

# Verificar services
kubectl get svc -n catalogapi
```

Para mais detalhes, consulte [README.md](README.md).
