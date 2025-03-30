#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username postgres --dbname postgres <<EOSQL
	CREATE DATABASE clouddrive;

    CREATE ROLE dev WITH LOGIN PASSWORD 'developer123';
    ALTER ROLE dev WITH Superuser;
    GRANT CONNECT ON DATABASE clouddrive TO dev;
EOSQL
