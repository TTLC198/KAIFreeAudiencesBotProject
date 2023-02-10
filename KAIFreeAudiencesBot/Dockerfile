FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

RUN apt-get update && apt-get -f install && apt-get -y install wget gnupg2 apt-utils
RUN wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN echo 'deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main' >> /etc/apt/sources.list
RUN apt-get update \
&& apt-get install -y google-chrome-stable --no-install-recommends --allow-downgrades fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-kacst fonts-freefont-ttf

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-stable"

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
