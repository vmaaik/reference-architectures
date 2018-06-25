#!/bin/bash

# Set variables for the new account, database, and collection
resourceGroupName='asa-ra-1-stream1'
location='southcentralus'
name='docdb-taxicab'
databaseName='docdb-taxicab-database'
collectionName='docdb-taxicabavgtrippermile-collection'

echo $resourceGroupName
az group list >> text.txt
# Create a resource group
az group create --name $resourceGroupName --location $location

# Create a DocumentDB API Cosmos DB account
az cosmosdb create --name $name --kind GlobalDocumentDB --resource-group $resourceGroupName --max-interval 10 --max-staleness-prefix 200 

# Create a database 
az cosmosdb database create --name $name --db-name $databaseName --resource-group $resourceGroupName

# Create a collection
az cosmosdb collection create --collection-name $collectionName --name $name --db-name $databaseName --resource-group $resourceGroupName