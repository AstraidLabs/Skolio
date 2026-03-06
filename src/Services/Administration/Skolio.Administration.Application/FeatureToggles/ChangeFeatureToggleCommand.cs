using MediatR;
using Skolio.Administration.Application.Contracts;

namespace Skolio.Administration.Application.FeatureToggles;

public sealed record ChangeFeatureToggleCommand(string FeatureCode, bool IsEnabled) : IRequest<FeatureToggleContract>;
