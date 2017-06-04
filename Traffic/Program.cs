using System;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
class Traffic
{
    enum FactorTableType
    {
        SmoothingFactor
    }

    static void Main(string[] args)
    {

        var jsonConfig = File.ReadAllText("network.json");
        var networkObject = JsonConvert.DeserializeObject<dynamic>(jsonConfig);
        var network = new Network();
        PopulateLinks(network, networkObject);

        Phase[] phases = new Phase[5];

        Stage[] stages = new Stage[3];

        phases[0] = new Phase();
        phases[1] = new Phase();
        phases[2] = new Phase();
        phases[3] = new Phase();
        phases[4] = new Phase();

        stages[0] = new Stage() { StartTime = 0, EndTime = 30 };
        stages[1] = new Stage() { StartTime = 35, EndTime = 60 };
        stages[2] = new Stage() { StartTime = 65, EndTime = 90 };

        phases[0].RunsInStages = new Stage[] { stages[0] };
        phases[0].ConflictsWithPhases = new Phase[] { phases[2], phases[3], phases[4] };

        phases[1].RunsInStages = new Stage[] { stages[0] };
        phases[1].ConflictsWithPhases = new Phase[] { phases[2], phases[3], phases[4] };

        phases[2].RunsInStages = new Stage[] { stages[1] };
        phases[2].ConflictsWithPhases = new Phase[] { phases[0], phases[1], phases[3], phases[4] };

        phases[3].RunsInStages = new Stage[] { stages[2] };
        phases[3].ConflictsWithPhases = new Phase[] { phases[0], phases[1], phases[2] };

        phases[4].RunsInStages = new Stage[] { stages[2] };
        phases[4].ConflictsWithPhases = new Phase[] { phases[0], phases[1], phases[2] };

        Link[] links = new Link[3];

        links[0] = new Link() { ControlledByPhases = new Phase[] { phases[0] } };

        links[1] = new Link { UpstreamLinks = new Link[] { links[0] } };

        links[2] = new Link { UpstreamLinks = new Link[] { links[0] } };

        int cycleTimeSeconds = 95;

        double[,] factorTable = new double[3, 1];


        var avgLinkLengthM = 200.0;
        var avgTrafficSpeedMS = 8.94; // 20 miles per hour expressed as metres per second
        var dispersionFactor = 0.7;
        var travelTimeFactor = 1.0;
        var linkTravelTime = (int)(avgLinkLengthM / avgTrafficSpeedMS);
        var smoothFactor = 1 / (1 + dispersionFactor * travelTimeFactor * linkTravelTime);

        // precompute smoothing factors
        for (int linkIndex = 0; linkIndex < links.Length; linkIndex++)
        {
            factorTable[linkIndex, (int)FactorTableType.SmoothingFactor] = 1 / (1 + dispersionFactor * travelTimeFactor * linkTravelTime);
        }

        int[,] inflows = new int[3, cycleTimeSeconds];

        int[,] outflows = new int[3, cycleTimeSeconds];

        int[,] queues = new int[3, cycleTimeSeconds];

        int[,] goflows = new int[3, cycleTimeSeconds];

        for (int timestep = 0; timestep < cycleTimeSeconds; timestep++)
        {
            inflows[0, timestep] = timestep < (cycleTimeSeconds / 2) ? 500 : 0;
            for (int linkIndex = 0; linkIndex < links.Length; linkIndex++)
            {
                Link thisLink = links[linkIndex];
                goflows[linkIndex, timestep] = thisLink.ControlledByPhases.All(p => p.IsFreeFlowAtTimeStep(timestep)) ? 1800 : 0;
            }
        }

        // precompute raised smoothing factors
        double[] raisedSmoothingFactors = new double[cycleTimeSeconds];
        for (int offToInfinity = linkTravelTime; offToInfinity < cycleTimeSeconds; offToInfinity++)
        {
            raisedSmoothingFactors[offToInfinity] = Math.Pow(1 - smoothFactor, offToInfinity - linkTravelTime);
        }

        for (int timestep = 0; timestep < cycleTimeSeconds; timestep++)
        {
            double flowAtTimestep = 0;

            for (int offToInfinity = linkTravelTime; offToInfinity < cycleTimeSeconds; offToInfinity++)
            {
                int upstreamTimestep = timestep - offToInfinity + linkTravelTime;
                flowAtTimestep += smoothFactor * raisedSmoothingFactors[offToInfinity] * (upstreamTimestep >= 0 ? inflows[0, upstreamTimestep] : 0);
            }
            inflows[0, timestep] = (int)flowAtTimestep;
        }

        int lastStepExcess = 0;
        for (int timestep = 0; timestep < cycleTimeSeconds; timestep++)
        {
            var flowThisStep = lastStepExcess + inflows[0, timestep];
            outflows[0, timestep] = Math.Min(flowThisStep, goflows[0, timestep]);
            lastStepExcess = flowThisStep - outflows[0, timestep];
        }

        Console.WriteLine("Example platoon dispersion on a single link");

        for (int y = 25; y >= 0; y--)
        {
            for (int timestep = 0; timestep < cycleTimeSeconds; timestep++)
            {

                var charToWrite = '.';
                charToWrite = ((int)inflows[1, timestep]) / 20 >= y ? 'X' : charToWrite;
                Console.Write(charToWrite);
            }
            Console.WriteLine();
        }

        char phasechar = 'X';
        char stagechar = '/';
        char nothingchar = '.';

        Console.WriteLine("Example stage-phase diagram");

        for (int i = 0; i < phases.Length * 3; i++)
        {

            int phaseno = i / 3;
            for (int j = 0; j < cycleTimeSeconds; j++)
            {
                char chartoDraw = nothingchar;

                Stage anyCurrentStage = stages.FirstOrDefault(s => s.StartTime <= j && s.EndTime > j);
                chartoDraw = anyCurrentStage != null ? stagechar : chartoDraw;

                if (i % 3 == 0 | i % 3 == 1)
                {
                    chartoDraw =
                     phases[phaseno].RunsInStages.Contains(anyCurrentStage) ? phasechar : chartoDraw;

                }
                Console.Write(chartoDraw);


            }
            Console.WriteLine();

        }
    }

