using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetable_Calculator
{
    public class Timetable
    {
        public bool valid = false;
        public double score;
        public Day[] days = new Day[7];

        public enum SortOrder
        {
            ascending,
            descending
        }

        public string PrintTimetable()
        {
            int headerLines = 2;
            int footerLines = 2;
            int columnSize = 22;
            int hoursColumnWidth = 9;
            int rowSize = 5;
            int startHour = 8;
            int endHour = 17;
            int startDay = 1;
            int endDay = 6;
            string[] daysOfWeek = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string[] output = new string[(endHour - startHour) * rowSize + headerLines + footerLines];

            // hours column
            // padding header lines
            for(int row = 0; row < headerLines; row++)
            {
                output[row] += NormLen("", hoursColumnWidth - 1, ' ') + "|";
            }
            //padding footer rows
            for(int row = output.Count() - 1; row > output.Count() - 1 - footerLines; row--)
            {
                output[row] += NormLen("", hoursColumnWidth - 1, ' ') + "|";
            }
            // main hours column loop
            for (int hour = 0; hour < endHour - startHour; hour++)
            {
                // print 24hr time
                output[headerLines + hour * rowSize] += NormLen(hour + startHour + ":00", hoursColumnWidth - 1, ' ') + '|';
                // print 12hr time
                if(hour + startHour > 12)
                {
                    output[headerLines + hour * rowSize + 1] += NormLen((hour - 12) + startHour + ":00", hoursColumnWidth - 1, ' ') + '|';
                }
                else
                {
                    output[headerLines + hour * rowSize + 1] += NormLen("", hoursColumnWidth - 1, ' ') + "|";
                }

                // pad remaining rows
                for(int rowOffset = 2; rowOffset < rowSize; rowOffset++)
                {
                    output[headerLines + hour * rowSize + rowOffset] += NormLen("", hoursColumnWidth - 1, ' ') + "|";
                }
            }


            for (int day = startDay; day < endDay; day++)
            {
                // header
                output[0] += NormLen(daysOfWeek[day], columnSize - 1, ' ') + "|";
                output[1] += NormLen(""             , columnSize - 1, ' ') + "|";

                // footer
                output[output.Count() - 2] += NormLen("", columnSize - 1, ' ') + "|"; ;
                output[output.Count() - 1] += NormLen(Convert.ToString(days[day].score), columnSize - 1, ' ') + "|"; ;

                for (int hour = startHour; hour < endHour; hour++)
                {
                    // pad if the slot is empty / shouldn't be printed
                    if (days[day].hours[hour] == null ||
                        days[day].hours[hour].paper == "Study Time" ||
                        days[day].hours[hour].paper == "Transport")
                    {
                        for (int rowOffset = 0; rowOffset < rowSize; rowOffset++)
                        {
                            output[headerLines + (hour - startHour) * rowSize + rowOffset] += NormLen("", columnSize - 1, rowOffset == rowSize - 1 ? '-' : ' ') + "|";
                        }
                    }
                    else
                    {
                        output[headerLines + (hour - startHour) * rowSize    ] += NormLen(days[day].hours[hour].paper,        columnSize - 1, ' ') + "|";
                        output[headerLines + (hour - startHour) * rowSize + 1] += NormLen("",                                 columnSize - 1, ' ') + "|"; // human readable paper name
                        output[headerLines + (hour - startHour) * rowSize + 2] += NormLen(days[day].hours[hour].name,         columnSize - 1, ' ') + "|";
                        output[headerLines + (hour - startHour) * rowSize + 3] += NormLen(days[day].hours[hour].fullLocation, columnSize - 1, ' ') + "|";
                        output[headerLines + (hour - startHour) * rowSize + 4] += NormLen("",                                 columnSize - 1, '-') + "|";
                    }
                }
            }

            string singleOutput = ""; // in one index with newlines rather than in an array
            foreach(string line in output)
            {
                singleOutput += line + "\n";
            }

            Console.WriteLine(singleOutput);
            return singleOutput;
        }

        /// <summary>
        ///  Normalize length of string by adding leading and trailing space chars
        /// </summary>
        private string NormLen(string input, int length, char repeatedChar)
        {
            if(input.Length < length)
                return
                    new String(repeatedChar, (int)Math.Floor((length - input.Length) / 2d)) +
                    input +
                    new String(repeatedChar, (int)Math.Ceiling((length - input.Length) / 2d));
            else
            {
                return input.Substring(0, length);
            }
        }

        public static Timetable[] FilterValidTimetables(Timetable[] timetables)
        {
            List<Timetable> validTimetables = new List<Timetable>();
            foreach(Timetable timetable in timetables)
            {
                if(timetable.valid)
                {
                    validTimetables.Add(timetable);
                }
            }
            return validTimetables.ToArray();
        }

        public static Timetable[] SortTimetables(Timetable[] timetables, SortOrder sortOrder)
        {
            float divisor = 1.3f;
            int gap = (int)(timetables.Count() / divisor);
            bool sorted = true;
            do
            {
                sorted = true;
                gap = (int)(gap / divisor);
                if (gap < 1)
                    gap = 1;

                for(int i = 0; i + gap < timetables.Count(); i++)
                {
                    if(sortOrder == SortOrder.ascending)
                    {   // if out of order
                        if(timetables[i].score > timetables[i + gap].score)
                        {
                            sorted = false;
                            Timetable temp = timetables[i];
                            timetables[i] = timetables[i + gap];
                            timetables[i + gap] = temp;
                        }
                    }
                    else // descending
                    {
                        if (timetables[i].score < timetables[i + gap].score)
                        {   // if out of order
                            sorted = false;
                            Timetable temp = timetables[i];
                            timetables[i] = timetables[i + gap];
                            timetables[i + gap] = temp;
                        }
                    }
                }

                // double check that it's sorted
                if (sorted)
                {
                    sorted = Sorted(timetables, sortOrder);
                }
            } while (!sorted);

            return timetables;
        }

        private static bool Sorted(Timetable[] timetables, SortOrder sortOrder)
        {
            for(int i = 0; i < timetables.Count() - 1; i++)
            {
                if(sortOrder == SortOrder.ascending)
                {
                    if (timetables[i].score > timetables[i + 1].score)
                        return false;
                }
                else
                {   // is descending
                    if (timetables[i].score < timetables[i + 1].score)
                        return false;
                }
            }
            return true;
        }

        public Timetable(TimetableOption timetableOption, Event[] events)
        {
            // calling days constructors
            for(int day = 0; day < days.Count(); day++)
            {
                days[day] = new Day();
            }

            // putting all predefined events into timetable
            for(int eventIndex = 0; eventIndex < events.Count(); eventIndex++)
            {
                EventOption eventOption = events[eventIndex].options[timetableOption.eventOptionIndices[eventIndex]];
                for(int time = eventOption.startTime; time < eventOption.endTime; time++)
                {
                    if(days[(int)eventOption.day].hours[time] != null)
                    {   // then an event is already on at this time, threfore this is a clash and the timetableOption is invalid
                        valid = false;
                        return;
                    }
                    else
                    {   // timeslot is available
                        days[(int)eventOption.day].hours[time] = eventOption;
                    }
                }
            }

            EventOption BikeRack = new EventOption("Transport", ",,I Bike Rack.0.0,Bike Rack");
                                                              //",,LSL.1.02 , Lab A"
            EventOption Library = new EventOption("Study Time", ",,M.3.0,Library");

            // entering M block as home location for gaps in timetable, entering I block bike rack as start and end caps
            for(int day = 0; day < days.Count(); day++)
            {
                // finding start time, setting bike rack start cap
                for(int hour = 0; hour < days[day].hours.Count(); hour++)
                {
                    if(days[day].hours[hour] != null)
                    {   // first filled slot
                        days[day].startTime = hour;
                        if(hour >= 1)
                        {
                            days[day].hours[hour - 1] = BikeRack;
                        }
                        break;
                    }
                }

                // finding end time, setting bike rack end cap
                for(int hour = days[day].hours.Count() - 1; hour >= 0; hour--)
                {
                    if (days[day].hours[hour] != null)
                    {   // last filled slot
                        days[day].endTime = hour;
                        if (hour < 23)
                        {
                            days[day].hours[hour + 1] = BikeRack;
                        }
                        break;
                    }
                }

                // filling all empty slots with the library
                for(int hour = days[day].startTime; hour < days[day].endTime; hour++)
                {
                    if(days[day].hours[hour] == null)
                    {
                        days[day].hours[hour] = Library;
                    }
                }
            }
            valid = true; // says that this timetable encountered no clashes while being constructed and could be used
        }

        public double CalcScore(Location[] locations)
        {
            const double generalAltitudePenalty = 20; // adjust this depending on how much you hate hills. Higher value = more hate
            const double stairsPenalty = 20;

            for (int day = 0; day < days.Count(); day++)
            {
                for (int hour = days[day].startTime; hour < days[day].endTime - 1; hour++) // -1 end stop because it compares the current index with index+1
                {
                    Location locationA = Location.ToLocation(days[day].hours[hour].block, locations);
                    Location locationB = Location.ToLocation(days[day].hours[hour + 1].block, locations);
                    days[day].score += DistanceCalc.Distance(locationA, locationB);

                    // general elevation change penalty calculation
                    double deltaElevation = Math.Abs(locationB.altitude - locationA.altitude);
                    if (deltaElevation > 0)
                    {   // uphill
                        days[day].score += deltaElevation * generalAltitudePenalty;
                    }
                    else
                    {   // downhill
                        days[day].score += deltaElevation * generalAltitudePenalty / 2;
                    }

                    // stairs elevation change penalty calculation
                    days[day].score += days[day].hours[hour].floor > 0 ? days[day].hours[hour].floor * stairsPenalty : days[day].hours[hour].floor * stairsPenalty / 2;
                }
                score += days[day].score;
                if(days[day].score < 0)
                {
                    throw new Exception("semantically incorrect result");
                }
            }
            return score;
        }
    }

    public class Day
    {
        public int startTime, endTime;
        public double score;
        public EventOption[] hours = new EventOption[24];
    }

    public class TimetableOption
    {
        public int[] eventOptionIndices;

        public TimetableOption(int[] eventOptionIndices)
        {
            this.eventOptionIndices = eventOptionIndices;
        }
    }

    public class Event
    {
        public string paper;
        public string name;
        public List<EventOption> options;

        public Event(string paper, string name, List<EventOption> options)
        {
            this.paper = paper;
            this.name = name;
            this.options = options;
        }
        
        public Event(string paper, string name)
        {
            this.paper = paper;
            this.name = name;
            this.options = new List<EventOption>();
        }
    }

    public class EventOption
    {
        public string paper;
        public string name;

        public int startTime; // times are done in hours, i.e. 11:00am = 11, 3:00pm = 15, etc.. 
        public int endTime;
        public int duration;

        public string fullLocation;
        public string block;
        public int floor;
        public int room;
        public DayEnum day;

        public enum DayEnum
        {
            Sunday = 0,
            Monday = 1,
            Tuesday = 2,
            Wednesday = 3,
            Thursday = 4,
            Friday = 5,
            Saturday = 6,

            Invalid = -1
        }

        public EventOption(string paper, string input)
        {
            string[] splitInput = input.Split(',');
            this.paper = paper;

            day = ToDay(splitInput[0]);

            if(splitInput[1].Contains('-'))
            {
                startTime = Convert.ToInt32(splitInput[1].Split('-')[0].Split(':')[0]);
                endTime = Convert.ToInt32(splitInput[1].Split('-')[1].Split(':')[0]);
                duration = endTime - startTime;
            }

            fullLocation = splitInput[2].Replace(" ", "");
            block = fullLocation.Split('.')[0];
            string tempFloor = fullLocation.Split('.')[1].Replace(" ", "");
            if(tempFloor == "G")
                floor = 0;
            else if(tempFloor == "B")
                floor = -1;
            else
                floor = Convert.ToInt32(tempFloor);
            room = Convert.ToInt32(fullLocation.Split('.')[2]);

            name = splitInput[3];
            if(name[0] == ' ')
            {
                name = name.Substring(1, name.Length - 1);
            }
        }

        static DayEnum ToDay(string input)
        {
            if (input.Length < 3)
                return DayEnum.Invalid;
            switch(input.Substring(0, 3).ToLower())
            {
                case "sun":
                    return DayEnum.Sunday;
                case "mon":
                    return DayEnum.Monday;
                case "tue":
                    return DayEnum.Tuesday;
                case "wed":
                    return DayEnum.Wednesday;
                case "thu":
                    return DayEnum.Thursday;
                case "fri":
                    return DayEnum.Friday;
                case "sat":
                    return DayEnum.Saturday;
            }
            return DayEnum.Invalid;
        }
    }
}
