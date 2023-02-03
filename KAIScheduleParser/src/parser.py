import calendar
import re
import time
import logging
from datetime import timedelta, datetime
import argparse
import requests
from requests.adapters import HTTPAdapter, Retry
import sqlite3

kaiUrl = 'https://kai.ru'
kaiScheduleUrl = 'https://kai.ru/raspisanie'
aud_last_idx = 0
clst_last_idx = 0
teach_last_idx = 0
begin_les_last_idx = 0
cursor = None

custom_date_formats = ['%d.%m.%Y', '%d%m.%Y', '%d.%m', '%d.%m.  .  .%Y', '%d.%m.  .  ']

proxies = {
    # custom proxies list
}

headers = {
    # custom headers list
    'Referer': 'https://kai.ru',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36'
}


def main():
    global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx, cursor, custom_date_formats

    parser = argparse.ArgumentParser('KAI schedule parser')
    parser.add_argument('-g', default=False, action='store_true', help='refresh groups information')
    parser.add_argument('-l', default=False, action='store_true', help='add lessons')
    parser.add_argument('-d', default=False, action='store_true', help='truncate lessons tables')
    parser.add_argument('-v', default=False, action='store_true', help='verbose logging')
    parser.add_argument('-f', '--date-formats-list', default=custom_date_formats,  nargs='+', help='custom date formats like this: '.join(custom_date_formats))
    parser.add_argument('-c', default='/db/schedule.db', help='Path to SQLite DB')

    try:
        args = parser.parse_args()
        sqlite_connection = sqlite3.connect(args.c)
        cursor = sqlite_connection.cursor()

        if args.v:
            logging.basicConfig(level=logging.DEBUG)

        if args.g:
            logging.debug('updating groups')
            try:
                update_groups()
            except BaseException as error:
                print(error)
            else:
                sqlite_connection.commit()

        if args.d:
            logging.debug('truncating lessons')
            try:
                truncate_table("classrooms")
                truncate_table("class_types")
                truncate_table("schedule_subject_dates")
                truncate_table("teachers")
                truncate_table("time_intervals")
            except BaseException as error:
                print(error)
            else:
                sqlite_connection.commit()

        if args.l:
            logging.debug('updating lessons')
            try:
                update_lessons()
            except BaseException as error:
                print(error)
            else:
                sqlite_connection.commit()

        if args.f is not None:
            custom_date_formats = args.f

    except BaseException as error:
        print(error)
    else:
        sqlite_connection.commit()
    finally:
        requests.session().close()
        cursor.close()
        sqlite_connection.close()


def normalize_string(string):
    """
    string normalization: remove extra spaces
    :param string: input string
    :return: normalized string
    """
    return " ".join(string.split())


def update_groups():
    truncate_table("groups")
    groups = get_groups()
    for group in groups:
        cursor.execute("insert into groups values (?, ?)", (group, groups[group]))
    logging.debug('groups is up-to-date')


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


def get_group_schedule(groupId: int,
                       session: requests.Session):
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


def create_dates(interval: int,
                 date_from: datetime.date,
                 date_until: datetime.date,
                 overall_number: int = -1) -> list:
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


def date_from_string(input_str: str,
                     default_year: str) -> datetime.date:
    input_str = re.search(r"\d\d[.]*(?:\d\d[.]*)+", input_str)[0]
    if re.fullmatch(r"\d\d[.]\d\d[.]\d{4}", input_str) is None:
        if re.fullmatch(r"\d\d[.]\d\d[.]", input_str) is not None:
            input_str += default_year
        else:
            input_str += "." + default_year

    for fmt in custom_date_formats:
        try:
            return datetime.date(datetime.strptime(input_str, fmt))
        except ValueError:
            pass
    raise ValueError('no valid date format found')


def parse_date(time_for_parse: str,
               default_start: datetime.date,
               default_end: datetime.date) -> list:
    if re.search(r"неч|чет|ежен|\s*", time_for_parse) \
            and not (not re.search(r"с|до", time_for_parse) and re.search(r"\d\d[.-]*(?:\d\d[.-]*)+", time_for_parse)):
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
            if re.search(r"[Нн]еч/[Чч]ет", time_for_parse) or re.search(r"ежен", time_for_parse) or re.search(r"\s*", time_for_parse):
                return create_dates(7, from_date, until_date)
            else:
                return list(create_dates(14, from_date, until_date))
    else:
        if re.search(r"\d-\d", time_for_parse):
            from_date, until_date = time_for_parse.split("-")
            return create_dates(7, date_from_string(from_date, str(default_start.year)),
                                date_from_string(until_date, str(default_start.year)))
        else:
            date_list = list()
            for date in re.findall(r"\d\d[.]*(?:\d\d[.]*)+", time_for_parse):
                date_list.append(date_from_string(date, str(default_start.year)))
            return date_list


