SevenDigital.Messaging
======================

A distributed contracts-based sender/handler messaging system built on RabbitMQ and MassTransit

Path of Least Resistence
========================
* Install Erlang [http://www.erlang.org/download.html] and RabbitMQ server [http://www.rabbitmq.com/download.html]
* Add all references from `/binaries`, call `new MessagingConfiguration().WithDefaults()` in your app startup.
* Use ObjectFactory to get a new `INodeFactory` instance
* To listen to messages, `factory.Listener().Handle<IMyMessageInterface>().With<MyHandlerType>()`
* Each listener can handle any number of message=> handler pairs, and a message can have more than one handler (they all fire in parallel)
* To send a message, `factory.Sender().SendMessage(new MyMessage())`

Notes
-----
* Creating listener nodes takes time and resources. Do it infrequently -- usually one at the start of your app.
* Your handler will get `new()`'d for every message. Don't do heavy things in the handler!

Moving Parts
============

binaries
--------
These are all the projects and their dependencies.
You may wish to pull the non-SevenDigital dependencies from NuGet.

SevenDigital.Messaging
----------------------
The core abstraction over RabbitMQ / MassTransit. Your starting point is INodeFactory.
Call `ConfigureMessaging.WithDefaults().AndMessagingServer("my.rabbitserver.com")` when your app starts.

SevenDigital.Messaging.Types
----------------------------
Holds the IMessage contract separately, so you don't have to include the whole system to define contracts.
