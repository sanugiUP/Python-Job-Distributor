using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PythonJobServerLibrary
{
    [ServiceContract]
    public interface PythonServerInterface
    {
        [OperationContract]
        PythonJob RequestJob();

        [OperationContract]
        void PostResult(int jobNumber, string jobResult);
    }
}
