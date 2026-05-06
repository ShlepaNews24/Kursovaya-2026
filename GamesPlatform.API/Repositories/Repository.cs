using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Models; 

namespace GamesPlatform.API.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
        
        public Task UpdateAsync(T entity) 
        { 
            _dbSet.Update(entity); 
            return Task.CompletedTask; 
        }
        
        public Task DeleteAsync(T entity) 
        { 
            _dbSet.Remove(entity); 
            return Task.CompletedTask; 
        }
        
        public async Task<bool> ExistsAsync(int id) => await _dbSet.FindAsync(id) != null;
    }
}