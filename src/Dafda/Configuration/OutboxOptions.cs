using System;
using System.Collections.Generic;
using Dafda.Outbox;
using Dafda.Producing;
using Microsoft.Extensions.DependencyInjection;

namespace Dafda.Configuration
{
    public sealed class OutboxOptions
    {
        private readonly IServiceCollection _services;
        private readonly OutgoingMessageRegistry _outgoingMessageRegistry;
        private readonly TopicPayloadSerializerRegistry _topicPayloadSerializerRegistry = new TopicPayloadSerializerRegistry(() => new DefaultPayloadSerializer());

        private MessageIdGenerator _messageIdGenerator = MessageIdGenerator.Default;
        private IOutboxNotifier _notifier = new DoNotNotify();

        internal OutboxOptions(IServiceCollection services, OutgoingMessageRegistry outgoingMessageRegistry)
        {
            _services = services;
            _outgoingMessageRegistry = outgoingMessageRegistry;
        }

        public void WithMessageIdGenerator(MessageIdGenerator messageIdGenerator)
        {
            _messageIdGenerator = messageIdGenerator;
        }

        public void Register<T>(string topic, string type, Func<T, string> keySelector) where T : class
        {
            _outgoingMessageRegistry.Register(topic, type, keySelector);
        }

        public void WithOutboxMessageRepository<T>() where T : class, IOutboxMessageRepository
        {
            _services.AddTransient<IOutboxMessageRepository, T>();
        }

        public void WithOutboxMessageRepository(Func<IServiceProvider, IOutboxMessageRepository> implementationFactory)
        {
            _services.AddTransient(implementationFactory);
        }

        public void WithNotifier(IOutboxNotifier notifier)
        {
            _notifier = notifier;
        }

        public void WithDefaultPayloadSerializer(IPayloadSerializer payloadSerializer)
        {
            WithDefaultPayloadSerializer(() => payloadSerializer);
        }

        public void WithDefaultPayloadSerializer(Func<IPayloadSerializer> payloadSerializerFactory)
        {
            _topicPayloadSerializerRegistry.SetDefaultPayloadSerializer(payloadSerializerFactory);
        }

        public void WithPayloadSerializer(string topic, IPayloadSerializer payloadSerializer)
        {
            WithPayloadSerializer(topic, () => payloadSerializer);
        }

        public void WithPayloadSerializer(string topic, Func<IPayloadSerializer> payloadSerializerFactory)
        {
            _topicPayloadSerializerRegistry.Register(topic, payloadSerializerFactory);
        }

        internal OutboxConfiguration Build()
        {
            return new OutboxConfiguration(_messageIdGenerator, _notifier, _topicPayloadSerializerRegistry);
        }

        private class DoNotNotify : IOutboxNotifier
        {
            public void Notify()
            {
            }
        }
    }

    internal sealed class TopicPayloadSerializerRegistry
    {
        private readonly Dictionary<string, Func<IPayloadSerializer>> _serializerFactories = new Dictionary<string, Func<IPayloadSerializer>>();
        private Func<IPayloadSerializer> _defaultPayloadSerializerFactory;

        public TopicPayloadSerializerRegistry(Func<IPayloadSerializer> defaultPayloadSerializerFactory)
        {
            _defaultPayloadSerializerFactory = defaultPayloadSerializerFactory;
        }

        public void Register(string topic, Func<IPayloadSerializer> serializerFactory)
        {
            _serializerFactories.Add(topic, serializerFactory);
        }

        public void SetDefaultPayloadSerializer(Func<IPayloadSerializer> factory)
        {
            _defaultPayloadSerializerFactory = factory;
        }

        public IPayloadSerializer Get(string topic)
        {
            if (_serializerFactories.TryGetValue(topic, out var factory))
            {
                return factory();
            }

            return _defaultPayloadSerializerFactory();
        }
    }
}