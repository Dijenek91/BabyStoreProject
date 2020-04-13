using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BabyStore.DAL;

namespace BabyStore.RepositoryLayer
{
    public class GenericUnitOfWork//this needs more work check out:https://dotnettutorials.net/lesson/unit-of-work-csharp-mvc/
    {
        private StoreContext _dbContext = new StoreContext();

        public GenericRepository<TEntityType> GetRepoInstance<TEntityType>() where TEntityType : class
        {
            return new GenericRepository<TEntityType>(_dbContext);
        }

        public void SaveChanges()
        {
            _dbContext.SaveChanges();
        }
    }
}