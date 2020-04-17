using System.Data.Entity;
using System.Web.Mvc;

namespace BabyStore.RepositoryLayer.UnitOfWork
{
    public delegate void VerifyEntityAndSetRowVersion<TEntity>(TEntity databaseCategoryValues, TEntity currentUIValues, TEntity categoryToUpdate);

    public interface IUnitOfWork<out TContext>
        where TContext : DbContext, new()
    {
        TContext Context { get; }
        //void CreateTransaction();
        //void Commit();
        //void Rollback();
        void Save();

        bool Save<TEntity>(ModelStateDictionary modelState, TEntity entityToUpdate, VerifyEntityAndSetRowVersion<TEntity> verifyEntityAndSetRowVersionFunc) 
            where TEntity : class;
            

        IGenericRepository<TEntity> GenericRepository<TEntity>() where TEntity : class;
    }
}
