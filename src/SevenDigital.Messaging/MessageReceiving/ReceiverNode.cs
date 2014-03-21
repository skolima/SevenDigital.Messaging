using System;
using System.Linq;
using DispatchSharp;
using DispatchSharp.WorkerPools;
using SevenDigital.Messaging.Base;
using SevenDigital.Messaging.Infrastructure;
using SevenDigital.Messaging.MessageReceiving.RabbitPolling;
using SevenDigital.Messaging.Routing;

namespace SevenDigital.Messaging.MessageReceiving
{
	/// <summary>
	/// Standard receiver node for Messaging.
	/// You do not need to create this yourself. Use `Messaging.Receiver()`
	/// </summary>
	/// <remarks>
	/// The receiver node binds a rabbit work queue to a message handler.
	/// When the user binds a message type to a handler, this gets added to the
	/// handler and the rabbit work queue.
	/// </remarks>
	public sealed class ReceiverNode : IReceiverNode
	{
		readonly IReceiverControl _parent;
		readonly IRoutingEndpoint _endpoint;
		readonly IDispatch<IPendingMessage<object>> _receivingDispatcher;
		readonly IHandlerManager _handler; // message type => [handler types]
		readonly ITypedPollingNode _pollingNode;
		readonly string _routingKey;

		/// <summary>
		/// Create a new message receiver node. You do not need to create this yourself. Use `Messaging.Receiver()`
		/// </summary>
		public ReceiverNode(IReceiverControl parent, IRoutingEndpoint endpoint, string routingKey,
			IHandlerManager handler, IPollingNodeFactory pollerFactory, IDispatcherFactory dispatchFactory)
		{
			_parent = parent;
			_endpoint = endpoint;
			_routingKey = routingKey;
			_handler = handler;
			_pollingNode = pollerFactory.Create(endpoint, routingKey);

			_receivingDispatcher = dispatchFactory.Create(
				_pollingNode,
				new ThreadedWorkerPool<IPendingMessage<object>>("SDMessaging_Receiver_" + endpoint));

			_receivingDispatcher.AddConsumer(HandleIncomingMessage);
			_receivingDispatcher.SetMaximumInflight(MessagingSystem.Concurrency);
		}

		/// <summary>
		/// Bind messages to handler types.
		/// </summary>
		public void Register(IBinding bindings)
		{
			var shouldStart = false;
			foreach (var binding in bindings.AllBindings())
			{
				shouldStart = true;
				Type messageType = binding.Item1, handlerType = binding.Item2;

				_pollingNode.AddMessageType(messageType);
				_handler.AddHandler(messageType, handlerType);
			}
			if (shouldStart) _receivingDispatcher.Start();
		}

		/// <summary>
		/// Gets the name of the destination queue used by messaging
		/// </summary>
		public string DestinationName { get { return _endpoint.ToString(); } }

		/// <summary>
		/// Remove a handler from all message bindings. The handler will no longer be called.
		/// </summary>
		/// <typeparam name="THandler">Type of hander previously bound with `Handle&lt;T&gt;().With&lt;THandler&gt;()`</typeparam>
		public void Unregister<THandler>()
		{
			_handler.RemoveHandler(typeof(THandler));
		}

		/// <summary>
		/// Set maximum number of concurrent handlers on this node
		/// </summary>
		public void SetConcurrentHandlers(int max)
		{
			_receivingDispatcher.SetMaximumInflight(max);
		}

		/// <summary>
		/// Handles messages received from messaging base
		/// </summary>
		public void HandleIncomingMessage(IPendingMessage<object> incoming)
		{
			_handler.TryHandle(incoming);
		}

		/// <summary>
		/// Stop this node and deregister from parent.
		/// </summary>
		public void Dispose()
		{
			_pollingNode.Stop();
			var handlers = _handler.GetMatchingHandlers(typeof(IMessage)).ToArray();
			foreach (var handler in handlers)
			{
				_handler.RemoveHandler(handler);
			}
			_receivingDispatcher.Stop();
			_parent.Remove(this);
		}

		#region Equality members

#pragma warning disable 1591
		private bool Equals(ReceiverNode other)
		{
			return Equals(_endpoint, other._endpoint) && string.Equals(_routingKey, other._routingKey);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ReceiverNode && Equals((ReceiverNode) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_endpoint != null ? _endpoint.GetHashCode() : 0)*397) ^ (_routingKey != null ? _routingKey.GetHashCode() : 0);
			}
		}
#pragma warning restore 1591

		#endregion
	}
}