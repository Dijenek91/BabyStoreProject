﻿using System.Collections.Generic;
using System.Linq;

namespace BabyStore.RepositoryLayer
{
    public interface IGenericRepository<TEntity> where TEntity:class
    {
        IEnumerable<TEntity> GetAllRecords();

        TEntity Find(int? entityId);

        void Add(TEntity entity);

        void Update(TEntity entity);

        void Delete(TEntity entity);

        void DetachEntry(TEntity entity);

        void SetOriginalValueRowVersion(TEntity entity, byte[] rowVersion);

        IQueryable<TEntity> GetTable();

    }
}
