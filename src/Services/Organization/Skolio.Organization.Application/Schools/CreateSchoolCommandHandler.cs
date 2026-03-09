using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Application.Schools;

public sealed class CreateSchoolCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateSchoolCommand, SchoolContract>
{
    public async Task<SchoolContract> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        var schoolOperator = SchoolOperator.Create(
            Guid.NewGuid(),
            request.SchoolOperator.LegalEntityName,
            request.SchoolOperator.LegalForm,
            request.SchoolOperator.CompanyNumberIco,
            Address.Create(
                request.SchoolOperator.RegisteredOfficeAddress.Street,
                request.SchoolOperator.RegisteredOfficeAddress.City,
                request.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                request.SchoolOperator.RegisteredOfficeAddress.Country),
            request.SchoolOperator.ResortIdentifier,
            request.SchoolOperator.DirectorSummary,
            request.SchoolOperator.StatutoryBodySummary);

        var founder = Founder.Create(
            Guid.NewGuid(),
            request.Founder.FounderType,
            request.Founder.FounderCategory,
            request.Founder.FounderName,
            request.Founder.FounderLegalForm,
            request.Founder.FounderIco,
            Address.Create(
                request.Founder.FounderAddress.Street,
                request.Founder.FounderAddress.City,
                request.Founder.FounderAddress.PostalCode,
                request.Founder.FounderAddress.Country),
            request.Founder.FounderEmail);

        var school = School.Create(
            Guid.NewGuid(),
            request.Name,
            request.SchoolType,
            request.SchoolKind,
            request.SchoolIzo,
            request.SchoolEmail,
            request.SchoolPhone,
            request.SchoolWebsite,
            Address.Create(
                request.MainAddress.Street,
                request.MainAddress.City,
                request.MainAddress.PostalCode,
                request.MainAddress.Country),
            request.EducationLocationsSummary,
            request.RegistryEntryDate,
            request.EducationStartDate,
            request.MaxStudentCapacity,
            request.TeachingLanguage,
            schoolOperator.Id,
            founder.Id,
            request.PlatformStatus);

        await commandStore.AddSchoolOperatorAsync(schoolOperator, cancellationToken);
        await commandStore.AddFounderAsync(founder, cancellationToken);
        await commandStore.AddSchoolAsync(school, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);

        return new SchoolContract(
            school.Id,
            school.Name,
            school.SchoolType,
            school.SchoolKind,
            school.SchoolIzo,
            school.SchoolEmail,
            school.SchoolPhone,
            school.SchoolWebsite,
            new AddressContract(
                school.MainAddress.Street,
                school.MainAddress.City,
                school.MainAddress.PostalCode,
                school.MainAddress.Country),
            school.EducationLocationsSummary,
            school.RegistryEntryDate,
            school.EducationStartDate,
            school.MaxStudentCapacity,
            school.TeachingLanguage,
            school.SchoolOperatorId,
            school.FounderId,
            school.PlatformStatus,
            school.IsActive,
            school.SchoolAdministratorUserProfileId,
            schoolOperator.Adapt<SchoolOperatorContract>(mapsterConfig),
            founder.Adapt<FounderContract>(mapsterConfig));
    }
}
