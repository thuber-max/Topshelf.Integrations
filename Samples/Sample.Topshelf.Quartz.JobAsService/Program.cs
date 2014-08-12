using System;
using Ninject.Modules;
using Quartz;
using Topshelf;
using Topshelf.Ninject;
using Topshelf.Quartz;
using Topshelf.Quartz.Ninject;

namespace Sample.Topshelf.Quartz.JobAsService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(c =>
            {
                // Topshelf.Ninject (Optional) - Initiates Ninject and consumes Modules
                c.UseNinject(new SampleModule());
                // Topshelf.Quartz.Ninject (Optional) - Construct IJob instance with Ninject
                c.UseQuartzNinject();

                c.ScheduleQuartzJobAsService(q =>
                        q.WithJob(() =>
                            JobBuilder.Create<SampleJob>().Build())
                        .AddTrigger(() =>
                            TriggerBuilder.Create()
                                .WithSimpleSchedule(builder => builder
                                    .WithIntervalInSeconds(5)
                                    .RepeatForever())
                                .Build())
                                    );
            });

        }
    }

    public class SampleJob : IInterruptableJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Console.WriteLine("The current time is: {0}", DateTime.Now);
        }

        public void Interrupt()
        {
        }
    }

    public class SampleModule : NinjectModule
    {
        public override void Load()
        {

        }
    }
}
