#!/bin/bash

if [ "$#" -ne 2 ]; then
  echo "Usage: sudo $0 <DOMAIN> <OUTPUT>" >&2
  exit 1
fi

DOMAIN=${1}
OUTPUT=${2}


# List the files just to be sure
ls -al "/etc/letsencrypt/live/${DOMAIN}"
IN="/etc/letsencrypt/live/${DOMAIN}/cert.pem"
INKEY="/etc/letsencrypt/live/${DOMAIN}/privkey.pem"

echo Converting
openssl pkcs12 -export -out ${OUTPUT} -in ${IN} -inkey ${INKEY}

echo Done
ls -al
