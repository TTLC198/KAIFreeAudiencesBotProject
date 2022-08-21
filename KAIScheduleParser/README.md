## KAI website schedule parser

### Usage
This parser will allow you to get the entire schedule through the kai website, getting information about all classrooms and teachers.
You must be sure you are using the database as in the [project](https://github.com/TTLC198/KAIFreeAudiencesBotProject/blob/master/DB/schedule.db). 

_Group table update_
```sh
python3 parser.py -g -c [Path/To/Your/SQLite.db]
```

_Update all training sessions_
```sh
python3 parser.py -l -c [Path/To/Your/SQLite.db]
```

_With verbose logging_
```sh
python3 parser.py -g -v -c [Path/To/Your/SQLite.db]
```

_All-in-one_

You can also update the groups and the entire schedule by specifying the list of arguments, as in the example.
```sh
python3 parser.py -g -l -v
```