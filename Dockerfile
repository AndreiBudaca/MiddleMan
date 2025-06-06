FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App

COPY . ./
RUN dotnet publish ./MiddleMan.Web -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build /App/out .
EXPOSE 80/tcp
ENTRYPOINT ["dotnet", "MiddleMan.Web.dll", "--urls", "http://0.0.0.0:80"]