using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using SimpleJSON;

namespace Dracon.WebServer
{
    class Program
    {
        
        static int writeDelta = 10;
        static EventQueue queue;

        public static WebServer ws;

        static bool autoRun = true;
        static void Main(string[] args)
        {
            queue = new EventQueue();
            RunServer();
        }

        public static string[] GetInputCommands()
        {
            string input = Console.ReadLine();
            string[] cmds = input.Split(' ');
            return cmds;
        }

        public static void Write(string data)
        {
            Console.Write("> ");
            foreach (char c in data)
            {
                Console.Write(c);
                System.Threading.Thread.Sleep(writeDelta);
            }
            Console.Write("\n");
        }

        public static void Write(string data, int delta)
        {
            foreach (char c in data)
            {
                Console.Write(c);
                System.Threading.Thread.Sleep(delta);
            }
            Console.Write("\n");
        }

        public static void RunServer()
        {
            bool valid = false;
            if (autoRun)
            {
                ws = new WebServer(SendResponse, "http://+:2856/"/*, "http://+:2856/cgrs/"*/);

                ws.Run();
                //Write("Web server process initialised. Press any key to quit.");
                Console.ReadKey();
                ws.Stop();
                Write("\n");
                Write("Server terminated.");
            } else
            {
                while (!valid)
                {
                    Write("Please confirm the launch of a local web server on port 2856 at endpoint /. (Y/N). Continue?");
                    string a = Console.ReadLine();
                    if (a.ToLower() == "y")
                    {
                        valid = true;
                        ws = new WebServer(SendResponse, "http://+:2856/"/*, "http://+:2856/cgrs/"*/);

                        ws.Run();
                        //Write("Web server process initialised. Press any key to quit.");
                        Console.ReadKey();
                        ws.Stop();
                        Write("\n");
                        Write("Server terminated.");
                    }
                    else if (a.ToLower() == "n")
                    {
                        valid = true;
                        Write("Cancelling launch.");
                    }
                }
            }
        }

        public static byte[] SendByteResponse (HttpListenerRequest request)
        {
            string ext = Path.GetExtension(request.Url.AbsolutePath);
            string path = request.RawUrl.Replace(ext, "");
            byte[] b = null;
            if (path == "/logo")
            {
                b = File.ReadAllBytes(@"Logo_Dark.png");
            } else
            {
                b = File.ReadAllBytes(@"Logo_Dark.png");
            }
            //Console.WriteLine(b.Length);
            return b;
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            Console.WriteLine("Forming text");
            string[] requestChannel = request.RawUrl.Split('/');
            if (requestChannel.Length > 0)
            {
                if (requestChannel[1] == "cgrs")
                {
                    return CGRSResponse(request);
                } else if (requestChannel[1] == "editor")
                {
                    return EditorResponse(request);
                } else if (requestChannel[1] == "chorelist")
                {
                    return ChoreResponse(request);
                }
            }
            string report = request.HttpMethod + request.RawUrl + "\n";
            foreach (string s in requestChannel)
            {
                report += "-" + s + "- ";
            }
            Console.Write(report);
            string response = File.ReadAllText(@"ReturnData.txt");
            return response;
        }

