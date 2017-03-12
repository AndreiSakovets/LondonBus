using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestTask.Models
{
    public class LineScheduleModel
    {
        public int id { get; set; }
        public string stationName { get; set;}
        public List<string> scheduleString { get; set; }
        public LineScheduleModel(int id,string stationName, List<string> scheduleString) {
            this.id = id;
            this.stationName = stationName;
            this.scheduleString = scheduleString;
        }
    }
}
