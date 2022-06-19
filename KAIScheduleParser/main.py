import json
from datetime import timedelta, datetime
import requests
import sqlite3

kaiUrl = 'https://kai.ru/raspisanie'

def normalizeString(string):
    while "  " in string:
        string = string.replace("  ", " ")
    return string[:-1]


def getConnectionString():
    with open(
            r'C:\Users\yoreh\RiderProjects\KAIFreeAudiencesBotProject\KAIFreeAudiencesBot\KAIFreeAudiencesBot\appsettings.Development.json') as file:
        config = json.load(file)
        return config['ConnectionStrings']['ScheduleConnectionSqlite'][12:]


def getGroup(groupnum):
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='getGroupsURL',
                  query=groupnum)
    responseJson = requests.get(kaiUrl, params=params).json()
    groups = []
    for i in responseJson:
        groups.append(type('', (object,),
                           {
                               'id': str(i['id']),
                               'group': i['group']
                           })())

    return groups


def getScheduleById(groupid):
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='schedule',
                  groupId=groupid)
    responseJson = requests.get(kaiUrl, params=params).json()
    return responseJson


def main():
    schedules = []
    timeRanges = []
    teachers = []
    lessons = []
    groups = []
    classrooms = []

    db = sqlite3.connect(getConnectionString())
    cur = db.cursor()

    for i in range(1, 2):
        for j in getGroup(str(i)):
            schedule = getScheduleById(j.id)
            if len(schedule) != 0:
                groups.append(type('', (object,),
                                   {
                                       'id': j.id,
                                       'group_number': j.group
                                   })())
                schedules.append(schedule)

    for schGroups in schedules:
        for schDays in schGroups.values():
            for schLessons in schDays:
                dayTime = normalizeString(schLessons['dayTime'])
                if dayTime != '':
                    if len(timeRanges) > 0:
                        if not any(dayTime == tr.start_time for tr in timeRanges):
                            timeRanges.append(type('', (object,),
                                                   dict(start_time=dayTime, end_time=datetime.strftime((
                                                                                                                   datetime.strptime(
                                                                                                                       dayTime,
                                                                                                                       '%H:%M') + timedelta(
                                                                                                               hours=1,
                                                                                                               minutes=30)),
                                                                                                       '%H:%M')))())
                    else:
                        timeRanges.append(type('', (object,),
                                               dict(start_time=dayTime, end_time=datetime.strftime((datetime.strptime(
                                                   dayTime, '%H:%M') + timedelta(hours=1, minutes=30)), '%H:%M')))())

                audNum = normalizeString(schLessons['audNum'])
                building = normalizeString(schLessons['buildNum'])
                if audNum != '' and building != '':
                    if len(classrooms) > 0:
                        if not any(audNum == cr.classroom_number and building == cr.building for cr in classrooms):
                            classrooms.append(type('', (object,),
                                                   dict(classroom_number=audNum, building=building))())
                    else:
                        classrooms.append(type('', (object,),
                                               dict(classroom_number=audNum, building=building))())

                full_name = normalizeString(schLessons['prepodName'])
                if full_name != '':
                    if len(teachers) > 0:
                        if not any(full_name == t.full_name for t in teachers):
                            teachers.append(type('', (object,),
                                                 dict(full_name=full_name))())
                    else:
                        teachers.append(type('', (object,),
                                             dict(full_name=full_name))())

                # timeRanges sort
                trSorted = sorted(timeRanges, key=lambda x: datetime.strptime(x.start_time, '%H:%M'))
    print('123')

    # db.commit()
    # db.close()


if __name__ == '__main__':
    main()
