namespace TaskTrain.Core;

public enum InsertionStatusEnum
{
    Success,
    ConstraintViolation,
    Failed
}

public enum UpdateStatusEnum 
{
    Success,
    Failed,
    NotFound,
    MultipleUpdates
}

public enum DeletionStatusEnum 
{
    Success,
    Failed,
    NotFound,
    MultipleDeletes
}

public interface IRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
{
    TEntity GetById(TKey id);
    Task<TEntity> GetByIdAsync(TKey id);

    IEnumerable<TEntity> GetAll();
    Task<IEnumerable<TEntity>> GetAllAsync();

    InsertionStatusEnum Insert(TEntity entity);
    Task<InsertionStatusEnum> InsertAsync(TEntity entity);

    UpdateStatusEnum Update(TEntity entity);
    Task<UpdateStatusEnum> UpdateAsync(TEntity entity);

    DeletionStatusEnum Delete(TEntity entity);
    Task DeleteAsync(TEntity entity);
}