    private static void PopulateLinks(Network network, dynamic networkObject)
    {
        var links = new List<Link>();
        var upstreamInitializers = new Dictionary<Link, Func<Link[]>>();
        foreach (dynamic linkObj in networkObject.links)
        {
            int[] upstreamLinkIds = ((JArray)linkObj.upstreamLinks)?.Select(jv => (int)jv).ToArray();
            int[] downstreamLinkIds = ((JArray)linkObj.downstreamLinks)?.Select(jv => (int)jv).ToArray();
            var link = JsonConvert.DeserializeObject<Link>(JsonConvert.SerializeObject((object)linkObj));
            upstreamInitializers.Add(link, () => upstreamLinkIds?.Join(links, id => id, l => l.Id, (i, l) => l).ToArray());
            links.Add(link);
        }
        foreach (var link in links)
        {
            link.UpstreamLinks = upstreamInitializers[link]();
        }
        network.Links = links.ToArray();
    }
}

public class Network
{
    [JsonProperty("controllers")]
    public Controller[] Controllers;

    [JsonProperty("links")]
    public Link[] Links;
}

public class Controller
{
    [JsonProperty("phases")]
    public Phase[] Phases;

    [JsonProperty("stages")]
    public Stage[] Stages;


    public int CycleTime;
}
public class Phase
{
    public Stage[] RunsInStages;
    public Phase[] ConflictsWithPhases;

    public bool IsFreeFlowAtTimeStep(int timestep)
    {
        return
        RunsInStages == null // if this phase doesn't run in any stages, assume it is always green, as a phase that stops traffic forever is pointless in reality
        ||
        RunsInStages.Any(s => s.StartTime <= timestep && s.EndTime > timestep);
    }
}


public class Stage
{
    public int StartTime;
    public int EndTime;
}

public class Link
{
    [JsonProperty("id")] 
    public int Id;
    public Phase[] ControlledByPhases = new Phase[0];

    public Link[] UpstreamLinks;

    public Link[] DownstreamLinks;
}