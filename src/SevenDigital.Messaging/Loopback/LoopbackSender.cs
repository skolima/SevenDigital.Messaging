using System;

namespace SevenDigital.Messaging.Loopback
{
	/// <summary>
	/// Loopback sender node
	/// </summary>
	public sealed class LoopbackSender : ISenderNode
	{
		readonly ILoopbackReceiver _loopbackReceiver;

		/// <summary>
		/// Create a loopback node.
		/// You shouldn't create this yourself.
		/// Use `Messaging.Sender()` in loopback mode
		/// </summary>
		public LoopbackSender(IReceiver loopbackReceiver)
		{
			_loopbackReceiver = loopbackReceiver as ILoopbackReceiver;
			if (_loopbackReceiver == null) throw new Exception("Tried to start a loopback sender outside of loopback mode");
		}

		/// <summary>
		/// Send the given message. Does not guarantee reception.
		/// </summary>
		/// <param name="message">Message to be send. This must be a serialisable type</param>
		public void SendMessage<T>(T message) where T : class, IMessage
		{
			SendMessage(message, string.Empty);
		}

		/// <summary>
		/// Send the given message. Does not guarantee reception.
		/// </summary>
		/// <param name="message">Message to be send. This must be a serialisable type</param>
		/// <param name="routingKey">Routing key to use to send the message</param>
		public void SendMessage<T>(T message, string routingKey) where T : class, IMessage
		{
			_loopbackReceiver.Send(message, routingKey);
		}

		/// <summary> No action </summary>
		public void Dispose() { }
	}
}