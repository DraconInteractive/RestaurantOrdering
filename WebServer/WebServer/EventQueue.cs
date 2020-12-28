using System;
using System.Collections.Generic;
using System.Text;

namespace Dracon.WebServer
{
    public class EventQueue
    {
        public class Event
        {
            public string owner;
            public string game;
            public string name;
        }
        public List<Event> events;

        public EventQueue ()
        {
            events = new List<Event>();
        }

        public void Add (string owner, string game, string name)
        {
            events.Add(new Event()
            {
                owner = owner,
                name = name,
                game = game
            });
        }

        public Event[] PopQueue (string owner, string game = "")
        {
            List<Event> result = new List<Event>();

            if (!string.IsNullOrEmpty(game))
            {
                foreach (Event e in events)
                {
                    if (e.owner == owner && e.game == game)
                    {
                        result.Add(e);
                    }
                }
                events.RemoveAll(e => (e.owner == owner && e.game == game));
            } else
            {
                foreach (Event e in events)
                {
                    if (e.owner == owner)
                    {
                        result.Add(e);
                    }
                }
                events.RemoveAll(e => e.owner == owner);
            }
            
            return result.ToArray();
        }
    }

}
