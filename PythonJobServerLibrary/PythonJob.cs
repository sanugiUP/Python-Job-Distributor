using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonJobServerLibrary
{
    public class PythonJob
    {
        public string pythonScript { get; set; }
        public string result { get; set; }
        public int jobNumber { get; set; }
        public bool jobRequested { get; set; }
        public byte[] hash { get; set; }

    }
}
