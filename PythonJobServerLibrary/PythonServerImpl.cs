using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PythonJobServerLibrary
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false, InstanceContextMode = InstanceContextMode.Single)]
    public class PythonServerImpl : PythonServerInterface
    {
        private PythonJobPool jobPool = new PythonJobPool();
        public void PostResult(int jobNumber, string jobResult)
        {
            List<PythonJob> jobList = PythonJobPool.getAllJobs();

            for (int i = 0; i < jobList.Count; i++)
            {
                if (jobList[i].jobNumber == jobNumber)
                {
                    jobList[i].result = jobResult;
                    break;
                }
            }
        }

        public PythonJob RequestJob()
        {
            List<PythonJob> jobList = PythonJobPool.getAllJobs();
            PythonJob send_job = new PythonJob();

            for(int i = 0; i < jobList.Count; i++)
            {
                if (jobList[i].jobRequested == false)
                {
                    jobList[i].jobRequested = true; 
                    send_job = jobList[i];
                    break;
                }
            }
            return send_job;
        }
    }
}
