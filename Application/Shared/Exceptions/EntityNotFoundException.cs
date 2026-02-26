using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Domain.Entities;

namespace Application.Shared.Exceptions;

public class EntityNotFoundException: Exception
{
    public EntityNotFoundException(string message) : base(message) { }
    
    public EntityNotFoundException(string name, object key) 
        : base($"Entity \"{name}\" with key ({key}) was not found.") { }
}
