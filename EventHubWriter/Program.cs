using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace EventHubWriter
{
    /// <summary>
    /// A hybrid C#/Java topology
    ///     The C# spout creates random 'device' data
    ///     Data is then written to Event Hub using the Java bolt
    /// </summary>
    [Active(true)]
    class Program : TopologyDescriptor
    {
        static void Main(string[] args)
        {
        }
        /// <summary>
        /// Builds a topology that can be submitted to Storm on HDInsight
        /// </summary>
        /// <returns>A topology builder</returns>
        public ITopologyBuilder GetTopologyBuilder()
        {
            //The friendly name is 'EventHubWriter'
            TopologyBuilder topologyBuilder = new TopologyBuilder("EventHubWriter");

            //Get the partition count
            int partitionCount = Properties.Settings.Default.EventHubPartitionCount;
            //Create a deserializer for JSON to java.lang.String
            //so that Java components can consume data emitted by
            //C# components
            List<string> javaDeserializerInfo =
                new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONDeserializer", "java.lang.String" };
            
            //Set the spout
            topologyBuilder.SetSpout(
                "Spout",
                Spout.Get,
                new Dictionary<string, List<string>>()
                {
                    {Constants.DEFAULT_STREAM_ID, new List<string>(){"Event"}}
                },
                partitionCount). //Parallelism hint uses partition count
                DeclareCustomizedJavaDeserializer(javaDeserializerInfo); //Deserializer for the output stream

            //Create constructor for the Java bolt
            JavaComponentConstructor constructor = new JavaComponentConstructor(
                "com.microsoft.eventhubs.bolt.EventHubBoltConfig",
                new List<Tuple<string, object>>()
                {
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, Properties.Settings.Default.EventHubPolicyName),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, Properties.Settings.Default.EventHubPolicyKey),
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, Properties.Settings.Default.EventHubNamespace), 
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, "servicebus.windows.net"), //suffix for servicebus fqdn
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, Properties.Settings.Default.EventHubName), 
                    Tuple.Create<string, object>(JavaComponentConstructor.JAVA_LANG_STRING, "true")
                });

            topologyBuilder.SetJavaBolt(
                    "EventHubBolt",
                    constructor,
                    partitionCount). //Parallelism hint uses partition count
                shuffleGrouping("Spout"); //Consume data from spout

            topologyBuilder.SetTopologyConfig(new Dictionary<string, string>()
                {
                    {"topology.workers", "1"}  //Change to set the number of workers to create
                });

            return topologyBuilder;
        }
    }
}

