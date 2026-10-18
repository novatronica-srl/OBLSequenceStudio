#nullable disable
namespace OBLSequenceStudio.STBootLib
{
    /// <summary>Provides a wait timeout for bootloader-related tasks without cancelling the underlying operation.</summary>
    static class STBootTimeout
    {

        /* used for cancelling */
        /// <summary>Waits for task completion or throws cancellation when the timeout elapses.</summary>
        /// <param name="task">Operation whose result is awaited; it may continue after the wait times out.</param>
        /// <param name="timeout">Wait timeout in milliseconds.</param>
        /// <returns>The completed task result.</returns>
        /// <exception cref="OperationCanceledException">The timeout completes before the operation.</exception>
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


