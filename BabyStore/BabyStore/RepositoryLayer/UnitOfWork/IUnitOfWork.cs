using System;
using System.Data.Entity;
using System.Web.Mvc;

namespace BabyStore.RepositoryLayer.UnitOfWork
{
    public delegate void VerifyEntityAndSetRowVersion<TEntity>(TEntity databaseEntityValues, TEntity uiFilledEntityValues, TEntity returnModel);

    public interface IUnitOfWork<out TContext>: IDisposable
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
