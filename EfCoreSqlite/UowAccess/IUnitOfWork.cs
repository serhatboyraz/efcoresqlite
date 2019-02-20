using System;
using EfCoreSqlite.Repository;

namespace EfCoreSqlite.UowAccess
{
    /// <summary>
    /// UnitOfWork sınıfı tarafından kullanılacak arayüz.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> GetRepository<T>() where T : class;
        int SaveChanges();
    }
}
