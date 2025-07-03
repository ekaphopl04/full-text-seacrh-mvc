#!/bin/bash

# Create the database
psql -c "CREATE DATABASE fulltextsearch;" postgres

# Run the SQL script to set up schema and sample data
psql -d fulltextseacrhdb -f Database/PostgresFullTextSearch.sql

echo "Database setup complete!"
