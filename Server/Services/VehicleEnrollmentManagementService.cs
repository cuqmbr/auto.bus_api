using System.Dynamic;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Helpers;
using Server.Models;
using SharedModels.DataTransferObjects.Model;
using SharedModels.QueryParameters;
using SharedModels.QueryParameters.Objects;
using Utils;

namespace Server.Services;

public class VehicleEnrollmentManagementService : IVehicleEnrollmentManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ISortHelper<ExpandoObject> _enrollmentSortHelper;
    private readonly IDataShaper<VehicleEnrollmentDto> _enrollmentDataShaper;
    private readonly IPager<ExpandoObject> _pager;
    private readonly ISessionUserService _sessionUserService;

    public VehicleEnrollmentManagementService(ApplicationDbContext dbContext, IMapper mapper,
        ISortHelper<ExpandoObject> enrollmentSortHelper, IDataShaper<VehicleEnrollmentDto> enrollmentDataShaper,
        IPager<ExpandoObject> pager, ISessionUserService sessionUserService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _enrollmentSortHelper = enrollmentSortHelper;
        _enrollmentDataShaper = enrollmentDataShaper;
        _pager = pager;
        _sessionUserService = sessionUserService;
    }

    public async Task<(bool isSucceed, IActionResult actionResult, VehicleEnrollmentDto enrollment)>
        AddEnrollment(CreateVehicleEnrollmentDto createEnrollmentDto)
    {
        if (_sessionUserService.GetAuthUserRole() != Identity.Roles.Administrator.ToString())
        {
            if (!(await _sessionUserService.IsAuthUserCompanyOwner()).isCompanyOwner &&
                !await _sessionUserService.IsAuthUserCompanyVehicle(createEnrollmentDto.VehicleId))
            {
                return (false, new UnauthorizedResult(), null!);
            }
        }
        
        var enrollment = _mapper.Map<VehicleEnrollment>(createEnrollmentDto);
    
        await _dbContext.VehicleEnrollments.AddAsync(enrollment);
        await _dbContext.SaveChangesAsync();

        enrollment = await _dbContext.VehicleEnrollments
            .Include(ve => ve.RouteAddressDetails)
            .FirstAsync(ve => ve.Id == enrollment.Id);
    
        return (true, null!, _mapper.Map<VehicleEnrollmentDto>(enrollment));
    }

    public async Task<(bool isSucceed, IActionResult actionResult, IEnumerable<ExpandoObject> enrollments, PagingMetadata<ExpandoObject> pagingMetadata)>
        GetEnrollments(VehicleEnrollmentParameters parameters)
    {
        var dbEnrollments = _dbContext.VehicleEnrollments
            .Include(ve => ve.RouteAddressDetails)
            .AsQueryable();
        
        if (_sessionUserService.GetAuthUserRole() != Identity.Roles.Administrator.ToString())
        {
            var result = await _sessionUserService.IsAuthUserCompanyOwner();
            if (!result.isCompanyOwner)
            {
                return (false, new UnauthorizedResult(), null!, null!);
            }
            
            dbEnrollments = dbEnrollments.Include(e => e.Vehicle)
                .Where(e => e.Vehicle.CompanyId == result.companyId);
        }

        SearchByAllEnrollmentFields(ref dbEnrollments, parameters.Search);
        FilterByEnrollmentVehicleId(ref dbEnrollments, parameters.VehicleId);
        FilterByEnrollmentRouteId(ref dbEnrollments, parameters.RouteId);
        FilterByEnrollmentDepartureDateTime(ref dbEnrollments, parameters.FromDepartureDateTime, parameters.ToDepartureDateTime);
        FilterByEnrollmentCancelledValue(ref dbEnrollments, parameters.IsCancelled);
        FilterByEnrollmentTotalDuration(ref dbEnrollments, parameters.FromTotalTripDuration, parameters.ToTotalTripDuration);
        FilterByEnrollmentTotalCost(ref dbEnrollments, parameters.FromCost, parameters.ToCost);

        var enrollmentDtos = _mapper.ProjectTo<VehicleEnrollmentDto>(dbEnrollments);
        
        var shapedData = _enrollmentDataShaper.ShapeData(enrollmentDtos, parameters.Fields).AsQueryable();
        
        try
        {
            shapedData = _enrollmentSortHelper.ApplySort(shapedData, parameters.Sort);
        }
        catch (Exception)
        {
            return (false, new BadRequestObjectResult("Invalid sorting string"), null!, null!);
        }
        
        var pagingMetadata = _pager.ApplyPaging(ref shapedData, parameters.PageNumber,
            parameters.PageSize);
        
        return (true, null!, shapedData, pagingMetadata);

        void SearchByAllEnrollmentFields(ref IQueryable<VehicleEnrollment> enrollment, string? search)
        {
            if (!enrollment.Any() || String.IsNullOrWhiteSpace(search))
            {
                return;
            }

            enrollment = enrollment.Where(e =>
                e.CancellationComment != null && e.CancellationComment.ToLower().Contains(search.ToLower()));
        }
        
        void FilterByEnrollmentVehicleId(ref IQueryable<VehicleEnrollment> enrollments, int? vehicleId)
        {
            if (!enrollments.Any() || vehicleId == null)
            {
                return;
            }

            enrollments = enrollments.Where(e => e.VehicleId == vehicleId);
        }
        
        void FilterByEnrollmentRouteId(ref IQueryable<VehicleEnrollment> enrollments, int? routeId)
        {
            if (!enrollments.Any() || routeId == null)
            {
                return;
            }

            enrollments = enrollments.Where(e => e.RouteId == routeId);
        }
        
        void FilterByEnrollmentDepartureDateTime(ref IQueryable<VehicleEnrollment> enrollments, DateTime? fromDateTime, DateTime? toDateTime)
        {
            if (!enrollments.Any() || fromDateTime == null || toDateTime == null)
            {
                return;
            }

            enrollments = enrollments.Where(e =>
                e.DepartureDateTimeUtc >= fromDateTime.Value.ToUniversalTime() &&
                e.DepartureDateTimeUtc <= toDateTime.Value.ToUniversalTime());
        }
        
        void FilterByEnrollmentCancelledValue(ref IQueryable<VehicleEnrollment> enrollments, bool? isCancelled)
        {
            if (!enrollments.Any() || !isCancelled.HasValue)
            {
                return;
            }

            enrollments = enrollments.Where(e => (bool) isCancelled ? e.CancellationComment != null : e.CancellationComment == null);
        }

        void FilterByEnrollmentTotalDuration(ref IQueryable<VehicleEnrollment> enrollments, TimeSpan? fromDuration, TimeSpan? toDuration)
        {
            if (!enrollments.Any() )
            {
                return;
            }

            fromDuration ??= TimeSpan.Zero;
            toDuration ??= TimeSpan.MaxValue;

            List<int> filteredEnrollmentsIds = new List<int>();
            foreach (var enrollment in enrollments)
            {
                TimeSpan duration = TimeSpan.Zero;
                foreach (var details in enrollment.RouteAddressDetails)
                {
                    duration += details.WaitTimeSpan + details.TimeSpanToNextCity;
                }

                if (duration >= fromDuration && duration <= toDuration)
                {
                    filteredEnrollmentsIds.Add(enrollment.Id);
                }
            }
            
            enrollments = enrollments.Where(e =>
                filteredEnrollmentsIds.Any(id => id == e.Id));
        }
        
        void FilterByEnrollmentTotalCost(ref IQueryable<VehicleEnrollment> enrollments, double? fromCost, double? toCost)
        {
            if (!enrollments.Any() )
            {
                return;
            }

            fromCost ??= 0;
            toCost ??= Double.MaxValue;

            List<int> filteredEnrollmentsIds = new List<int>();
            foreach (var enrollment in enrollments)
            {
                double cost = 0;
                foreach (var details in enrollment.RouteAddressDetails)
                {
                    cost += details.CostToNextCity;
                }

                if (cost >= fromCost && cost <= toCost)
                {
                    filteredEnrollmentsIds.Add(enrollment.Id);
                }
            }
            
            enrollments = enrollments.Where(e =>
                filteredEnrollmentsIds.Any(id => id == e.Id));
        }
    }

    public async Task<(bool isSucceed, IActionResult actionResult, ExpandoObject enrollment)>
        GetEnrollment(int id, string? fields)
    {
        if (!await IsEnrollmentExists(id))
        {
            return (false, new NotFoundResult(), null!);
        }
        
        if (_sessionUserService.GetAuthUserRole() != Identity.Roles.Administrator.ToString())
        {
            if (!await _sessionUserService.IsAuthUserCompanyVehicleEnrollment(id))
            {
                return (false, new UnauthorizedResult(), null!);
            }
        }

        var dbEnrollment = await _dbContext.VehicleEnrollments
            .Include(ve => ve.RouteAddressDetails)
            .FirstAsync(e => e.Id == id);

        if (String.IsNullOrWhiteSpace(fields))
        {
            fields = VehicleEnrollmentParameters.DefaultFields;
        }

        var enrollmentDto = _mapper.Map<VehicleEnrollmentDto>(dbEnrollment);
        var shapedData = _enrollmentDataShaper.ShapeData(enrollmentDto, fields);

        return (true, null!, shapedData);
    }

    public async Task<(bool isSucceed, IActionResult actionResult, VehicleEnrollmentDto enrollment)>
        UpdateEnrollment(int vehicleEnrollmentId, UpdateVehicleEnrollmentDto updateEnrollmentDto)
    {
        if (vehicleEnrollmentId != updateEnrollmentDto.Id)
        {
            return (false, new BadRequestObjectResult("Query id and object id must match"), null!);
        }
        
        var enrollment = _mapper.Map<VehicleEnrollment>(updateEnrollmentDto);
        _dbContext.VehicleEnrollments.Update(enrollment);
        
        if (_sessionUserService.GetAuthUserRole() != Identity.Roles.Administrator.ToString())
        {
            if (!(await _sessionUserService.IsAuthUserCompanyOwner()).isCompanyOwner &&
                !await _sessionUserService.IsAuthUserCompanyVehicle(updateEnrollmentDto.VehicleId))
            {
                return (false, new UnauthorizedResult(), null!);
            }
        }
        
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await IsEnrollmentExists(updateEnrollmentDto.Id))
            {
                return (false, new NotFoundResult(), null!);
            }
            
            throw;
        }

        var dbEnrollment = await _dbContext.VehicleEnrollments.FirstAsync(e => e.Id == enrollment.Id);
        
        return (true, null!, _mapper.Map<VehicleEnrollmentDto>(dbEnrollment));
    }

    public async Task<(bool isSucceed, IActionResult actionResult)> DeleteEnrollment(int id)
    {
        if (_sessionUserService.GetAuthUserRole() != Identity.Roles.Administrator.ToString())
        {
            if (!await _sessionUserService.IsAuthUserCompanyVehicleEnrollment(id))
            {
                return (false, new UnauthorizedResult());
            }
        }
        
        if (!await IsEnrollmentExists(id))
        {
            return (false, new NotFoundResult());
        }
    
        var dbEnrollment = await _dbContext.VehicleEnrollments.FirstAsync(e => e.Id == id);
    
        _dbContext.VehicleEnrollments.Remove(dbEnrollment);
        await _dbContext.SaveChangesAsync();
    
        return (true, null!);
    }

    private async Task<bool> IsEnrollmentExists(int id)
    {
        return await _dbContext.VehicleEnrollments.AnyAsync(e => e.Id == id);
    } 
}