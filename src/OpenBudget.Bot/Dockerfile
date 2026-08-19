FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/OpenBudget.Bot/OpenBudget.Bot.csproj", "src/OpenBudget.Bot/"]
COPY ["src/OpenBudget.Application/OpenBudget.Application.csproj", "src/OpenBudget.Application/"]
COPY ["src/OpenBudget.Domain/OpenBudget.Domain.csproj", "src/OpenBudget.Domain/"]
COPY ["src/OpenBudget.Infrastructure/OpenBudget.Infrastructure.csproj", "src/OpenBudget.Infrastructure/"]
RUN dotnet restore "src/OpenBudget.Bot/OpenBudget.Bot.csproj"

COPY src/ src/
WORKDIR "/src/src/OpenBudget.Bot"
RUN dotnet publish "OpenBudget.Bot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OpenBudget.Bot.dll"]
