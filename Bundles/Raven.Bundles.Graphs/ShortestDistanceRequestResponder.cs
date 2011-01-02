using System;
using System.Collections.Generic;
using Lucene.Net.QueryParsers;
using Newtonsoft.Json.Linq;
using QuickGraph;
using Raven.Database;
using Raven.Database.Data;
using Raven.Database.Indexing;
using Raven.Database.Server.Responders;
using Raven.Http.Abstractions;
using Raven.Database.Queries;
using System.Linq;
using Raven.Database.Json;
using QuickGraph.Algorithms;
using Raven.Http.Extensions;

namespace Raven.Bundles.Graphs
{
	public class ShortestDistanceRequestResponder : RequestResponder
	{
		public override string UrlPattern
		{
			get { return "^/graphs/shortestdistance$"; }
		}

		public override string[] SupportedVerbs
		{
			get { return new[] { "GET" }; }
		}

		public override void Respond(IHttpContext context)
		{
			var q = new ShortestDistanceQuery
			{
				Algorithm = (ShortestDistanceAlgorithm)Enum.Parse(typeof(ShortestDistanceAlgorithm), context.Request.QueryString["algorithm"]),
				GraphName = context.Request.QueryString["graph"],
				Source = context.Request.QueryString["source"],
				Target = context.Request.QueryString["target"]
			};

			var queryResult = Database.ExecuteDynamicQuery(null, new IndexQuery
			{
				Query = "@metadata.Raven-Graph-Type:Vertex AND @metadata.Raven-Graph-Name:" + QueryParser.Escape(q.GraphName),
				FieldsToFetch = new[] {"__document_id"},
				PageSize = int.MaxValue // we have to have access to the entire result-set
			});

			var graph = new DelegateVertexAndEdgeListGraph<string, EquatableEdge<string>>(queryResult.Results.Select(x => x.Value<string>("__document_id")),
				new EdgeFetcher
					{
						Database = Database,
						GraphName = q.GraphName
					}.TryGetOutEdges);


			var shortestPath = GetShortestPathEnumerator(graph, q.Source, q.Algorithm);
			IEnumerable<EquatableEdge<string>> edges;
			if (shortestPath(q.Target, out edges) == false)
			{
				context.WriteJson(new object[0]);
				return;
			}
			context.WriteJson(edges);
		}

		private static TryFunc<string, IEnumerable<EquatableEdge<string>>> GetShortestPathEnumerator(IVertexAndEdgeListGraph<string, EquatableEdge<string>> graph, 
			string source,
			ShortestDistanceAlgorithm algorithm)
		{
			TryFunc<string, IEnumerable<EquatableEdge<string>>> shortestPath;
			switch (algorithm)
			{
				case ShortestDistanceAlgorithm.Djikstra:
					shortestPath = graph.ShortestPathsDijkstra(edge => 1, source);
					break;
				case ShortestDistanceAlgorithm.AStar:
					shortestPath = graph.ShortestPathsAStar(edge => 1, s => 1.0, source);
					break;
				case ShortestDistanceAlgorithm.BellmanFord:
					shortestPath = graph.ShortestPathsBellmanFord(edge => 1, source);
					break;
				case ShortestDistanceAlgorithm.Dag:
					shortestPath = graph.ShortestPathsDag(edge => 1, source);
					break;
				default:
					throw new ArgumentOutOfRangeException("algorithm", algorithm.ToString());
			}
			return shortestPath;
		}

		public class EdgeFetcher
		{
			public DocumentDatabase Database { get; set; }

			public string GraphName { get; set; }

			public bool TryGetOutEdges(string documentId, out IEnumerable<EquatableEdge<string>> result)
			{
				result =
					from jEdge in GetQueryResultsForEdge()
					let edge = jEdge.JsonDeserialization<InternalGraphEdge>()
					select new EquatableEdge<string>(edge.Source, edge.Target);

				return true;
			}

			private IEnumerable<JObject> GetQueryResultsForEdge()
			{
				var queryResultsForEdge = Database.ExecuteDynamicQuery(null, new IndexQuery
				{
					Query = "@metadata.Raven-Graph-Type:Edge AND @metadata.Raven-Graph-Name:" + QueryParser.Escape(GraphName),
					PageSize = int.MaxValue // we have to have access to the entire result-set
				}).Results;
				return queryResultsForEdge;
			}
		}
	}
}