using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using BabyStore.DAL;

namespace BabyStore.RepositoryLayer
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity:class
    {
        private DbSet<TEntity> _dbSet;
        private StoreContext _dbContext;

        public GenericRepository(StoreContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<TEntity>();
        } 

        public IEnumerable<TEntity> GetAllRecords()
        {
            return  _dbSet.ToList();
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

        public TEntity GetById(int? entityId)
        {
            return _dbSet.Find(entityId);
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