        public static string CGRSResponse (HttpListenerRequest request)
        {

            using (StreamReader sr = new StreamReader(request.InputStream))
            {
                string data = sr.ReadToEnd();
                JSONNode N;
                bool isJson;
                try
                {
                    N = JSON.Parse(data);
                    isJson = true;
                }
                catch (Exception e)
                {
                    isJson = false;
                    return "<html><body><h1>Dracon Interactive CGRS Server</h1></body></html>"; ;
                }

                if (isJson)
                {
                    string cmd = N["Command"].Value;
                    cmd = cmd.ToLower();

                    if (cmd == "new user")
                    {
                        string user = N["Details"]["User"].Value;

                        var r = JSON.Parse("{}");
                        if (user == "" || user == " ")
                        {
                            r["response"] = "Invalid request.";
                        }
                        else
                        {  
                            r["response"] = "User Created";
                            r["user"] = user;
                        }
                        return r.ToString();
                    }
                    else if (cmd == "update user")
                    {
                        string user = N["Details"]["User"].Value;
                        string value = N["Details"]["Value"].Value;

                        var r = JSON.Parse("{}");

                        if (user == "" || user == " " || value == "" || value == " ")
                        {
                            r["response"] = "Invalid request";
                        }
                        else
                        {
                            
                            r["response"] = "Updated user values";
                            r["user"] = user;
                            r["value"] = value;
                        }
                        return r.ToString();
                    }
                    else if (cmd == "register event")
                    {
                        string user = N["Details"]["User"].Value;
                        string game = N["Details"]["Game"].Value;
                        string eventName = N["Details"][game]["EventName"].Value;

                        var r = JSON.Parse("{}");

                        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(game) && !string.IsNullOrEmpty(eventName))
                        {
                            queue.Add(user, game, eventName);

                            r["response"] = "New event registered";
                            r["user"] = user;
                            r["game"] = game;
                            r["eventName"] = eventName;
                            
                        } else
                        {
                            r["response"] = "Invalid Request";
                        }

                        return r.ToString();  
                    }
                    else if (cmd == "empty queue")
                    {
                        string user = N["Details"]["User"].Value;
                        string game = N["Details"]["Game"].Value;
                        EventQueue.Event[] events = null;
                        if (game == "" || game == " ")
                        {
                            events = queue.PopQueue(user);
                        }
                        else
                        {
                            events = queue.PopQueue(user, game);
                        }
                        if (events != null)
                        {
                            var NN = JSON.Parse("{}");
                            for (int i = 0; i < events.Length; i++)
                            {
                                NN[events[i].game][i] = events[i].name;
                            }
                            return NN.ToString();
                        }
                        else
                        {
                            var r = JSON.Parse("{}");
                            r["response"] = "Invalid Request / No Events";
                            return r.ToString();
                        }
                    }
                    return cmd;
                }
            }
            return "{\n\t\"response\":\"Dracon Interactive - CGRS\"\n}";
        }

        public static Dictionary<string, string> EditorUsers = new Dictionary<string, string>()
        {
            { "DefaultUser", "DefaultScene"}
        };
        public static string EditorResponse (HttpListenerRequest request)
        {
            using (StreamReader sr = new StreamReader(request.InputStream))
            {
                string data = sr.ReadToEnd();
                if (string.IsNullOrEmpty(data))
                {
                    return "<html><body><h1>Dracon Interactive Editor Server</h1></body></html>";
                }
                try
                {
                    var N = JSON.Parse(data);
                    if (N["Command"].Value == "GET")
                    {
                        var NN = JSON.Parse("{}");
                        foreach (string key in EditorUsers.Keys)
                        {
                            NN["Users"][key]["Scene"] = EditorUsers[key];
                        }
                        return NN.ToString();
                    } else if (N["Command"].Value == "ADD")
                    {
                        string user = N["User"].Value;
                        string scene = N["Scene"].Value;

                        if (EditorUsers.ContainsKey(user))
                        {
                            EditorUsers[user] = scene;
                        }
                        else
                        {
                            EditorUsers.Add(user, scene);
                        }

                        var NN = JSON.Parse("{}");
                        foreach (string key in EditorUsers.Keys)
                        {
                            NN["Users"][key]["Scene"] = EditorUsers[key];
                        }
                        return NN.ToString();
                    } else if (N["Command"].Value == "UPDATE")
                    {
                        string user = N["User"].Value;
                        string scene = N["Scene"].Value;

                        if (EditorUsers.ContainsKey(user))
                        {
                            EditorUsers[user] = scene;
                        } else
                        {
                            EditorUsers.Add(user, scene);
                        }

                        var NN = JSON.Parse("{}");
                        foreach (string key in EditorUsers.Keys)
                        {
                            NN["Users"][key]["Scene"] = EditorUsers[key];
                        }
                        return NN.ToString();
                    }
                } catch
                {
                    var r = JSON.Parse("{}");
                    r["response"] = "Invalid Request";
                    return r.ToString();
                }
                
            }
            /*
            var N = JSON.Parse("{}");
            N["Users"]["Peter"]["Scene"] = "Default";*/
            return "";
        }

        public static string ChoreResponse (HttpListenerRequest request)
        {
            string[] requestChannel = request.RawUrl.Split('/');
            string data = File.ReadAllText(@"ChoreData.txt");
            if (requestChannel.Length > 2)
            {
                if (requestChannel[2] == "user")
                {
                    data = File.ReadAllText(@"ChoreUserData.txt");
                }
            }
            
            return data;
        }
    }
    class Chore
    {
        string name;
        float progress;
        string iconName;
    }
}

