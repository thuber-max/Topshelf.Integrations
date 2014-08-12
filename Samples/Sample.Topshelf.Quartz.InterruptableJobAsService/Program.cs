using System;
using System.Threading;
using Quartz;
using Topshelf;
using Topshelf.Quartz;

namespace Sample.Topshelf.Quartz.InterruptableJobAsService
{
    class Program
    {
        static void Main()
        {
            HostFactory.Run(c => c.ScheduleQuartzJobAsService(q =>
                q.WithJob(() =>
                    JobBuilder.Create<SampleJob>().Build())
                    .AddTrigger(() =>
                        TriggerBuilder.Create()
                            .WithSimpleSchedule(builder => builder
                                .WithIntervalInMinutes(5)
                                .RepeatForever())
                            .Build())
                ));

        }
    }

    public class SampleJob : IInterruptableJob
    {
        private CancellationToken _token;
        private readonly CancellationTokenSource _source;
        public SampleJob()
		{
			_source = new CancellationTokenSource();
			_token = _source.Token;
		}

		public void Execute(IJobExecutionContext context)
		{
			while (!_token.IsCancellationRequested)
			{
                Console.WriteLine("The current time is: {0}", DateTime.Now);
				Thread.Sleep(2000);
			}
		}

		public void Interrupt()
		{
			_source.Cancel();
		}
    }
}
