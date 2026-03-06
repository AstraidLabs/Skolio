FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/SchoolPlatform.Frontend/package.json ./
RUN npm install
COPY frontend/SchoolPlatform.Frontend/ .
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
