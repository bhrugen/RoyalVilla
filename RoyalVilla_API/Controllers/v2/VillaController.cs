using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using RoyalVilla.DTO;
using System.Collections;

namespace RoyalVilla_API.Controllers.v2
{
    [Route("v2/api/villa")]
    [ApiController]
    //[Authorize(Roles = "Customer,Admin")]
    public class VillaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public VillaController(ApplicationDbContext db, IMapper mapper)
        {
            _db= db;
            _mapper= mapper;
        }


        [HttpGet]
        //[Authorize(Roles ="Admin")]
        public async Task<ActionResult<String>> GetVillas()
        {
            return "This is V2";
        }

        [HttpGet("{id:int}")]
        //[AllowAnonymous]
        public async Task<ActionResult<String>> GetVillaById(int id)
        {
           return "This is V2 for ID "+id;
        }

       

    }
}
