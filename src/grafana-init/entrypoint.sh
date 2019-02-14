#!/bin/sh

if [ ! $GRAFANA_URL ]; then
  echo 'Environment variable $GRAFANA_URL not set or empty'
  exit 1
fi

post() {
    curl -s -X POST -d "$1" \
        -H 'Content-Type: application/json;charset=UTF-8' \
        "$GRAFANA_URL$2" \
        -o /dev/null -w " POST $GRAFANA_URL$2 -> %{http_code}\n"
}

if [ ! -f "grafana.init" ]; then
    until curl -s "$GRAFANA_URL/api/datasources" -o /dev/null; do
        echo "Waiting for Grafana at $GRAFANA_URL"
        sleep 2
    done

    for datasource in ./datasources/*; do
        echo -n "Creating datasource from $datasource:"
        post "$(envsubst < $datasource)" "/api/datasources"
    done

    for dashboard in ./dashboards/*; do
        echo -n "Creating dashboard from $dashboard:"
        post "$(cat $dashboard)" "/api/dashboards/db"
    done

    touch "grafana.init"
fi
