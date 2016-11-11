using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Net.BitTorrent.Client;
using System.Threading;

namespace System.Net.BitTorrent.Client
{

    public class MainLoopFixture : IDisposable
    {
        public MainLoop Loop { get; private set; }

        public MainLoopFixture()
        {
            Loop = new MainLoop("Test Loop");
        }

        public void Dispose()
        {
        }
    }

    public class MainLoopTests:IClassFixture<MainLoopFixture>
    {
        //static void Main(string[] args)
        //{
        //    MainLoopTests t = new MainLoopTests();
        //    t.FixtureSetup();
        //    t.Setup();
        //    t.TaskTest();
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        t.Setup();
        //        t.RepeatedTask();
        //    }
        //    t.Setup();
        //    t.LongRunningTask();
        //}
        private int count;
        MainLoop loop;

        public MainLoopTests(MainLoopFixture mf)
        {
            loop = mf.Loop;
        }

        public MainLoopTests()
        {
            count = 0;
        }

        [Fact]
        public void TaskTest()
        {
            Assert.Equal(5, loop.QueueWait((MainLoopJob) delegate { return 5; }));

            ManualResetEvent handle = new ManualResetEvent(false);
            loop.QueueWait((MainLoopTask)delegate { handle.Set(); });
#if IS_CORECLR
            Assert.True(handle.WaitOne(5000), "#2");
#else
            Assert.True(handle.WaitOne(5000, true), "#2");
#endif
        }

        [Fact]
        public void RepeatedTask()
        {
            //Console.WriteLine("Starting");
            ManualResetEvent handle = new ManualResetEvent(false);
            loop.QueueTimeout(TimeSpan.FromMilliseconds(0), delegate {
                this.count++;
                if (count == 3)
                {
                    handle.Set();
                    return false;
                }

                return true;
            });
#if IS_CORECLR
            Assert.True(handle.WaitOne(5000), $"#1: Executed {count} times");
#else
            Assert.True(handle.WaitOne(5000, true), $"#1: Executed {count} times");
#endif
            Assert.Equal(3, count);
        }

        [Fact]
        public void LongRunningTask()
        {
            ManualResetEvent handle = new ManualResetEvent(false);
            loop.QueueTimeout(TimeSpan.FromMilliseconds(10), delegate {
                System.Threading.Thread.Sleep(50);
                if (++count == 3)
                {
                    handle.Set();
                    return false;
                }

                return true;
            });
#if IS_CORECLR
            Assert.True(handle.WaitOne(5000), $"#1: Executed {count} times");
#else
            Assert.True(handle.WaitOne(5000, false), $"#1: Executed {count} times");
#endif
            Assert.Equal(3, count);
        }
    }
}
