using System.Collections.Generic;

namespace FreeSpace2TranslationTools.Services
{
	internal static class ShipTypes
	{
		public static readonly Dictionary<string, string> Types = new() {
			{ "support", "Support" },
			{ "cargo", "Cargo" },
			{ "fighter", "Fighter" },
			{ "bomber", "Bomber" },
			{ "cruiser", "Cruiser" },
			{ "freighter", "Freighter" },
			{ "capital", "Destroyer" },
			{ "transport", "Transport" },
			{ "navbuoy", "Nav Buoy" },
			{ "sentrygun", "Sentry Gun" },
			{ "escapepod", "Escape Pod" },
			{ "supercap", "Super Destroyer" },
			{ "drydock", "Dry Dock" },
			{ "corvette", "Corvette" },
			{ "gas miner", "Gas Miner" },
			{ "awacs", "Awacs" },
			{ "knossos", "Knossos" }
		};
	}
}
