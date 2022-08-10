import json
import re
import time
import logging
from datetime import timedelta, datetime
import argparse
import requests
from requests.adapters import HTTPAdapter, Retry
import sqlite3
from bs4 import BeautifulSoup

kaiUrl = 'https://kai.ru'
kaiScheduleUrl = 'https://kai.ru/raspisanie'
appsettingsJsonPath = r'C:\Users\yoreh\RiderProjects\KAIFreeAudiencesBotProject\KAIFreeAudiencesBot\KAIFreeAudiencesBot\appsettings.Development.json'
aud_last_idx = 0
clst_last_idx = 0
teach_last_idx = 0
begin_les_last_idx = 0
cursor = None

proxies = {
    # custom proxies list
}

headers = {
    # custom headers list
    'Referer': 'https://kai.ru',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36'
}


def main():
    global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx, cursor

    parser = argparse.ArgumentParser('KAI schedule parser')
    parser.add_argument('-g', default=False, action='store_true', help='refresh groups information')
    parser.add_argument('-l', default=False, action='store_true', help='add lessons')
    parser.add_argument('-p', default=True, action='store_true', help='get week parity')

    args = parser.parse_args()

    logging.basicConfig(level=logging.DEBUG)
    sqlite_connection = sqlite3.connect(get_connection_string(appsettingsJsonPath))
    cursor = sqlite_connection.cursor()

    if args.g:
        update_groups()
    elif args.l:
        update_lessons()
    elif args.p:
        get_week_parity()

    sqlite_connection.commit()
    requests.session().close()
    cursor.close()
    sqlite_connection.close()

    # try:
    #     sqlite_connection = sqlite3.connect(get_connection_string(appsettingsJsonPath))
    #     cursor = sqlite_connection.cursor()
    #
    #     if args.g:
    #         update_groups()
    #     elif args.l:
    #         update_lessons()
    # except BaseException as error:
    #     print(error)
    # else:
    #     sqlite_connection.commit()
    # finally:
    #     requests.session().close()
    #     cursor.close()
    #     sqlite_connection.close()


def normalize_string(string):
    """
    string normalization: remove extra spaces
    :param string: input string
    :return: normalized string
    """
    return " ".join(string.split())


def get_connection_string(path):
    """
    get connection string
    :param path: path to appsettings.json
    :return: SQLite connection string
    """
    with open(path) as file:
        config = json.load(file)
        return config['ConnectionStrings']['ScheduleConnectionSqlite'][12:]


def update_groups():
    cursor.execute("delete from groups")
    groups = get_groups()
    for group in groups:
        cursor.execute("insert into groups values (?, ?)", (group, groups[group]))
    print('groups is up-to-date')


def get_week_parity():
    response = requests.get(kaiUrl, headers=headers, proxies=proxies)
    response.close()
    soup = BeautifulSoup(response.content, "html.parser")
    temp = soup.body.find_all('span', attrs={'id': 'weekParity'})
    print(temp)


def get_groups():
    """
    get groups dictionary from kai api
    :return: groups dictionary
    """
    groups = dict()
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='getGroupsURL')
    response = requests.get(kaiScheduleUrl, params=params, headers=headers, proxies=proxies)
    response.close()

    for group in response.json():
        groups[str(group['id'])] = group['group']

    return groups


def get_group_schedule(groupId: int, session: requests.Session):
    """
    get group schedule by group id from kai api
    :param groupId: input group id
    :param session: request session
    :return: schedule in json format
    """
    params = dict(p_p_id='pubStudentSchedule_WAR_publicStudentSchedule10',
                  p_p_lifecycle='2',
                  p_p_resource_id='schedule',
                  groupId=groupId)
    response = session.get(kaiScheduleUrl, params=params, headers=headers, proxies=proxies)
    response.close()
    return response.json()


