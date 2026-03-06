using MediatR;
using Skolio.Administration.Application.Contracts;

namespace Skolio.Administration.Application.SystemSettings;

public sealed record ChangeSystemSettingCommand(string Key, string Value, bool IsSensitive) : IRequest<SystemSettingContract>;
