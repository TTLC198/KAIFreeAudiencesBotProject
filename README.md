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
