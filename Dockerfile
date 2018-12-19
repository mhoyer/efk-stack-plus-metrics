# bases on https://www.exoscale.com/syslog/docker-logging/

FROM busybox:latest
ADD log.sh /
CMD /log.sh