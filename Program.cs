using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SqsTransfer
{
    class Program
    {
        protected static IServiceProvider ServiceProvider;

        static async Task Main(string[] args)
        {
            LoadDependencies();

            const string sourceUrl = "";
            var sqsClient = ServiceProvider.GetService<IAmazonSQS>();
            bool hasMore;
            
            do
            {
                var messages = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = sourceUrl,
                    MaxNumberOfMessages = 10
                });
                
                hasMore = messages.Messages.Count != 0;

                foreach (var msg in messages.Messages)
                {
                    await sqsClient.SendMessageAsync(new SendMessageRequest
                    {
                        QueueUrl = "",
                        MessageBody = msg.Body
                    });

                    await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                    {
                        QueueUrl = sourceUrl,
                        ReceiptHandle = msg.ReceiptHandle
                    });
                }
            } while (hasMore);
#if DEBUG
            Console.WriteLine("Finished...");
            Console.ReadLine();
#endif
        }

        private static void LoadDependencies()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appSettings.json", false, false);

            var configuration = builder.Build();
            var services = new ServiceCollection();
            services.AddOptions();
            services.AddDefaultAWSOptions(configuration.GetAWSOptions());
            services.AddLogging(opt => opt.AddConsole());
            services.TryAddAWSService<IAmazonSQS>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
