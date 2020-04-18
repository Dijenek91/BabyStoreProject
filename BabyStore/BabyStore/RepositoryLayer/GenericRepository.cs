using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using BabyStore.DAL;
using BabyStore.RepositoryLayer.UnitOfWork;

namespace BabyStore.RepositoryLayer
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>, IDisposable 
        where TEntity : class
    {
        private IDbSet<TEntity> _entities;
        private string _errorMessage = string.Empty;
        private bool _isDisposed;

        public StoreContext Context { get; set; }

        public GenericRepository(IUnitOfWork<StoreContext> unitOfWork) : this(unitOfWork.Context)
        {
        }

        public GenericRepository(StoreContext context)
        {
            _isDisposed = false;
            Context = context;
        }
        
        public virtual IQueryable<TEntity> GetTable()
        {
            return Entities;
        }

        public virtual void SetOriginalValueRowVersion(TEntity entity, byte[] rowVersion)
        {
            Context.Entry(entity).OriginalValues["RowVersion"] = rowVersion;
        }

        protected virtual IDbSet<TEntity> Entities
        {
            get { return _entities ?? (_entities = Context.Set<TEntity>()); }
        }

        public virtual IEnumerable<TEntity> GetAllRecords()
        {
            return Entities.ToList();
        }

        public virtual TEntity Find(int? id)
        {
            return Entities.Find(id);
        }

        public virtual void Add(TEntity entity)
        {
            try
            {
                IsEntityNull(entity);
                CheckAndInitializeContext();

                Entities.Add(entity);
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        _errorMessage += string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage) + Environment.NewLine;
                throw new Exception(_errorMessage, dbEx);
            }
        }
       

        public void BulkInsert(IEnumerable<TEntity> entities)
        {
            try
            {
                AreEntitiesNull(entities);
                CheckAndInitializeContext();
                Context.Configuration.AutoDetectChangesEnabled = false;
                Context.Set<TEntity>().AddRange(entities);
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        _errorMessage += string.Format("Property: {0} Error: {1}", validationError.PropertyName,
                        validationError.ErrorMessage) + Environment.NewLine;
                    }
                }
                throw new Exception(_errorMessage, dbEx);
            }
        }

        public virtual void Update(TEntity entity)
        {
            try
            {
                IsEntityNull(entity);
                CheckAndInitializeContext();
                SetEntryModified(entity);
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        _errorMessage += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                throw new Exception(_errorMessage, dbEx);
            }
        }
        public virtual void Delete(TEntity entity)
        {
            try
            {
                IsEntityNull(entity);
                CheckAndInitializeContext();

                if (Context.Entry(entity).State == EntityState.Detached)
                {
                    Entities.Attach(entity);
                }
                Entities.Remove(entity);
            }
            catch (DbEntityValidationException dbEx)
            {
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                    foreach (var validationError in validationErrors.ValidationErrors)
                        _errorMessage += Environment.NewLine + string.Format("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                throw new Exception(_errorMessage, dbEx);
            }
        }

        public virtual void SetEntryModified(TEntity entity)
        {
            Context.Entry(entity).State = EntityState.Modified;
        }

        public void Dispose()
        {
            if (Context != null)
            {
                Context.Dispose();
            }
            _isDisposed = true;
        }
        
        #region Private helper methods
        /// <summary>
        /// Checks if entity is null
        /// </summary>
        /// <param name="entity">
        /// Entity parameter that will be checked
        /// <throw>
        /// If entity is null, this method will throw ArgumentNullException
        /// </throw>
        private static void IsEntityNull(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");
        }

        /// <summary>
        /// Checks if entity is null
        /// </summary>
        /// <param name="entities">
        /// IEnumrable entities parameter that will be verified
        /// </param>
        /// <throw>
        /// If entity is null, this method will throw ArgumentNullException
        /// </throw>
        private static void AreEntitiesNull(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException("entities");
        }

        private void CheckAndInitializeContext()
        {
            if (Context == null || _isDisposed)
                Context = new StoreContext();
        }

        #endregion

    }

}