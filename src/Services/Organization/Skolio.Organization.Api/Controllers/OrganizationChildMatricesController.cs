using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.ServiceDefaults.Authorization;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/child-matrices")]
public sealed class OrganizationChildMatricesController(OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet("school-structure")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetSchoolStructureMatrices(CancellationToken cancellationToken)
    {
        var entries = await dbContext.OrganizationSchoolStructureMatrixEntries
            .Include(x => x.ParentScopeMatrix)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(e => new
        {
            e.Id,
            e.ParentScopeMatrixId,
            ParentSchoolType = e.ParentScopeMatrix.SchoolType.ToString(),
            e.Code,
            e.TranslationKey,
            e.UsesGradeLevels,
            e.UsesClasses,
            e.UsesGroups,
            e.GroupIsPrimaryStructure
        }));
    }

    [HttpGet("registry")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetRegistryMatrices(CancellationToken cancellationToken)
    {
        var entries = await dbContext.OrganizationRegistryMatrixEntries
            .Include(x => x.ParentScopeMatrix)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(e => new
        {
            e.Id,
            e.ParentScopeMatrixId,
            ParentSchoolType = e.ParentScopeMatrix.SchoolType.ToString(),
            e.Code,
            e.TranslationKey,
            e.RequiresIzo,
            e.RequiresRedIzo,
            e.RequiresIco,
            e.RequiresDataBox,
            e.RequiresFounder,
            e.RequiresTeachingLanguage
        }));
    }

    [HttpGet("capacity")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetCapacityMatrices(CancellationToken cancellationToken)
    {
        var entries = await dbContext.OrganizationCapacityMatrixEntries
            .Include(x => x.ParentScopeMatrix)
            .OrderBy(x => x.ParentScopeMatrixId).ThenBy(x => x.CapacityType)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(e => new
        {
            e.Id,
            e.ParentScopeMatrixId,
            ParentSchoolType = e.ParentScopeMatrix.SchoolType.ToString(),
            e.Code,
            e.TranslationKey,
            CapacityType = e.CapacityType.ToString(),
            e.IsRequired
        }));
    }

    [HttpGet("academic-structure")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetAcademicStructureMatrices(CancellationToken cancellationToken)
    {
        var entries = await dbContext.OrganizationAcademicStructureMatrixEntries
            .Include(x => x.ParentScopeMatrix)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(e => new
        {
            e.Id,
            e.ParentScopeMatrixId,
            ParentSchoolType = e.ParentScopeMatrix.SchoolType.ToString(),
            e.Code,
            e.TranslationKey,
            e.UsesSubjects,
            e.UsesFieldOfStudy,
            e.SubjectIsClassBound,
            e.FieldOfStudyIsRequired
        }));
    }

    [HttpGet("assignment")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetAssignmentMatrices(CancellationToken cancellationToken)
    {
        var entries = await dbContext.OrganizationAssignmentMatrixEntries
            .Include(x => x.ParentScopeMatrix)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return Ok(entries.Select(e => new
        {
            e.Id,
            e.ParentScopeMatrixId,
            ParentSchoolType = e.ParentScopeMatrix.SchoolType.ToString(),
            e.Code,
            e.TranslationKey,
            e.AllowsClassRoomAssignment,
            e.AllowsGroupAssignment,
            e.AllowsSubjectAssignment,
            e.StudentRequiresClassPlacement,
            e.StudentRequiresGroupPlacement
        }));
    }

    [HttpGet("by-school-type/{schoolType}")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> GetAllBySchoolType(SchoolType schoolType, CancellationToken cancellationToken)
    {
        var matrix = await dbContext.SchoolContextScopeMatrices
            .FirstOrDefaultAsync(x => x.SchoolType == schoolType, cancellationToken);

        if (matrix is null) return NotFound();

        var structure = await dbContext.OrganizationSchoolStructureMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrix.Id, cancellationToken);
        var registry = await dbContext.OrganizationRegistryMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrix.Id, cancellationToken);
        var capacityEntries = await dbContext.OrganizationCapacityMatrixEntries
            .Where(x => x.ParentScopeMatrixId == matrix.Id)
            .ToListAsync(cancellationToken);
        var academic = await dbContext.OrganizationAcademicStructureMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrix.Id, cancellationToken);
        var assignment = await dbContext.OrganizationAssignmentMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrix.Id, cancellationToken);

        return Ok(new
        {
            MatrixId = matrix.Id,
            SchoolType = matrix.SchoolType.ToString(),
            matrix.Code,
            Structure = structure is not null ? new { structure.UsesGradeLevels, structure.UsesClasses, structure.UsesGroups, structure.GroupIsPrimaryStructure } : null,
            Registry = registry is not null ? new { registry.RequiresIzo, registry.RequiresRedIzo, registry.RequiresIco, registry.RequiresDataBox, registry.RequiresFounder, registry.RequiresTeachingLanguage } : null,
            Capacity = capacityEntries.Select(c => new { CapacityType = c.CapacityType.ToString(), c.IsRequired }),
            Academic = academic is not null ? new { academic.UsesSubjects, academic.UsesFieldOfStudy, academic.SubjectIsClassBound, academic.FieldOfStudyIsRequired } : null,
            Assignment = assignment is not null ? new { assignment.AllowsClassRoomAssignment, assignment.AllowsGroupAssignment, assignment.AllowsSubjectAssignment, assignment.StudentRequiresClassPlacement, assignment.StudentRequiresGroupPlacement } : null
        });
    }
}
