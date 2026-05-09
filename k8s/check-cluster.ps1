# Script para verificar e configurar cluster Kubernetes
# Uso: .\check-cluster.ps1

Write-Host "Verificando configuracao do Kubernetes..." -ForegroundColor Cyan

# Verificar se kubectl está instalado
try {
    $kubectlVersion = kubectl version --client --short 2>&1
    Write-Host "[OK] kubectl instalado" -ForegroundColor Green
    Write-Host "   $kubectlVersion" -ForegroundColor Gray
} catch {
    Write-Host "[ERRO] kubectl nao encontrado" -ForegroundColor Red
    Write-Host "   Instale o kubectl: https://kubernetes.io/docs/tasks/tools/" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Verificar contexto atual
Write-Host "Verificando contexto Kubernetes..." -ForegroundColor Cyan
try {
    $currentContext = kubectl config current-context 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Contexto atual: $currentContext" -ForegroundColor Green
    } else {
        Write-Host "[AVISO] Nenhum contexto configurado" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[AVISO] Nenhum contexto configurado" -ForegroundColor Yellow
}

# Listar todos os contextos
Write-Host ""
Write-Host "Contextos disponiveis:" -ForegroundColor Cyan
$contexts = kubectl config get-contexts 2>&1
if ($contexts -match "NAME") {
    Write-Host $contexts
} else {
    Write-Host "   Nenhum contexto encontrado" -ForegroundColor Yellow
}

Write-Host ""

# Verificar conexão com cluster
Write-Host "Testando conexao com cluster..." -ForegroundColor Cyan
try {
    $null = kubectl cluster-info 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Cluster conectado com sucesso!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Informacoes do cluster:" -ForegroundColor Cyan
        kubectl cluster-info
        Write-Host ""
        Write-Host "Nodes disponiveis:" -ForegroundColor Cyan
        kubectl get nodes
    } else {
        Write-Host "[ERRO] Nao foi possivel conectar ao cluster" -ForegroundColor Red
        Write-Host ""
        Write-Host "Solucoes possiveis:" -ForegroundColor Yellow
        Write-Host "   1. Docker Desktop: Habilite Kubernetes em Settings > Kubernetes" -ForegroundColor White
        Write-Host "   2. Minikube: Execute 'minikube start'" -ForegroundColor White
        Write-Host "   3. Kind: Execute 'kind create cluster'" -ForegroundColor White
        Write-Host ""
        Write-Host "   Consulte k8s/SETUP_CLUSTER.md para mais detalhes" -ForegroundColor Cyan
    }
} catch {
    Write-Host "[ERRO] Erro ao conectar ao cluster" -ForegroundColor Red
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""

# Verificar se Docker Desktop tem Kubernetes
Write-Host "Verificando Docker Desktop..." -ForegroundColor Cyan
try {
    $dockerInfo = docker info 2>&1
    if ($dockerInfo -match "Kubernetes") {
        Write-Host "[OK] Docker Desktop detectado" -ForegroundColor Green
        Write-Host "   Verifique se Kubernetes esta habilitado em Settings > Kubernetes" -ForegroundColor Yellow
    } else {
        Write-Host "[AVISO] Docker Desktop pode nao ter Kubernetes habilitado" -ForegroundColor Yellow
    }
} catch {
    Write-Host "[AVISO] Docker nao esta rodando ou nao esta acessivel" -ForegroundColor Yellow
}

Write-Host ""

# Verificar Minikube
Write-Host "Verificando Minikube..." -ForegroundColor Cyan
try {
    $null = minikube status 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Minikube detectado" -ForegroundColor Green
        minikube status
    } else {
        Write-Host "[INFO] Minikube nao esta rodando ou nao esta instalado" -ForegroundColor Gray
    }
} catch {
    Write-Host "[INFO] Minikube nao esta instalado" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Para mais informacoes, consulte:" -ForegroundColor Cyan
Write-Host "   - k8s/SETUP_CLUSTER.md" -ForegroundColor White
Write-Host "   - k8s/README.md" -ForegroundColor White
