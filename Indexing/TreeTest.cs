using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IndexingInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.TestingHost;
using Xunit;

namespace Indexing
{
    public class TreeTest
    {
        /// <summary>
        /// Tests that <see cref="CoreHostingExtensions.UseLocalhostClustering"/> works for multi-silo clusters.
        /// </summary>
        [Fact]
        public async Task LocalhostClusterTest()
        {
            var silo1 = new HostBuilder().UseOrleans(siloBuilder =>
            {
                siloBuilder
                .AddMemoryGrainStorage("MemoryStore")
                .AddMemoryGrainStorage("indexStore")
                .AddMemoryGrainStorageAsDefault()
                .ConfigureLogging(sb => sb.SetMinimumLevel(LogLevel.Trace).AddDebug())
                .UseLocalhostClustering();
            }).Build();

            //var silo2 = new HostBuilder().UseOrleans(siloBuilder =>
            //{
            //    siloBuilder
            //    .AddMemoryGrainStorage("MemoryStore")
            //    .AddMemoryGrainStorageAsDefault()
            //    .ConfigureLogging(sb=>sb.SetMinimumLevel(LogLevel.Trace).AddDebug())
            //    .UseLocalhostClustering(baseSiloPort + 1, baseGatewayPort + 1, new IPEndPoint(IPAddress.Loopback, baseSiloPort));
            //}).Build();

            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Build();

            try
            {
                await Task.WhenAll(silo1.StartAsync()
                //, silo2.StartAsync()
                );


                await client.Connect();
                var grain = client.GetGrain<IBPTree<string, string>>(Guid.NewGuid());
                await grain.InitializeTree(6);
                await grain.Add(new KeyValuePair<string, string>("K10", "world"));
                var jsonString = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K20", "to you"));
                var jsonString02 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K30", "are you"));
                var jsonString03 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K40", "loves you"));
                var jsonString04 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K50", "cruel world"));
                var jsonString05 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K60", "harmless"));
                var jsonString06 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K42", "world"));
                var jsonString07 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K44", "to you"));
                var jsonString08 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K46", "are you"));
                var jsonString10 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K48", "loves you"));
                var jsonString11 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K80", "cruel world"));
                var jsonString12 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K90", "harmless"));
                var jsonString13 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("K45", "world"));
                var jsonString14 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("good morning2", "to you"));
                var jsonString15 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("how2", "are you"));
                var jsonString16 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("who2", "loves you"));
                var jsonString17 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("goodbye2", "cruel world"));
                var jsonString18 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("mostly2", "harmless"));
                var jsonString19 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("mostly4", "harmless"));
                var jsonString20 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("mostly5", "harmless"));
                var jsonString21 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("mostly6", "harmless"));
                var jsonString22 = await grain.ToJson();
                Assert.NotEqual("",jsonString);
                await grain.Add(new KeyValuePair<string, string>("hello", "harmless"));
                var jsonString23 = await grain.ToJson();
                Assert.NotEqual("",jsonString);

                var result = await grain.Get("hello");
                Assert.Equal("harmless", result);
                var res2 = await grain.Get("heklo");
                var result2 = await grain.Get("who2");
                Assert.Equal("loves you", result);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("fred"))
                {
                    throw new InvalidOperationException();
                }
            }
            finally
            {
                var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                Utils.SafeExecute(() => silo1.StopAsync(cancelled.Token));
                //Utils.SafeExecute(() => silo2.StopAsync(cancelled.Token));
                Utils.SafeExecute(() => silo1.Dispose());
                //Utils.SafeExecute(() => silo2.Dispose());
                Utils.SafeExecute(() => client.Close());
                Utils.SafeExecute(() => client.Dispose());
            }
        }

    }
}
