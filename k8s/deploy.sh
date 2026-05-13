#!/bin/bash
# Script de deploy para CatalogAPI no Kubernetes
# Uso: ./deploy.sh

set -e

echo "🚀 Iniciando deploy do CatalogAPI no Kubernetes..."

# Verificar se kubectl está disponível
if ! command -v kubectl &> /dev/null; then
    echo "❌ kubectl não encontrado. Por favor, instale o kubectl primeiro."
    exit 1
fi

# Verificar conexão com o cluster
if ! kubectl cluster-info &> /dev/null; then
    echo "❌ Não foi possível conectar ao cluster Kubernetes."
    exit 1
fi

echo "✅ Cluster Kubernetes conectado"

# Aplicar manifestos na ordem correta
echo "📦 Criando namespace..."
kubectl apply -f namespace.yaml

echo "🔐 Criando secrets e configmaps..."
kubectl apply -f configmap.yaml
kubectl apply -f secrets.yaml

echo "🐘 Deployando PostgreSQL..."
kubectl apply -f postgres/

echo "🐰 Deployando RabbitMQ..."
kubectl apply -f rabbitmq/

echo "📚 Deployando CatalogAPI..."
kubectl apply -f catalogapi/

echo "📈 Configurando HPA..."
kubectl apply -f catalogapi/hpa.yaml

echo "🌐 Configurando Ingress (opcional)..."
kubectl apply -f ingress.yaml

echo "⏳ Aguardando pods ficarem prontos..."
kubectl wait --for=condition=ready pod -l app=postgres -n catalogapi --timeout=300s || true
kubectl wait --for=condition=ready pod -l app=rabbitmq -n catalogapi --timeout=300s || true
kubectl wait --for=condition=ready pod -l app=catalogapi -n catalogapi --timeout=300s || true

echo "✅ Deploy concluído!"
echo ""
echo "📊 Status dos recursos:"
kubectl get all -n catalogapi

echo ""
echo "🔍 Para ver logs:"
echo "  kubectl logs -f deployment/catalogapi -n catalogapi"
echo ""
echo "🌐 Para acessar via port-forward:"
echo "  kubectl port-forward svc/catalogapi-service 8080:8080 -n catalogapi"
