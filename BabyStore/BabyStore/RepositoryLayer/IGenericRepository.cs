using System.Collections.Generic;
using System.Threading.Tasks;

namespace BabyStore.RepositoryLayer
{
    public interface IGenericRepository<TEntity> where TEntity:class
    {
        IEnumerable<TEntity> GetAllRecords();

        TEntity GetById(int? entityId);

        void Add(TEntity entity);

        void Update(TEntity entity);

        void Delete(TEntity entity);
    }
}
