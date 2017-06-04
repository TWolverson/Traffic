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

            var str = JsonConvert.SerializeObject(network, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            Console.Write(str);
            File.WriteAllText("newnetwork.json", str);
        }
    }
}
