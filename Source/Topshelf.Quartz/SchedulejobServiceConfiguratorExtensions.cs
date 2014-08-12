using System;
using System.Linq;
using Quartz;
using Quartz.Collection;
using Quartz.Impl;
using Quartz.Spi;
using Topshelf.Logging;
using Topshelf.ServiceConfigurators;

namespace Topshelf.Quartz
{
	public static class ScheduleJobServiceConfiguratorExtensions
	{
		private static readonly Func<IScheduler> DefaultSchedulerFactory = () =>
			                                                                   {
				                                                                   var schedulerFactory = new StdSchedulerFactory();
				                                                                   return schedulerFactory.GetScheduler();
			                                                                   };

		private static Func<IScheduler> _customSchedulerFactory;
		private static IScheduler Scheduler;
		internal static IJobFactory JobFactory;

		public static Func<IScheduler> SchedulerFactory
		{
			get { return _customSchedulerFactory ?? DefaultSchedulerFactory; }
			set { _customSchedulerFactory = value; }
		}

		private static IScheduler GetScheduler()
		{
			var scheduler = SchedulerFactory();
			
			if(JobFactory != null)
				scheduler.JobFactory = JobFactory;

			return scheduler;
		}

		public static ServiceConfigurator<T> UsingQuartzJobFactory<T, TJobFactory>(this ServiceConfigurator<T> configurator, Func<TJobFactory> jobFactory)
			where T : class
			where TJobFactory : IJobFactory
		{
			JobFactory = jobFactory();
			return configurator;
		}

		public static ServiceConfigurator<T> UsingQuartzJobFactory<T,TJobFactory>(this ServiceConfigurator<T> configurator) where T : class where TJobFactory : IJobFactory, new()
		{
			return UsingQuartzJobFactory(configurator, () => new TJobFactory());
		}

		public static ServiceConfigurator<T> ScheduleQuartzJob<T>(this ServiceConfigurator<T> configurator, Action<QuartzConfigurator> jobConfigurator) where T : class
		{
			ConfigureJob<T>(configurator, jobConfigurator);
			return configurator;
		}

		private static void ConfigureJob<T>(ServiceConfigurator<T> configurator, Action<QuartzConfigurator> jobConfigurator) where T : class
		{
			var log = HostLogger.Get(typeof(ScheduleJobServiceConfiguratorExtensions));

			var jobConfig = new QuartzConfigurator();
			jobConfigurator(jobConfig);

			if (jobConfig.JobEnabled == null || jobConfig.JobEnabled() || (jobConfig.Job == null || jobConfig.Triggers == null))
			{
				var jobDetail = jobConfig.Job();
				var jobTriggers = jobConfig.Triggers.Select(triggerFactory => triggerFactory()).Where(trigger => trigger != null);

				configurator.BeforeStartingService(() =>
					                                   {
														   log.Debug("[Topshelf.Quartz] Scheduler starting up...");
														   if (Scheduler == null)
															   Scheduler = GetScheduler();
														   
														   
															if (Scheduler != null && jobDetail != null && jobTriggers.Any())
															{
																var triggersForJob = new HashSet<ITrigger>(jobTriggers);
																Scheduler.ScheduleJob(jobDetail, triggersForJob, false);
																log.Info(string.Format("[Topshelf.Quartz] Scheduled Job: {0}", jobDetail.Key));
					
																foreach(var trigger in triggersForJob)
																	log.Info(string.Format("[Topshelf.Quartz] Job Schedule: {0} - Next Fire Time (local): {1}", trigger, trigger.GetNextFireTimeUtc().HasValue ? trigger.GetNextFireTimeUtc().Value.ToLocalTime().ToString() : "none"));

																Scheduler.Start();
																log.Info("[Topshelf.Quartz] Scheduler started...");
															}

					                                   });

                configurator.BeforeStoppingService(() =>
                                                        {
                                                            log.Info("[Topshelf.Quartz] Scheduler shutting down...");

                                                            var executingJobs = Scheduler.GetCurrentlyExecutingJobs();
                                                            foreach (var job in executingJobs)
                                                            {
                                                                log.InfoFormat("[Topshelf.Quartz] Attempt to interrupt currently executing job: {0}", job.JobDetail.JobType);
                                                                if (job.JobInstance.GetType().GetInterfaces().Contains(typeof(IInterruptableJob)))
                                                                {
                                                                    ((IInterruptableJob)job.JobInstance).Interrupt();
                                                                }
                                                            }
                                                            Scheduler.Shutdown(true);
                                                            log.Info("[Topshelf.Quartz] Scheduler shut down...");
                                                        });

			}
		}
	}
}