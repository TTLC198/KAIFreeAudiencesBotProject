import json
import time
from datetime import timedelta, datetime
import numpy as np
import random
import re

import requests
import sqlite3

kaiUrl = 'https://kai.ru/raspisanie'
aud_last_idx = 0
clst_last_idx = 0
teach_last_idx = 0
begin_les_last_idx = 0


def normalize_string(string):
	"""
	:param string:
	:return: normalize string
	"""
	return " ".join(string.split())


def getConnectionString():
	with open(
			r'C:\Users\24122\Source\Repos\KAIFreeAudiencesBot\KAIFreeAudiencesBot\KAIFreeAudiencesBot'
			r'\appsettings.Development.json') as file:
		config = json.load(file)
		return config['ConnectionStrings']['ScheduleConnectionSqlite'][12:]


def getGroup():
	groups = dict()
	for n in range(1, 10):
		params = dict(p_p_id = 'pubStudentSchedule_WAR_publicStudentSchedule10',
		              p_p_lifecycle = '2',
		              p_p_resource_id = 'getGroupsURL',
		              query = n)
		response_json = requests.get(kaiUrl, params = params).json()
		for group in response_json:
			groups[str(group['id'])] = group['group']

	return groups


def getScheduleById(groupid):
	params = dict(p_p_id = 'pubStudentSchedule_WAR_publicStudentSchedule10',
	              p_p_lifecycle = '2',
	              p_p_resource_id = 'schedule',
	              groupId = groupid)
	return requests.get(kaiUrl, params = params).json()


def create_lessons(dates: str, def_start: datetime, def_end: datetime, auditory: str, cls_type: str, teacher: str,
                   begin_lesson: datetime, group_id, cursor):
	global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx
	dates = parse_date(dates, def_start, def_end)
	cursor.execute("select cr_id from classrooms where cr_name like ?", (auditory, ))
	aud_id = cursor.fetchone()
	if aud_id is None:
		cursor.execute("insert into classrooms values (null, ?)", (auditory, ))
		aud_last_idx += 1
		aud_id = (aud_last_idx, )
	aud_id = int(aud_id[0])
	cursor.execute("select ct_id from class_types where ct_name like ?", (cls_type, ))
	clst_id = cursor.fetchone()
	if clst_id is None:
		cursor.execute("insert into class_types values (null, ?)", (cls_type, ))
		clst_last_idx += 1
		clst_id = (clst_last_idx, )
	clst_id = int(clst_id[0])
	cursor.execute("select t_id from teachers where t_name like ?", (teacher, ))
	teacher_id = cursor.fetchone()
	if teacher_id is None:
		cursor.execute("insert into teachers values (null, ?)", (teacher, ))
		teach_last_idx += 1
		teacher_id = (teach_last_idx, )
	teacher_id = int(teacher_id[0])
	cursor.execute("select ti_id from time_intervals where ti_start = ?", (begin_lesson.strftime(r"%H:%M"), ))
	time_int_id = cursor.fetchone()
	if time_int_id is None:
		end_time = begin_lesson + timedelta(hours = 1, minutes = 30)
		cursor.execute("insert into time_intervals values (null, ?, ?)", (begin_lesson.strftime(r"%H:%M"),
		                                                                  end_time.strftime(r"%H:%M")))
		begin_les_last_idx += 1
		time_int_id = (begin_les_last_idx, )
	time_int_id = int(time_int_id[0])
	for date in dates:
		cursor.execute("insert into schedule_subject_dates values (null, ?, ?, ?, ?, ?, ?)", (time_int_id,
		                                                                                      teacher_id,
		                                                                                      aud_id,
		                                                                                      clst_id,
		                                                                                      date.strftime(r"%d.%m.%Y"),
		                                                                                      group_id[0]))


def main():
	#try:
	global aud_last_idx, clst_last_idx, teach_last_idx, begin_les_last_idx
	sqlite_connection = sqlite3.connect(getConnectionString())
	cursor = sqlite_connection.cursor()
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
	for groupId in groups[10:50]:
		time.sleep(10)
		schedule = getScheduleById(groupId)
		if len(schedule) != 0:
			for day in schedule.values():
				for lesson in day:
					day_number = normalize_string(lesson['dayNum'])
					dates = normalize_string(lesson["dayDate"])
					if re.search(r"[Нн]еч", dates) is not None:
						start_date = datetime.strptime(default[3][0],
						                               r"%d.%m.%Y") + timedelta(days = int(day_number))
					elif re.search(r"[Чч]ет", dates) is not None:
						start_date = datetime.strptime(default[2][0],
						                               r"%d.%m.%Y") + timedelta(days = int(day_number))
					else:
						start_date = datetime.strptime(default[0][0],
						                               r"%d.%m.%Y") + timedelta(days = int(day_number))
					default_end = datetime.strptime(default[1][0], r"%d.%m.%Y")
					auditory = normalize_string(lesson["audNum"])
					class_type = normalize_string(lesson["disciplType"])
					teacher = " ".join([parte.capitalize() for parte in
					                    normalize_string(lesson["prepodName"]).split()])
					time_interval = datetime.strptime(normalize_string(lesson["dayTime"]), "%H:%M")
					if re.search(r"л.*р.*", class_type) is not None:
						create_lessons(dates, start_date, default_end, auditory, class_type, teacher,
						               time_interval, groupId, cursor)
						time_interval += timedelta(hours = 1, minutes = 40)
						create_lessons(dates, start_date, default_end, auditory, class_type, teacher,
						               time_interval, groupId, cursor)
					else:
						create_lessons(dates, start_date, default_end, auditory, class_type, teacher,
						               time_interval, groupId, cursor)
	sqlite_connection.commit()
	if (sqlite_connection):
		cursor.close()
		sqlite_connection.close()
	"""except BaseException as error:
		print(error)
	finally:
		if (sqlite_connection):
			cursor.close()
			sqlite_connection.close()"""


def create_dates(interval: int, date_from: datetime, date_until: datetime, overall_number: int = -1) -> list:
	date_list = list([date_from, ])
	if overall_number != -1:
		date_list.extend([date_from + timedelta(days = interval * idx) for idx in range(1, overall_number)])
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


if __name__ == '__main__':
	main()

"""

						default_start += timedelta(days = int(day_number))
						dates = parse_date(normalize_string(lesson["dayDate"]), default_start, default_end)

		cursor.execute("delete from groups")
		groups = getGroup()
cursor.execute("insert into groups values (?, ?)", (groupId, groups[groupId]))


groupId = 22108
			
"""
