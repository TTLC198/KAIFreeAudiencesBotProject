FROM ubuntu:trusty
RUN sudo apt-get -y update && \
    sudo apt-get -y upgrade && \
    sudo apt-get install -y sqlite3 libsqlite3-dev
    
RUN mkdir /db && \
    /usr/bin/sqlite3 /db/schedule.db
    
WORKDIR /db
COPY dump.sql /db
RUN /usr/bin/sqlite3 schedule.db < dump.sql

ENTRYPOINT /usr/bin/sqlite3 