FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /App
COPY . ./
RUN dotnet publish ./MiddleMan.Web -o out

FROM node:24-alpine AS node-development-dependencies-env
COPY ./MiddleMan.Web/ClientApp /app
WORKDIR /app
RUN npm ci

FROM node:24-alpine AS node-production-dependencies-env
COPY ./MiddleMan.Web/ClientApp/package.json ./MiddleMan.Web/ClientApp/package-lock.json /app/
WORKDIR /app
RUN npm ci --omit=dev

FROM node:24-alpine AS node-build-env
COPY ./MiddleMan.Web/ClientApp /app/
COPY --from=node-production-dependencies-env /app/node_modules /app/node_modules
WORKDIR /app
RUN npm run build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
RUN mkdir blobs && cd blobs && mkdir websocket-client-methods
COPY --from=dotnet-build /App/out .
COPY --from=node-build-env /app/build ./ClientApp/build
EXPOSE 80/tcp
ENTRYPOINT ["dotnet", "MiddleMan.Web.dll", "--urls", "http://0.0.0.0:80"]