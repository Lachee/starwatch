using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Starwatch.Extensions
{
    public static class TaskExtensions
    {
        public static Task CallAsyncWithLog (this Task task, Logging.Logger logger, string format = null)
        {
            return task.ContinueWith((t) =>
            {
                if (!(t.Exception is null))
                {
                    if (format is null)
                        logger.LogError(t.Exception);

                    else
                        logger.LogError(t.Exception, format);

                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
