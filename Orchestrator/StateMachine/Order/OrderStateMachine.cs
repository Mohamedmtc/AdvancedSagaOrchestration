using Automatonymous;
using MassTransit;
using Messages.Order.Event;
using Orchestrator.Presistance;
using Orchestrator.StateMachine.Order.Command;
using Microsoft.Extensions.Logging; // Add this using statement
using System;

namespace Orchestrator.StateMachine.Order
{
    public sealed class OrderStateMachine : MassTransitStateMachine<OrderStateData>
    {
        private readonly ILogger<OrderStateMachine> _logger; // Add a logger field

        public State OrderStarted { get; private set; }
        public State OrderCancelled { get; private set; }
        public State OrderFinished { get; private set; }

        public Event<IOrderStartedEvent> OrderStartedEvent { get; private set; }
        public Event<IOrderCanceledEvent> OrderCancelledEvent { get; private set; }
        public Event<IOrderFinishedEvent> OrderFinishedEvent { get; private set; }

        public OrderStateMachine(ILogger<OrderStateMachine> logger) // Inject the logger through the constructor
        {
            _logger = logger; // Initialize the logger field

            Event(() => OrderStartedEvent, x => x.CorrelateById(context => context.Message.OrderId));
            Event(() => OrderCancelledEvent, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderFinishedEvent, x => x.CorrelateById(m => m.Message.OrderId));
            InstanceState(x => x.CurrentState);

            Initially(
               When(OrderStartedEvent)
                .Then(context =>
                {
                    context.Instance.OrderCreationDateTime = DateTime.Now;
                    context.Instance.OrderId = context.Data.OrderId;
                    context.Instance.PaymentCardNumber = context.Data.PaymentCardNumber;
                    context.Instance.ProductName = context.Data.ProductName;
                    context.Instance.IsCanceled = context.Data.IsCanceled;

                    // Log the step
                    _logger.LogInformation($"Order Started - OrderId: {context.Data.OrderId}");
                })
               .TransitionTo(OrderStarted)
                .Publish(context => new CheckOrderStateCommand(context.Instance)));

            During(OrderStarted,
               When(OrderCancelledEvent)
                   .Then(context =>
                   {
                       context.Instance.Exception = context.Data.ExceptionMessage;
                       context.Instance.OrderCancelDateTime = DateTime.Now;

                       // Log the step
                       _logger.LogInformation($"Order Cancelled - OrderId: {context.Instance.OrderId}");
                   })
                    .TransitionTo(OrderCancelled));

            During(OrderStarted,
                When(OrderFinishedEvent)
                    .Then(context =>
                    {
                        context.Instance.OrderFinishedDateTime = DateTime.Now;

                        // Log the step
                        _logger.LogInformation($"Order Finished - OrderId: {context.Instance.OrderId}");
                    })
                     .TransitionTo(OrderFinished)
                     .Finalize());

            SetCompletedWhenFinalized();
        }
    }
}
