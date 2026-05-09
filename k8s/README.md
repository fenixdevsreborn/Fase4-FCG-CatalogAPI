# CatalogAPI - Kubernetes Deployment

Este diretório contém todos os manifestos Kubernetes necessários para implantar o CatalogAPI como um microserviço profissional em um cluster Kubernetes.

## Estrutura

```
k8s/
├── namespace.yaml                    # Namespace dedicado
├── configmap.yaml                    # Configurações não sensíveis
├── secrets.yaml                      # Credenciais (template)
├── postgres/
│   ├── deployment.yaml              # Deployment PostgreSQL
│   ├── service.yaml                 # Service PostgreSQL
│   ├── pvc.yaml                     # PersistentVolumeClaim
│   ├── configmap.yaml               # Config PostgreSQL
│   └── init-script-configmap.yaml   # Script de inicialização
├── rabbitmq/
│   ├── deployment.yaml              # Deployment RabbitMQ
│   ├── service.yaml                 # Service RabbitMQ
│   └── pvc.yaml                     # PersistentVolumeClaim
├── auth-service/
│   ├── deployment.yaml              # Deployment Auth Service
│   └── service.yaml                 # Service Auth Service
├── catalogapi/
│   ├── deployment.yaml              # Deployment CatalogAPI
│   ├── service.yaml                 # Service CatalogAPI
│   └── hpa.yaml                     # HorizontalPodAutoscaler
├── ingress.yaml                      # Ingress (opcional)
└── README.md                        # Este arquivo
```

## Pré-requisitos

1. **Cluster Kubernetes** configurado e acessível
   - Kubernetes 1.24+ recomendado
   - kubectl configurado e conectado ao cluster

2. **StorageClass** configurado no cluster
   - Os PVCs usam o StorageClass padrão
   - Ajuste `storageClassName` nos arquivos PVC se necessário

3. **Ingress Controller** (opcional, para usar o Ingress)
   - NGINX Ingress Controller, Traefik, ou outro
   - Ajuste `ingressClassName` e annotations no `ingress.yaml` conforme necessário

4. **Métricas Server** (para HPA funcionar)
   ```bash
   # Verificar se está instalado
   kubectl get deployment metrics-server -n kube-system
   
   # Se não estiver, instale (exemplo para minikube):
   minikube addons enable metrics-server
   ```

5. **Imagens Docker** construídas e disponíveis
   - CatalogAPI: precisa fazer build da imagem
   - Auth Service: precisa fazer build da imagem
   - PostgreSQL e RabbitMQ: usam imagens públicas

## Build das Imagens Docker

Antes de fazer o deploy, você precisa construir as imagens Docker:

### CatalogAPI

```bash
# Na raiz do projeto
docker build -t catalogapi:latest -f Dockerfile .

# Se usar um registry:
docker tag catalogapi:latest registry.example.com/catalogapi:v1.0.0
docker push registry.example.com/catalogapi:v1.0.0
```

### Auth Service

```bash
# No diretório auth-service
docker build -t auth-service:latest -f Dockerfile .

# Se usar um registry:
docker tag auth-service:latest registry.example.com/auth-service:v1.0.0
docker push registry.example.com/auth-service:v1.0.0
```

### Para Minikube (usar imagens locais)

```bash
# Configurar Docker para usar o Docker do Minikube
eval $(minikube docker-env)

# Build das imagens
docker build -t catalogapi:latest -f Dockerfile .
docker build -t auth-service:latest -f auth-service/Dockerfile ./auth-service
```

## Deploy

### Ordem de Deploy

A ordem de deploy é importante devido às dependências:

1. **Namespace**
2. **Secrets e ConfigMaps**
3. **PostgreSQL** (Deployment + Service + PVC)
4. **RabbitMQ** (Deployment + Service + PVC)
5. **Auth Service** (Deployment + Service)
6. **CatalogAPI** (Deployment + Service)
7. **HPA** (HorizontalPodAutoscaler)
8. **Ingress** (opcional)

### Deploy Completo

