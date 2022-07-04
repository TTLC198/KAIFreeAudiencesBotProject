import json
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
	params = dict(p_p_id = 'pubStudentSchedule_WAR_publicStudentSchedule10',
	              p_p_lifecycle = '2',
	              p_p_resource_id = 'getGroupsURL')
	response_json = requests.get(kaiUrl, params = params).json()
	groups = dict()
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
		cursor.execute("delete from temp_time")
		cursor.execute("select * from groups")
		groups = cursor.fetchall()
		random.seed()
		random.shuffle(groups)
		# groups = getGroup()
		temp_time = set()
		for groupId in groups[:100]:
			# cursor.execute("insert into groups values (?, ?)", (groupId, groups[groupId]))
			schedule = getScheduleById(groupId)
			if len(schedule) != 0:
				for day in schedule.values():
					for lesson in day:
						temp_t = normalize_string(lesson["dayDate"])
						if temp_t != '':
							temp_time.add((temp_t,))
		cursor.executemany("insert into temp_time values (?)", temp_time)
		sqlite_connection.commit()

	except sqlite3.Error as error:
		print(error)
	finally:
		if (sqlite_connection):
			cursor.close()
			sqlite_connection.close()


def create_dates(interval: int, date_from: datetime, date_until: datetime, overall_number: int = -1):
	date_list = list([date_from, ])
	if overall_number != -1:
		date_list.extend([date_from + timedelta(interval * idx) for idx in range(1, overall_number)])
	else:
		while date_from < date_until:
			date_from += timedelta(interval)
			date_list.append(date_from)
	print("Hello")



def parse_date():
	create_dates(14, datetime.strptime("01/01/22", r"%x"), datetime.strptime("06/30/22", r"%x"))
	try:
		sqlite_connection = sqlite3.connect(getConnectionString())
		cursor = sqlite_connection.cursor()
		cursor.execute("select * from temp_time")
		tempor_times = cursor.fetchall()
		cursor.execute("select * from default_values")
		default = cursor.fetchall()
		Eachsq = list()
		Each = list()
		Oddsq = list()
		Odd = list()
		Evensq = list()
		Even = list()
		Datesq = list()
		Date = list()
		for tempor_time_tuple in tempor_times:
			tempor_time = tempor_time_tuple[0].lower()
			if re.search(r"неч|чет|ежен", tempor_time) and not (
					not re.search(r"с|до", tempor_time) and re.search(r"\d\d[.-]*(?:\d\d[.-]*)+", tempor_time)):
				if re.search(r"\(\d+\)", tempor_time):
					if re.search(r"/", tempor_time) or re.search(r"ежен", tempor_time):
						Eachsq.append((tempor_time, re.search(r"\d+", tempor_time)[0]))
					elif re.search(r"[Нн]еч", tempor_time):
						Oddsq.append((tempor_time, re.search(r"\d+", tempor_time)[0]))
					elif re.search(r"[Чч]ет", tempor_time):
						Evensq.append((tempor_time, re.search(r"\d+", tempor_time)[0]))
				else:
					fromD = re.search(r"с \d\d[.-]*(?:\d\d[.-]*)+", tempor_time)
					untilD = re.search(r"(?:по|до) \d\d[.-]*(?:\d\d[.-]*)+", tempor_time)
					if not fromD:
						fromD = default[0]
					if not untilD:
						untilD = default[1]
					if re.search(r"/", tempor_time) or re.search(r"ежен", tempor_time):
						Each.append((tempor_time,
						             re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                       fromD[0])[0],
						             re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                       untilD[0])[0]))
					elif re.search(r"[Нн]еч", tempor_time):
						Odd.append((tempor_time,
						            re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                      fromD[0])[0],
						            re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                      untilD[0])[0]))
					elif re.search(r"[Чч]ет", tempor_time):
						Even.append((tempor_time,
						             re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                       fromD[0])[0],
						             re.search(r"\d\d[.]*(?:\d\d[.]*)+",
						                       untilD[0])[0]))
			else:
				if re.search(r"-", tempor_time):
					Datesq.append((tempor_time, tempor_time.split("-")))
				else:
					Date.append((tempor_time, re.findall(r"\d\d[.]*(?:\d\d[.]*)+", tempor_time)))

		print("123")
		sqlite_connection.commit()

	except BaseException as error:
		print(error)
	finally:
		if (sqlite_connection):
			cursor.close()
			sqlite_connection.close()


if __name__ == '__main__':
	parse_date()
