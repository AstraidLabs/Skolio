FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY src/Frontend/Skolio.Frontend/package.json ./
RUN npm install
COPY src/Frontend/Skolio.Frontend .
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Skolio.WebHost/Skolio.WebHost.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend-build /src/frontend/dist ./wwwroot/app
ENTRYPOINT ["dotnet", "Skolio.WebHost.dll"]
