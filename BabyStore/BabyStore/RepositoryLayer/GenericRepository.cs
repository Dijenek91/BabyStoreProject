using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using BabyStore.DAL;

namespace BabyStore.RepositoryLayer
{
    public class GenericRepository<TEntity>:IGenericRepository<TEntity> where TEntity:class
    {
        private DbSet<TEntity> _dbSet;
        private StoreContext _dbContext;

        public GenericRepository(StoreContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<TEntity>();
        } 

        public async Task<IEnumerable<TEntity>> GetAllRecords()
        {
            return await _dbSet.ToListAsync();
        }

        public void Add(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public void Update(TEntity entity)
        {
            _dbSet.Attach(entity);
            _dbContext.Entry(entity).State = EntityState.Modified;
        }

        public async Task<TEntity> GetFirstOrDefault(int entityId)
        {
            return await _dbSet.FindAsync(entityId);
        }

        public void Delete(TEntity entity)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }
    }
}