﻿using Azure.Messaging.ServiceBus;
using EasyDesk.CleanArchitecture.Application.Events.ExternalEvents;
using EasyDesk.CleanArchitecture.Infrastructure.Events.ServiceBus;
using EasyDesk.CleanArchitecture.Infrastructure.Json;
using EasyDesk.CleanArchitecture.Infrastructure.Time;
using EasyDesk.Tools;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace EScooter.ScooterDataMock
{
    /// <summary>
    /// An event emitted when a scooter is registered to the system.
    /// </summary>
    public record ScooterCreated(Guid Id) : ExternalEvent;

    /// <summary>
    /// An event emitted when a scooter is deleted from the system.
    /// </summary>
    public record ScooterDeleted(Guid Id) : ExternalEvent;

    /// <summary>
    /// The main window of the application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IExternalEventPublisher _publisher;

        /// <summary>
        /// Creates a new instance of the main window.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables("ESCOOTER_")
                .AddUserSecrets(GetType().Assembly)
                .Build();

            var connectionString = config.GetValue<string>("AzureServiceBusSettings:ConnectionString");
            var settings = AzureServiceBusSenderDescriptor.Topic("development/service-events");
            var client = new ServiceBusClient(connectionString);
            var eventBusPublisher = new AzureServiceBusPublisher(client, settings);
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
            var serializer = new NewtonsoftJsonSerializer(serializerSettings);

            _publisher = new ExternalEventPublisher(eventBusPublisher, new MachineDateTime(), serializer);
        }

        private async Task UseScooterId(AsyncAction<Guid> action)
        {
            if (!Guid.TryParse(_txtScooterId.Text, out var id))
            {
                LogError("Invalid Scooter ID");
            }
            await action(id);
        }

        private void LogError(string message) => Log("ERROR", message);

        private void LogInfo(string message) => Log("INFO", message);

        private void Log(string prefix, string message)
        {
            var formattedMessage = $"[{DateTime.Now}] {prefix} - {message}";
            _lstLog.Items.Add(formattedMessage);
        }

        private async Task Publish(ExternalEvent ev)
        {
            try
            {
                await _publisher.Publish(ev);
                LogInfo($"Published event {ev}");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
        }

        private async void CreateClicked(object sender, RoutedEventArgs e)
        {
            await UseScooterId(id => Publish(new ScooterCreated(id)));
        }

        private async void CreateRandomClicked(object sender, RoutedEventArgs e)
        {
            await Publish(new ScooterCreated(Guid.NewGuid()));
        }

        private async void DeleteClicked(object sender, RoutedEventArgs e)
        {
            await UseScooterId(id => Publish(new ScooterDeleted(id)));
        }
    }
}
