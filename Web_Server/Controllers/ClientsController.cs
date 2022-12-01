using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Web_Server.Models;

namespace Web_Server.Controllers
{
    [RoutePrefix("api/clients")]
    public class ClientsController : ApiController
    {
        private clientdbEntities db = new clientdbEntities();


        [Route("getclients")]
        [HttpGet]
        public IHttpActionResult GetClients()
        {
            List<Client> clientList = new List<Client>();
            foreach(Client c in db.Clients)
            {
                clientList.Add(c);
            }

            if(clientList.Count != 0)
            {
                return Ok(clientList);
            }
            else
            {
                return BadRequest();
            }
        }


        [Route("getcount")]
        [HttpGet]
        public IHttpActionResult getcount()
        {
            int count = db.Clients.Count<Client>();
            return Ok(count);
        }


        [Route("updateclient/{id}")]
        [Route("updateclient")]
        [HttpPut]
        public IHttpActionResult UpdateClient(int id, Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != client.portnumber)
            {
                return BadRequest();
            }

            db.Entry(client).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok("Updated");
        }


        
        [HttpPost]
        [Route("registerclient")]
        public IHttpActionResult RegisterClient(Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Clients.Add(client);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (ClientExists(client.portnumber))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = client.portnumber }, client);
        }



        [HttpGet]
        [Route("getclient/{id}")]
        [Route("getclient")]
        public IHttpActionResult GetClient(int id)
        {
            Client client = db.Clients.Find(id);
            if (client == null)
            {
                return NotFound();
            }

            return Ok(client);
        }



        [HttpDelete]
        [Route("deleteclient/{id}")]
        [Route("deleteclient")]
        public IHttpActionResult DeleteClient(int id)
        {
            Client client = db.Clients.Find(id);
            if (client == null)
            {
                return NotFound();
            }

            db.Clients.Remove(client);
            db.SaveChanges();

            return Ok(client);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        

        private bool ClientExists(int id)
        {
            return db.Clients.Count(e => e.portnumber == id) > 0;
        }
    }
}