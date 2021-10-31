using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace TesmMedia
{
    class Program
    {
        private const string requestUrl =
            "https://api.openweathermap.org/data/2.5/forecast?q=%20Saint%20Petersburg,ru&appid=c986959d1fd2ecb4376e73e676671d28&units=metric&cnt=25";

        private static async Task Main(string[] args)
        {
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(requestUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to request OpenWeatherApi, status code: {response.StatusCode}");
                        return;
                    }
                    var stringResult = await response.Content.ReadAsStringAsync();
                    Root deserializedResponse = null;
                    try
                    {
                        deserializedResponse = JsonConvert.DeserializeObject<Root>(stringResult);
                    } 
                    catch (JsonSerializationException e)
                    {
                        Console.WriteLine($"Failed to desearialize JSON: {e.Message}");
                        return;
                    }
                    
                    
                    //Сервис openweathermap предоставляет данные рассвета и заката только в платной версии,
                    //в прогнозе на 5 дней они указаны только для текущего дня. Но так как сейчас осень и день убывает,
                    // текущий день и будет с самым длинным световым днем. Но формально запрос сделан за пять дней
                    // для удовлетворения требований задачи.
                    long sunriseUnixTime = deserializedResponse.city.sunrise;
                    long sunsetUnixTime = deserializedResponse.city.sunset;
                    var sunriseTime = DateTimeOffset.FromUnixTimeSeconds(sunriseUnixTime);
                    var sunsetTime = DateTimeOffset.FromUnixTimeSeconds(sunsetUnixTime);

                    var maxLightDayDuration = sunsetTime - sunriseTime;
                    string maxLightDuratiobDate = sunsetTime.Date.ToShortDateString();

                    Console.WriteLine(
                        $"Maximal light day duration is {maxLightDayDuration}, it occurs on {maxLightDuratiobDate}.");

                    var tomorrow = DateTimeOffset.Now.AddDays(1);
                    var allNightForecastsForTomorrow = deserializedResponse.weatherInfo
                        .Where(info => DateTimeOffset.FromUnixTimeSeconds(info.unixTime).DayOfYear ==  tomorrow.DayOfYear)
                        .Where(info => info.sys.dayOrNight == "n");

                    var maxNightTemparatureDiff = 0.0;
                    foreach (var forecast in allNightForecastsForTomorrow)
                    {
                        var currentTemperatureDiff = Math.Abs(forecast.baseInfo.temp - forecast.baseInfo.feels_like);
                        if (currentTemperatureDiff > maxNightTemparatureDiff)
                        {
                            maxNightTemparatureDiff = currentTemperatureDiff;
                        }
                    }

                    Console.WriteLine(
                        $"Maximal difference between real temperature and felt one tomorrow night is {maxNightTemparatureDiff}.");
                }
            }
        }


    }

    public class Main
    {
        public double temp { get; set; }
        public double feels_like { get; set; }
    }

    public class Sys
    {
        [JsonProperty(PropertyName = "pod")]
        public string dayOrNight { get; set; }
    }


    public class List
    {
        [JsonProperty(PropertyName = "dt")]
        public int unixTime { get; set; }

        [JsonProperty(PropertyName = "main")]
        public Main baseInfo { get; set; }

        public Sys sys { get; set; }
    }

    public class City
    {
        public int sunrise { get; set; }
        public int sunset { get; set; }
    }

    public class Root
    {
        [JsonProperty(PropertyName = "list")]
        public List<List> weatherInfo { get; set; }

        public City city { get; set; }
    }

    
}
