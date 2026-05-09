# Configuração de Cluster Kubernetes Local

Este guia ajuda você a configurar um cluster Kubernetes local para testar o CatalogAPI.

## Opções Disponíveis

### Opção 1: Docker Desktop (Recomendado para Windows)

O Docker Desktop inclui Kubernetes que pode ser habilitado facilmente.

#### Passos:

1. **Abrir Docker Desktop**
   - Clique com botão direito no ícone do Docker na bandeja do sistema
   - Selecione "Settings" ou "Configurações"

2. **Habilitar Kubernetes**
   - Vá para "Kubernetes" no menu lateral
   - Marque "Enable Kubernetes"
   - Clique em "Apply & Restart"
   - Aguarde o Kubernetes iniciar (pode levar alguns minutos)

3. **Verificar Instalação**
   ```powershell
   kubectl cluster-info
   kubectl get nodes
   ```

4. **Se necessário, configurar o contexto**
   ```powershell
   kubectl config use-context docker-desktop
   ```

### Opção 2: Minikube

Minikube é uma ferramenta que cria um cluster Kubernetes local em uma VM.

#### Instalação:

1. **Baixar Minikube**
   - Acesse: https://minikube.sigs.k8s.io/docs/start/
   - Baixe o instalador para Windows
   - Execute o instalador

2. **Iniciar Minikube**
   ```powershell
   minikube start
   ```

3. **Verificar**
   ```powershell
   kubectl cluster-info
   minikube status
   ```

4. **Configurar Docker para usar o Docker do Minikube (para builds locais)**
   ```powershell
   minikube docker-env | Invoke-Expression
   ```

### Opção 3: Kind (Kubernetes in Docker)

Kind cria clusters Kubernetes usando containers Docker.

#### Instalação:

1. **Instalar via Chocolatey**
   ```powershell
   choco install kind
   ```

   Ou baixar manualmente de: https://kind.sigs.k8s.io/docs/user/quick-start/

2. **Criar Cluster**
   ```powershell
   kind create cluster --name catalogapi
   ```

3. **Verificar**
   ```powershell
   kubectl cluster-info --context kind-catalogapi
   ```

### Opção 4: Usar Cluster Remoto

Se você tem acesso a um cluster Kubernetes remoto (Azure AKS, AWS EKS, GCP GKE, etc.):

1. **Configurar kubectl para o cluster**
   - Siga as instruções do seu provedor de cloud
   - Exemplo para Azure AKS:
     ```powershell
     az aks get-credentials --resource-group <resource-group> --name <cluster-name>
     ```

2. **Verificar conexão**
   ```powershell
   kubectl cluster-info
   kubectl get nodes
   ```

## Verificação da Configuração

Após configurar qualquer uma das opções acima, verifique:

```powershell
# Ver contexto atual
kubectl config current-context

# Ver informações do cluster
kubectl cluster-info

# Ver nodes
kubectl get nodes

# Ver todos os contextos disponíveis
kubectl config get-contexts
```

## Próximos Passos

Após configurar o cluster:

1. **Instalar Metrics Server** (necessário para HPA)
   ```powershell
   # Para Minikube
   minikube addons enable metrics-server
   
   # Para Kind ou outros clusters
   kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
   ```

2. **Verificar StorageClass** (para PVCs funcionarem)
   ```powershell
   kubectl get storageclass
   ```

3. **Fazer deploy do CatalogAPI**
   ```powershell
   kubectl apply -f k8s/
   ```

## Troubleshooting

### Erro: "current-context is not set"

```powershell
# Ver contextos disponíveis
kubectl config get-contexts

# Definir contexto
kubectl config use-context <nome-do-contexto>
```

### Erro: "Unable to connect to the server"

- Verifique se o cluster está rodando
- Para Docker Desktop: verifique se Kubernetes está habilitado
- Para Minikube: execute `minikube start`
- Para Kind: verifique se o cluster existe: `kind get clusters`

### Erro: "dial tcp [::1]:8080"

Isso indica que o kubectl está tentando se conectar a localhost:8080, o que significa que não há um contexto configurado. Siga os passos acima para configurar um cluster.

### Verificar Configuração do kubectl

```powershell
# Ver arquivo de configuração
kubectl config view

# Ver contexto atual
kubectl config current-context

# Listar todos os contextos
kubectl config get-contexts
```

## Recomendações

- **Para desenvolvimento local**: Use Docker Desktop (mais fácil) ou Minikube
- **Para testes mais realistas**: Use Kind ou Minikube com múltiplos nodes
- **Para produção**: Use um cluster gerenciado (AKS, EKS, GKE)

## Recursos Adicionais

- [Docker Desktop Kubernetes](https://docs.docker.com/desktop/kubernetes/)
- [Minikube Documentation](https://minikube.sigs.k8s.io/docs/)
- [Kind Documentation](https://kind.sigs.k8s.io/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
