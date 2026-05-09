# Script de deploy para CatalogAPI no Kubernetes (PowerShell)
# Uso: .\deploy.ps1

$ErrorActionPreference = "Stop"

Write-Host "🚀 Iniciando deploy do CatalogAPI no Kubernetes..." -ForegroundColor Green

# Verificar se kubectl está disponível
try {
    $null = Get-Command kubectl -ErrorAction Stop
} catch {
    Write-Host "❌ kubectl não encontrado. Por favor, instale o kubectl primeiro." -ForegroundColor Red
    exit 1
}

# Verificar conexão com o cluster
try {
    $null = kubectl cluster-info 2>&1
} catch {
    Write-Host "❌ Não foi possível conectar ao cluster Kubernetes." -ForegroundColor Red
    exit 1
}

Write-Host "✅ Cluster Kubernetes conectado" -ForegroundColor Green

# Aplicar manifestos na ordem correta
Write-Host "📦 Criando namespace..." -ForegroundColor Cyan
kubectl apply -f namespace.yaml

Write-Host "🔐 Criando secrets e configmaps..." -ForegroundColor Cyan
kubectl apply -f configmap.yaml
kubectl apply -f secrets.yaml

Write-Host "🐘 Deployando PostgreSQL..." -ForegroundColor Cyan
Get-ChildItem -Path postgres -Filter *.yaml | ForEach-Object {
    kubectl apply -f $_.FullName
}

Write-Host "🐰 Deployando RabbitMQ..." -ForegroundColor Cyan
Get-ChildItem -Path rabbitmq -Filter *.yaml | ForEach-Object {
    kubectl apply -f $_.FullName
}

Write-Host "🔒 Deployando Auth Service..." -ForegroundColor Cyan
Get-ChildItem -Path auth-service -Filter *.yaml | ForEach-Object {
    kubectl apply -f $_.FullName
}

Write-Host "📚 Deployando CatalogAPI..." -ForegroundColor Cyan
Get-ChildItem -Path catalogapi -Filter *.yaml | ForEach-Object {
    kubectl apply -f $_.FullName
}

Write-Host "🌐 Configurando Ingress (opcional)..." -ForegroundColor Cyan
kubectl apply -f ingress.yaml

Write-Host "⏳ Aguardando pods ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "✅ Deploy concluído!" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Status dos recursos:" -ForegroundColor Cyan
kubectl get all -n catalogapi

Write-Host ""
Write-Host "🔍 Para ver logs:" -ForegroundColor Cyan
Write-Host "  kubectl logs -f deployment/catalogapi -n catalogapi"
Write-Host ""
Write-Host "🌐 Para acessar via port-forward:" -ForegroundColor Cyan
Write-Host "  kubectl port-forward svc/catalogapi-service 8080:8080 -n catalogapi"
