# KAIFreeAudiencesBot

<img src="https://github.com/TTLC198/KAIFreeAudiencesBot/blob/master/logo.png" align="right" width="150" />

Телеграм бот для отображения свободных аудиторий в зданиях КАИ.
## Stack
* ASP.NET 6.0, C# 8.0
* sqlite3
* python
* ngrok(for debug only)
* docker-compose
## Preview
<img src="https://github.com/TTLC198/KAIFreeAudiencesBot/blob/master/preview.png" align="left" width="1100" />

## Usage

### Generate .env file 
```sh
echo ngrokAuthToken="your-auth-token" > .env 
```

### Change appsettings.json file
#### Generate json file from example
```sh
cp KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.example KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.json
```

```sh
nano KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.json
```
#### Change values in json file

You need to replace the values in BotConfiguration: HostAddress and BotApiKey.\
Leave value in HostAddress **blank** for auto detection when using ngrok.

```json
{
  "ConnectionStrings": {
    "ScheduleConnectionSqlite": "Data Source=//db//schedule.db"
  },
  "BotConfiguration": {
    "HostAddress": "https://host.address",
    "BotApiKey": "bot123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Building
```sh
docker compose build 
```

### Start
```sh
docker compose up -d
```

### Stop
```sh
docker compose down
```

### Run parser for update schedule

See more instructions [here](https://github.com/TTLC198/KAIFreeAudiencesBotProject/tree/master/KAIScheduleParser).
```sh
docker compose run parser bash
```
