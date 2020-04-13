using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabyStore.RepositoryLayer
{
    public interface IGenericRepository<TEntity> where TEntity:class
    {
        Task<IEnumerable<TEntity>> GetAllRecords();

        void Add(TEntity entity);

        void Update(TEntity entity);

        Task<TEntity> GetFirstOrDefault(int entityId);

        void Delete(TEntity entity);
    }
}
