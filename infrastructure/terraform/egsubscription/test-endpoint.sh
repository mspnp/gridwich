#!/bin/bash

# Exit if any of the intermediate steps fail
set -e

eval "$(jq -r '@sh "URL=\(.url) CODE=\(.testcode)"')"

RESULT_CODE=$(curl -s -d "[{\"id\": \"11111111-1111-1111-1111-111111111111\", \"topic\": \"/subscriptions/10101010-1010-1010-1010-101010101010\", \"subject\": \"\", \"data\": {\"validationCode\": \"$CODE\"}, \"eventType\": \"Microsoft.EventGrid.SubscriptionValidationEvent\", \"eventTime\": \"2018-01-25T22:12:19.4556811Z\", \"metadataVersion\": \"1\", \"dataVersion\": \"1\" }]" -H "Content-Type: application/json" -H "aeg-event-type: SubscriptionValidation" -X POST "$URL" | jq -r -R 'fromjson? | .validationResponse')

# Safely produce a JSON object containing the result value.
# jq will ensure that the value is properly quoted
# and escaped to produce a valid JSON string.
jq -n --arg jqCode "$RESULT_CODE" '{"code":$jqCode}'