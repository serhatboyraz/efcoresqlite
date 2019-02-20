using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace EfCoreSqlite
{
    public class SampleDBContext : DbContext
    {
        private static bool _created = false;
        public SampleDBContext()
        {
            if (!_created)
            {
                _created = true;
                Database.EnsureCreated();
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseSqlite(@"Data Source=Sample.db");
        }

        public DbSet<Category> Categories { get; set; }
    }
}
