using MongoDB.Driver;
using MongoDB.Bson;
using SmartParking.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartParking.Core.Data
{
    public class FixMongoDBSchema
    {
        private readonly IMongoDatabase _database;

        public FixMongoDBSchema(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
            _database = client.GetDatabase(configuration.GetSection("DatabaseSettings")["DatabaseName"]);
        }

        public async Task FixTransactionSchema()
        {
            Console.WriteLine("Starting MongoDB schema fix for Transactions collection...");

            try
            {
                // Check if the collection exists
                if (!await CollectionExistsAsync("Transactions"))
                {
                    Console.WriteLine("Transactions collection does not exist. Creating new collection.");
                    await _database.CreateCollectionAsync("Transactions");
                    Console.WriteLine("Created new Transactions collection");
                    return;
                }

                // Create a new collection with the correct schema
                if (await CollectionExistsAsync("Transactions_New"))
                {
                    await _database.DropCollectionAsync("Transactions_New");
                    Console.WriteLine("Dropped existing Transactions_New collection");
                }

                await _database.CreateCollectionAsync("Transactions_New");
                Console.WriteLine("Created new Transactions_New collection");

                // Get raw transactions from the current collection to avoid deserialization issues
                var rawCollection = _database.GetCollection<BsonDocument>("Transactions");
                var rawTransactions = await rawCollection.Find(new BsonDocument()).ToListAsync();
                Console.WriteLine($"Found {rawTransactions.Count} transactions in the database");

                if (rawTransactions.Count == 0)
                {
                    Console.WriteLine("No transactions to migrate.");
                    return;
                }

                var newCollection = _database.GetCollection<BsonDocument>("Transactions_New");

                // Migrate each transaction to the new collection with the updated schema
                foreach (var rawTransaction in rawTransactions)
                {
                    // Add new fields if they don't exist
                    if (!rawTransaction.Contains("createdAt"))
                    {
                        rawTransaction["createdAt"] = rawTransaction.Contains("timestamp") ?
                            rawTransaction["timestamp"] : DateTime.UtcNow;
                    }

                    if (!rawTransaction.Contains("updatedAt"))
                    {
                        rawTransaction["updatedAt"] = DateTime.UtcNow;
                    }

                    if (!rawTransaction.Contains("metadata"))
                    {
                        rawTransaction["metadata"] = new BsonDocument();
                    }

                    // Fix PaymentDetails if it exists
                    if (rawTransaction.Contains("paymentDetails") && rawTransaction["paymentDetails"].IsBsonDocument)
                    {
                        var paymentDetails = rawTransaction["paymentDetails"].AsBsonDocument;

                        // Add transactionId field if it doesn't exist
                        if (!paymentDetails.Contains("transactionId") && rawTransaction.Contains("transactionId"))
                        {
                            paymentDetails["transactionId"] = rawTransaction["transactionId"];
                        }
                    }

                    await newCollection.InsertOneAsync(rawTransaction);
                }

                Console.WriteLine($"Migrated {rawTransactions.Count} transactions to the new collection");

                // Rename collections to swap the old and new
                try
                {
                    // Drop old backup if it exists
                    if (await CollectionExistsAsync("Transactions_Old"))
                    {
                        await _database.DropCollectionAsync("Transactions_Old");
                        Console.WriteLine("Dropped existing Transactions_Old collection");
                    }

                    await _database.RenameCollectionAsync("Transactions", "Transactions_Old");
                    Console.WriteLine("Renamed Transactions to Transactions_Old");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error renaming Transactions to Transactions_Old: {ex.Message}");
                    // If we can't rename, drop the original collection
                    await _database.DropCollectionAsync("Transactions");
                    Console.WriteLine("Dropped original Transactions collection");
                }

                try
                {
                    await _database.RenameCollectionAsync("Transactions_New", "Transactions");
                    Console.WriteLine("Renamed Transactions_New to Transactions");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error renaming Transactions_New to Transactions: {ex.Message}");
                    // If we can't rename, create a new collection and copy the data
                    var tempCollection = _database.GetCollection<BsonDocument>("Transactions_New");
                    var transactions = await tempCollection.Find(new BsonDocument()).ToListAsync();

                    await _database.CreateCollectionAsync("Transactions");
                    var finalCollection = _database.GetCollection<BsonDocument>("Transactions");

                    if (transactions.Count > 0)
                    {
                        await finalCollection.InsertManyAsync(transactions);
                    }
                    Console.WriteLine("Created new Transactions collection with migrated data");
                }

                Console.WriteLine("MongoDB schema fix completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing MongoDB schema: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await _database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }
    }
}
