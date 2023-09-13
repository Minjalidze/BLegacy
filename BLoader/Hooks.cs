namespace BLoader;

public class Hooks
{
    public static void OnPlayerDisconnected()
    {
        BClient.Bootstrapper.OnPlayerDisconnected();
    }
    public static void OnPlayerInitialized()
    {
        BClient.Bootstrapper.OnPlayerInitialized();
    }
}