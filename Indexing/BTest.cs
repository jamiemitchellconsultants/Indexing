using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BIndexGrains;
using IBIndex;
using IndexingInterfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Xunit;

namespace Indexing
{
    public class BTest
    {
        [Fact]
        public void ItemTest()
        {
            var target = new Item<int, int>(1, 2);
            var actual = target.ToJson();
            var expected = @"{""Key"":1,""Value"":2}";
            Assert.Equal(actual,expected);
        }

        [Fact]
        public async Task GrainTest()
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
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Build();

            try
            {
                await Task.WhenAll(silo1.StartAsync()
                    //, silo2.StartAsync()
                );

                var testItem = new List<IBIndex.IItem<string, Guid>>
                {
                    new Item<string, Guid>("key 10", Guid.NewGuid()),
                    new Item<string, Guid>("key 20", Guid.NewGuid()),
                    new Item<string, Guid>("key 30", Guid.NewGuid()),
                    new Item<string, Guid>("key 40", Guid.NewGuid()),
                    new Item<string, Guid>("key 50", Guid.NewGuid()),
                    new Item<string, Guid>("key 60", Guid.NewGuid()),
                    new Item<string, Guid>("key 70", Guid.NewGuid()),
                    new Item<string, Guid>("key 80", Guid.NewGuid())
                };
                await client.Connect();
                var leafGuid= Guid.NewGuid();
                var testLeafGrain = client.GetGrain<IBLeaf<string, Guid>>(leafGuid);

                await testLeafGrain.Initialize(6, null, new SortedDictionary<string, IBIndex.IItem<string, Guid>>());

                var actual=await testLeafGrain.Add(testItem[0]);
                actual = await testLeafGrain.Add(testItem[1]);
                actual = await testLeafGrain.Add(testItem[2]);
                actual = await testLeafGrain.Add(testItem[3]);
                actual = await testLeafGrain.Add(testItem[4]);
                actual = await testLeafGrain.Add(testItem[5]);
                Assert.Null(actual);
                actual = await testLeafGrain.Add(testItem[6]);
                Assert.NotNull(actual);

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
