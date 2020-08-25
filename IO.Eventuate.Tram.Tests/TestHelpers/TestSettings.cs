namespace IO.Eventuate.Tram.Tests
{
	/// <summary>
	/// Test configuration settings
	/// </summary>
	public static class TestSettings
	{
		public static string KafkaBootstrapServers { get; set; } =  "kafka:29092";
		public static string EventuateTramDbConnection { get; set; } = "Server=mssql,1433;Database=TramDb;User Id=sa;Password=App@Passw0rd";
		public static string EventuateTramDbSchema { get; set; } = "eventuate";
	}

}