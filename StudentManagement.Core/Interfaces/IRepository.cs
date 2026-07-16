using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    void Add(T entity);
    T? GetById(int id);
    IEnumerable<T> GetAll();
    void Update(T entity);
    void Delete(int id);
}