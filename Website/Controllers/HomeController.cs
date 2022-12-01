using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using Website.Models;

namespace Website.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Home";
            RestClient restClient = new RestClient("http://localhost:55924/");
            RestRequest request = new RestRequest("api/clients/getclients", Method.Get);
            RestResponse response = restClient.Get(request);
            List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(response.Content);
            return View(clients);
        }

    }
}
