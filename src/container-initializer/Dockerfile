FROM alpine:3.9

RUN apk add --no-cache \
        curl \
        gettext

WORKDIR /app

COPY grafana/ ./grafana
COPY *.sh ./

ENTRYPOINT ["/app/entrypoint.sh"]
