using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace BabyStore.RepositoryLayer.UnitOfWork
{
    public class GenericUnitOfWork<TContext> : IUnitOfWork<TContext>, IDisposable
        where TContext: DbContext, new()
    {
        private readonly TContext _dbContext;
        private bool _disposed;
        private string _errorMessage = string.Empty;
        private DbContextTransaction _objTrans;
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

        public void CreateTransaction()
        {
            _objTrans = _dbContext.Database.BeginTransaction();
        }


        public void Commit()
        {
            _objTrans.Commit();
        }

        public void Rollback()  
        {
            _objTrans.Rollback();
            _objTrans.Dispose();
        }

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



        public GenericRepository2<T> GenericRepository<T>() where T : class
        {
            if (_repositories == null)
            {
                _repositories = new Dictionary<string, object>();
            }


            var type = typeof(T).Name;
            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository2<T>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _dbContext);
                _repositories.Add(type, repositoryInstance);
            }
            return (GenericRepository2<T>)_repositories[type];
        }
    
    }
}