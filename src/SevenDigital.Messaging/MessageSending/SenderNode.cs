using System.Threading;
using DispatchSharp;
using DispatchSharp.WorkerPools;
using SevenDigital.Messaging.Base;
using SevenDigital.Messaging.Base.Routing;
using SevenDigital.Messaging.Base.Serialisation;
using SevenDigital.Messaging.Infrastructure;
using SevenDigital.Messaging.Logging;
using SevenDigital.Messaging.MessageReceiving;

namespace SevenDigital.Messaging.MessageSending
{
	/// <summary>
	/// Standard sender node for Messaging.
	/// You do not need to create this yourself. Use `MessagingSystem.Sender()`
	/// </summary>
	public sealed class SenderNode : ISenderNode
	{
		const int SingleThreaded = 1;
		readonly IMessagingBase _messagingBase;
		readonly ISleepWrapper _sleeper;
		readonly IOutgoingQueueFactory _queueFactory;
		IDispatch<byte[]> _sendingDispatcher;
		PersistentWorkQueue _persistentQueue;

		/// <summary>
		/// Create a new message sending node. You do not need to create this yourself.
		/// Use `MessagingSystem.Sender()`
		/// </summary>
		public SenderNode(
			IMessagingBase messagingBase,
			IDispatcherFactory dispatchFactory,
			ISleepWrapper sleeper,
			IOutgoingQueueFactory queueFactory
			)
		{
			_messagingBase = messagingBase;
			_sleeper = sleeper;
			_queueFactory = queueFactory;


			_persistentQueue = new PersistentWorkQueue(_queueFactory, _sleeper);

			_sendingDispatcher = dispatchFactory.Create(
				_persistentQueue,
				new ThreadedWorkerPool<byte[]>("SDMessaging_Sender")
			);

			_sendingDispatcher.SetMaximumInflight(SingleThreaded);
			_sendingDispatcher.AddConsumer(SendWaitingMessage);
			_sendingDispatcher.Exceptions += SendingExceptions;
			_sendingDispatcher.Start();
		}

		/// <summary>
		/// Handle exceptions thrown during sending.
		/// </summary>
		public void SendingExceptions(object sender, ExceptionEventArgs<byte[]> e)
		{
			_sleeper.SleepMore();
			e.WorkItem.Cancel();

			Log.Warning("Sender failed: " + e.SourceException.GetType() + "; " + e.SourceException.Message);
		}

		/// <summary>
		/// Send the given message. Does not guarantee reception.
		/// </summary>
		/// <param name="message">Message to send. This must be a serialisable type</param>
		public void SendMessage<T>(T message) where T : class, IMessage
		{
			SendMessage(message, string.Empty);
		}

		/// <summary>
		/// Send the given message. Does not guarantee reception.
		/// </summary>
		/// <param name="message">Message to send. This must be a serialisable type</param>
		/// <param name="routingKey">Routing key to use for delivery.</param>
		public void SendMessage<T>(T message, string routingKey) where T : class, IMessage
		{
			var prepared = _messagingBase.PrepareForSend(message, routingKey, ExchangeType.Topic);
			_sendingDispatcher.AddWork(prepared.ToBytes());
			HookHelper.TrySentHooks(message);
		}

		/// <summary>
		/// Internal immediate send. Use SendMessage() instead.
		/// </summary>
		public void SendWaitingMessage(byte[] message)
		{
			_messagingBase.SendPrepared(PreparedMessage.FromBytes(message));
			_sleeper.Reset();
		}

		/// <summary>
		/// Shutdown the sender
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_persistentQueue", Justification = "fields are disposed with an interlock")]
		public void Dispose()
		{
			var lDispatcher = Interlocked.Exchange(ref _sendingDispatcher, null);
			if (lDispatcher != null)
				lDispatcher.WaitForEmptyQueueAndStop(MessagingSystem.ShutdownTimeout);

			var lQueue = Interlocked.Exchange(ref _persistentQueue, null);
			if (lQueue != null)
				lQueue.Dispose();

			_queueFactory.Cleanup();
		}
	}
}