using System;
using Xunit;
using Traffic;
using Newtonsoft.Json;
using System.IO;

namespace Traffic.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void then_serializing_links_works()
        {
            var links = new Link[2];
            links[0] = new Link() { Id = 1 };
            links[1] = new Link()
            {
                Id = 2,
                UpstreamLinks = new Link[]
                        {
                            links[0]
                        }
            };
            var network = new Network()
            {
                Links = links
            };

            var controllers = new Controller[1];
            var phases = new Phase[5]
            {
                        new Phase(),
                        new Phase(),
                        new Phase(),
                        new Phase(),
                        new Phase()
                        };


            phases[0].ConflictsWithPhases = new Phase[]{
                phases[2],
                phases[3],
                phases[4]
            };
            phases[1].ConflictsWithPhases = new Phase[]{
                phases[2],
                phases[3],
                phases[4]
            };

            phases[2].ConflictsWithPhases = new Phase[]{
                phases[0],
                phases[1],
                phases[3],
                phases[4]
            };

            phases[3].ConflictsWithPhases = new Phase[]{
                phases[0],
                phases[1],
                phases[2]
            };

            phases[4].ConflictsWithPhases = new Phase[]{
                phases[0],
                phases[1],
                phases[2]
            };

            var stages = new Stage[3] { new Stage { StartTime = 0, EndTime = 30 }, new Stage { StartTime = 35, EndTime = 60 }, new Stage { StartTime = 65, EndTime = 90 } };

            phases[0].RunsInStages = new[] { stages[0] };
            phases[1].RunsInStages = new[] { stages[0] };
            phases[2].RunsInStages = new[] { stages[1] };
            phases[3].RunsInStages = new[] { stages[2] };
            phases[4].RunsInStages = new[] { stages[2] };

            controllers[0] = new Controller
            {
                Phases = phases,
                Stages = stages,
                CycleTime = 95
            };

            network.Controllers = controllers;
            
            var str = JsonConvert.SerializeObject(network, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            Console.Write(str);
            File.WriteAllText("newnetwork.json", str);
        }
    }
}
