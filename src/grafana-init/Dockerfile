FROM alpine:3.9

RUN apk add --no-cache \
        curl \
        gettext

WORKDIR /app

COPY dashboards/ ./dashboards
COPY datasources/ ./datasources
COPY entrypoint.sh ./

ENTRYPOINT ["/app/entrypoint.sh"]
