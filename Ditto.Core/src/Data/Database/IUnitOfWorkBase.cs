using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ditto.Data.Database
{
    public partial interface IUnitOfWorkBase : IDisposable
    {
        // Commit all the changes 
        int Complete();
        Task<int> CompleteAsync(CancellationToken cancellationToken);
    }
}
