# # Build stage
# FROM node:20-alpine AS build

# WORKDIR /app

# # Copy package files
# COPY package*.json ./

# # Install dependencies
# RUN npm cache clean --force && \
#     npm config set registry https://registry.npmjs.org/ && \
#     npm install --legacy-peer-deps --loglevel=verbose 2>&1 && \
#     ls -la node_modules | head -20

# # Copy source
# COPY . .

# # Build
# RUN npm run build

# Runtime stage - using nginx
FROM nginx:alpine

# Copy pre-built React app
COPY build /usr/share/nginx/html 



# Copy nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
