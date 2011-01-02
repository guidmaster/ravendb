//-----------------------------------------------------------------------
// <copyright file="ShortestDistance.cs" company="Hibernating Rhinos LTD">
//     Copyright (c) Hibernating Rhinos LTD. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
extern alias database;
using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Reflection;
using System.Threading;
using Raven.Bundles.Expiration;
using Raven.Bundles.Graphs;
using Raven.Bundles.Graphs.Client;
using Raven.Client;
using Raven.Client.Document;
using Raven.Server;
using Xunit;

namespace Raven.Bundles.Tests.Graphs
{
	public class ShortestDistance : IDisposable
	{
		private readonly DocumentStore documentStore;
		private readonly string path;
		private readonly RavenDbServer ravenDbServer;

		public ShortestDistance()
		{
			path = Path.GetDirectoryName(Assembly.GetAssembly(typeof (Versioning.Versioning)).CodeBase);
			path = Path.Combine(path, "TestDb").Substring(6);
			database::Raven.Database.Extensions.IOExtensions.DeleteDirectory("Data");
			ravenDbServer = new RavenDbServer(
				new database::Raven.Database.Config.RavenConfiguration
					{
						Port = 58080,
						RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true,
						DataDirectory = path,
						Catalog =
							{
								Catalogs =
									{
										new AssemblyCatalog(typeof (ShortestDistanceRequestResponder).Assembly)
									}
							},
					});
			ExpirationReadTrigger.GetCurrentUtcDate = () => DateTime.UtcNow;
			documentStore = new DocumentStore
			                	{
			                		Url = "http://localhost:58080"
			                	};
			documentStore.Initialize();
		}

		#region IDisposable Members

		public void Dispose()
		{
			documentStore.Dispose();
			ravenDbServer.Dispose();
			database::Raven.Database.Extensions.IOExtensions.DeleteDirectory(path);
		}

		#endregion

		[Fact]
		public void CanQueryForShortestDistance()
		{
			using (var session = documentStore.OpenSession())
			{
				var ayende = new User
				             	{
				             		Name = "Ayende"
				             	};
				var arava = new User
				            	{
				            		Name = "Arava"
				            	};
				var oren = new User
				           	{
				           		Name = "Oren"
				           	};

				session.Store(ayende);
				session.Store(oren);
				session.Store(arava);

				session.Bundles.CreateRelation(new Relation
				                               	{
				                               		Source = ayende,
				                               		Target = oren,
				                               		GraphName = "Names"
				                               	});

				session.Bundles.CreateRelation(new Relation
				                               	{
				                               		Source = oren,
				                               		Target = arava,
				                               		GraphName = "Names"
				                               	});

				session.SaveChanges();
			}

			using (var session = documentStore.OpenSession())
			{
				var graphQuery = session.Bundles.GraphQuery(new ShortestDistanceQuery
				{
					Algorithm = ShortestDistanceAlgorithm.Djikstra,
					GraphName = "Names",
					Source = "users/1",
					Target = "users/3"
				});

				Assert.NotEmpty(graphQuery);

				Assert.Equal("users/1", graphQuery[0].Source);
				Assert.Equal("users/2", graphQuery[0].Target);
				Assert.Equal("users/2", graphQuery[1].Source);
				Assert.Equal("users/3", graphQuery[1].Target);
			}
		}
	}
}