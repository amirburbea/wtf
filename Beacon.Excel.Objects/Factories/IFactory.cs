namespace Beacon.Excel.Objects.Factories
{
    public interface IFactory<T>
        where T : class
    {
        void Release(T component);

        T Resolve();
    }
}