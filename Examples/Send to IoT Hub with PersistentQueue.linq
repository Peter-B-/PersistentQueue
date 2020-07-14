<Query Kind="Program">
  <NuGetReference>Microsoft.Azure.Devices.Client</NuGetReference>
  <NuGetReference>PersistentQueue</NuGetReference>
  <Namespace>Microsoft.Azure.Devices.Client</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>Persistent.Queue</Namespace>
</Query>

async Task Main()
{
	var queueConfiguration = new PersistentQueueConfiguration(@"c:\temp\IoT Send Queue")
	{
		MinDequeueBatchSize = 1,
		MaxDequeueBatchSize = 100
	};
	
	using var queue = queueConfiguration.CreateQueue();

	var sendLoopTask = Task.Run(() => SendLoop(queue));
	var messageCreateTask = Task.Run(() => EnqueueMessages(queue));

	try
	{
		await sendLoopTask;
	}
	catch (TaskCanceledException)
	{	
		"Sendloop stopped".Dump();
	}

	try
	{
		await messageCreateTask;
	}
	catch (TaskCanceledException)
	{
		"Message creation stopped".Dump();
	}

	"Done".Dump();
}

async Task EnqueueMessages(PersistentQueue queue)
{
	var messageNo = 0;
	while (!this.QueryCancelToken.IsCancellationRequested)
	{
		var payload = new
		{
			ContentType = "TestMessage",
			UUID = Guid.NewGuid(),
			MachineName = "Simulation",
			Payload = new
			{
				No = messageNo++,
			},
			EdgeSendTime = DateTimeOffset.Now,
			v = 1
		};

		var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);

		queue.Enqueue(jsonBytes);

		await Task.Delay(TimeSpan.FromSeconds(5), this.QueryCancelToken);
	}

}

async Task SendLoop(PersistentQueue queue)
{
	var connectionString = Util.GetPassword("IoT Device connection string");
	using var client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp);

	while (!this.QueryCancelToken.IsCancellationRequested)
	{
		var dequeueResult = await queue.DequeueAsync(this.QueryCancelToken);

		var messages = dequeueResult.Items
			.Select(item => new Message(item.ToArray()))
			.ToList();

		try
		{
			await client.SendEventBatchAsync(messages, this.QueryCancelToken);

			dequeueResult.Commit();
			$"{dequeueResult.Items.Count} messages sent".Dump();
		}
		catch (Exception ex)
		{
			ex.Dump("Send failed");
			await Task.Delay(1000);
		}
	}
}

