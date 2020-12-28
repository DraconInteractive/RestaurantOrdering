using Newtonsoft.Json;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace Dracon.WebServer
{
    class WebServer_Helpers
    {
        public static Dictionary<string, string> GetURLParameters(string queryString)
        {
            try
            {
                queryString = queryString.Trim('?');
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                List<string> keypairs = new List<string>(queryString.Split('&'));
                foreach (string s in keypairs)
                {
                    string[] pair = s.Split('=');
                    parameters.Add(pair[0], pair[1]);
                }
                return parameters;
            }
            catch
            {
                return null;
            }
        }

        public static bool IDVerified(Dictionary<string, string> urlParams)
        {
            if (urlParams == null || urlParams.Count <= 0)
            {
                return false;
            }
            if (urlParams.ContainsKey("id"))
            {
                try
                {
                    string d = StringCipher.Decrypt(urlParams["id"], "Dracon");
                    if (d == "dracona veritas")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static string HTMLButton(string text, string onClick)
        {
            return "<button type=\"button\" onclick=\"" + onClick + "\">" + text + "</button>";
        }

        public static string GetLocalData()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dataPath = Path.Combine(folder + "/data.txt");
            string data;
            using (StreamReader sr = new StreamReader(dataPath))
            {
                data = sr.ReadToEnd();
            }
            return data;
        }

        public static string GetMainHTML()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dataPath = Path.Combine(folder + "/mainHTML.html");
            string data;
            using (StreamReader sr = new StreamReader(dataPath))
            {
                data = sr.ReadToEnd();
            }
            return data;
        }

        public static float KelvinCelsius(float d, bool kelvin = false)
        {
            return kelvin ? (d - 273.15f) : (d + 273.15f);
        }

        public static string GETURL(string url)
        {
            //Program.Respond("Intent recognised: Retrieving recipe...");
            WebRequest request = WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            //Console.Write(recipeJson);
            reader.Close();
            response.Close();

            return json;

        }

        static string returnTemplate = "{\"events\": [{\"event\": \"bot\",\"timestamp\":" + DateTime.Now.ToString("yyyyMMddHHmmss") + "}],\"responses\": [{0}]}";

        public static string GetWeather(string input)
        {
            Program.Write("Intent recognised: Retrieving weather...");
            string location = "Perth";
            string url_weather = @"http://api.openweathermap.org/data/2.5/weather?q=" + location + @"&APPID=7954c53a8c5f97a7587bb810cf9ff72a";
            string weatherJson = GETURL(url_weather);
            var wN = JSON.Parse(weatherJson);
            string weather_main = wN["weather"][0]["main"].Value;
            string weather_temp = KelvinCelsius(float.Parse(wN["main"]["temp"].Value), true).ToString() + " C";
            Program.Write("Weather: " + "\n" + weather_main + "\n" + weather_temp);
            return returnTemplate.Replace("{0}", "{\"text\": \"" + "Weather: " + weather_main + "\"}, {\"text\":\"" + "Temp: " + weather_temp + "\"}");
        }
        public static string GetRecipe(string input)
        {
            Program.Write("Intent recognised: Retrieving recipe...");
            string recipePhrase = "chicken";
            string url_recipe = @"https://www.food2fork.com/api/search?key=8f428856c38e6ca7f6aebddf764ee981&q=" + recipePhrase;
            string recipeJson = GETURL(url_recipe);
            var rN = JSON.Parse(recipeJson);
            string recipeTitle = rN["recipes"][0]["title"].Value;
            string recipeURL = rN["recipes"][0]["source_url"].Value;
            Program.Write("Returning: \n" + recipeTitle + "\n" + recipeURL);
            return returnTemplate.Replace("{0}", "{\"text\": \"" + recipeTitle + "\"}, {\"text\":\"" + recipeURL + "\"}");
        }
        public static string GetPhilosophy(string input)
        {
            Program.Write("Intent recognised: Retrieving quote...");
            string quoteData = GETURL(@"https://quotes.rest/qod");
            Program.Write("Quote Data: \n" + quoteData);
            var qN = JSON.Parse(quoteData);
            string quote = qN["contents"]["quotes"][0]["quote"];
            string author = qN["contents"]["quotes"][0]["author"];
            return returnTemplate.Replace("{0}", "{\"text\": \"" + quote + "\"}, {\"text\":\"" + " - " + author + "\"}");
        }
    }
}
