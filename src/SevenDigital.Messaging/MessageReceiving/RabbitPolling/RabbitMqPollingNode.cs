using System;
using DispatchSharp;
using DispatchSharp.QueueTypes;
using SevenDigital.Messaging.Base;
using SevenDigital.Messaging.Base.Routing;
using SevenDigital.Messaging.Infrastructure;
using SevenDigital.Messaging.Logging;
using SevenDigital.Messaging.Routing;

namespace SevenDigital.Messaging.MessageReceiving.RabbitPolling
{
	/// <summary>
	/// A pull-based, polling, blocking RabbitMQ work item queue
	/// </summary>
	public class RabbitMqPollingNode : ITypedPollingNode
	{
		readonly string _endpoint;
		readonly IMessagingBase _messagingBase;
		readonly ISleepWrapper _sleeper;
		readonly ConcurrentSet<Type> _boundMessageTypes;
		readonly string _routingKey;

		/// <summary>
		/// Create a work item queue that will try to pull items from a named RabbitMQ endpoint
		/// </summary>
		/// <param name="endpoint">Destination endpoint to pull messages from</param>
		/// <param name="routingKey"></param>
		/// <param name="messagingBase">RabbitMQ connection provider</param>
		/// <param name="sleeper">Sleeper to rate limit polling</param>
		public RabbitMqPollingNode(IRoutingEndpoint endpoint, string routingKey, IMessagingBase messagingBase, ISleepWrapper sleeper)
		{
			_endpoint = endpoint.ToString();
			_messagingBase = messagingBase;
			_sleeper = sleeper;
			_boundMessageTypes = new ConcurrentSet<Type>();
			_routingKey = routingKey;
		}

		/// <summary>
		/// Not currently implemented. Will throw an exception.
		/// </summary>
		/// <remarks>This might be useful at some point to inject test messages?</remarks>
		public void Enqueue(IPendingMessage<object> work)
		{
			throw new InvalidOperationException("This queue self populates and doesn't currently support direct injection.");
		}

		/// <summary>
		/// Try and get an item from this queue. Success is encoded in the WQI result 'HasItem' 
		/// </summary>
		public IWorkQueueItem<IPendingMessage<object>> TryDequeue()
		{
			var msg = SleepingGetMessage();
			return
				msg == null
				? new WorkQueueItem<IPendingMessage<object>>()
				: new WorkQueueItem<IPendingMessage<object>>(msg, m => m.Finish(), m => m.Cancel());
		}

		/// <summary>
		/// Add a message type that might be bound to the endpoint. This
		/// is used to rebuild endpoints in case of failure.
		/// </summary>
		public void AddMessageType(Type type)
		{
			lock (_boundMessageTypes)
			{
				_boundMessageTypes.Add(type);
				TryRebuildQueues();
			}
		}

		/// <summary>
		/// Stop receiving messages
		/// </summary>
		public void Stop()
		{
			lock (_boundMessageTypes)
			{
				_boundMessageTypes.Clear();
			}
		}

		/// <summary>
		/// Approximate snapshot length 
		/// </summary>
		public int Length()
		{
			return 0;
		}

		/// <summary>
		/// Advisory method: block if the queue is waiting to be populated.
		/// </summary>
		public bool BlockUntilReady()
		{
			return true;
		}

		IPendingMessage<object> SleepingGetMessage()
		{
			if (_boundMessageTypes.Count < 1)
			{
				_sleeper.SleepMore();
				return null;
			}

			IPendingMessage<object> message = EnsureQueuesAndPollForMessage();

			if (message != null)
			{
				_sleeper.Reset();
			}
			else
			{
				_sleeper.SleepMore();
			}

			return message;
		}

		IPendingMessage<IMessage> EnsureQueuesAndPollForMessage()
		{
			try
			{
				return _messagingBase.TryStartMessage<IMessage>(_endpoint);
			}
			catch (Exception ex)
			{
				if (IsMissingQueue(ex))
				{
					TryRebuildQueues();

					return null; // will get messages next time
				}
				if (DoubleAck(ex))
				{
					Log.Warning("Messaging node reported an non-acknowledgable message. This may be a bug in SevenDigital.Messaging");
					return null;
				}
				throw;
			}
		}

		static bool DoubleAck(Exception exception)
		{
			var e = exception as RabbitMQ.Client.Exceptions.OperationInterruptedException;
			return (e != null)
				&& (e.ShutdownReason.ReplyCode == 406);
		}

		static bool IsMissingQueue(Exception exception)
		{
			var e = exception as RabbitMQ.Client.Exceptions.OperationInterruptedException;
			return (e != null)
				&& (e.ShutdownReason.ReplyCode == 404);
		}

		void TryRebuildQueues()
		{
			_messagingBase.ResetCaches();
			var boundTypes = _boundMessageTypes.ToArray();
			foreach (var sourceMessage in boundTypes)
			{
				_messagingBase.CreateDestination(sourceMessage, _endpoint, _routingKey, ExchangeType.Topic);
			}
		}
	}
}