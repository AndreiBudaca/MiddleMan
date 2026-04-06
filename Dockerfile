FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /App
COPY MiddleMan.sln .
COPY MiddleMan.Web/MiddleMan.Web.csproj ./MiddleMan.Web/
COPY MiddleMan.Data/MiddleMan.Data.csproj ./MiddleMan.Data/
COPY MiddleMan.Communication/MiddleMan.Communication.csproj ./MiddleMan.Communication/
COPY MiddleMan.Core/MiddleMan.Core.csproj ./MiddleMan.Core/
COPY MiddleMan.Service/MiddleMan.Service.csproj ./MiddleMan.Service/
RUN dotnet restore
COPY . ./
RUN dotnet publish ./MiddleMan.Web -o out --no-restore -c Release

FROM node:24-alpine AS node-build-env
COPY ./MiddleMan.Web/ClientApp/package.json ./MiddleMan.Web/ClientApp/package-lock.json /app/
WORKDIR /app
RUN npm ci
COPY ./MiddleMan.Web/ClientApp /app/
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
RUN mkdir -p blobs/websocket-client-methods
COPY --from=dotnet-build /App/out .
COPY --from=node-build-env /app/build ./ClientApp/build
EXPOSE 80/tcp
ENTRYPOINT ["dotnet", "MiddleMan.Web.dll", "--urls", "http://0.0.0.0:80"]