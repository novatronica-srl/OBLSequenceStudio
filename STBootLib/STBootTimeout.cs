#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    static class STBootTimeout
    {

        /* used for cancelling */
        public static async Task<T> WithTimeout<T>(this Task<T> task, int timeout)
        {
            /* cancellation token source with timeout */
            var cts = new CancellationTokenSource(timeout);
            /* task completition source */
            var tcs = new TaskCompletionSource<bool>();

            using (cts.Token.Register(s => 
                ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs)) {
                /* timeout occured? or task finished normally? */
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException();
            }

            return await task;
        }
    }
}


