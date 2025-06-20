namespace game.remoteconfig
{
	public interface IRemoteConfig
	{
		T GetValue<T>(string key);
	}
}