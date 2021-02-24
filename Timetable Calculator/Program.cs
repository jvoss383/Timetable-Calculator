using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Timetable_Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            // getting location data
            Console.WriteLine("enter path to location lookup table: ");
            //string locationImportPath = Console.ReadLine().Replace('\"', ' ');
            string locationImportPath = @"C:\Users\jvoss\Downloads\University Timetable - Locations (3).tsv";
            Location[] locations = ImportData.Locations(locationImportPath);

            // getting timetable data
            Console.WriteLine("enter path to timetable data: ");
            //string timetableDataImportPath = Console.ReadLine().Replace('\"', ' ');
            string timetableDataImportPath = @"C:\Users\jvoss\Downloads\University Timetable - Timetable Calculator (2).tsv";
            Event[] events = ImportData.Events(timetableDataImportPath);

            TimetableOption[] timetableOptions = GenerateTimetableOptions(events);

            // generating timetables and calculating their scores
            Timetable[] timetables = new Timetable[timetableOptions.Count()];

            int validTimetables = 0;
            for(int i = 0; i < timetables.Count(); i++)
            {
                timetables[i] = new Timetable(timetableOptions[i], events); // this line does the heavy lifting of creating the TimeTable object
                if(timetables[i].valid)
                {
                    validTimetables++;
                    timetables[i].CalcScore(locations);
                    /*Console.WriteLine(String.Format(
                        "valid timetables: {0}\n" +
                        "current timetable index: {1}\n" +
                        "total timetables: {2}\n" +
                        "current timetable score: {3}",
                        validTimetables,
                        i, 
                        timetables.Count(),
                        timetables[i].score));//*/
                }
            }

            // filtering out invalid timetables
            timetables = Timetable.FilterValidTimetables(timetables);

            Console.WriteLine("sorting...");
            Timetable.SortTimetables(timetables, Timetable.SortOrder.ascending);
            Console.WriteLine("done");

            while(true)
            {
                try
                {
                    Console.WriteLine("enter timetable index: ");
                    timetables[Convert.ToInt32(Console.ReadLine())].PrintTimetable();
                }
                catch
                {
                    Console.WriteLine("invalid input");
                }
            }
        }

        static TimetableOption[] GenerateTimetableOptions(Event[] events)
        {
            int combinations = 1;
            foreach(Event event_ in events)
            {
                combinations *= event_.options.Count();
            }

            TimetableOption[] timetableOptions = new TimetableOption[combinations];
            // setting first timetableOption to all zeroes
            timetableOptions[0] = new TimetableOption(new int[events.Count()]);
            for (int i = 0; i < timetableOptions[0].eventOptionIndices.Count(); i++)
            {
                timetableOptions[0].eventOptionIndices[i] = 0;
            }

            // generating all other possible combinations
            for (int i = 1; i < combinations; i++)
            {
                timetableOptions[i] = IterateTimetableOptionIndices(timetableOptions[i - 1], events);
            }

            return timetableOptions;
        }

        /// <summary>
        /// Basically a counter where the base of each digit is equal to the number of eventOptions available for that event
        /// </summary>
        private static TimetableOption IterateTimetableOptionIndices(TimetableOption timetableOptionIndices, Event[] events)
        {
            TimetableOption newTimetableOptionIndices = new TimetableOption((int[])timetableOptionIndices.eventOptionIndices.Clone());
            for (int eventIndex = newTimetableOptionIndices.eventOptionIndices.Count() - 1; eventIndex > 0; eventIndex--) // should be while eventIndex > 0 so that it never directly iterates the most significant digit, otherwise it will eventually try to rollover into the [-1]th digit
            {
                newTimetableOptionIndices.eventOptionIndices[eventIndex]++;

                // then this pass rolls it over to 0, and the next index increases by one
                if (newTimetableOptionIndices.eventOptionIndices[eventIndex] == events[eventIndex].options.Count())
                {
                    newTimetableOptionIndices.eventOptionIndices[eventIndex] = 0;
                    //newTimetableOptionIndices.eventOptionIndices[eventIndex - 1]++; // no need to iterate because it will hit the normal iteration line next anyway
                }
                else // just iterating the current eventIndex was fine, no further rollovers required
                {
                    return newTimetableOptionIndices;
                }
            }
            return null;
        }
    }


    public class ImportData
    {
        public static Location[] Locations(string importPath)
        {
            string[] file = File.ReadAllLines(importPath);
            Location[] locations = new Location[file.Count() - 1];
            for(int i = 1; i < file.Count(); i++)
            {
                string[] splitLine = file[i].Split('\t'); // for .tsv files
                locations[i - 1] = new Location(splitLine[0], Convert.ToDouble(splitLine[1]), Convert.ToDouble(splitLine[2]), Convert.ToDouble(splitLine[3]));
            }
            return locations;
        }

        public static Event[] Events(string importPath)
        {
            string[] file = File.ReadAllLines(importPath);

            string currentPaper = "";
            List<Event> events = new List<Event>();
            for(int row = 0; row < file.Count(); row++)
            {
                string[] splitRow = file[row].Split('\t');

                // if row is labelled "Paper"
                if(splitRow[0] == "Paper")
                {
                    currentPaper = splitRow[1];
                    continue;
                }

                // if row is labelled "Event"
                if(splitRow[0] == "Event")
                {
                    // go horizontally across the events
                    for (int column = 1; column < splitRow.Count(); column++)
                    {
                        // go vertically adding the event options
                        Event newEvent = new Event(currentPaper, splitRow[column]);
                        
                        for(int eventOptionIndex = 1; row + eventOptionIndex < file.Count(); eventOptionIndex++)
                        {
                            // if row is empty then there are no more EventOptions for this event
                            if (file[row + eventOptionIndex].Split('\t')[column] == "")
                            {
                                break;
                            }

                            newEvent.options.Add(new EventOption(currentPaper, file[row + eventOptionIndex].Split('\t')[column]));
                        }
                        events.Add(newEvent);
                    }
                }
            }
            return events.ToArray();
        }
    }

    public class DistanceCalc
    {
        public static double Distance(Location pointA, Location pointB)
        {
            double radius = 6371;
            double dLatitude = ToRadians(pointA.latitude - pointB.latitude);
            double dLongitude = ToRadians(pointA.longitude - pointB.longitude);

            double a =
              Math.Sin(dLatitude / 2) * Math.Sin(dLatitude / 2) +
              Math.Cos(ToRadians(pointA.latitude)) * Math.Cos(ToRadians(pointB.latitude)) *
              Math.Sin(dLongitude / 2) * Math.Sin(dLongitude / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = radius * c;

            double correctionFactor = 23890.17d / 31150.28d;
            d *= correctionFactor;
            //d *= 1000; // km -> m
            return d;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }

    public class Location
    {
        public string name;
        public double longitude;
        public double latitude;
        public double altitude;

        public Location(string name_In, double longitude_In, double latitude_In, double altitude_In)
        {
            name = name_In;
            longitude = longitude_In;
            latitude = latitude_In;
            altitude = altitude_In;
        }
    
        public static Location ToLocation(string locationName, Location[] locations)
        {
            foreach(Location location in locations)
            {
                if(location.name.ToLower() == locationName.ToLower())
                {
                    return location;
                }
            }
            return null;
        }
    }
}

namespace ExtensionMethods
{ 
    public static class Extensions
    {
        public static Timetable_Calculator.Event FindEvent (this List<Timetable_Calculator.Event> list, string name)
        {
            foreach (Timetable_Calculator.Event timeTableEvent in list)
            {
                if(timeTableEvent.name == name)
                {
                    return timeTableEvent;
                }
            }

            return null;
        }
    }
}
