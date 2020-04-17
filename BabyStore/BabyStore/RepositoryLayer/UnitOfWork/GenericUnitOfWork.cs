using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using BabyStore.Models.BabyStoreModelClasses;

namespace BabyStore.RepositoryLayer.UnitOfWork
{
    public class GenericUnitOfWork<TContext> : IUnitOfWork<TContext>, IDisposable
        where TContext: DbContext, new()
    {
        private readonly TContext _dbContext; 
        private bool _disposed;
        private string _errorMessage = string.Empty;
        //private DbContextTransaction _objTrans;   //uncomment the lines of code for enabling transactions
        private Dictionary<string, object> _repositories;

        public GenericUnitOfWork ()
        {
            _dbContext = new TContext();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public TContext Context
        {
            get
            {
                return _dbContext;
            }
        }

        //public void CreateTransaction()
        //{
        //    _objTrans = _dbContext.Database.BeginTransaction();
        //}


        //public void Commit()
        //{
        //    _objTrans.Commit();
        //}

        //public void Rollback()  
        //{
        //    _objTrans.Rollback();
        //    _objTrans.Dispose();
        //}

        public void Save()
        {
            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        _errorMessage += string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage) + Environment.NewLine;
                throw new Exception(_errorMessage, dbEx);
            }

        }
        
        //save that checks concurrency 
        public bool Save<TEntity>(ModelStateDictionary modelState, TEntity entityToUpdate, VerifyEntityAndSetRowVersion<TEntity> verifyEntityAndSetRowVersionFunc)
            where TEntity : class
        {
            try
            {
                _dbContext.SaveChanges();
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        _errorMessage += string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage) + Environment.NewLine;
                throw new Exception(_errorMessage, dbEx);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var exEntry = ex.Entries.Single();
                var currentUIValues = (TEntity)exEntry.Entity;
                var databaseCategory = exEntry.GetDatabaseValues();
                if (databaseCategory == null)
                {
                    modelState.AddModelError(string.Empty, "Unable to save your changes because the category has been deleted by another user.");
                }
                else
                {
                    var entityDatabaseValues = (TEntity)databaseCategory.ToObject();
                    verifyEntityAndSetRowVersionFunc(entityDatabaseValues, currentUIValues, entityToUpdate);
                    return false;
                }
            }
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
            }

            _disposed = true;
        }



        public IGenericRepository<T> GenericRepository<T>() 
            where T : class
        {
            if (_repositories == null)
            {
                _repositories = new Dictionary<string, object>();
            }


            var type = typeof(T).Name;
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _dbContext);
                _repositories.Add(type, repositoryInstance);
            }
            return (GenericRepository<T>)_repositories[type];
        }
    
    }
}