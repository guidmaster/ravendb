﻿using System;
using System.Threading;
using System.Transactions;
using Newtonsoft.Json.Linq;
using Raven.Client.Document;
using Xunit;
using TransactionInformation = Raven.Http.TransactionInformation;

namespace Raven.Tests.Bugs.DTC
{
	public class UsingDtcForDelete : LocalClientTest
	{
		private string documentKey;

		[Fact]
		public void ShouldWork()
		{
			using (var store = NewDocumentStore("esent", false))
			{
				documentKey = "tester123";

				var transactionInformation = new TransactionInformation
				{
					Id = Guid.NewGuid()
				};

				store.DocumentDatabase.Put(documentKey, null, new JObject(),
				                     JObject.Parse(
				                     	@"{
  ""Raven-Entity-Name"": ""MySagaDatas"",
  ""Raven-Clr-Type"": ""TestNServiceBusSagaWithRavenDb.MySagaData, TestNServiceBusSagaWithRavenDb"",
  ""Last-Modified"": ""Mon, 21 Mar 2011 19:59:58 GMT"",
  ""Non-Authoritive-Information"": false
}"), transactionInformation);
				store.DatabaseCommands.Commit(transactionInformation.Id);


				var deleteTx = new TransactionInformation
				{
					Id = Guid.NewGuid()
				};
				store.DocumentDatabase.Delete(documentKey, null, deleteTx);

				store.DocumentDatabase.Commit(deleteTx.Id);
			}
		}
	}
}