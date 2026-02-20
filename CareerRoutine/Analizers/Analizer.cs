using CareerRoutine.Models;
using System.Diagnostics;

namespace CareerRoutine.Analizers
{
    internal class Analizer
    {
        public static bool Output(List<Job> jobs)
        {
            Trace.WriteLine("Title,Sender,ReceivedAt");
            foreach (var job in jobs)
            {
                Trace.WriteLine($"\"{job.Title}\",\"{job.Sender}\",\"{job.ReceivedAt}\"");
            }
            return true;
        }
    }
}
