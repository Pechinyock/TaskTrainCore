namespace TaskTrain.Core;

public interface IEntity<TKey>
{
    TKey Id { get; }
}
