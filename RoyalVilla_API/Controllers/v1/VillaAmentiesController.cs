using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoyalVilla_API.Data;
using RoyalVilla_API.Models;
using RoyalVilla.DTO;
using System.Collections;
using Asp.Versioning;

namespace RoyalVilla_API.Controllers.v1
{
    [Route("api/v{version:apiVersion}/villa-amenitie")]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiController]
    //[Authorize(Roles = "Customer,Admin")]
    public class VillaAmentiesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public VillaAmentiesController(ApplicationDbContext db, IMapper mapper)
        {
            _db= db;
            _mapper= mapper;
        }


        [HttpGet]
        //[Authorize(Roles ="Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<VillaAmentiesDTO>>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<VillaAmentiesDTO>>>> GetVillaAmenities()
        {
            var villas = await _db.VillaAmenities.ToListAsync();
            var dtoResponseVillaAmenities = _mapper.Map<List<VillaAmentiesDTO>>(villas);
            var response = ApiResponse<IEnumerable<VillaAmentiesDTO>>.Ok(dtoResponseVillaAmenities, "Villa Amenities retrieved successfully");
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        //[AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<VillaAmentiesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<VillaAmentiesDTO>>> GetVillaAmenitiesById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return NotFound(ApiResponse<object>.NotFound("VillaAmenities ID must be greater than 0"));
                }

                var villaAmenities = await _db.VillaAmenities.FirstOrDefaultAsync(u => u.Id == id);
                if (villaAmenities == null) 
                {
                    return NotFound(ApiResponse<object>.NotFound($"VillaAmenities with ID {id} was not found"));
                }
                return Ok(ApiResponse<VillaAmentiesDTO>.Ok(_mapper.Map<VillaAmentiesDTO>(villaAmenities), "Records retrieved successfully"));
            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<VillaAmentiesDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaAmentiesDTO>>> CreateVillaAmenities(VillaAmentiesCreateDTO villaAmentiesDTO)
        {
            try
            {
                if (villaAmentiesDTO == null)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa Amenities data is required"));
                }


                var villaExists = await _db.Villa.FirstOrDefaultAsync(u => u.Id== villaAmentiesDTO.VillaId);

                if (villaExists == null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"Villa with the ID '{villaAmentiesDTO.VillaId}'does not exist."));
                }

                VillaAmenities villaAmenities = _mapper.Map<VillaAmenities>(villaAmentiesDTO);
                villaAmenities.CreatedDate = DateTime.Now;
                await _db.VillaAmenities.AddAsync(villaAmenities);
                await _db.SaveChangesAsync();

                var response = ApiResponse<VillaAmentiesDTO>.CreatedAt(_mapper.Map<VillaAmentiesDTO>(villaAmenities), "Villa Amenities created successfully");
                return CreatedAtAction(nameof(CreateVillaAmenities), new {id=villaAmenities.Id},response);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa amenities:", ex.Message);
                return StatusCode(500,errorResponse);
            }
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<VillaAmentiesDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<VillaAmentiesDTO>>> UpdateVillaAmenities(int id, VillaAmentiesUpdateDTO villaAmentiesDTO)
        {
            try
            {
                if (villaAmentiesDTO == null)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa Amenities data is required"));
                }

                if (id != villaAmentiesDTO.Id)
                {
                    return BadRequest(ApiResponse<object>.BadRequest("Villa Amenities ID in URL does not match VillaAmenities ID in request body"));
                }

                var villaExists = await _db.Villa.FirstOrDefaultAsync(u => u.Id == villaAmentiesDTO.VillaId);

                if (villaExists == null)
                {
                    return Conflict(ApiResponse<object>.Conflict($"Villa with the ID '{villaAmentiesDTO.VillaId}'does not exist."));
                }

                var existingVillaAmenities = await _db.VillaAmenities.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVillaAmenities == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa Amenities with ID {id} was not found"));
                }

              

                _mapper.Map(villaAmentiesDTO, existingVillaAmenities);
                existingVillaAmenities.UpdatedDate = DateTime.Now;
                
                await _db.SaveChangesAsync();
                var response = ApiResponse<VillaAmentiesDTO>.Ok(_mapper.Map<VillaAmentiesDTO>(existingVillaAmenities), "Villa Amenities updated successfully");
                return Ok(response);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa amenities:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }


        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteVillaAmenities(int id)
        {
            try
            {
                var existingVillaAmenities = await _db.VillaAmenities.FirstOrDefaultAsync(u => u.Id == id);

                if (existingVillaAmenities == null)
                {
                    return NotFound(ApiResponse<object>.NotFound($"Villa Amenities with ID {id} was not found"));
                }

                _db.VillaAmenities.Remove(existingVillaAmenities);
                await _db.SaveChangesAsync();

                var response = ApiResponse<object>.NoContent("Villa Amenities deleted successfully");
                return Ok(response);

            }
            catch (Exception ex)
            {
                var errorResponse = ApiResponse<object>.Error(500, "An error occurred while creating the villa amenities:", ex.Message);
                return StatusCode(500, errorResponse);
            }
        }

    }
}
