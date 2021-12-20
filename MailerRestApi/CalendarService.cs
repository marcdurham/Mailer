using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Mailer
{
    public class CalendarService
    {
        private readonly CalendarOptions _options;
        private readonly IMemoryCache _memoryCache;

        public CalendarService(IOptions<CalendarOptions> options, IMemoryCache memoryCache)
        {
            _options = options.Value;
            _memoryCache = memoryCache;
        }

        public async Task<string[]> GetCalendarsAsync()
        {
            //string baseAddress = "http://localhost:57012/";
            var httpClient = new HttpClient(); // { BaseAddress = new Uri(baseAddress) };
            //var myPocos = await httpClient.GetFromJsonAsync<string>("api/mypocos");

            List<Calendar> calendars = new();
            foreach (var calendar in _options.Calendars)
            {
                calendars.Add(
                    new Calendar
                    {
                        Name = calendar.Name,
                        IcsUri = calendar.IcsUri
                    });
            }

            List<string> eventList = new();
            foreach (Calendar calendar in calendars)
            {
                string? text = await httpClient.GetStringAsync(calendar.IcsUri);
                Ical.Net.Calendar icalCal = Ical.Net.Calendar.Load(text);

                var upcoming = icalCal.Events.Where(e => 
                    e.Start.GreaterThanOrEqual(new Ical.Net.DataTypes.CalDateTime(DateTime.Today.AddDays(-14))));

                foreach(var evnt in upcoming)
                {
                    eventList.Add($"{evnt.Start.ToString()} {evnt.Summary}");
                }

            }

            return await Task.FromResult(eventList.ToArray());
        }

        public async Task<string> GetCalendarFileAsync(string prefix)
        {
            //string baseAddress = "http://localhost:57012/";
            var httpClient = new HttpClient(); // { BaseAddress = new Uri(baseAddress) };
            //var myPocos = await httpClient.GetFromJsonAsync<string>("api/mypocos");

            List<Calendar> calendars = new();
            foreach (var calendar in _options.Calendars)
            {
                calendars.Add(
                    new Calendar
                    {
                        Name = calendar.Name,
                        IcsUri = calendar.IcsUri
                    });
            }

            List<string> eventList = new();
            var shortCalendar = new Ical.Net.Calendar();
            
            foreach (Calendar calendar in calendars)
            {
                string? text = null;
                if (!_memoryCache.TryGetValue(calendar.IcsUri, out string cachedText))
                {
                    cachedText = await httpClient.GetStringAsync(calendar.IcsUri);

                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromSeconds(10));

                    _memoryCache.Set(calendar.IcsUri, cachedText, cacheEntryOptions);
                }

                text = cachedText; //await httpClient.GetStringAsync(calendar.IcsUri);
                Ical.Net.Calendar icalCal = Ical.Net.Calendar.Load(text);

                var upcoming = icalCal.Events.Where(e =>
                    e.Start.GreaterThanOrEqual(new Ical.Net.DataTypes.CalDateTime(DateTime.Today.AddDays(-31))));

                foreach (var evnt in upcoming)
                {
                    eventList.Add($"{evnt.Start.ToString()} {evnt.Summary}");

                    if (prefix == "*" && !evnt.Summary.Contains(":")
                        || evnt.Summary.ToUpper().StartsWith($"{prefix.ToUpper()}:"))
                    {

                        var calEvent = new CalendarEvent
                        {
                            Start = new CalDateTime(evnt.Start),
                            End = new CalDateTime(evnt.Start.AddHours(1)),
                            Summary = $"{evnt.Summary}",
                        };
                        shortCalendar.Events.Add(calEvent);
                    }
                }

            }

            //return await Task.FromResult(eventList.ToArray());

            //var e = new CalendarEvent
            //{
            //    Start = new CalDateTime(now),
            //    End = new CalDateTime(later),
            //    RecurrenceRules = new List<RecurrencePattern> { rrule },
            //};

            //var calendar = new Calendar();
            //calendar.Events.Add(e);

            var serializer = new CalendarSerializer();
            var serializedCalendar = serializer.SerializeToString(shortCalendar);
            //return await Task.FromResult(serializedCalendar);
            return await Task.FromResult(serializedCalendar);
        }
    }
}
