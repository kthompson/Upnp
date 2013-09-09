using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Upnp.Extensions
{
    public static class Threading
    {
        public static Task HandleExceptions(this Task task, Action<Exception> onException = null)
        {
            return task.ContinueWith(t =>
            {
                var exception = t.Exception.Flatten();
                onException = onException ?? (exc => Trace.WriteLine(exc, "Task.HandleExceptions"));
                foreach (var ex in exception.InnerExceptions)
                    onException(ex);
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
