create table class_types
(
    ct_id   INTEGER not null
        constraint class_types_pk
            primary key autoincrement,
    ct_name TEXT    not null
);

create table classrooms
(
    cr_id       INTEGER not null
        constraint classrooms_pk
            primary key autoincrement,
    cr_name     TEXT    not null,
    cr_building INTEGER
);

create table groups
(
    g_id   INTEGER not null
        constraint groups_pk
            primary key autoincrement,
    g_name TEXT    not null
);

create table schedule_subject_dates
(
    ssd_id    INTEGER not null
        constraint schedule_subject_dates_pk
            primary key autoincrement,
    ssd_ti_id INTEGER not null
        references time_intervals,
    ssd_t_id  INTEGER not null
        references teachers,
    ssd_cr_id INTEGER not null
        references classrooms,
    ssd_ct_id INTEGER not null
        references class_types,
    ssd_date  TEXT    not null,
    ssd_g_id  INTEGER
        references groups
);

create table teachers
(
    t_id   INTEGER not null
        constraint teachers_pk
            primary key autoincrement,
    t_name TEXT    not null
);

create table default_values
(
    dv_value varchar not null,
    dv_id    INTEGER
        constraint default_values_pk
            primary key autoincrement
);

create table time_intervals
(
    ti_id    INTEGER not null
        constraint time_intervals_pk
            primary key autoincrement,
    ti_start TEXT    not null,
    ti_end   TEXT    not null
);

INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (1, '08:15', '09:45');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (2, '09:55', '11:25');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (3, '12:10', '13:40');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (4, '15:30', '17:00');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (5, '17:10', '18:40');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (6, '17:05', '18:35');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (7, '18:45', '20:15');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (8, '13:50', '15:20');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (9, '18:00', '19:30');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (10, '19:40', '21:10');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (11, '11:35', '13:05');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (12, '08:00', '09:30');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (13, '09:40', '11:10');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (14, '11:20', '12:50');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (15, '13:30', '15:00');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (16, '15:10', '16:40');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (17, '16:50', '18:20');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (18, '13:00', '14:30');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (19, '18:25', '19:55');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (20, '20:00', '21:30');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (21, '18:30', '20:00');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (22, '20:05', '21:35');
INSERT INTO time_intervals (ti_id, ti_start, ti_end) VALUES (23, '21:40', '23:10');

INSERT INTO class_types (ct_id, ct_name) VALUES (1, 'лек');
INSERT INTO class_types (ct_id, ct_name) VALUES (2, 'пр');
INSERT INTO class_types (ct_id, ct_name) VALUES (3, 'л.р.');
INSERT INTO class_types (ct_id, ct_name) VALUES (4, 'конс');

INSERT INTO default_values (dv_value, dv_id) VALUES ('09.01.2022', 1);
INSERT INTO default_values (dv_value, dv_id) VALUES ('30.06.2022', 2);
INSERT INTO default_values (dv_value, dv_id) VALUES ('09.01.2022', 3);
INSERT INTO default_values (dv_value, dv_id) VALUES ('16.01.2022', 4);