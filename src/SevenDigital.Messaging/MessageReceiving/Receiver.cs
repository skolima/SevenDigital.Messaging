using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SevenDigital.Messaging.Base.Routing;
using SevenDigital.Messaging.Infrastructure;
using SevenDigital.Messaging.MessageReceiving.RabbitPolling;
using SevenDigital.Messaging.MessageReceiving.Testing;
using SevenDigital.Messaging.Routing;
using StructureMap;

namespace SevenDigital.Messaging.MessageReceiving
{
	/// <summary>
	/// Standard node factory for messaging.
	/// You don't need to create this yourself, use `MessagingSystem.Receiver()`
	/// </summary>
	/// <remarks>
	/// The Receiver is a factory class for ReceiverNodes. It provides
	/// the API point to decide to use a unique endpoint name (Listen) or
	/// a specific endpoint name (TakeFrom)
	/// </remarks>
	public sealed class Receiver : IReceiver, IReceiverControl, IReceiverTesting
	{
		readonly IUniqueEndpointGenerator _uniqueEndPointGenerator;
		ConcurrentBag<IReceiverNode> _registeredNodes;
		readonly IMessageRouter _messageRouter;
		readonly IPollingNodeFactory _pollerFactory;
		readonly IDispatcherFactory _dispatchFactory;
		readonly object _lockObject;

		/// <summary>
		/// Create a new node factory.
		/// You don't need to create this yourself, use `MessagingSystem.Receiver()`
		/// </summary>
		public Receiver(
			IUniqueEndpointGenerator uniqueEndPointGenerator,
			IMessageRouter messageRouter,
			IPollingNodeFactory pollerFactory,
			IDispatcherFactory dispatchFactory)
		{
			_messageRouter = messageRouter;
			_pollerFactory = pollerFactory;
			_dispatchFactory = dispatchFactory;
			_uniqueEndPointGenerator = uniqueEndPointGenerator;
			_lockObject = new object();
			_registeredNodes = new ConcurrentBag<IReceiverNode>();
			PurgeOnConnect = false;
			DeleteIntegrationEndpointsOnShutdown = false;
		}

		/// <summary>
		/// Map handlers to a listener on a named endpoint.
		/// All other listeners on this endpoint will compete for messages
		/// (i.e. only one listener will get a given message)
		/// </summary>
		public IReceiverNode TakeFrom(Endpoint endpoint, Action<IMessageBinding> bindings)
		{
			return TakeFrom(endpoint, string.Empty, bindings);
		}

		/// <summary>
		/// Map handlers to a listener on a named endpoint.
		/// All other listeners on this endpoint will compete for messages
		/// (i.e. only one listener will get a given message)
		/// </summary>
		public IReceiverNode TakeFrom(Endpoint endpoint, string routingKey, Action<IMessageBinding> bindings)
		{
			lock (_lockObject)
			{
				if (PurgeOnConnect) PurgeEndpoint(endpoint);

				var node = new ReceiverNode(
					this, endpoint, routingKey,
					ObjectFactory.GetInstance<IHandlerManager>(),
					_pollerFactory, _dispatchFactory);
				_registeredNodes.Add(node);

				var binding = new Binding();
				if (bindings != null) bindings(binding);
				node.Register(binding);

				return node;
			}
		}

		/// <summary>
		/// Map handlers to a listener on a unique endpoint.
		/// All listeners mapped this way will receive all messages.
		/// </summary>
		public IReceiverNode Listen(Action<IMessageBinding> bindings)
		{
			return Listen(string.Empty, bindings);
		}

		/// <summary>
		/// Map handlers to a listener on a unique endpoint.
		/// All listeners mapped this way will receive all messages.
		/// </summary>
		public IReceiverNode Listen(string routingKey, Action<IMessageBinding> bindings)
		{
			return TakeFrom(_uniqueEndPointGenerator.Generate(), routingKey, bindings);
		}

		/// <summary>
		/// Close all receiver nodes that have been created
		/// </summary>
		public void Shutdown()
		{
			lock (_lockObject)
			{
				foreach (var node in _registeredNodes)
				{
					node.Dispose();
				}
				_registeredNodes = new ConcurrentBag<IReceiverNode>();
				if (DeleteIntegrationEndpointsOnShutdown) DeleteIntegrationEndpoints();
			}
		}

		/// <summary>
		/// Unregister a node from the shutdown list
		/// </summary>
		public void Remove(IReceiverNode node)
		{
			lock (_lockObject)
			{
				var next = new ConcurrentBag<IReceiverNode>(_registeredNodes.Where(n=> !Equals(n, node)));
				_registeredNodes = next;
			}
		}

		/// <summary>
		/// Set maximum concurrent handlers per receiver node
		/// </summary>
		public void SetConcurrentHandlers(int max)
		{
			lock (_lockObject)
			{
				foreach (var node in _registeredNodes)
				{
					node.SetConcurrentHandlers(max);
				}
			}
		}

		/// <summary>
		/// Set purging policy. If true, all waiting messages are DELETED when a handler is registered.
		/// This setting is meant for integration tests.
		/// </summary>
		public bool PurgeOnConnect { get; set; }

		/// <summary>
		/// Set cleanup policy. If true, all endpoints generated in integration mode
		/// are deleted when the receiver is disposed.
		/// Default is false.
		/// </summary>
		public bool DeleteIntegrationEndpointsOnShutdown { get; set; }

		/// <summary>
		/// Shutdown all nodes.
		/// </summary>
		public void Dispose()
		{
			Shutdown();
		}

		void DeleteIntegrationEndpoints()
		{
			lock (_lockObject)
			{
				try
				{
					_messageRouter.RemoveRouting(DeleteNameFilter);
				}
				catch
				{
					Ignore();
				}
			}
		}

		static void Ignore() { }

		/// <summary>
		/// Returns true if a queue name would be deleted if 
		/// <see cref="DeleteIntegrationEndpointsOnShutdown"/> is set
		/// </summary>
		public bool DeleteNameFilter(string queueName)
		{
			var name = queueName.ToLowerInvariant();
			var delete = name.ToLowerInvariant().Contains(".integration.")
				   || name.EndsWith("sevendigital.messaging_listener")
				   || name.StartsWith("test_listener_");
			return delete;
		}

		void PurgeEndpoint(Endpoint endpoint)
		{
			_messageRouter.AddDestination(endpoint.ToString());
			_messageRouter.Purge(endpoint.ToString());
		}

		IEnumerable<IReceiverNode> IReceiverTesting.CurrentNodes()
		{
			return _registeredNodes;
		}
	}

	namespace Testing
	{
		/// <summary>
		/// Unit testing methods for IReceiver
		/// </summary>
		public interface IReceiverTesting
		{
			/// <summary> Registered node list </summary>
			IEnumerable<IReceiverNode> CurrentNodes();

			/// <summary>
			/// Returns true if a queue name would be deleted is DIEOS is set
			/// </summary>
			bool DeleteNameFilter(string queueName);

		}
	}
}