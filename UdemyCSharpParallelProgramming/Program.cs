using System;
using System.Threading.Tasks;
using System.Threading;

namespace UdemyCSharpParallelProgramming
{
    class Program
    {

        public static void Write(char c)
        {
            int i = 1000;

            while (i-- > 0)
            {
                Console.Write(c);
            }
        }

        public static void Write(object o)
        {
            int i = 1000;

            while (i-- > 0)
            {
                Console.Write(o);
            }
        }

        public static int TextLength(object o)
        {
            Console.WriteLine($"\nTask with id {Task.CurrentId} processing object {o}...");
            return o.ToString().Length;
        }

        static void Main(string[] args)
        {
            // StartMultipleTasks();
            // CancelTasksWithSoftExit();
            // CancelTasksViaException_RecommendedWay();
            // WaitForTimeToPass();
            // WaitingForTasks();
            CatchAggregateException();

            Console.WriteLine("Main done. Hit enter");
            Console.ReadKey();
        }

        private static void CatchAggregateException()
        {
            try
            {
                HandlingExceptions();
            }
            catch (AggregateException ae)
            {
                ae.Handle(e =>
                {
                    if (e is AccessViolationException)
                    {
                        Console.WriteLine($"Catched {nameof(AccessViolationException)}");
                        return true;
                    }
                    return false;
                });
            }
        }

        private static void HandlingExceptions()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var t = new Task(() =>
            {
                throw new InvalidOperationException("Can't do this!") { Source = "t" };
            });
            t.Start();

            var t2 = new Task(() =>
            {
                throw new AccessViolationException("Can't access this!") { Source = "t2" };
            });
            t2.Start();

            try
            {
                Task.WaitAll(t, t2);
            }
            catch (AggregateException ae)
            {
                ae.Handle(e =>
                {
                    if (e is InvalidOperationException)
                    {
                        Console.WriteLine($"Catched {nameof(InvalidOperationException)}");
                        return true;
                    }
                    return false;
                });
            }
        }

        private static void WaitingForTasks()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var t = new Task(() =>
            {
                Console.WriteLine("I take 5 seconds");
                for (int i = 0; i < 5; i++)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(1000);
                }

                Console.WriteLine("I'm done");
            }, token);

            t.Start();

            Task t2 = Task.Factory.StartNew(() => Thread.Sleep(3000), token);

            Task.WaitAll(new[] { t, t2 }, 6000, token);

            Console.WriteLine($"Task t status is {t.Status}");
            Console.WriteLine($"Task t2 status is {t2.Status}");

        }

        private static void WaitForTimeToPass()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var t = new Task(() =>
            {
                Console.WriteLine($"Press any key, you've 5 seconds");
                var result = token.WaitHandle.WaitOne(5000);
                Console.WriteLine(result ? "disarmed" : "boom");
            }, token);

            t.Start();
            Console.ReadKey();
            cts.Cancel();

        }

        private static void CancelTasksWithSoftExit()
        {
            var cts = new System.Threading.CancellationTokenSource();
            var token = cts.Token;

            var t = new Task(
                () =>
                {
                    int i = 0;
                    while (true)
                    {
                        // Soft exit -> check the token if cancellation is requested
                        if (token.IsCancellationRequested)
                            break;
                        Console.WriteLine($"{i++}\t");
                    }
                }, token

                );
            t.Start();



            Console.ReadKey();
            cts.Cancel();
        }


        private static void CancelTasksViaException_RecommendedWay()
        {
            var planned = new System.Threading.CancellationTokenSource();
            var emergency = new System.Threading.CancellationTokenSource();
            var preventative = new System.Threading.CancellationTokenSource();

            // Multiple CTS can cause a task to be cancelled -> the CTSs can be combined
            var paranoid = System.Threading.CancellationTokenSource.CreateLinkedTokenSource
                (planned.Token
                , emergency.Token
                , preventative.Token);

            var t = new Task(
                () =>
                {
                    int i = 0;
                    while (true)
                    {
                        // Exit via exception
                        paranoid.Token.ThrowIfCancellationRequested();
                        Console.WriteLine($"{i++}\t");
                    }
                }, paranoid.Token

                );
            t.Start();

            Task.Factory.StartNew(() =>
            {
                paranoid.Token.WaitHandle.WaitOne();
                Console.WriteLine("Wait handle release, cancelation was requested");
            });


            Console.ReadKey();
            planned.Cancel();
        }

        private static void StartMultipleTasks()
        {
            string text1 = "testing", text2 = "this";

            var task1 = new Task<int>(TextLength, text1);
            task1.Start();

            Task<int> task2 = Task.Factory.StartNew(TextLength, text2);

            Console.WriteLine($"Length of {text1} is {task1.Result}"); // task1.Result blocks synchronously!!!
            Console.WriteLine($"Length of {text2} is {task2.Result}");
        }
    }
}
