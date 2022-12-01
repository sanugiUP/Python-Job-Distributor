using Newtonsoft.Json;
using PythonJobServerLibrary;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Windows;
using Web_Server.Models;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Threading.Tasks;
using System.IO;

namespace ClientOne
{
    public delegate string CheckForResult(int jobNumber);
    public partial class MainWindow : Window
    {
        /* REF : https://en.wikipedia.org/wiki/Localhost */
        private static int client_one_port_no = 8000;
        private static string client_one_ip = "127.0.0.2";
        private static int jobsTaken = 0;

        public MainWindow()
        {
            InitializeComponent();

            /* Register Client One To DB */
            registerClient();

            /* Setting Up And Running Threads */
            initializeThreads();
        }

        private void registerClient()
        {
            RestClient restClient = new RestClient("http://localhost:55924/");
            Client clientOne = new Client();
            clientOne.ipaddress = client_one_ip;
            clientOne.portnumber = client_one_port_no;
            clientOne.jobscompleted = 0;

            RestRequest request = new RestRequest("api/clients/registerclient", Method.Post);
            request.AddJsonBody(clientOne);
            RestResponse response = restClient.Execute(request);

            if (response.StatusCode.ToString().ToLower().Equals("conflict"))
            {
                // do nothing because client one is already registered
            }
        }

        private void initializeThreads()
        {
            /* REF : https://www.c-sharpcorner.com/UploadFile/1c8574/threads-in-wpf/ */
            /* REF : https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadstart?view=net-6.0 */

            /* Creating Server Thread */
            ThreadStart serverThreadDelegate = new ThreadStart(serverThreadFunction);
            Thread server_thread = new Thread(serverThreadDelegate);
            server_thread.Start();

            /* Creating Network Thread */
            ThreadStart networkThreadDelegate = new ThreadStart(networkThreadFunction);
            Thread network_thread = new Thread(networkThreadDelegate);
            network_thread.Start();
        }

        private void serverThreadFunction()
        { 
            /* Setting Up The Python Job Server Which Allows Client One To Request Jobs And Other Clients Post Results */
            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();

            host = new ServiceHost(typeof(PythonServerImpl));
            host.AddServiceEndpoint(typeof(PythonServerInterface), tcp, "net.tcp://" + client_one_ip + ":" + client_one_port_no + "/PythonJobService");
            host.Open();

            /* While Loop Is Used To Keep The Server Thread Executing Without Exiting */
            while (host.State.ToString().Equals("Opened"))
            {
                /* Infinite Loop */
            }
            //host.Close();
        }

