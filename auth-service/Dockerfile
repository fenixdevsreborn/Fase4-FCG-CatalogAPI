# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files and install all dependencies (including dev dependencies)
COPY package*.json ./
RUN npm ci

# Production stage
FROM node:22-alpine AS production
WORKDIR /app

# Copy package files and install only production dependencies
COPY package*.json ./
RUN npm ci --only=production

# Copy application code
COPY index.js ./

# Create non-root user for security
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nodejs -u 1001 && \
    chown -R nodejs:nodejs /app

USER nodejs

EXPOSE 3000

CMD ["node", "index.js"]
