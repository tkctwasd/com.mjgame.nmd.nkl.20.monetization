public interface IData<T>
{
    T LoadData();
    void Save(string data);
}