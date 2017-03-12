using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using TestTask.Models;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TestTask.Controllers
{
    [Produces("application/json")]
    public class ScheduleController : Controller
    {
        private readonly string sourceURLHead = "https://api.tfl.gov.uk";
        private readonly string sourceURLTail = "?app_id=7c54879d&app_key=166837b7ebca6528e0a2c9ab011f62c6";
        
        [HttpGet("schedule/{id}")]
        public async Task<IActionResult> Index([FromRoute] string id)
        {
            IList<LineScheduleModel> schedule = new List<LineScheduleModel>();
            string responseFromServer;
            int itemId = 1;
            DateTime timeNow = DateTime.Now;
            TimeSpan ts = new TimeSpan(24 + timeNow.Hour, timeNow.Minute, 0);
            TimeSpan daySpan = new TimeSpan(24, 0, 0);

            string url = sourceURLHead + "/Line/" + id + "/Route/Sequence/inbound" + sourceURLTail;
            try
            {
                responseFromServer = await getResponseAsync(url);
            }
            catch(WebException we)
            {
                return Json("[]");
            }

            // Extract naptanId and commonName for each stop of the route
            try
            {
                JObject routeInfo = JObject.Parse(responseFromServer);
                IList<StopModel> routeStopsList = new List<StopModel>();
                IList<JToken> results = routeInfo["stopPointSequences"][0]["stopPoint"].ToList();
                foreach (JToken res in results)
                {
                    StopModel nid = JsonConvert.DeserializeObject<StopModel>(res.ToString());
                    routeStopsList.Add(nid);
                }

                // Build a schedule for each station
                foreach (StopModel stop in routeStopsList)
                {
                    url = sourceURLHead + "/Line/" + id + "/Timetable/" + stop.id + sourceURLTail;
                    try
                    {
                        responseFromServer = await getResponseAsync(url);
                    }
                    catch (WebException we)
                    {
                        responseFromServer = null;
                    }

                    if (responseFromServer != null)
                    {
                        //Here we want to know the time of the last journey for all days [Monday-Friday],[Saturday],[Sunday]
                        JObject lastJourneysSearch = JObject.Parse(responseFromServer);
                        IList<HourMinuteModel> lastJourneyList = new List<HourMinuteModel>();
                        results = lastJourneysSearch["timetable"]["routes"][0]["schedules"].Children()["lastJourney"].ToList();
                        foreach (JToken res in results)
                        {
                            HourMinuteModel lj = JsonConvert.DeserializeObject<HourMinuteModel>(res.ToString());
                            lastJourneyList.Add(lj);
                        }
                        // As soon as we know the time of the last journey, we have to check which schedule to stick to
                        // E.g. if Saturday/Sunday just started, we should follow Friday/Saturday schedules.                   
                        int actualScheduleIndex = 0;
                        if (timeNow.DayOfWeek == DayOfWeek.Sunday)
                        {
                            if (ts.Days * 24 * 60 < Int32.Parse(lastJourneyList[2].hour) * 60 +
                             Int32.Parse(lastJourneyList[2].minute))
                            {
                                actualScheduleIndex = 1;
                            }
                            else
                            {
                                actualScheduleIndex = 2;
                            }
                        }

                        if (timeNow.DayOfWeek == DayOfWeek.Saturday)
                        {
                            if (ts.Days * 24 * 60 < Int32.Parse(lastJourneyList[1].hour) * 60 +
                             Int32.Parse(lastJourneyList[1].minute))
                            {
                                actualScheduleIndex = 0;
                            }
                            else
                            {
                                actualScheduleIndex = 1;
                            }
                        }

                        if (timeNow.DayOfWeek == DayOfWeek.Monday)
                        {
                            if (ts.Days * 24 * 60 < Int32.Parse(lastJourneyList[0].hour) * 60 +
                             Int32.Parse(lastJourneyList[0].minute))
                            {
                                actualScheduleIndex = 2;
                            }
                            else
                            {
                                actualScheduleIndex = 0;
                            }
                        }

                        // When we know which schedule to use, build the list of arrival times
                        IList<HourMinuteModel> journeysList = new List<HourMinuteModel>();
                        results = lastJourneysSearch["timetable"]["routes"][0]["schedules"][actualScheduleIndex]["knownJourneys"].Children().ToList();
                        foreach (JToken res in results)
                        {
                            HourMinuteModel jrn = JsonConvert.DeserializeObject<HourMinuteModel>(res.ToString());
                            journeysList.Add(jrn);
                        }
                        List<string> stopTimesList = new List<string>();
                        foreach (HourMinuteModel hm in journeysList)
                        {
                            TimeSpan span = new TimeSpan(Int32.Parse(hm.hour), Int32.Parse(hm.minute), 0);
                            if (span.Days > 0)
                            {
                                span = span - daySpan;
                            }
                            stopTimesList.Add(span.ToString().Substring(0, 5));
                        }

                        // Create a new LineSchedule object
                        schedule.Add(new LineScheduleModel(itemId++, stop.name, stopTimesList));
                    }
                    else
                    {
                        List<string> noresult = new List<string>();
                        noresult.Add("The schedule for the station is not available at the moment");
                        schedule.Add(new LineScheduleModel(itemId++, stop.name, noresult));
                    }
                }
            }
            catch(JsonException je)
            {
                return null;
            }
            return Json(schedule);
        }

        public async Task<String> getResponseAsync(string url)
        {
            string responseFromServer;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                var dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();
            }
            catch (WebException we)
            {
                throw;
            }
            return responseFromServer;
        }
    }
}
