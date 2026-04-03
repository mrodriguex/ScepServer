#!/bin/bash	

SCEP_CA_DIR="./scep-ca"

# mkdir -p "$SCEP_CA_DIR"
# openssl req -x509 -newkey rsa:2048 -sha256 -days 3650 -nodes \
#   -keyout "$SCEP_CA_DIR/ca.key" -out "$SCEP_CA_DIR/ca.crt" \
#   -subj "/CN=Local SCEP CA/O=Acme/L=Mexico/C=ES"

# openssl pkcs12 -export \
#   -out "$SCEP_CA_DIR/ca.pfx" \
#   -inkey "$SCEP_CA_DIR/ca.key" \
#   -in "$SCEP_CA_DIR/ca.crt" \
#   -passout pass:Password!123

openssl pkcs12 -in "$SCEP_CA_DIR/ca.pfx" -clcerts -nokeys -out "$SCEP_CA_DIR/ca.pem" -passin pass:Password!123

openssl x509 -in "$SCEP_CA_DIR/ca.pem" -text -noout

sudo cp "$SCEP_CA_DIR/ca.pem" /usr/local/share/ca-certificates/scep-ca.crt
sudo update-ca-certificates

sudo openssl verify /usr/local/share/ca-certificates/scep-ca.crt