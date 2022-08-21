FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["KAIFreeAudiencesBot/KAIFreeAudiencesBot.csproj", "KAIFreeAudiencesBot/"]
RUN dotnet restore "KAIFreeAudiencesBot/KAIFreeAudiencesBot.csproj"
COPY . .
WORKDIR "/src/KAIFreeAudiencesBot"
RUN dotnet build "KAIFreeAudiencesBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KAIFreeAudiencesBot.csproj" -c Release -o /app/publish

FROM base AS final

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT dotnet KAIFreeAudiencesBot.dll --urls http://0.0.0.0:5000
