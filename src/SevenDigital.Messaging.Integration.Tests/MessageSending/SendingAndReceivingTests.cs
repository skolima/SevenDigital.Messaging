// ReSharper disable InconsistentNaming

using System.Configuration;
using System.IO;
using NUnit.Framework;
using SevenDigital.Messaging.Integration.Tests.MessageSending.BaseCases;
using SevenDigital.Messaging.Integration.Tests._Helpers;
using SevenDigital.Messaging.Routing;
using StructureMap;

namespace SevenDigital.Messaging.Integration.Tests.MessageSending
{
	/*
	 * What is this?
	 * =============
	 * 
	 * A suite of tests covering basic send/receive behaviour
	 * for all the various modes messaging can be started in:
	 *  - Default (RabbitMQ, Store-and-Forward), uses purging
	 *  - No Persist (RabbitMQ directly), uses purging
	 *  - Loopback mode (no RabbitMQ, no threading, no real queueing)
	 *  - Local queue (no RabbitMQ, Store-and-Forward, with real queueing and multi-process support)
	 *
	 */

	[TestFixture]
	public class SendingAndReceiving_WithDefaultQueue_Tests:SendingAndReceivingBase
	{
		public override void ConfigureMessaging()
		{
			var server = ConfigurationManager.AppSettings["rabbitServer"];
			MessagingSystem.Configure.WithDefaults().SetMessagingServer(server).SetIntegrationTestMode();

			ObjectFactory.Configure(map=>map.For<IUniqueEndpointGenerator>().Use<TestEndpointGenerator>());
		}
	}
	
	[TestFixture]
	public class SendingAndReceiving_WithNonPersistentQueue_Tests : SendingAndReceivingBase
	{
		public override void ConfigureMessaging()
		{
			var server = ConfigurationManager.AppSettings["rabbitServer"];
			MessagingSystem.Configure.WithDefaults().NoPersistentMessages()
				.SetMessagingServer(server).SetIntegrationTestMode();

			ObjectFactory.Configure(map=>map.For<IUniqueEndpointGenerator>().Use<TestEndpointGenerator>());
		}
	}
	
	[TestFixture]
	public class SendingAndReceiving_WithLoopbackMode_Tests : SendingAndReceivingBase
	{
		public override void ConfigureMessaging()
		{
			MessagingSystem.Configure.WithLoopbackMode();

			ObjectFactory.Configure(map=>map.For<IUniqueEndpointGenerator>().Use<TestEndpointGenerator>());
		}
	}
}