using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DDDSample
{
    public interface IBoundedContext
    {
        IEnumerable<object> Ports();
        IEnumerable<Func<CancellationToken, Task>> Tasks();
        IEnumerable<object> Projections();
        IEnumerable<IApplicationService> ApplicationServices();
        IEnumerable<object> FuncApplicationServices();
        void Build();
    }
}