def create_dates(interval: int, date_from: datetime, date_until: datetime, overall_number: int = -1) -> list:
    date_list = list([date_from, ])
    if overall_number != -1:
        date_list.extend([date_from + timedelta(days=interval * idx) for idx in range(1, overall_number)])
    else:
        while date_from < date_until:
            date_from += timedelta(interval)
            date_list.append(date_from)
        else:
            date_list.pop()
    return date_list


def date_from_string(input_str: str, default_year: str) -> datetime:
    input_str = re.search(r"\d\d[.]*(?:\d\d[.]*)+", input_str)[0]
    if re.fullmatch(r"\d\d[.]\d\d[.]\d{4}", input_str) is None:
        if re.fullmatch(r"\d\d[.]\d\d[.]", input_str) is not None:
            input_str += default_year
        else:
            input_str += "." + default_year
    return datetime.strptime(input_str, r"%d.%m.%Y")


def parse_date(time_for_parse: str, default_start: datetime, default_end: datetime) -> list:
    if re.search(r"неч|чет|ежен", time_for_parse) and not (
            not re.search(r"с|до", time_for_parse) and re.search(r"\d\d[.-]*(?:\d\d[.-]*)+", time_for_parse)):
        if re.search(r"\(\d+\)", time_for_parse):
            if re.search(r"/", time_for_parse) or re.search(r"ежен", time_for_parse):
                return list(create_dates(7, default_start, default_end, int(re.search(r"\d+", time_for_parse)[0])))
            else:
                return create_dates(14, default_start, default_end, int(re.search(r"\d+", time_for_parse)[0]))
        else:
            from_date = re.search(r"с \d\d[.-]*(?:\d\d[.-]*)+", time_for_parse)
            until_date = re.search(r"(?:по|до) \d\d[.-]*(?:\d\d[.-]*)+", time_for_parse)
            if from_date is None:
                from_date = default_start
            else:
                from_date = date_from_string(from_date[0], str(default_start.year))
            if until_date is None:
                until_date = default_end
            else:
                until_date = date_from_string(until_date[0], str(default_start.year))
            if re.search(r"/", time_for_parse) or re.search(r"ежен", time_for_parse):
                return create_dates(7, from_date, until_date)
            else:
                return list(create_dates(14, from_date, until_date))
    else:
        if re.search(r"-", time_for_parse):
            from_date, until_date = time_for_parse.split("-")
            return create_dates(7, date_from_string(from_date, str(default_start.year)),
                                date_from_string(until_date, str(default_start.year)))
        else:
            date_list = list()
            for date in re.findall(r"\d\d[.]*(?:\d\d[.]*)+", time_for_parse):
                date_list.append(date_from_string(date, str(default_start.year)))
            return date_list


