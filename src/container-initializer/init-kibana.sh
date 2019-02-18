#!/bin/sh

if [ ! $KIBANA_URL ]; then
  echo 'Environment variable $KIBANA_URL not set or empty'
  exit 1
fi
if [ ! $ELASTICSEARCH_URL ]; then
  echo 'Environment variable $ELASTICSEARCH_URL not set or empty'
  exit 1
fi

post() {
    curl -sX POST -d "$1" \
        -H 'Content-Type: application/json;charset=UTF-8' \
        -H 'kbn-xsrf: true' \
        "$KIBANA_URL$2" \
        -o /dev/null \
        -w " POST $KIBANA_URL$2 -> %{http_code}\n"
}

FLUENT_INDEX_PATTERN='fluent*'

if [ ! -f "kibana.init" ]; then
    until curl -s "$KIBANA_URL/api/saved_objects/_find?type=index-pattern" -o /dev/null; do
        echo "Waiting for Kibana at $KIBANA_URL"
        sleep 2
    done

    until curl -s "$ELASTICSEARCH_URL/$FLUENT_INDEX_PATTERN" -o /dev/null; do
        echo "Waiting for Elasticsearch at $ELASTICSEARCH_URL"
        sleep 2
    done

    sleep 10
    echo -n "Creating initial Index Pattern in Kibana:"
    post "{\"attributes\":{\"title\":\"$FLUENT_INDEX_PATTERN\",\"timeFieldName\":\"@timestamp\"}}" "/api/saved_objects/index-pattern"

    touch "kibana.init"
fi
