using Unity.Netcode;

namespace Wither.Scripts;
public class MrovBlackoutCheck : NetworkBehaviour
{
    public static string CheckCurrentWeather()
    {
        WeatherRegistry.Weather currentWeather = WeatherRegistry.WeatherManager.GetCurrentWeather(StartOfRound.Instance.currentLevel);
        if (currentWeather != null)
        {
            return currentWeather.Name;
        }
        return "";
    }
}