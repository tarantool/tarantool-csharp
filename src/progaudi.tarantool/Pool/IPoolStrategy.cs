namespace ProGaudi.Tarantool.Client.Pool
{
    internal interface IPoolStrategy<in TKey, TObj> 
        where TObj : class
    {
        TObj GetNext();
        TObj GetByKey(TKey key);
        TObj[] GetAll();
        void Add(TKey key, TObj conn);
        TObj DeleteByKey(TKey key);
        bool IsEmpty();
    }
}