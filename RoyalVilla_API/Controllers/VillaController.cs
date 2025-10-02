using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using RoyalVilla_API.Models.DTO;
using System.Collections;

namespace RoyalVilla_API.Controllers
{
    [Route("api/villa")]
    [ApiController]
    public class VillaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public VillaController(ApplicationDbContext db)
        {
            _db= db;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Villa>>> GetVillas()
        {
            return Ok(await _db.Villa.ToListAsync());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Villa>> GetVillaById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Villa ID must be greater than 0");
                }

                var villa = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);
                if (villa == null) 
                {
                    return NotFound($"Villa with ID {id} was not found");
                }
                return Ok(villa);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred while retrieving villa with ID {id}: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Villa>> CreateVilla(VillaCreateDTO villaDTO)
        {
            try
            {
                if (villaDTO == null)
                {
                    return BadRequest("Villa data is required");
                }

                Villa villa = new()
                {
                    Name = villaDTO.Name,
                    Details = villaDTO.Details,
                    ImageUrl = villaDTO.ImageUrl,
                    Occupancy = villaDTO.Occupancy,
                    Sqft = villaDTO.Sqft,
                    Rate = villaDTO.Rate,
                    CreatedDate = DateTime.Now
                };

                await _db.Villa.AddAsync(villa);
                await _db.SaveChangesAsync();
                
                return Ok(villaDTO);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred while creating the villa: {ex.Message}");
            }
        }


    }
}