```bash
# Aplicar todos os manifestos na ordem correta
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/postgres/
kubectl apply -f k8s/rabbitmq/
kubectl apply -f k8s/auth-service/
kubectl apply -f k8s/catalogapi/
kubectl apply -f k8s/catalogapi/hpa.yaml
kubectl apply -f k8s/ingress.yaml  # Opcional
```

### Deploy com um comando (se todas as dependências estiverem resolvidas)

```bash
kubectl apply -f k8s/
```

## Verificação

### Verificar Status dos Pods

```bash
# Ver todos os recursos no namespace
kubectl get all -n catalogapi

# Ver pods específicos
kubectl get pods -n catalogapi

# Ver status detalhado
kubectl get pods -n catalogapi -o wide
```

### Verificar Logs

```bash
# Logs do CatalogAPI
kubectl logs -f deployment/catalogapi -n catalogapi

# Logs de um pod específico
kubectl logs -f <pod-name> -n catalogapi

# Logs de todos os pods do CatalogAPI
kubectl logs -f -l app=catalogapi -n catalogapi
```

### Verificar Services

```bash
# Listar services
kubectl get svc -n catalogapi

# Detalhes de um service
kubectl describe svc catalogapi-service -n catalogapi
```

### Verificar Health Checks

```bash
# Port-forward para testar localmente
kubectl port-forward svc/catalogapi-service 8080:8080 -n catalogapi

# Em outro terminal, testar health check
curl http://localhost:8080/health
```

### Verificar HPA

```bash
# Status do HPA
kubectl get hpa -n catalogapi

# Detalhes do HPA
kubectl describe hpa catalogapi-hpa -n catalogapi
```

## Acessar Serviços

### CatalogAPI

```bash
# Port-forward
kubectl port-forward svc/catalogapi-service 8080:8080 -n catalogapi

# Acessar via browser ou curl
curl http://localhost:8080/health
curl http://localhost:8080/swagger
```

### RabbitMQ Management UI

```bash
# Port-forward para a UI de gerenciamento
kubectl port-forward svc/rabbitmq-management 15672:15672 -n catalogapi

# Acessar via browser
# http://localhost:15672
# Usuário: guest
# Senha: guest
```

### PostgreSQL

```bash
# Port-forward
kubectl port-forward svc/postgres-service 5432:5432 -n catalogapi

# Conectar com psql ou outra ferramenta
psql -h localhost -p 5432 -U admin -d catalogdb
```

## Escalabilidade

O HPA está configurado para escalar automaticamente o CatalogAPI baseado em:

- **CPU**: 70% de utilização
- **Memória**: 80% de utilização
- **Mínimo de réplicas**: 2
- **Máximo de réplicas**: 10

### Escalar Manualmente

```bash
# Escalar para 5 réplicas
kubectl scale deployment catalogapi --replicas=5 -n catalogapi

# Verificar
kubectl get pods -l app=catalogapi -n catalogapi
```

## Troubleshooting

### Pods não iniciam

```bash
# Verificar eventos
kubectl get events -n catalogapi --sort-by='.lastTimestamp'

# Descrever pod para ver erros
kubectl describe pod <pod-name> -n catalogapi

# Ver logs anteriores se o pod reiniciou
kubectl logs <pod-name> -n catalogapi --previous
```

### Problemas de Conexão

```bash
# Verificar se os services estão corretos
kubectl get svc -n catalogapi

# Testar conectividade entre pods
kubectl exec -it <pod-name> -n catalogapi -- ping postgres-service

# Verificar DNS
kubectl exec -it <pod-name> -n catalogapi -- nslookup postgres-service.catalogapi.svc.cluster.local
```

### Problemas de Storage

```bash
# Verificar PVCs
kubectl get pvc -n catalogapi

# Ver detalhes do PVC
kubectl describe pvc postgres-pvc -n catalogapi

# Verificar StorageClasses disponíveis
kubectl get storageclass
```

### Problemas de Health Check

```bash
# Testar health check manualmente
kubectl exec -it <pod-name> -n catalogapi -- curl http://localhost:8080/health

# Verificar configuração dos probes
kubectl describe deployment catalogapi -n catalogapi
```

