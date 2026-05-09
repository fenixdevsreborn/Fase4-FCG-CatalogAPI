-- Script de inicialização do banco de dados
-- Este script é executado automaticamente quando o container PostgreSQL é criado pela primeira vez
-- O banco de dados 'catalogdb' é criado automaticamente pela variável POSTGRES_DB no docker-compose

-- Criar extensões úteis para o banco de dados
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
