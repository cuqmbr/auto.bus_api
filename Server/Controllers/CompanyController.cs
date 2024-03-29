using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Server.Services;
using SharedModels.DataTransferObjects.Model;
using SharedModels.QueryParameters.Objects;

namespace Server.Controllers;

[Authorize]
[Route("api/companies")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly ICompanyManagementService _companyManagementService;
    
    public CompanyController(ICompanyManagementService companyManagementService)
    {
        _companyManagementService = companyManagementService;
    }

    [Authorize(Policy = "AdministratorAccess")]
    [HttpPost]
    public async Task<IActionResult> AddCompany(CreateCompanyDto company)
    {
        var result = await _companyManagementService.AddCompany(company);
    
        if (!result.isSucceed)
        {
            return result.actionResult;
        }
    
        return CreatedAtAction(nameof(GetCompany), new {id = result.company.Id}, result.company);
    }

    [Authorize(Policy = "AdministratorAccess")]
    [HttpGet]
    public async Task<IActionResult> GetCompanies([FromQuery] CompanyParameters parameters)
    {
        var result = await _companyManagementService.GetCompanies(parameters);

        if (!result.isSucceed)
        {
            return result.actionResult;
        }
        
        Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(result.pagingMetadata));
        
        return Ok(result.companies);
    }
    
    [Authorize(Policy = "AdministratorAccess")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompany(int id, [FromQuery] string? fields)
    {
        var result = await _companyManagementService.GetCompany(id, fields);

        if (!result.isSucceed)
        {
            return result.actionResult;
        }

        return Ok(result.company);
    }

    [Authorize(Policy = "AdministratorAccess")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, UpdateCompanyDto company)
    {
        var result = await _companyManagementService.UpdateCompany(company);
    
        if (!result.isSucceed)
        {
            return result.actionResult;
        }
    
        return Ok(result.company);
    }
    
    [Authorize(Policy = "AdministratorAccess")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var result = await _companyManagementService.DeleteCompany(id);
        
        if (!result.isSucceed)
        {
            return result.actionResult;
        }
    
        return NoContent();
    }
}