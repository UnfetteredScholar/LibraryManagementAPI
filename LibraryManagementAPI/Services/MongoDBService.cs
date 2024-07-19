using MongoDB.Driver;
using LibraryManagementAPI.Models;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;


namespace LibraryManagementAPI.Services
{
    public class MongoDBService<T>
    {
        private readonly IMongoCollection<T> _collection;


        public MongoDBService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionString);

            var database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);

            _collection = database.GetCollection<T>($"{typeof(T).Name}s");
        }

        public async Task<T?> GetAsync(Expression<Func<T,bool>> filter)
        {
            return await _collection.Find<T>(filter).FirstOrDefaultAsync();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _collection.Find<T>(_ => true).ToListAsync();
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> filter)
        {
            return await _collection.Find<T>(filter).ToListAsync();
        }

        public async Task CreateAsync(T newObject)
        {
            await _collection.InsertOneAsync(newObject);
        }

        public async Task ReplaceAsync(T newObject, Expression<Func<T, bool>> filter)
        {
            await _collection.ReplaceOneAsync<T>(filter, newObject);
        }

        public async Task UpdateAsync(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            await _collection.UpdateOneAsync<T>(filter, update);
        }

        public async Task UpdateManyAsync(Expression<Func<T, bool>> filter, UpdateDefinition<T> update)
        {
            await _collection.UpdateManyAsync<T>(filter, update);
        }

        public async Task DeleteAsync(Expression<Func<T, bool>> filter)
        {
            await _collection.DeleteOneAsync<T>(filter);
        }
    }


}