def get_defaults_values(is_even: bool,
                  is_end: bool,
                  date: datetime.date = datetime.date(datetime.now())):
    if 1 <= date.month <= 6:
        initial_date = datetime(year=date.year, month=1, day=1).date()
        end_date = datetime(year=date.year, month=6, day=30).date()
    else:
        initial_date = datetime(year=date.year, month=9, day=1).date()
        end_date = datetime(year=date.year, month=12, day=31).date()
    if is_end:
        return end_date
    else:
        if initial_date.isocalendar().week % 2 != is_even:
            return initial_date - timedelta(days=initial_date.isocalendar().weekday)
        else:
            return initial_date + timedelta(days=(8 - initial_date.isocalendar().weekday))


def truncate_table(table: str):
    cursor.execute(f"delete from {table}")
    cursor.execute(f"update sqlite_sequence set seq = 0 where name = '{table}'")


def update_lessons():
    cursor.execute("select * from groups")
    groups = cursor.fetchall()

    if not groups:
        raise Exception('argument error, please run update groups first with argument -g')

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
                    dates_string = normalize_string(lesson['dayDate'])
                    if re.search(r"[Нн]еч", dates_string) is not None:
                        start_date = get_defaults_values(is_even=False, is_end=False) + timedelta(days=int(day_number) - 1)
                    elif re.search(r"[Чч]ет", dates_string) is not None:
                        start_date = get_defaults_values(is_even=True, is_end=False) + timedelta(days=int(day_number) - 1)
                    else:
                        start_date = get_defaults_values(is_even=False, is_end=False) + timedelta(days=int(day_number) - 1)
                    default_end = get_defaults_values(is_even=False, is_end=True)

                    auditory = normalize_string(lesson["audNum"])

                    if "---" in auditory:
                        continue

                    building = normalize_string(lesson["buildNum"])
                    class_type = normalize_string(lesson["disciplType"])

                    teacher = " ".join([parte.capitalize() for parte in
                                        normalize_string(lesson["prepodName"]).split()])
                    time_interval = datetime.strptime(normalize_string(lesson["dayTime"]), "%H:%M")
                    if re.search(r"л.*р.*", class_type) is not None:
                        create_lessons(dates_string, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
                        time_interval += timedelta(hours=1, minutes=40) #TODO Сделать обработку лаб с 11:20
                        create_lessons(dates_string, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
                    else:
                        create_lessons(dates_string, start_date, default_end, auditory, building, class_type, teacher,
                                       time_interval, groupId)
        task_time = round(time.time() - start_timestamp, 2)
        rps = round(N / task_time, 1)
        logging.debug(
            f"| Requests: {N}; Total time: {task_time} s; RPS: {rps}. |\n"
        )

    logging.debug('lessons is up-to-date')
    logging.debug('total time {} s'.format(round(time.time() - total_time, 2)))


def create_lessons(dates_string: str,
                   def_start: datetime,
                   def_end: datetime,
                   auditory: str,
                   building: int,
                   cls_type: str,
                   teacher: str,
                   begin_time: datetime.time,
                   group_id):

    try:
        global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx
        cursor.execute("select cr_id from classrooms where cr_name = ? and cr_building = ?", (auditory, building))
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
        cursor.execute("select ti_id from time_intervals where ti_start = ?", (begin_time.strftime(r"%H:%M"),))
        time_int_id = cursor.fetchone()
        if time_int_id is None:
            end_time = begin_time + timedelta(hours=1, minutes=30)
            cursor.execute("insert into time_intervals values (null, ?, ?)", (begin_time.strftime(r"%H:%M"),
                                                                              end_time.strftime(r"%H:%M")))
            begin_les_last_idx += 1
            time_int_id = (begin_les_last_idx,)
        time_int_id = int(time_int_id[0])

        if "20.01,03.02,17.02,03.03,17.03,14.04 неч (2-ая подгруппа)" in dates_string:
            temp = 123

        dates = parse_date(dates_string, def_start, def_end)

        for date in dates:
            cursor.execute("insert into schedule_subject_dates values (null, ?, ?, ?, ?, ?, ?)",
                           (time_int_id,
                            teacher_id,
                            aud_id,
                            clst_id,
                            date.strftime(r"%d.%m.%Y"),
                            group_id[0]))
    except BaseException as error:
        logging.warning(f'Start error')
        logging.warning(
            "Properties: time_int_id: %s,\nteacher_id: %s,\naud_id: %s,\nclst_id: %s,\n dates: %s,\ngroup_id: %s;",
            *[time_int_id,
              teacher_id,
              aud_id,
              clst_id,
              dates_string,
              group_id[0]])
        logging.warning(error)
        logging.warning("End error\n")


if __name__ == '__main__':
    main()
