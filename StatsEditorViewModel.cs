using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StarRuptureSaveEditor.ViewModels;

/// <summary>
/// Enables editing of player skill progression and survival values.
/// </summary>
public partial class StatsEditorViewModel : ObservableObject
{
    private readonly Action _onModified;
    private JsonArray? _skillsArray;
    private JsonObject? _survivalNode;
    private string? _playerId;

    private static readonly string[] _targetSkills = new[] { "Movement", "Combat", "Survival" };
    private static readonly string[] _survivalStatKeys = new[]
    {
        "health",
        "energy",
        "shield",
        "hydration",
        "calories",
        "toxicity",
        "radiation",
        "heat",
        "drain",
        "corrosion",
        "infection",
        "oxygen",
        "medToolCharge",
        "grenadeCharge",
        "movementSpeedMultiplier"
    };

    [ObservableProperty]
    private string _statusMessage = "No player data loaded";

    public StatsEditorViewModel(Action onModified)
    {
        _onModified = onModified;
        Skills = new ObservableCollection<SkillProgressViewModel>();
        SurvivalStats = new ObservableCollection<SurvivalStatViewModel>();
    }

    public ObservableCollection<SkillProgressViewModel> Skills { get; }
    public ObservableCollection<SurvivalStatViewModel> SurvivalStats { get; }

    public string? PlayerId => _playerId;

    public void LoadFromJson(JsonNode? root)
    {
        Skills.Clear();
        SurvivalStats.Clear();
        _skillsArray = null;
        _survivalNode = null;
        _playerId = null;

        if (root == null)
        {
            StatusMessage = "No data loaded";
            OnPropertyChanged(nameof(PlayerId));
            return;
        }

        var allPlayers = root["itemData"]?["GameStateData"]?["allCharactersBaseSaveData"]?["allPlayersSaveData"]?.AsObject();
        if (allPlayers == null || allPlayers.Count == 0)
        {
            StatusMessage = "No player data found in save file";
            OnPropertyChanged(nameof(PlayerId));
            return;
        }

        var firstPlayer = allPlayers.First();
        _playerId = firstPlayer.Key;

        var progressionState = firstPlayer.Value?["playerProgressionState"]?.AsObject();
        var survivalData = firstPlayer.Value?["survivalData"]?.AsObject();
        _skillsArray = progressionState?["skills"]?.AsArray();
        _survivalNode = survivalData;

        foreach (var skillName in _targetSkills)
        {
            int level = 0;
            double experience = 0;

            if (_skillsArray != null)
            {
                foreach (var entry in _skillsArray)
                {
                    var entryObj = entry?.AsObject();
                    if (entryObj == null) continue;

                    if (entryObj.TryGetPropertyValue("skill", out var nameNode) &&
                        nameNode?.GetValue<string>() == skillName)
                    {
                        level = entryObj["level"]?.GetValue<int>() ?? 0;
                        experience = entryObj["experience"]?.GetValue<double>() ?? 0;
                        break;
                    }
                }
            }

            Skills.Add(new SkillProgressViewModel(skillName, level, experience, OnChildModified));
        }

        foreach (var statName in _survivalStatKeys)
        {
            double current = 0;
            double min = 0;
            double max = 0;

            var statNode = _survivalNode?[statName]?.AsObject();
            if (statNode != null)
            {
                current = statNode["current"]?.GetValue<double>() ?? current;
                min = statNode["min"]?.GetValue<double>() ?? min;
                max = statNode["max"]?.GetValue<double>() ?? max;
            }

            SurvivalStats.Add(new SurvivalStatViewModel(statName, current, min, max, OnChildModified));
        }

        StatusMessage = $"Loaded stats for {(_playerId ?? "Unknown player")}";
        OnPropertyChanged(nameof(PlayerId));
    }

    public void ApplyToJson(JsonNode root)
    {
        if (_skillsArray != null)
        {
            foreach (var skill in Skills)
            {
                JsonObject? target = null;

                foreach (var entry in _skillsArray)
                {
                    var entryObj = entry?.AsObject();
                    if (entryObj == null) continue;

                    if (entryObj.TryGetPropertyValue("skill", out var nameNode) &&
                        nameNode?.GetValue<string>() == skill.SkillName)
                    {
                        target = entryObj;
                        break;
                    }
                }

                if (target != null)
                {
                    target["level"] = JsonValue.Create(skill.Level);
                    target["experience"] = JsonValue.Create(skill.Experience);
                }
                else
                {
                    var newEntry = new JsonObject
                    {
                        ["skill"] = JsonValue.Create(skill.SkillName),
                        ["level"] = JsonValue.Create(skill.Level),
                        ["experience"] = JsonValue.Create(skill.Experience)
                    };
                    _skillsArray.Add(newEntry);
                }
            }
        }

        if (_survivalNode != null)
        {
            foreach (var stat in SurvivalStats)
            {
                var statObject = _survivalNode[stat.StatName]?.AsObject();
                if (statObject != null)
                {
                    statObject["current"] = JsonValue.Create(stat.Current);
                    statObject["min"] = JsonValue.Create(stat.Min);
                    statObject["max"] = JsonValue.Create(stat.Max);
                }
                else
                {
                    _survivalNode[stat.StatName] = new JsonObject
                    {
                        ["current"] = JsonValue.Create(stat.Current),
                        ["min"] = JsonValue.Create(stat.Min),
                        ["max"] = JsonValue.Create(stat.Max)
                    };
                }
            }
        }
    }

    private void OnChildModified()
    {
        _onModified();
    }
}

/// <summary>
/// ViewModel for a single skill entry.
/// </summary>
public partial class SkillProgressViewModel : ObservableObject
{
    private readonly Action _onModified;

    [ObservableProperty]
    private int _level;

    [ObservableProperty]
    private double _experience;

    public SkillProgressViewModel(string skillName, int level, double experience, Action onModified)
    {
        SkillName = skillName;
        _level = level;
        _experience = experience;
        _onModified = onModified;
    }

    public string SkillName { get; }

    partial void OnLevelChanged(int value)
    {
        _onModified();
    }

    partial void OnExperienceChanged(double value)
    {
        _onModified();
    }
}

/// <summary>
/// ViewModel for a single survival stat value.
/// </summary>
public partial class SurvivalStatViewModel : ObservableObject
{
    private readonly Action _onModified;

    [ObservableProperty]
    private double _current;

    [ObservableProperty]
    private double _min;

    [ObservableProperty]
    private double _max;

    public SurvivalStatViewModel(string statName, double current, double min, double max, Action onModified)
    {
        StatName = statName;
        _current = current;
        _min = min;
        _max = max;
        _onModified = onModified;
    }

    public string StatName { get; }

    partial void OnCurrentChanged(double value)
    {
        _onModified();
    }

    partial void OnMinChanged(double value)
    {
        _onModified();
    }

    partial void OnMaxChanged(double value)
    {
        _onModified();
    }
}
