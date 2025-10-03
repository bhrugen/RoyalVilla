using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
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
        private readonly IMapper _mapper;

        public VillaController(ApplicationDbContext db, IMapper mapper)
        {
            _db= db;
            _mapper= mapper;
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

                Villa villa = _mapper.Map<Villa>(villaDTO);

                await _db.Villa.AddAsync(villa);
                await _db.SaveChangesAsync();
                
                return CreatedAtAction(nameof(CreateVilla), new {id=villa.Id},villa);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred while creating the villa: {ex.Message}");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Villa>> UpdateVilla(int id, VillaUpdateDTO villaDTO)
        {
            try
            {
                if (villaDTO == null)
                {
                    return BadRequest("Villa data is required");
                }

                if (id != villaDTO.Id)
                {
                    return BadRequest("Villa ID in URL does not match Villa ID in request body");
                }
                

                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVilla ==null)
                {
                    return NotFound($"Villa with ID {id} was not found");
                }

                var duplicateVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Name.ToLower() == villaDTO.Name.ToLower()
                && u.Id != id);

                if (duplicateVilla != null)
                {
                    return Conflict($"A villa with the name '{villaDTO.Name}' already exists");
                }

                _mapper.Map(villaDTO,existingVilla);
                existingVilla.UpdatedDate = DateTime.Now;
                
                await _db.SaveChangesAsync();

                return Ok(villaDTO);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred while updating the villa: {ex.Message}");
            }
        }


        [HttpDelete("{id:int}")]
        public async Task<ActionResult<Villa>> DeleteVilla(int id)
        {
            try
            {
                var existingVilla = await _db.Villa.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVilla == null)
                {
                    return NotFound($"Villa with ID {id} was not found");
                }

                _db.Villa.Remove(existingVilla);
                await _db.SaveChangesAsync();

                return NoContent();

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"An error occurred while deleting the villa: {ex.Message}");
            }
        }

    }
}
