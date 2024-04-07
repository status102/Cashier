using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TradeRecorder.Model
{
	public class Opcodes
	{
		[JsonPropertyName("version")]
		public string Version { get; set; }
		[JsonPropertyName("region")]
		public string Region { get; set; }
		[JsonPropertyName("lists")]
		public OpcodeList Lists { get; set; } = new();

		public class OpcodeList
		{
			[JsonPropertyName("ServerZoneIpcType")]
			public IList<OpcodePair> ServerZoneIpcType { get; set; } = new List<OpcodePair>();
			[JsonPropertyName("ClientZoneIpcType")]
			public IList<OpcodePair> ClientZoneIpcType { get; set; } = new List<OpcodePair>();
			[JsonPropertyName("ServerLobbyIpcType")]
			public IList<OpcodePair> ServerLobbyIpcType { get; set; } = new List<OpcodePair>();
			[JsonPropertyName("ClientLobbyIpcType")]
			public IList<OpcodePair> ClientLobbyIpcType { get; set; } = new List<OpcodePair>();
			[JsonPropertyName("ServerChatIpcType")]
			public IList<OpcodePair> ServerChatIpcType { get; set; } = new List<OpcodePair>();
			[JsonPropertyName("ClientChatIpcType")]
			public IList<OpcodePair> ClientChatIpcType { get; set; } = new List<OpcodePair>();
		}

		public class OpcodePair
		{
			[JsonPropertyName("name")]
			public string Name { get; set; }
			[JsonPropertyName("opcode")]
			public ushort Opcode { get; set; }
		}
	}
}