### Problemas de Imagem

```bash
# Verificar se a imagem existe
kubectl describe pod <pod-name> -n catalogapi | grep Image

# Se usar Minikube, garantir que está usando o Docker do Minikube
eval $(minikube docker-env)
docker images | grep catalogapi
```

## Atualização

### Atualizar Deployment

```bash
# Atualizar imagem
kubectl set image deployment/catalogapi catalogapi=catalogapi:v1.1.0 -n catalogapi

# Verificar rollout
kubectl rollout status deployment/catalogapi -n catalogapi

# Reverter se necessário
kubectl rollout undo deployment/catalogapi -n catalogapi
```

### Atualizar ConfigMap

```bash
# Editar ConfigMap
kubectl edit configmap catalogapi-config -n catalogapi

# Ou aplicar novo arquivo
kubectl apply -f k8s/configmap.yaml

# Reiniciar pods para aplicar mudanças
kubectl rollout restart deployment/catalogapi -n catalogapi
```

## Segurança

### Secrets em Produção

⚠️ **IMPORTANTE**: O arquivo `secrets.yaml` contém credenciais em texto plano apenas para desenvolvimento. Em produção:

1. Use **Sealed Secrets** (https://github.com/bitnami-labs/sealed-secrets)
2. Use **External Secrets Operator** (https://external-secrets.io/)
3. Use serviços gerenciados (Azure Key Vault, AWS Secrets Manager, GCP Secret Manager)
4. Use **HashiCorp Vault**

### Criar Secrets Manualmente

```bash
kubectl create secret generic catalogapi-secrets \
  --from-literal=postgres-connection-string="Host=postgres-service;Port=5432;Database=catalogdb;Username=admin;Password=SEU_PASSWORD" \
  --from-literal=postgres-username="admin" \
  --from-literal=postgres-password="SEU_PASSWORD" \
  --from-literal=postgres-database="catalogdb" \
  --from-literal=rabbitmq-username="guest" \
  --from-literal=rabbitmq-password="SEU_PASSWORD" \
  --namespace=catalogapi
```

## Limpeza

### Remover Tudo

```bash
# Remover todos os recursos
kubectl delete -f k8s/

# Ou remover namespace (remove tudo dentro)
kubectl delete namespace catalogapi
```

⚠️ **ATENÇÃO**: Remover o namespace também remove os PVCs e dados persistentes!

### Remover Apenas Aplicação (manter dados)

```bash
kubectl delete deployment catalogapi -n catalogapi
kubectl delete svc catalogapi-service -n catalogapi
kubectl delete hpa catalogapi-hpa -n catalogapi
```

## Monitoramento e Observabilidade

### Métricas

O CatalogAPI já está configurado com health checks. Para monitoramento completo:

1. **Prometheus**: Instalar Prometheus Operator
2. **Grafana**: Configurar dashboards
3. **Loki**: Para agregação de logs (com Serilog)

### Logs

Os logs estão sendo gerados pelo Serilog. Para centralizar:

1. Configurar **Fluentd** ou **Fluent Bit** como DaemonSet
2. Enviar logs para **Elasticsearch** ou **Loki**
3. Visualizar em **Kibana** ou **Grafana**

## Próximos Passos

1. **Backup**: Configurar backups automáticos do PostgreSQL
2. **Monitoring**: Integrar Prometheus/Grafana
3. **CI/CD**: Configurar pipeline de deploy automático
4. **TLS**: Configurar certificados SSL/TLS para Ingress
5. **Network Policies**: Implementar políticas de rede para isolamento
6. **Resource Quotas**: Configurar limites de recursos por namespace
7. **Pod Disruption Budgets**: Garantir disponibilidade durante atualizações

## Suporte

Para problemas ou dúvidas, consulte:
- Documentação do Kubernetes: https://kubernetes.io/docs/
- Logs dos pods: `kubectl logs -f <pod-name> -n catalogapi`
- Eventos do cluster: `kubectl get events -n catalogapi --sort-by='.lastTimestamp'`
