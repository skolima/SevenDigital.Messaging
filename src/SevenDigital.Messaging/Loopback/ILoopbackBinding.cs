using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SevenDigital.Messaging.Loopback
{
	/// <summary>
	/// Container for bindings between handlers and messages
	/// </summary>
	public interface ILoopbackBinding : IEnumerable<KeyValuePair<TypeRoutingKeyPair, ConcurrentBag<Type>>>
	{
		/// <summary>
		/// List all handlers that have been registered on this node.
		/// </summary>
		IEnumerable<Type> ForMessage<T>(string routingKey = "");

		/// <summary>
		/// Return registered handlers for the exact message type
		/// </summary>
		ConcurrentBag<Type> this[TypeRoutingKeyPair msg] { get; set; }

		/// <summary>
		/// List all message types registered
		/// </summary>
		IEnumerable<TypeRoutingKeyPair> MessagesRegistered { get; }

		/// <summary>
		/// Test if the exact message type has been registered
		/// </summary>
		bool IsMessageRegistered(TypeRoutingKeyPair msg);

		/// <summary>
		/// Add a new message type with no handler bindings
		/// </summary>
		void AddMessageType(TypeRoutingKeyPair msg);
	}
}