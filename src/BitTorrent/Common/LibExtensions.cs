#if IS_CORECLR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Synchronous;
using System.Threading.Tasks;

namespace System.Net.BitTorrent
{
    public static class LibExtensions
    {
        public static IAsyncResult BeginRead(this Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = stream.ReadAsync(buffer, offset, count);
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static int EndRead(this Stream stream, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<int>)asyncResult).WaitAndUnwrapException();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
        public static IAsyncResult BeginConnect(this Socket socket, EndPoint endpoint, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource(state);
            var task = socket.ConnectAsync(endpoint);
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult();

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static void EndConnect(this Socket socket, IAsyncResult asyncResult)
        {
            try
            {
                ((Task)asyncResult).WaitAndUnwrapException();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public static IAsyncResult BeginReceive(this Socket socket, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static int EndReceive(this Socket socket, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<int>)asyncResult).WaitAndUnwrapException();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public static IAsyncResult BeginSend(this Socket socket, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = socket.SendAsync(new ArraySegment<byte>(buffer, offset, count), SocketFlags.None);
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static int EndSend(this Socket socket, IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<int>)asyncResult).WaitAndUnwrapException();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
        public static IAsyncResult BeginReceive(this UdpClient udpClient, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<UdpReceiveResult>(state);
            var task = udpClient.ReceiveAsync();
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static byte[] EndReceive(this UdpClient udpClient, IAsyncResult asyncResult, ref IPEndPoint remoteEP)
        {
            try
            {
                var result= ((Task<UdpReceiveResult>)asyncResult).WaitAndUnwrapException();
                remoteEP = result.RemoteEndPoint;
                return result.Buffer;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public static IAsyncResult BeginAccept(this Socket udpClient,AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<Socket>(state);
            var task = udpClient.AcceptAsync();
            task.ContinueWith(t =>
            {
                // Copy the task result into the returned task.
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                // Invoke the user callback if necessary.
                if (callback != null)
                    callback(tcs.Task);
            });
            return tcs.Task;
        }

        public static Socket EndAccept(this Socket udpClient,IAsyncResult asyncResult)
        {
            try
            {
                var result = ((Task<Socket>)asyncResult).WaitAndUnwrapException();
                return result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        public static IEnumerable<T> ConvertAll<F, T>(this IEnumerable<F> collection, Func<F, T> converter)
        {
            foreach (var item in collection)
            {
                yield return converter(item);
            }
        }

        public static void Close(this UdpClient client)
        {
            client.Dispose();
        }
    }
}
#endif
