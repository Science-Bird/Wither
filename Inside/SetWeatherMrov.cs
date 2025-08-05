using Unity.Netcode;

namespace Wither.Inside;
public class SetWeatherMrov : NetworkBehaviour
{
    public static void EnableWeather()
    {
        WeatherRegistry.WeatherEffectController.EnableCurrentWeatherEffects();
    }
}