﻿using SevenDigital.Messaging.MessageSending;
using SevenDigital.Messaging.Routing;
using StructureMap;

namespace SevenDigital.Messaging
{
	/// <summary>
	/// Configuration helper for structure map and SevenDigital.Messaging
	/// </summary>
	public class MessagingConfiguration
	{
		/// <summary>
		/// Configure SevenDigital.Messaging with defaults.
		/// After calling this method, you can use the INodeFactory as a collaborator.
		/// The default host is "localhost"
		/// </summary>
		public MessagingConfiguration WithDefaults()
		{
			ObjectFactory.Configure(map => {
				map.For<INodeFactory>().Singleton().Use<NodeFactory>();
				map.For<IMessagingHost>().Use(()=> new Host("localhost"));
				map.For<ISenderEndpointGenerator>().Use<SenderEndpointGenerator>();
				map.For<IUniqueEndpointGenerator>().Use<UniqueEndpointGenerator>();
				map.For<IServiceBusFactory>().Use<ServiceBusFactory>();
			});

			return this;
		}

		/// <summary>
		/// Configure target messaging host. This should be the IP or hostname of a server 
		/// running RabbitMQ service.
		/// </summary>
		/// <param name="host">IP or hostname of a server running RabbitMQ service</param>
		public MessagingConfiguration WithMessagingServer(string host)
		{
			ObjectFactory.Configure(map => map.For<IMessagingHost>().Use(()=> new Host(host)));
			return this;
		}

		/// <summary>
		/// Add an event hook the the messaging system
		/// </summary>
		public MessagingConfiguration AddEventHook<T>() where T : IEventHook
		{
			ObjectFactory.Configure(map => map.For<IEventHook>().Add<T>());
			return this;
		}

		/// <summary>
		/// Remove all event hooks from the event system
		/// </summary>
		public void ClearEventHooks()
		{
			ObjectFactory.EjectAllInstancesOf<IEventHook>();
		}
	}
}