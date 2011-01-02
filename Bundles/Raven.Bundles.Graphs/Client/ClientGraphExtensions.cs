using System;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Raven.Client.Client;
using System.Linq;

namespace Raven.Bundles.Graphs.Client
{
	public static class ClientGraphExtensions
	{
		public static GraphEdge[] GraphQuery(this IDocumentSessionBundleEndpoint self, ShortestDistanceQuery query)
		{
			var serverClient = ((ServerClient)self.Session.Advanced.DatabaseCommands);

			var url = serverClient.Url + "/graphs/shortestdistance?algorithm=" + query.Algorithm + 
				"&source=" + query.Source +
				"&target=" + query.Target + 
				"&graph=" + query.GraphName;
			var jsonRequest = HttpJsonRequest.CreateHttpJsonRequest(self,
			                                                            url,
			                                                            "GET",
			                                                            serverClient.Credentials,
			                                                            serverClient.Convention);

			var readResponseString = jsonRequest.ReadResponseString();

			var jArray = JArray.Parse(readResponseString);

			return jArray.OfType<JObject>().Select(x => x.Deserialize<GraphEdge>(serverClient.Convention))
				.ToArray();
		}

		public static void CreateRelation(this IDocumentSessionBundleEndpoint self, Relation relation)
		{
			var sourceMetadata = self.Session.Advanced.GetMetadataFor(relation.Source);
			sourceMetadata["Raven-Graph-Type"] = "Vertex";
			sourceMetadata["Raven-Graph-Name"] = relation.GraphName;


			var targetMetadata = self.Session.Advanced.GetMetadataFor(relation.Target);
			targetMetadata["Raven-Graph-Type"] = "Vertex";
			targetMetadata["Raven-Graph-Name"] = relation.GraphName;

			var sourceDocId = self.Session.Advanced.GetDocumentId(relation.Source);
			var targetDocId = self.Session.Advanced.GetDocumentId(relation.Target);

			if(string.IsNullOrEmpty(sourceDocId))
				throw new ArgumentException("Could not find the source document id");
			if (string.IsNullOrEmpty(targetDocId))
				throw new ArgumentException("Could not find the target document id");

			var edge = new InternalGraphEdge
			                        	{
											Id = "Edges/" + sourceDocId + "/" + targetDocId,
			                        		Source = sourceDocId,
			                        		Target = targetDocId
			                        	};

			self.Session.Store(edge);

			var edgeMetadata = self.Session.Advanced.GetMetadataFor(edge);
			edgeMetadata["Raven-Graph-Type"] = "Edge";
			edgeMetadata["Raven-Graph-Name"] = relation.GraphName;
		}
	}
}