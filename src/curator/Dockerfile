FROM python:3.7-alpine

RUN pip install elasticsearch-curator==5.6.0 &&\
    rm -rf /var/cache/apk/*

WORKDIR /root
COPY ./config/ .curator
RUN /usr/bin/crontab .curator/crontab.txt

CMD ["/usr/sbin/crond","-f"]