def update_lessons():
    cursor.execute("select * from groups")
    groups = cursor.fetchall()

    cursor.execute("select * from default_values")
    default = cursor.fetchall()

    cursor.execute("select cr_id from classrooms order by cr_id desc limit 1")
    aud = cursor.fetchone()
    aud_last_idx = 0 if aud is None else int(aud[0])
    cursor.execute("select ct_id from class_types order by ct_id desc limit 1")
    clst = cursor.fetchone()
    clst_last_idx = 0 if clst is None else int(clst[0])

    cursor.execute("select t_id from teachers order by t_id desc limit 1")
    teacher = cursor.fetchone()
    teach_last_idx = 0 if teacher is None else int(teacher[0])

    cursor.execute("select ti_id from time_intervals order by ti_id desc limit 1")
    begin_les = cursor.fetchone()
    begin_les_last_idx = 0 if begin_les is None else int(begin_les[0])

    session = requests.Session()
    retry = Retry(connect=3, backoff_factor=15)
    adapter = HTTPAdapter(max_retries=retry)
    session.mount('https://', adapter)

    N = len(groups)

    total_time = time.time()

    for groupId in groups:

        start_timestamp = time.time()
        schedule = get_group_schedule(groupId[0], session)

        if len(schedule) != 0:
            for day in schedule.values():
                for lesson in day:
                    day_number = normalize_string(lesson['dayNum'])
                    dates = normalize_string(lesson['dayDate'])
                    if re.search(r"[Нн]еч", dates) is not None:
                        start_date = datetime.strptime(default[3][0],
                                                       r"%d.%m.%Y") + timedelta(days=int(day_number))
                    elif re.search(r"[Чч]ет", dates) is not None:
                        start_date = datetime.strptime(default[2][0],
                                                       r"%d.%m.%Y") + timedelta(days=int(day_number))
                    else:
                        start_date = datetime.strptime(default[0][0],
                                                       r"%d.%m.%Y") + timedelta(days=int(day_number))

                    default_end = datetime.strptime(default[1][0], r"%d.%m.%Y")
                    auditory = normalize_string(lesson["audNum"])

                    if "---" in auditory:
                        continue

                    building = normalize_string(lesson["buildNum"])

                    if not building.isdigit():
                        continue

                    class_type = normalize_string(lesson["disciplType"])
                    teacher = " ".join([parte.capitalize() for parte in
                                        normalize_string(lesson["prepodName"]).split()])
                    time_interval = datetime.strptime(normalize_string(lesson["dayTime"]), "%H:%M")
                    if re.search(r"л.*р.*", class_type) is not None:
                        create_lessons(dates, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
                        time_interval += timedelta(hours=1, minutes=40)
                        create_lessons(dates, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
                    else:
                        create_lessons(dates, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
        task_time = round(time.time() - start_timestamp, 2)
        rps = round(N / task_time, 1)
        print(
            f"| Requests: {N}; Total time: {task_time} s; RPS: {rps}. |\n"
        )

    print('lessons is up-to-date')
    print('total time {} s'.format(round(time.time() - total_time, 2)))


def create_lessons(dates: str, def_start: datetime, def_end: datetime, auditory: str, building: int, cls_type: str, teacher: str,
                   begin_lesson: datetime, group_id):
    try:
        global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx
        dates = parse_date(dates, def_start, def_end)
        cursor.execute("select cr_id from classrooms where cr_name like ?", (auditory,))
        aud_id = cursor.fetchone()
        if aud_id is None:
            cursor.execute("insert into classrooms values (null, ?, ?)", (auditory, building))
            aud_last_idx += 1
            aud_id = (aud_last_idx,)
        aud_id = int(aud_id[0])
        cursor.execute("select ct_id from class_types where ct_name like ?", (cls_type,))
        clst_id = cursor.fetchone()
        if clst_id is None:
            cursor.execute("insert into class_types values (null, ?)", (cls_type,))
            clst_last_idx += 1
            clst_id = (clst_last_idx,)
        clst_id = int(clst_id[0])
        cursor.execute("select t_id from teachers where t_name like ?", (teacher,))
        teacher_id = cursor.fetchone()
        if teacher_id is None:
            cursor.execute("insert into teachers values (null, ?)", (teacher,))
            teach_last_idx += 1
            teacher_id = (teach_last_idx,)
        teacher_id = int(teacher_id[0])
        cursor.execute("select ti_id from time_intervals where ti_start = ?", (begin_lesson.strftime(r"%H:%M"),))
        time_int_id = cursor.fetchone()
        if time_int_id is None:
            end_time = begin_lesson + timedelta(hours=1, minutes=30)
            cursor.execute("insert into time_intervals values (null, ?, ?)", (begin_lesson.strftime(r"%H:%M"),
                                                                              end_time.strftime(r"%H:%M")))
            begin_les_last_idx += 1
            time_int_id = (begin_les_last_idx,)
        time_int_id = int(time_int_id[0])
        for date in dates:
            cursor.execute("insert into schedule_subject_dates values (null, ?, ?, ?, ?, ?, ?)", (time_int_id,
                                                                                                  teacher_id,
                                                                                                  aud_id,
                                                                                                  clst_id,
                                                                                                  date.strftime(
                                                                                                      r"%d.%m.%Y"),
                                                                                                  group_id[0]))
    except BaseException as error:
        print('group id = {}'.format(group_id[0]))
        print(error)


if __name__ == '__main__':
    main()
