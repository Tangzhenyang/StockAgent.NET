FROM node:24-alpine AS build

WORKDIR /app

COPY src/StockAgent.Web/package*.json ./
RUN npm ci

COPY src/StockAgent.Web/ ./

ARG VITE_API_BASE_URL=
RUN VITE_API_BASE_URL=${VITE_API_BASE_URL} npm run build

FROM nginx:1.29-alpine AS runtime

COPY src/StockAgent.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
