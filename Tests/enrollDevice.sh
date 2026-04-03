#!/bin/bash

curl -X POST "https://scep.hookhub.app/scep?operation=PKIOperation" \
  -H "Content-Type: application/json" \
  -d '{
    "companyId": 1,
    "deviceIdentifier": "DEV-1002",
    "deviceName": "LAPTOP-1002",
    "subject": "CN=LAPTOP-1002,O=Mrodriguex Corp",
    "challenge": "acme-challenge",
    "validityDays": 365
  }'
