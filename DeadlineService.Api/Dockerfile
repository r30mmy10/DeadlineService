FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DeadlineService.Api/DeadlineService.Api.csproj DeadlineService.Api/
RUN dotnet restore DeadlineService.Api/DeadlineService.Api.csproj

COPY DeadlineService.Api/ DeadlineService.Api/
RUN dotnet publish DeadlineService.Api/DeadlineService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DeadlineService.Api.dll"]
