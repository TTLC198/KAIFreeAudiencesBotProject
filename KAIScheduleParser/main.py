import json
from datetime import timedelta, datetime
import numpy as np

import requests
import sqlite3

kaiUrl = 'https://kai.ru/raspisanie'


def normalizeString(string):
    return " ".join(string.split())


def getConnectionString():
    with open(
            r'C:\Users\24122\Source\Repos\KAIFreeAudiencesBot\KAIFreeAudiencesBot\KAIFreeAudiencesBot'
            r'\appsettings.Development.json') as file:
        config = json.load(file)
        return config['ConnectionStrings']['ScheduleConnectionSqlite'][12:]


def getGroup(groupnum):
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='getGroupsURL',
                  query=groupnum)
    response_json = requests.get(kaiUrl, params=params).json()
    groups = dict()
    for group in response_json:
        groups[str(group['id'])] = group['group']

    return groups


def getScheduleById(groupid):
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='schedule',
                  groupId=groupid)
    return requests.get(kaiUrl, params=params).json()


def main():
    try:
        sqlite_connection = sqlite3.connect(getConnectionString())
        cursor = sqlite_connection.cursor()
        cursor.execute("delete from groups")
        for i in range(1, 10):
            groups = getGroup(i)
            for groupId in groups:
                cursor.execute("Insert into groups values (?, ?)", (groupId, groups[groupId]))
                schedule = getScheduleById(groupId)
                if len(schedule) != 0:
                    for day in schedule:

        sqlite_connection.commit()

    except sqlite3.Error as error:
        print(error)
    finally:
        if (sqlite_connection):
            cursor.close()
            sqlite_connection.close()


if __name__ == '__main__':
    main()