        private void networkThreadFunction()
        {
            List<Client> clients = new List<Client>();
            /* REF : https://www.geeksforgeeks.org/c-infinite-loop/ */
            while (true)
            {
                /* Look For New Clients Repeatedly */
                RestClient restClient = new RestClient("http://localhost:55924/");
                RestRequest request = new RestRequest("api/clients/getclients", Method.Get);
                RestResponse response = restClient.Get(request);

                if (response.StatusCode.ToString().ToLower().Equals("ok"))
                {
                    clients = JsonConvert.DeserializeObject<List<Client>>(response.Content);
                }
                restClient.Dispose();

                if(clients != null)
                {
                    /* Check Each Client For Jobs, And Do Them If Can */
                    for (int i = 0; i < clients.Count; i++)
                    {
                        /* Ignoring Client One */
                        if (clients[i].portnumber != client_one_port_no)
                        {
                            /* Connect To Client[i] Using ChannelFactory */
                            ChannelFactory<PythonServerInterface> foobFactory;
                            NetTcpBinding tcp = new NetTcpBinding();

                            string URL = "net.tcp://" + clients[i].ipaddress + ":" + clients[i].portnumber + "/PythonJobService";
                            foobFactory = new ChannelFactory<PythonServerInterface>(tcp, URL);
                            PythonServerInterface foob = foobFactory.CreateChannel();

                            /* Download Job From Job Pool */
                            PythonJob pythonJob = foob.RequestJob();
                            if (!String.IsNullOrEmpty(pythonJob.pythonScript)) /* Check If A Job Is Available */
                            {
                                /* Display Client Job Status */

                                try
                                {
                                    client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Received Job " + pythonJob.jobNumber + "!"));
                                }
                                catch (Exception)
                                {
                                    RestClient restClient2 = new RestClient("http://localhost:55924/");
                                    RestRequest requestThree = new RestRequest("api/clients/deleteclient/{id}", Method.Delete);
                                    requestThree.AddUrlSegment("id", client_one_port_no);
                                    RestResponse responseThree = restClient2.Execute(requestThree);
                                    break;
                                }
                                Thread.Sleep(3000); /* To Show Status */

                                /* Verify Hash */
                                /* REF : https://stackoverflow.com/questions/12342714/how-to-compare-two-arrays-of-bytes */
                                var sha256 = SHA256.Create();
                                byte[] hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(pythonJob.pythonScript));

                                if (hash.SequenceEqual(pythonJob.hash))
                                {
                                    /* Decode Base64 String */
                                    if (!String.IsNullOrEmpty(pythonJob.pythonScript))
                                    {
                                        byte[] encodedBytes = Convert.FromBase64String(pythonJob.pythonScript);
                                        String pythonCode = System.Text.Encoding.UTF8.GetString(encodedBytes);

                                        /* Execute Job Using Iron Python */
                                        try
                                        {
                                            client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Working On Job " + pythonJob.jobNumber + "!"));

                                            /* REF : https://stackoverflow.com/questions/5414657/extract-substring-from-a-string */
                                            string func_name = pythonCode.Substring(pythonCode.IndexOf("def") + 4, pythonCode.IndexOf("():") - 4);

                                            ScriptEngine engine = Python.CreateEngine();
                                            ScriptScope scope = engine.CreateScope();
                                            engine.Execute(pythonCode, scope);
                                            dynamic testFunction = scope.GetVariable(func_name);
                                            var result = testFunction();

                                            /* Incrementing Completed Jobs Count In GUI */
                                            jobsTaken++;
                                            completed_jobs.Dispatcher.Invoke(new Action(() => completed_jobs.Text = jobsTaken.ToString()));

                                            /* Incrementing Completed Jobs Count In DB */
                                            Client clientOne = new Client();
                                            clientOne.jobscompleted = jobsTaken;
                                            clientOne.ipaddress = client_one_ip;
                                            clientOne.portnumber = client_one_port_no;

                                            RestClient restClient2 = new RestClient("http://localhost:55924/");
                                            RestRequest requestTwo = new RestRequest("api/clients/updateclient/{id}", Method.Put);
                                            requestTwo.AddUrlSegment("id", client_one_port_no);
                                            requestTwo.AddJsonBody(clientOne);
                                            RestResponse responseTwo = restClient2.Execute(requestTwo);

                                            /* Post Result To Requested Client */
                                            Thread.Sleep(4000);
                                            foob.PostResult(pythonJob.jobNumber, result.ToString());
                                            client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Finished Job " + pythonJob.jobNumber + "!"));
                                            restClient2.Dispose();
                                            /* TO DO */
                                            /*if (result != null)
                                            {
                                                result_display.Dispatcher.Invoke(new Action(() => result_display.Text = result.ToString()));
                                            }*/
                                            // Figure out a way to update the client who requested the job, that the job is complete
                                        }
                                        catch (Exception)
                                        {
                                            error_msg_diplay.Dispatcher.Invoke(new Action(() => error_msg_diplay.Text = "Error Occured! Inccorect Python Script!"));
                                            foob.PostResult(pythonJob.jobNumber, "Incorrect Python Script Detected");
                                        }
                                    }
                                    else
                                    {
                                        // do something (python code inside job is empty)
                                        error_msg_diplay.Dispatcher.Invoke(new Action(() => error_msg_diplay.Text = "Empty Python Script Detected!"));
                                        foob.PostResult(pythonJob.jobNumber, " Python Script Detected");
                                    }
                                }
                                else
                                {
                                    // something is wrong. Hashes are not equal
                                    MessageBox.Show("Incorrect Hashes");
                                }
                            }
                        }
                    }
                }
                else if(clients.Count == 1)
                {
                    client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Task Aborted! No Clients In Swarm!"));
                }
               
            }

        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            string error_check = UserInput.Text.Substring(UserInput.Text.IndexOf("("), 2);
            client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
            error_msg_diplay.Dispatcher.Invoke(new Action(() => error_msg_diplay.Text = ""));

