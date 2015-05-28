using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SCP;
using Microsoft.SCP.Topology;

namespace EventHubReader
{
    /// <summary>
    /// A hybrid C#/Java topology
    ///     The Java-based EventHubSpout reads from event hub
    ///     Data is then parsed by C# and written to Table Storage
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
            //The friendly name of this topology is 'EventHubReader'
            TopologyBuilder topologyBuilder = new TopologyBuilder("EventHubReader");

            //Get the partition count
            int partitionCount = Properties.Settings.Default.EventHubPartitionCount;
            //Create the configuration for the EventHub spout
            EventHubSpoutConfig ehConfig = new EventHubSpoutConfig(
                Properties.Settings.Default.EventHubPolicyName,
                Properties.Settings.Default.EventHubPolicyKey,
                Properties.Settings.Default.EventHubNamespace,
                Properties.Settings.Default.EventHubName,
                partitionCount);

            //Set the spout to use the JavaComponentConstructor
            topologyBuilder.SetEventHubSpout(
                "EventHubSpout",  //Friendly name of this component
                ehConfig,      //Pass in the Java constructor
                partitionCount);  //Parallelism hint - partition count

            // Use a JSON Serializer to serialize data from the Java Spout into a JSON string
            List<string> javaSerializerInfo = new List<string>() { "microsoft.scp.storm.multilang.CustomizedInteropJSONSerializer" };

            //Set the C# bolt that consumes data from the spout
            //NOTE: The EventHubSpout component requires ACK's to be returned
            //by downstream components. If not, it will stop receiveing messages
            //after the configured MaxPendingMsgsPerPartition value (default 1024).
            topologyBuilder.SetBolt(
                "Bolt",                                              //Friendly name of this component
                Bolt.Get,
                new Dictionary<string, List<string>>(),
                partitionCount,                                      //Parallelisim hint - partition count
                true).                                               //Enable ACK's, needed for the spout    
                DeclareCustomizedJavaSerializer(javaSerializerInfo). //Use the serializer when sending to the bolt
                shuffleGrouping("EventHubSpout");                    //Consume data from the 'EventHubSpout' component

            //Create a new configuration for the topology
            StormConfig config = new StormConfig();
            config.setNumWorkers(1); //Set the number of workers

            //Set the configuration for the topology
            topologyBuilder.SetTopologyConfig(config);

            return topologyBuilder;
        }
    }
}

