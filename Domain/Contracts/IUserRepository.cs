using Domain.Entities;

namespace Domain.Contracts;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
