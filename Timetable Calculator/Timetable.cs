using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Timetable_Calculator
{
    public class Timetable
    {
        public bool valid = false;
        public double score;
        public double distance;
        public double dElevation;
        public Day[] days = new Day[7];

        public enum SortOrder
        {
            ascending,
            descending
        }

        public void ExportICS(string outputLocation, int timetableIndex)
        {
            outputLocation = outputLocation[outputLocation.Length - 1] == '\\' ? outputLocation : outputLocation + "\\"; // adds '\\' char if absent
            outputLocation += timetableIndex + "_calendar.ics";

            List<EventOption> calendarEvents = new List<EventOption>();

            for(int day = 0; day < 7; day++)
            {
                for(int hour = 0; hour < 24; hour++)
                {
                    if (!calendarEvents.Contains(days[day].hours[hour]))
                    {
                        calendarEvents.Add(days[day].hours[hour]);
                    }
                }
            }

            Calendar calendar = new Calendar();
            calendar.AddTimeZone(new VTimeZone("Pacific/Auckland"));

            foreach(EventOption eventOption in calendarEvents)
            {
                if(eventOption != null && eventOption.realClass)
                {
                    int startHour = eventOption.startTime;
                    int startMinute = startHour > 12 ? 10 : 0;
                    int endMinute = startMinute + 50;
                    if(endMinute == 60)
                    {
                        endMinute = 0;
                    }
                    int endHour = eventOption.startTime + eventOption.duration;
                    if(endMinute == 50)
                    {
                        endHour--;
                        // class doesn't actually span the change of hour, therefore sutract the hour change added by eventOption.duration.
                    }

                    int dateDay = 20 + (int)eventOption.day - 1 - 7;
                    int dateMonth = 9;
                    int dateYear = 2021;

                    string room = Convert.ToString(eventOption.room);
                    if(room.Length == 1)
                    {
                        room = "0" + room;
                    }
                    string floor = Convert.ToString(eventOption.floor);
                    if(floor == "0")
                    {
                        floor = "G";
                    }
                    else if (floor == "-1")
                    {
                        floor = "B";
                    }

                    string location = eventOption.block + "." + floor + "." + room;

                    RecurrencePattern recurrencePattern = new RecurrencePattern()
                    {
                        Frequency = FrequencyType.Weekly,
                        Interval = 1,
                        ByDay = new List<WeekDay>
                        {
                            new WeekDay
                            {
                                DayOfWeek = (DayOfWeek)(eventOption.day)
                            }
                        },
                        FirstDayOfWeek = DayOfWeek.Sunday,
                        Until = new DateTime(2021,10,15)
                    };

                    CalendarEvent calendarEvent = new CalendarEvent()
                    {
                        Summary = "[" + location + "] " + eventOption.paperName + " " + eventOption.name,
                        //Description = ,
                        Start = new CalDateTime(dateYear, dateMonth, dateDay, startHour, startMinute, 0),
                        Duration = TimeSpan.FromMinutes(eventOption.duration * 60 - 10),
                        RecurrenceRules = new List<RecurrencePattern>() { recurrencePattern }
                    };

                    calendar.Events.Add(calendarEvent);
                }
            }

            CalendarSerializer iCalSerializer = new CalendarSerializer();
            string result = iCalSerializer.SerializeToString(calendar);
            File.WriteAllText(outputLocation, result);
        }

        public string ExportTSV(string outputLocation, int timetableIndex)
        {
            outputLocation = outputLocation[outputLocation.Length - 1] == '\\' ? outputLocation : outputLocation + "\\"; // adds '\\' char if absent
            outputLocation += timetableIndex + ".tsv";

            int startHour = 8;
            int endHour = 17;
            int startDay = 1;
            int endDay = 6;
            string[] daysOfWeek = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string output = "";

            // header
            output += "\t";
            for (int day = startDay; day < endDay; day++)
            {
                output += daysOfWeek[day] + "\t";
            }
            output += "\n";

            for (int hour = startHour; hour < endHour; hour++)
            {
                for(int row = 0; row < 4; row++)
                {
                    // hours column
                    if (row == 0)
                        output += hour + ":00\t";
                    else
                        output += "\t";

                    for (int day = startDay; day < endDay; day++)
                    {
                        if(days[day].hours[hour] != null && days[day].hours[hour].realClass)
                        {
                            switch (row)
                            {
                                case 0:
                                    output += days[day].hours[hour].paper + "\t";
                                    break;
                                case 1:
                                    output += days[day].hours[hour].paperName + "\t";
                                    break;
                                case 2:
                                    output += days[day].hours[hour].name + "\t";
                                    break;
                                case 3:
                                    output += days[day].hours[hour].fullLocation + "\t";
                                    break;

                            }
                        }
                        else
                        {   // empty slot
                            output += "\t";
                        }
                    }
                    output += "\n";
                }
            }

            using (StreamWriter sw = new StreamWriter(outputLocation))
            {
                sw.Write(output);
            }
            return output;
        }

        public string PrintTimetable()
        {
            int headerLines = 2;
            int footerLines = 4;
            int columnSize = 23;
            int hoursColumnWidth = 9;
            int rowSize = 5;
            int startHour = 8;
            int endHour = 17;
            int startDay = 1;
            int endDay = 6;
            string[] daysOfWeek = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

            string[] output = new string[(endHour - startHour) * rowSize + headerLines + footerLines];
            Console.WindowWidth = hoursColumnWidth + columnSize * (endDay - startDay) + 1;

            // hours column
            // padding header lines
            for (int row = 0; row < headerLines; row++)
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
                for (int i = 1; i < headerLines; i++)
                {
                    output[i] += NormLen("", columnSize - 1, ' ') + "|";
                }

                // footer
                output[output.Count() - 3] += NormLen(Convert.ToString(days[day].score), columnSize - 1, ' ') + "|"; ;
                output[output.Count() - 2] += NormLen(Convert.ToString(Math.Round(days[day].distance * 1000, 0)), columnSize - 1, ' ') + "|"; ;
                output[output.Count() - 1] += NormLen(Convert.ToString(Math.Round(days[day].dElevation, 1)), columnSize - 1, ' ') + "|"; ;
                for (int row = output.Count() - 4; row > output.Count() - 1 - footerLines; row--)
                {
                    output[row] += NormLen("", columnSize - 1, ' ') + "|"; ;
                }

                // body
                for (int hour = startHour; hour < endHour; hour++)
                {
                    // pad if the slot is empty / shouldn't be printed
                    if (days[day].hours[hour] == null ||
                        !days[day].hours[hour].realClass)
                    {
                        for (int rowOffset = 0; rowOffset < rowSize; rowOffset++)
                        {
                            output[headerLines + (hour - startHour) * rowSize + rowOffset] += NormLen("", columnSize - 1, rowOffset == rowSize - 1 ? '-' : ' ') + "|";
                        }
                    }
                    else
                    {
                        output[headerLines + (hour - startHour) * rowSize    ] += NormLen(days[day].hours[hour].paper,        columnSize - 1, ' ') + "|";
                        output[headerLines + (hour - startHour) * rowSize + 1] += NormLen(days[day].hours[hour].paperName,    columnSize - 1, ' ') + "|"; // human readable paper name
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

        public static Timetable[] CustomFilterTimetables(Timetable[] timetables)
        {
            List<Timetable> outputTimetables = new List<Timetable>();
            foreach (Timetable timetable in timetables)
            {
                int threeHourGaps = 0;
                for (int day = 1; day < 6; day++)
                {
                    if (timetable.days[day].startTime >= 13)
                        threeHourGaps++;
                    if (timetable.days[day].endTime <= 13)
                        threeHourGaps++;
                }
                if (threeHourGaps >= 2)
                    outputTimetables.Add(timetable);
            }
            return outputTimetables.ToArray();
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

            EventOption bikeRack = new EventOption("Transport", "", ",,Bike Rack.0.0,Bike Rack");
            bikeRack.realClass = false;
            EventOption library = new EventOption("Study Time", "", ",,M.3.0,Library");
            library.realClass = false;

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
                            days[day].hours[hour - 1] = bikeRack;
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
                            days[day].hours[hour + 1] = bikeRack;
                        }
                        break;
                    }
                }

                // filling all empty slots with the library
                for(int hour = days[day].startTime; hour < days[day].endTime; hour++)
                {
                    if(days[day].hours[hour] == null)
                    {
                        days[day].hours[hour] = library;
                    }
                }
            }
            valid = true; // says that this timetable encountered no clashes while being constructed and could be used
        }

        public double NewCalcScore()
        {
            double gapLengthPenalty = 0;
            double hoursInClassesPenalty = 0;
            double continuousClassesPenalty = 0;

            for(int day = 0; day < 5; day++)
            {
                int hoursInClasses = 0;
                for(int hour = days[day].startTime; hour < days[day].endTime; hour++)
                {
                    // if it's a gap chunk
                    if (!days[day].hours[hour].realClass)
                    {
                        int gapLength = 1;
                        for (int seekHour = hour + 1; seekHour < days[day].endTime; seekHour++)
                        {
                            if (!days[day].hours[seekHour].realClass)
                            {
                                gapLength++;
                            }
                            else
                            {   // is a class, indicates end of gap
                                hour = seekHour;
                                break;
                            }
                        }

                        // application
                        if (gapLength == 1)
                        {
                            gapLengthPenalty += 2;
                        }
                        else if (gapLength > 2)
                        {
                            gapLengthPenalty++;
                        }

                    }
                    // if it's a class chunk
                    else
                    {
                        int classesLength = 1;
                        for (int seekHour = hour + 1; seekHour < days[day].endTime; seekHour++)
                        {
                            if (days[day].hours[seekHour].realClass)
                            {
                                classesLength++;
                            }
                            else
                            {   // is a class, indicates end of gap
                                hour = seekHour;
                                break;
                            }
                        }

                        // application
                        if(classesLength >= 4)
                        {
                            continuousClassesPenalty += classesLength * 2 - 7;
                        }
                    }
                }

                // application
                if (hoursInClasses >= 4)
                {
                    hoursInClassesPenalty += hoursInClasses - 3;
                }
            }

            return gapLengthPenalty + hoursInClassesPenalty + continuousClassesPenalty;
            
        }

        public double CalcScore(Location[] locations)
        {
            //const double generalAltitudePenalty = 20; // adjust this depending on how much you hate hills. Higher value = more hate
            //const double stairsPenalty = 20;
            //const double contiguousClassPenaltyThreshold = 3;
            //const double contiguousClassPenaltyAmount = 2; // amount per class over the threshold

            const double generalAltitudePenalty = 0; // adjust this depending on how much you hate hills. Higher value = more hate
            const double stairsPenalty = 0;
            const double contiguousClassPenaltyThreshold = 999;
            const double contiguousClassPenaltyAmount = 0; // amount per class over the threshold

            for (int day = 0; day < days.Count(); day++)
            {
                int contiguousClasses = 0;

                for (int hour = days[day].startTime; hour < days[day].endTime - 1; hour++) // -1 end stop because it compares the current index with index+1
                {
                    Location locationA = Location.ToLocation(days[day].hours[hour].block, locations);
                    Location locationB = Location.ToLocation(days[day].hours[hour + 1].block, locations);
                    double distance = DistanceCalc.Distance(locationA, locationB);
                    days[day].score += distance;
                    days[day].distance += distance;
                    //Console.WriteLine(NormLen(days[day].hours[hour].block + " --> " + days[day].hours[hour + 1].block, 14, ' ') + Math.Round(distance * 1000, 0));

                    // general elevation change penalty calculation
                    double deltaElevation = Math.Abs(locationB.altitude - locationA.altitude);
                    days[day].dElevation += deltaElevation;
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

                    // contiguous classes penalty
                    if(days[day].hours[hour].realClass)
                    {
                        contiguousClasses++;
                    }
                    else
                    {
                        if (contiguousClasses >= contiguousClassPenaltyThreshold)
                        {   // done at the end of a contiguous block
                            score += Math.Pow((contiguousClasses - contiguousClassPenaltyThreshold), 3) * contiguousClassPenaltyAmount;
                        }
                        contiguousClasses = 0;
                    }
                }
                score += days[day].score;
                distance += days[day].distance;
                dElevation += days[day].dElevation;
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
        public double distance;
        public double dElevation;
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
        public string paperName;
        public string name;
        public List<EventOption> options;

        public Event(string paper, string paperName, string name, List<EventOption> options)
        {
            this.paper = paper;
            this.paperName = paperName;
            this.name = name;
            this.options = options;
        }
        
        public Event(string paper, string paperName, string name)
        {
            this.paper = paper;
            this.paperName = paperName;
            this.name = name;
            this.options = new List<EventOption>();
        }
    }

    public class EventOption
    {
        public string paper;
        public string paperName;
        public string name;
        public bool realClass = true;

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

        public EventOption(string paper, string paperName, string input)
        {
            string[] splitInput = input.Split(',');
            this.paper = paper;
            this.paperName = paperName;

            day = ToDay(splitInput[0]);

            if(splitInput[1].Contains('-'))
            {
                startTime = Convert.ToInt32(splitInput[1].Split('-')[0].Split(':')[0]);
                endTime = Convert.ToInt32(splitInput[1].Split('-')[1].Split(':')[0]);
                duration = endTime - startTime;
            }

            fullLocation = splitInput[2];
            if (fullLocation[0] == ' ')
            {
                fullLocation = fullLocation.Replace(" ", "");
            }
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

        public Location ToLocation(Location[] locations)
        {
            return Location.ToLocation(block, locations);
        }
    }
}
