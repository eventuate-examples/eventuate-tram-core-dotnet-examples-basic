namespace IO.Eventuate.Tram.Tests
{
	/// <summary>
	/// Test configuration settings
	/// </summary>
	public static class TestSettings
	{
		public static string KafkaBootstrapServers { get; set; } = "kafka:9092";
		public static string EventuateTramDbConnection { get; set; } = "Server=LAPTOP-7G6A2VOR;Database=TramDb;User Id=sa;Password=Sa@123";
		public static string EventuateTramDbSchema { get; set; } = "eventuate";
	}

}