#/bin/sh

# based on https://www.exoscale.com/syslog/docker-logging/

# log.sh - run an infinite loop,
# and print the container hostname as well as the time every second.
i=0

while true
do
    echo "[$(uname -n)] $(date)"
    i=$((i+1))
    sleep 1
done
