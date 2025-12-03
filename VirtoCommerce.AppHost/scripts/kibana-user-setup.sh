#!/bin/sh
echo "Waiting for Elasticsearch to accept credentials..."

MAX_RETRIES=50
COUNT=0

while true; do
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" --cacert /certs/ca.crt -u "$ELASTIC_USER:$ELASTIC_PASSWORD" "$ELASTIC_HOST")

    if [ "$STATUS" = "200" ]; then
        echo "Elasticsearch is ready and authenticated (Status: 200)."
        break
    fi

    COUNT=$((COUNT+1))
    if [ $COUNT -ge $MAX_RETRIES ]; then
        echo "Timeout waiting for Elasticsearch (Last Status: $STATUS). Exiting."
        exit 1
    fi

    if [ "$STATUS" = "000" ]; then
        echo "Elasticsearch is unreachable (Connection Refused)... [Attempt $COUNT/$MAX_RETRIES]"
    else
        echo "Waiting for auth... (Status: $STATUS) [Attempt $COUNT/$MAX_RETRIES]"
    fi

    sleep 2
done

echo "Checking '$KIBANA_USER'..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" --cacert /certs/ca.crt -u "$KIBANA_USER:$KIBANA_PASSWORD" "$ELASTIC_HOST/_security/_authenticate")

if [ "$STATUS" = "200" ]; then
    echo "Success: User already configured."
    exit 0
fi

echo "Configuring '$KIBANA_USER'..."
RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST --cacert /certs/ca.crt -u "$ELASTIC_USER:$ELASTIC_PASSWORD" \
     -H "Content-Type: application/json" \
     "$ELASTIC_HOST/_security/user/$KIBANA_USER/_password" \
     -d "{\"password\": \"$KIBANA_PASSWORD\"}")

if echo "$RESPONSE" | grep -q "HTTP_STATUS:200"; then
     echo "Password set successfully."
else
     echo "Failed. Response: $RESPONSE"
     exit 1
fi
