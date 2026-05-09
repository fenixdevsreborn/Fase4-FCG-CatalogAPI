# PostgreSQL Local - CatalogAPI

## ⚠️ ATENÇÃO: Configuração Apenas para Desenvolvimento Local

Este diretório contém configurações de PostgreSQL **apenas para desenvolvimento local e testes isolados**.

**NÃO use estas configurações em produção ou com o Orchestrator!**

## Quando Usar

- Desenvolvimento local com Kubernetes (namespace `catalogapi`)
- Testes isolados do CatalogAPI
- Quando você precisa de um ambiente Kubernetes local separado

## Quando NÃO Usar

- ❌ Produção
- ❌ Deploy com o Orchestrator (use `Fase2-Orchestrator/k8s/postgres-catalog/`)
- ❌ Ambiente compartilhado

## Configuração de Produção

Para produção/orquestração, use:
- `Fase2-Orchestrator/k8s/postgres-catalog/` (namespace `fiap-gamestore`)

## Diferenças

| Aspecto | Local (este diretório) | Produção (Orchestrator) |
|---------|------------------------|-------------------------|
| Namespace | `catalogapi` | `fiap-gamestore` |
| Service Name | `postgres-service` | `postgres-catalog-service` |
| Deployment Name | `postgres` | `postgres-catalog` |
| PVC Name | `postgres-pvc` | `postgres-catalog-pvc` |