            if (!error_check.Equals("()"))
            {
                MessageBox.Show("Please Make Sure That Only One Function With One Return Value Is Entered");
            }
            else
            {
                if (!String.IsNullOrEmpty(UserInput.Text))
                {
                    /* Encode UserInput.Text in Base64 */
                    byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(UserInput.Text);
                    string base64_converted_string = Convert.ToBase64String(textBytes);

                    /* Create A Hash For base64_converted_string */
                    /* REF : https://stackoverflow.com/questions/12416249/hashing-a-string-with-sha256 */
                    var sha256 = SHA256.Create();
                    byte[] hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(base64_converted_string));

                    /* Create A New Python Job And Add It To The Pool */
                    PythonJob newJob = new PythonJob();
                    newJob.pythonScript = base64_converted_string;
                    newJob.jobRequested = false;
                    newJob.hash = hash;
                    newJob.jobNumber = PythonJobPool.getAllJobs().Count + 1;

                    PythonJobPool.addJobToPool(newJob);
                    Thread.Sleep(3000); /* To Show The Status Properly, Sleep 3 Seconds */
                    string text = "Job Number : " + newJob.jobNumber + " Added To Pool";
                    client_status.Dispatcher.Invoke(new Action(() => client_status.Text = text));

                    /* Await Result */
                    /* Constantly Check If The Job Number Result Is Available */
                    int timeout = 180000;
                    /* REF : https://www.anycodings.com/1questions/3909134/cannot-convert-from-method-group-to-funcampltlistampltclassaampgtampgt-when-instantiating-a-new-task */
                    Task<string> task = new Task<string>(() => CheckForResult(newJob.jobNumber)); ;
                    task.Start();
                    client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Awaiting Result..."));
                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    {
                        string res = await task;
                        client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
                        result_display.Text = res;
                    }
                    else
                    {
                        client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
                        error_msg_diplay.Text = "Job Completion Timeout! Please Try Again!";
                    }
                }
                else
                {
                    MessageBox.Show("Input Field Is Empty! Please Try Again");
                }
            }
        }

        private async void SubmitFromFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "TXT Files (*.txt)|*.txt|PYTHON Files (*.py)|*.py";
            Nullable<bool> path = dialog.ShowDialog();
            if (path == true)
            {
                string filepath = dialog.FileName;
                string textFromFile = File.ReadAllText(filepath);

                string error_check = textFromFile.Substring(textFromFile.IndexOf("("), 2);
                client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
                error_msg_diplay.Dispatcher.Invoke(new Action(() => error_msg_diplay.Text = ""));

                if (!error_check.Equals("()"))
                {
                    MessageBox.Show("Please Make Sure That Only One Function With One Return Value Is Submitted");
                }
                else
                {
                    if (!String.IsNullOrEmpty(textFromFile))
                    {
                        /* Encode UserInput.Text in Base64 */
                        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(textFromFile);
                        string base64_converted_string = Convert.ToBase64String(textBytes);

                        /* Create A Hash For base64_converted_string */
                        /* REF : https://stackoverflow.com/questions/12416249/hashing-a-string-with-sha256 */
                        var sha256 = SHA256.Create();
                        byte[] hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(base64_converted_string));

                        /* Create A New Python Job And Add It To The Pool */
                        PythonJob newJob = new PythonJob();
                        newJob.pythonScript = base64_converted_string;
                        newJob.jobRequested = false;
                        newJob.hash = hash;
                        newJob.jobNumber = PythonJobPool.getAllJobs().Count + 1;

                        PythonJobPool.addJobToPool(newJob);
                        Thread.Sleep(3000); /* To Show The Status Properly, Sleep 3 Seconds */
                        string text = "Job Number : " + newJob.jobNumber + " Added To Pool";
                        client_status.Dispatcher.Invoke(new Action(() => client_status.Text = text));

                        /* Await Result */
                        /* Constantly Check If The Job Number Result Is Available */
                        int timeout = 180000;
                        /* REF : https://www.anycodings.com/1questions/3909134/cannot-convert-from-method-group-to-funcampltlistampltclassaampgtampgt-when-instantiating-a-new-task */
                        Task<string> task = new Task<string>(() => CheckForResult(newJob.jobNumber)); ;
                        task.Start();
                        client_status.Dispatcher.Invoke(new Action(() => client_status.Text = "Awaiting Result..."));
                        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                        {
                            string res = await task;
                            client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
                            result_display.Text = res;
                        }
                        else
                        {
                            client_status.Dispatcher.Invoke(new Action(() => client_status.Text = ""));
                            error_msg_diplay.Text = "Job Completion Timeout! Please Try Again!";
                        }
                    }
                }
            }
        }

        private string CheckForResult(int jobNumber)
        {
            string ret_result = null;
            /* Keep Looping Till The Result Is Posted By Someone */
            while(ret_result == null)
            {
                List<PythonJob> jobList = PythonJobPool.getAllJobs();
                if (jobList.Count == 0)
                {
                    ret_result = "Task Aborted!";
                    break;
                }

                for (int i = 0; i < jobList.Count; i++)
                {
                    if (jobList[i].jobNumber == jobNumber && jobList[i].result != null)
                    {
                        ret_result = jobList[i].result;
                        break;
                    }
                }
            }
            //Thread.Sleep(6000); /* Sleep For 6 seconds to show status */
            return ret_result;
        }

    }
}

/* REF FOR RETURN/TAB IN MAIN WINDOW : https://stackoverflow.com/questions/7865375/start-a-new-line-in-wpf-textbox 
 * REF : https://stackoverflow.com/questions/4999988/clear-the-contents-of-a-file 
 * REF : https://learn.microsoft.com/en-us/dotnet/api/system.servicemodel.communicationstate?view=netframework-4.8#system-servicemodel-communicationstate-opening */
