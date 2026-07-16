using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace StudentManagement.Infrastructure.EntityFramework.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext _context;

        public EfRepository(AppDbContext context)
        {
            _context = context;
        }

        // Keep GetById tracked so updates work easily
        public virtual T? GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        // Use AsNoTracking() for general reads to keep memory clean and fast
        public virtual IEnumerable<T> GetAll()
        {
            return _context.Set<T>().AsNoTracking().ToList();
        }

        public virtual void Add(T entity)
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public virtual void Update(T entity)
        {
            // Attach the entity and mark it as modified
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public virtual void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                _context.Set<T>().Remove(entity);
                _context.SaveChanges();
            }
        }
    }
}