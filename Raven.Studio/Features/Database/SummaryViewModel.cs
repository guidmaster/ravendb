﻿using System.Threading.Tasks;
using Raven.Client;

namespace Raven.Studio.Features.Database
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.Linq;
	using Abstractions.Data;
	using Caliburn.Micro;
	using Collections;
	using Documents;
	using Framework;
	using Messages;

	public class SummaryViewModel : RavenScreen, IDatabaseScreenMenuItem,
		IHandle<DocumentDeleted>
	{
		readonly IServer server;
		readonly IEventAggregator events;

		public int Index { get { return 10; } }

		[ImportingConstructor]
		public SummaryViewModel(IServer server, IEventAggregator events)
			: base(events)
		{
			this.server = server;
			this.events = events;
			events.Subscribe(this);

			DisplayName = "Summary";

			server.CurrentDatabaseChanged += delegate { NotifyOfPropertyChange(string.Empty); };
		}

		public string DatabaseName
		{
			get { return server.CurrentDatabase; }
		}

		public IServer Server
		{
			get { return server; }
		}

		public BindableCollection<DocumentViewModel> RecentDocuments { get; private set; }

		public IEnumerable<Collection> Collections { get; private set; }

		public long LargestCollectionCount
		{
			get
			{
				return (Collections == null || !Collections.Any())
						? 0
						: Collections.Max(x => x.Count);
			}
		}

		public void Handle(DocumentDeleted message)
		{
			RecentDocuments
				.Where(x => x.Id == message.DocumentId)
				.ToList()
				.Apply(x => RecentDocuments.Remove(x));

			//TODO: update collections
			//Collections
			//    .Where(x => x.Name == message.Document.CollectionType)
			//    .Apply(x => x.Count--);
		}

		public void NavigateToCollection(Collection collection)
		{
			events.Publish(new DatabaseScreenRequested(() =>
			{
				var vm = IoC.Get<CollectionsViewModel>();
				vm.ActiveCollection = collection;
				return vm;
			}));
		}

		protected override void OnActivate()
		{
			using (var session = server.OpenSession())
			{
				WorkStarted("fetching collections");
				ExecuteCollectionQueryWithRetry(session, 5);

				WorkStarted("fetching recent documents");
				session.Advanced.AsyncDatabaseCommands
					.GetDocumentsAsync(0, 12)
					.ContinueOnSuccess(x =>
										{
											RecentDocuments = new BindableCollection<DocumentViewModel>(x.Result.Select(jdoc => new DocumentViewModel(jdoc)));
											NotifyOfPropertyChange(() => RecentDocuments);
											WorkCompleted("fetching recent documents");
										});
			}
		}

		private void ExecuteCollectionQueryWithRetry(IAsyncDocumentSession session, int retry)
		{
			session.Advanced.AsyncDatabaseCommands
				.GetCollectionsAsync(0, 25)
				.ContinueWith(task =>
				{
					if(task.Exception != null && retry > 0)
					{
						TaskEx.Delay(50)
							.ContinueWith(_ => ExecuteCollectionQueryWithRetry(session, retry - 1));
						return;
					}

					task.ContinueOnSuccess(x =>
					{
						Collections = x.Result;
						NotifyOfPropertyChange(() => LargestCollectionCount);
						NotifyOfPropertyChange(() => Collections);
						WorkCompleted("fetching collections");
					});
				});
		}
	}
}