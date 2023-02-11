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

create unique index classroom_idx
    on classrooms (cr_name, cr_building);

create table groups
(
    g_id   INTEGER not null
        constraint groups_pk
            primary key autoincrement,
    g_name TEXT    not null
);

create unique index group_idx
    on groups (g_id, g_name);

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

create index ssd_idx
    on schedule_subject_dates (ssd_cr_id);

create table teachers
(
    t_id   INTEGER not null
        constraint teachers_pk
            primary key autoincrement,
    t_name TEXT    not null
);

create table time_intervals
(
    ti_id    INTEGER not null
        constraint time_intervals_pk
            primary key autoincrement,
    ti_start TEXT    not null,
    ti_end   TEXT    not null
);

create unique index time_interval_idx
    on time_intervals (ti_start);