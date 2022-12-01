using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonJobServerLibrary
{
    public class PythonJobPool
    {
        /* List Of All The Jobs Posted By All The Clients */
        public static List<PythonJob> jobPool = new List<PythonJob>();
        public static List<PythonJob> getAllJobs()
        {
            return jobPool;
        }

        public static void addJobToPool(PythonJob pythonJob)
        {
            jobPool.Add(pythonJob);
        }

        public static void updateJobInPool(PythonJob pythonJob)
        {
            foreach (var job in jobPool)
            {
                if (job.jobNumber == pythonJob.jobNumber)
                {
                    job.jobRequested = pythonJob.jobRequested;
                    job.result = pythonJob.result;
                }
            }
        }

        public static void removeJobFromPool(PythonJob pythonJob)
        {
            //this.jobPool.Remove(pythonJob); // This will cause a problem if two jobs are same
            foreach(var job in jobPool)
            {
                if(job.jobNumber == pythonJob.jobNumber)
                {
                    jobPool.Remove(job);
                }
            }
        }

    }
}
