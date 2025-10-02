using Microsoft.AspNetCore.Mvc;

namespace RoyalVilla_API.Controllers
{
    [Route("api/villa")]
    [ApiController]
    public class VillaController : ControllerBase
    {
        [HttpGet]
        public string GetVillas()
        {
            return "Get all Villas";
        }

        [HttpGet("{id:int}")]
        public string GetVillaById(int id)
        {
            return "Get Villa: "+ id;
        }
    }
}
