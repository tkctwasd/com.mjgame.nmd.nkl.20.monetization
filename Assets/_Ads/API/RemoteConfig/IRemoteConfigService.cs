using System.Threading.Tasks;

public interface IRemoteConfigService
{
    void Initialize();
    Task FetchRemoteConfig();
    T GetValue<T>(string key);
}
