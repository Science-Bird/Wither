using System;
using System.Collections;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace Wither;
public class SetWeatherMrov : NetworkBehaviour
{
	public static void EnableWeather()
	{
		WeatherRegistry.WeatherEffectController.EnableCurrentWeatherEffects();
	}
}