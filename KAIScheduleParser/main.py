import json
import time
from datetime import timedelta, datetime
import numpy as np
import random
import re

import requests
import sqlite3

kaiUrl = 'https://kai.ru/raspisanie'


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


def main():
	try:
		sqlite_connection = sqlite3.connect(getConnectionString())
		cursor = sqlite_connection.cursor()
		cursor.execute("select * from groups")
		groups = cursor.fetchall()
		cursor.execute("select * from default_values")
		default = cursor.fetchall()
		for groupId in groups[:20]:
			time.sleep(10)
			schedule = getScheduleById(groupId)
			if len(schedule) != 0:
				for day in schedule.values():
					for lesson in day:
						default_start = datetime.strptime(default[0][0], r"%d.%m.%Y")
						default_end = datetime.strptime(default[1][0], r"%d.%m.%Y")
						day_number = normalize_string(lesson['dayNum'])
						default_start += timedelta(days = int(day_number))
						dates = parse_date(normalize_string(lesson["dayDate"]), default_start, default_end)
						auditory = normalize_string(lesson["audNum"])
						class_type = normalize_string(lesson["disciplType"])
						teacher = normalize_string(lesson["prepodName"]).capitalize()
						time_interval = normalize_string(lesson["dayTime"])
		sqlite_connection.commit()

	except BaseException as error:
		print(error)
	finally:
		if (sqlite_connection):
			cursor.close()
			sqlite_connection.close()


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

def cover_parser_date():
	try:
		sqlite_connection = sqlite3.connect(getConnectionString())
		cursor = sqlite_connection.cursor()
		cursor.execute("select * from temp_time")
		tempor_times = cursor.fetchall()
		cursor.execute("select * from default_values")
		default = cursor.fetchall()
		default_start = datetime.strptime(default[0][0], r"%d.%m.%Y")
		default_end = datetime.strptime(default[1][0], r"%d.%m.%Y")
		date_list = list()
		for tempor_time_tuple in tempor_times:
			tempor_time = tempor_time_tuple[0].lower()
			date_list.extend(parse_date(tempor_time, default_start, default_end))

		print("123")
		sqlite_connection.commit()
	except BaseException as error:
		print(error)
	finally:
		if (sqlite_connection):
			cursor.close()
			sqlite_connection.close()


if __name__ == '__main__':
	main()


"""

		cursor.execute("delete from groups")
		groups = getGroup()
cursor.execute("insert into groups values (?, ?)", (groupId, groups[groupId]))


groupId = 22108
			
"""