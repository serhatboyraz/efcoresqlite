using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EfCoreSqlite.Repository
{
    /// <summary>
    /// EntityFramework için hazırlıyor olduğumuz bu repositoriyi daha önceden tasarladığımız generic repositorimiz olan IRepository arayüzünü implemente ederek tasarladık.
    /// Bu şekilde tasarlamamızın ana sebebi ise veritabanına independent(bağımsız) bir durumda kalabilmek.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbContext DbContext;
        private readonly DbSet<T> DbSet; 
        /// <summary>
        /// Repository instance ı başlatırç
        /// </summary>
        /// <param name="dbContext">Veritabanı bağlantı nesnesi</param>
        /// <param name="authenticatedUser">Giriş yapmış kullanıcı session modeli</param>
        public Repository(DbContext dbContext)
        {
            DbContext = dbContext;
            DbSet = dbContext.Set<T>();
        }

        #region IRepository Members

        /// <summary>
        /// Tüm verileri getirir.
        /// SELECT * FROM T
        /// </summary>
        /// <returns></returns>
        public IQueryable<T> GetAll()
        {
            IQueryable<T> iQueryable = DbSet;
             
            return iQueryable;
        }

        /// <summary>
        /// Şarta göre tüm verileri getirir.
        /// SELECT * FROM T WHERE PREDICATE
        /// </summary>
        /// <param name="predicate">Veri şartı</param>
        /// <returns></returns>
        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> iQueryable = DbSet.Where(predicate); 
            return iQueryable;
        }

        public int Count()
        {
            IQueryable<T> iQueryable = DbSet; 
            return iQueryable.Count();
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> iQueryable = DbSet.Where(predicate); 
            return iQueryable.Count();
        }

        /// <summary>
        /// Şarta göre tek veri getirir
        /// SELECT TOP 1 * FROM T WHERE PREDICATE
        /// </summary>
        /// <param name="predicate">Veri şartı</param>
        /// <returns></returns>
        public T Get(Expression<Func<T, bool>> predicate)
        {
            IQueryable<T> iQueryable = DbSet.Where(predicate); 
            return iQueryable.ToList().FirstOrDefault();
        }

        /// <summary>
        /// Verileri kolonlarını seçerek getirir
        /// SELECT TOP 1 A,B,C,D FROM T WHERE PREDICATE
        /// </summary>
        /// <param name="where"></param>
        /// <param name="select"></param>
        /// <returns></returns>
        public IQueryable<dynamic> SelectList(Expression<Func<T, bool>> @where, Expression<Func<T, dynamic>> @select)
        {
            return DbSet
                .AsNoTracking()
                
                .Where(@where)
                .Select(@select);
        } 

        /// <summary>
        /// Verileri kolonlarını seçerek getirir Skip ve Limit dahil eder.
        /// SELECT TOP TAKECOUNT * FROM T WHERE PREDICATE ORDER BY SORT SORTTYPE
        /// </summary>
        /// <param name="where">Veri şartı</param>
        /// <param name="sort">Sıralama şartı</param>
        /// <param name="sortType">Sıralama tipi</param>
        /// <param name="skipCount">Getirilen verilerde atlanacak veri sayısı</param>
        /// <param name="takeCount">Getirilen verilerde alınacak veri sayısı</param>
        /// <returns></returns>
        public IQueryable<T> GetDataPart(Expression<Func<T, bool>> @where, Expression<Func<T, dynamic>> sort, SortTypeEnum sortType, int skipCount, int takeCount)
        {
            if (sortType == SortTypeEnum.DESC)
            {
                return DbSet
                    .AsNoTracking()
                    
                    .Where(@where)
                    //.Skip(skipCount) //Sql e cevirilemedigi icin hata veriyor.
                    .Take(takeCount + skipCount);
            }

            return DbSet
                .AsNoTracking()
                .OrderBy(sort)
                
                .Where(@where)
                //.Skip(skipCount) //Sql e cevirilemedigi icin hata veriyor.
                .Take(takeCount + skipCount);
        }

        /// <summary>
        /// Entity ile sql sorgusu göndermek için kullanılır.
        /// </summary>
        /// <param name="sqlQuery">Gönderilecek sql</param>
        /// <returns></returns>
        public List<T> SendSql(string sqlQuery)
        {
            return DbSet.FromSql(sqlQuery).AsNoTracking().ToList();
        }

        /// <summary>
        /// Verilen veriyi context üzerine ekler.
        /// </summary>
        /// <param name="entity">Eklenecek entity</param>
        public void Add(T entity)
        {  
            CheckDefaultValues(ref entity);

            DbSet.Add(entity);
        }

        /// <summary>
        /// Verilen veriyi context üzerinde günceller.
        /// </summary>
        /// <param name="entity">Güncellenecek entity</param>
        public void Update(T entity)
        { 
            CheckDefaultValues(ref entity);

            DbSet.Attach(entity);
            DbContext.Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        /// Verilen veriyi context üzerinden siler.
        /// </summary>
        /// <param name="entity">Delete entity</param>
        /// <param name="forceDelete">nesneyi veritabanından gerçekten sil. (HARD-DELETE)</param>
        public void Delete(T entity, bool forceDelete)
        { 
            CheckDefaultValues(ref entity);

            //Veriyi silmek yerine ACTIVE parametresi varsa o parametreyi X yap ve güncelle.
            if (entity.GetType().GetProperty("ACTIVE") != null && !forceDelete)
            {
                entity.GetType().GetProperty("ACTIVE")?.SetValue(entity, "");
                this.Update(entity);
            }
            else if (entity.GetType().GetProperty("PRST") != null && !forceDelete)
            {
                entity.GetType().GetProperty("PRST")?.SetValue(entity, "D");
                this.Update(entity);
            }
            else
            {
                // Önce entity'nin state'ini kontrol etmeliyiz.
                EntityEntry<T> dbEntityEntry = DbContext.Entry(entity);

                if (dbEntityEntry.State != EntityState.Deleted)
                {
                    dbEntityEntry.State = EntityState.Deleted;
                }
                else
                {
                    DbSet.Attach(entity);
                    DbSet.Remove(entity);
                }
            }
        }
         
        /// <summary>
        /// Verilen tipin standart degerini dondur.
        /// </summary>
        /// <param name="t">Tip</param>
        /// <returns></returns>
        private object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);
            if (t.Name.Equals("String"))
                return string.Empty;
            if (t.Name.Equals("Int32"))
                return 0;
            if (t.Name.Equals("Boolean"))
                return false;
            if (t.Name.Equals("Byte[]"))
                return new byte[0];
            return string.Empty;
        }

        private void CheckDefaultValues(ref T entity)
        {
            PropertyInfo[] props = typeof(T).GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = typeof(T).GetProperty(props[i].Name);
                if (prop != null)
                {
                    object value = prop.GetValue(entity);
                    if (value == null)
                        prop.SetValue(entity, GetDefaultValue(prop.PropertyType), null);
                }
            }
        }

        /// <summary>
        /// Aynı kayıt eklememek için objeyi kontrol ederek true veya false dönderir.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return DbSet.FirstOrDefault(predicate) != null;
        }

        #endregion
    }
}
