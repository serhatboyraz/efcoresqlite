using System;
using System.Linq;
using EfCoreSqlite.UowAccess;

namespace EfCoreSqlite
{
    class Program
    {
        static void Main(string[] args)
        {
            UnitOfWork uow = new UnitOfWork();
            uow.GetRepository<Category>().Add(new Category()
            {
                CategoryName = "test",
                CategoryID = 1
            });
            uow.SaveChanges();
            var x = uow.GetRepository<Category>().GetAll().ToList();
        }
    }
}