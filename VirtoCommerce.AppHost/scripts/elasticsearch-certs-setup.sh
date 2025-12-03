#!/bin/sh
set -e
cp /source/* /dest/
chown -R 1000:0 /dest
chmod 600 /dest/*.key
chmod 644 /dest/*.crt
echo "Certificates copied to volume."
