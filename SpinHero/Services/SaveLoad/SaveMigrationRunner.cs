using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SaveLoad
{
	public sealed class SaveMigrationRunner
	{
		private const string VersionKey = "Version";

		private readonly (int Version, Action<JObject> Apply)[] _steps;
		private readonly float _energyRegenSeconds;

		public SaveMigrationRunner(float energyRegenSeconds)
		{
			_energyRegenSeconds = energyRegenSeconds;
			_steps = new (int, Action<JObject>)[]
			{
				(2, RestoreSessionTimers),
				(3, MergeGuaranteeIndexes),
				(4, ResetSymbolQueues),
				(5, MoveWheelToWheelList),
				(6, CreateChapterRewards),
				(7, RenameHeroes),
			};
		}

		public int CurrentVersion => _steps[^1].Version;

		public string MigrateIfNeeded(string rawJson, out bool changed)
		{
			changed = false;
			if (string.IsNullOrEmpty(rawJson) || !TryParse(rawJson, out var root))
				return rawJson;

			var version = root[VersionKey]?.Value<int?>() ?? 0;
			if (version >= CurrentVersion || !TryApplySteps(root, version))
				return rawJson;

			root[VersionKey] = CurrentVersion;
			changed = true;
			Debug.Log($"[SaveMigrationRunner] migrated save from v{version} to v{CurrentVersion}");
			return root.ToString();
		}

		private bool TryParse(string rawJson, out JObject root)
		{
			try
			{
				root = JObject.Parse(rawJson);
				return true;
			}
			catch (Exception exception)
			{
				Debug.LogError($"[SaveMigrationRunner] failed to parse save for migration: {exception.Message}");
				root = null;
				return false;
			}
		}

		private bool TryApplySteps(JObject root, int version)
		{
			try
			{
				foreach (var step in _steps)
					if (version < step.Version)
						step.Apply(root);

				return true;
			}
			catch (Exception exception)
			{
				Debug.LogError($"[SaveMigrationRunner] migration from v{version} failed, save is kept as is: {exception.Message}");
				return false;
			}
		}

		private void RestoreSessionTimers(JObject root)
		{
			var now = DateTime.UtcNow;
			RestoreEnergyTimer(root, now);

			if (ReadDateTime(root["LastSessionTime"]) == default)
				root["LastSessionTime"] = now;
		}

		private void MergeGuaranteeIndexes(JObject root)
		{
			if (root["ItemsSave"] is not JObject itemsSave)
				return;

			var granted = 0;
			if (itemsSave["GuaranteeIndexes"] is JObject oldIndexes)
			{
				foreach (var pair in oldIndexes)
					granted += pair.Value?.Value<int?>() ?? 0;

				itemsSave.Remove("GuaranteeIndexes");
			}

			itemsSave["GuaranteeIndex"] = granted;
		}

		private void ResetSymbolQueues(JObject root)
		{
			if (root["SymbolsSave"] is not JObject symbolsSave)
				return;

			symbolsSave.Remove("GuaranteeSymbolIndex");
			symbolsSave["QueueIndexes"] = new JObject();
		}

		private void MoveWheelToWheelList(JObject root)
		{
			if (root["WheelSave"] is not JObject legacyWheelSave)
				return;

			var wheelSaves = root["WheelSaves"] as JObject ?? new JObject();
			if (wheelSaves["Wheel1"] == null)
				wheelSaves["Wheel1"] = legacyWheelSave;

			root["WheelSaves"] = wheelSaves;
			root.Remove("WheelSave");
		}

		private void CreateChapterRewards(JObject root)
		{
			var maxLevelReached = root["ProgressSave"]?["MaxLevelReached"]?.Value<int?>() ?? 0;
			root["ChapterRewardsSave"] = new JObject { ["ClaimedThroughLevel"] = maxLevelReached - 1 };
		}

		private void RenameHeroes(JObject root)
		{
			var heroes = root["HeroesSave"]?["Heroes"] as JObject;
			RenameHero(root, heroes, "Succubus", "Bomber");
			RenameHero(root, heroes, "Archer", "Bum");
			RenameHero(root, heroes, "Jester", "Joker");
		}

		private void RenameHero(JObject root, JObject heroes, string from, string to)
		{
			RenameKey(heroes, from, to);
			RenameKeysDeep(root, $"HeroShard{from}", $"HeroShard{to}");
		}

		private void RenameKeysDeep(JToken token, string from, string to)
		{
			if (token is JObject obj)
			{
				RenameKey(obj, from, to);
				foreach (var property in obj.Properties())
					RenameKeysDeep(property.Value, from, to);
			}
			else if (token is JArray array)
			{
				foreach (var item in array)
					RenameKeysDeep(item, from, to);
			}
		}

		private void RenameKey(JObject obj, string from, string to)
		{
			var property = obj?.Property(from);
			if (property == null || obj.Property(to) != null)
				return;

			property.Replace(new JProperty(to, property.Value));
		}

		private void RestoreEnergyTimer(JObject root, DateTime now)
		{
			if (ReadDateTime(root["NextEnergyTime"]) != default)
				return;

			var elapsed = root["EnergyTimerElapsed"]?.Value<float?>() ?? 0f;
			if (elapsed > 0f)
				root["NextEnergyTime"] = now.AddSeconds(_energyRegenSeconds - elapsed);
		}

		private DateTime ReadDateTime(JToken token) => token?.Value<DateTime?>() ?? default;
	}
}
