## Deployment

### Generate .env file (ngrok)
```sh
echo ngrokAuthToken="your-auth-token" > .env 
```

### Configure values (nginx)

The privkey (as key.pem) and fullchain (as cert.pem) certificates must be in the Nginx/certs folder when starting the project.
Quick installation of certificates (if you use a сertbot).

```sh
cp /etc/letsencrypt/live/your_host_address.com/fullchain.pem KAIFreeAudiencesBotProject/Nginx/certs/cert.pem
```

```sh
cp /etc/letsencrypt/live/your_host_address.com/privkey.pem KAIFreeAudiencesBotProject/Nginx/certs/key.pem
```


Be sure you set the port and address in [docker-compose.yaml](/docker-compose.yaml)

```yaml
environment:
   - NGINX_HOST=your_host_address.com   #set your host address 
   - NGINX_PORT=443                     #set your port (80, 88, 443, 8443)
```

### Change appsettings.json file
* #### Generate json file from example
```sh
cp KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.example KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.json
```

```sh
nano KAIFreeAudiencesBot/KAIFreeAudiencesBot/appsettings.json
```
* #### Change values in json file

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

It is supported to launch a telegram bot using both ngrok (for debugging) and using nginx to fulfill the [telegram requirements](https://core.telegram.org/bots/webhooks#always-ssl-tls) for traffic encryption.

* Start containers in _debug_ mode (ngrok)
    ```sh
    docker compose --profile debug up -d
    ```

* Start containers in _production_ mode (nginx)
    ```sh
    docker compose --profile prod up -d
    ```

### Stop
```sh
docker compose down
```

### Run parser for update schedule

See more instructions [here](parser.md).
```sh
docker compose run parser bash
```