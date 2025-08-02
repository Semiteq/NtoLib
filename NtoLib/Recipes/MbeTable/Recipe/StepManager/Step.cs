using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.PropertyDataType;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe.StepManager
{
    public class Step : IReadOnlyStep
    {
        public int NestingLevel { get; set; } = 0;

        public IReadOnlyDictionary<ColumnKey, PropertyWrapper> ReadOnlyStep => _step;
        private Dictionary<ColumnKey, PropertyWrapper> _step;

        private readonly ActionManager _actionManager;
        private readonly List<DependencyRule> _rules;
        private readonly HashSet<ColumnKey> _linkedColumns;
        private readonly ActionEntry[] _smoothActions;

        public event Action<ColumnKey> StepPropertyChanged;

        public Step(ActionManager actionManager)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            _step = new Dictionary<ColumnKey, PropertyWrapper>();

            _smoothActions = new[] { _actionManager.PowerSmooth, _actionManager.TemperatureSmooth };

            _rules = new List<DependencyRule>
            {
                new DependencyRule(
                    triggerKeys: new[] { ColumnKey.Speed, ColumnKey.InitialValue, ColumnKey.Setpoint },
                    outputKey: ColumnKey.Duration,
                    calculation: StepCalculationLogic.CalculateDurationFromSpeed),

                new DependencyRule(
                    triggerKeys: new[] { ColumnKey.Duration },
                    outputKey: ColumnKey.Speed,
                    calculation: StepCalculationLogic.CalculateSpeedFromDuration)
            };

            _linkedColumns = new HashSet<ColumnKey>(_rules.SelectMany(r => r.TriggerKeys).Union(_rules.Select(r => r.OutputKey)));
            ValidateRules();
        }
        
        public void InitializeProperties(Dictionary<ColumnKey, PropertyWrapper> initialProperties)
        {
            _step = initialProperties ?? throw new ArgumentNullException(nameof(initialProperties));
        }

        public bool TryUpdatePropertyAndDependencies(ColumnKey columnKey, object value, out ColumnKey[] affectedKeys, out string errorString)
        {
            var affectedKeysList = new List<ColumnKey> { columnKey };

            if (!_step[columnKey].TryChangeValue(value, out var newChangedWrapper, out errorString))
            {
                affectedKeys = Array.Empty<ColumnKey>();
                return false;
            }

            var pendingChanges = new Dictionary<ColumnKey, PropertyWrapper>
            {
                [columnKey] = newChangedWrapper
            };

            if (!TryCalculateDependencies(columnKey, pendingChanges, affectedKeysList, out errorString))
            {
                affectedKeys = Array.Empty<ColumnKey>();
                return false;
            }

            ApplyChanges(pendingChanges);
            affectedKeys = affectedKeysList.ToArray();
            errorString = string.Empty;
            return true;
        }

        private bool TryCalculateDependencies(ColumnKey triggerKey, Dictionary<ColumnKey, PropertyWrapper> pendingChanges, 
            List<ColumnKey> affectedKeys, out string errorString)
        {
            if (!IsRecalculationRequired(triggerKey))
            {
                errorString = string.Empty;
                return true;
            }

            var affectedRules = GetAffectedRules(triggerKey);

            foreach (var rule in affectedRules)
            {
                if (!TryApplyRule(rule, pendingChanges, out errorString))
                    return false;

                if (!affectedKeys.Contains(rule.OutputKey))
                    affectedKeys.Add(rule.OutputKey);
            }

            errorString = string.Empty;
            return true;
        }

        private bool TryApplyRule(DependencyRule rule, Dictionary<ColumnKey, PropertyWrapper> pendingChanges, out string errorString)
        {
            var calculationContext = CreateCalculationContext(rule.TriggerKeys, pendingChanges);
            var targetProperty = _step[rule.OutputKey];

            if (!rule.TryApply(calculationContext, targetProperty, out var newDependentWrapper, out errorString))
                return false;

            pendingChanges[rule.OutputKey] = newDependentWrapper;
            return true;
        }

        private IReadOnlyDictionary<ColumnKey, PropertyWrapper> CreateCalculationContext(HashSet<ColumnKey> requiredKeys, 
            Dictionary<ColumnKey, PropertyWrapper> pendingChanges)
        {
            var context = new Dictionary<ColumnKey, PropertyWrapper>();

            foreach (var key in requiredKeys)
            {
                context[key] = pendingChanges.ContainsKey(key) ? pendingChanges[key] : _step[key];
            }

            return context;
        }

        private IEnumerable<DependencyRule> GetAffectedRules(ColumnKey triggerKey)
        {
            return _rules.Where(rule => rule.TriggerKeys.Contains(triggerKey));
        }

        private bool IsRecalculationRequired(ColumnKey changedKey)
        {
            var actionValue = _step[ColumnKey.Action].GetValue<int>();
            var actionEntry = _actionManager.GetActionEntryById(actionValue);

            return _smoothActions.Contains(actionEntry) && _linkedColumns.Contains(changedKey);
        }

        private void ValidateRules()
        {
            var outputKeys = _rules.Select(r => r.OutputKey).ToList();
            var duplicates = outputKeys.GroupBy(x => x).Where(g => g.Count() > 1);

            if (duplicates.Any())
            {
                var conflictingKeys = string.Join(", ", duplicates.Select(g => g.Key));
                throw new InvalidOperationException($"Конфликт правил: несколько правил пытаются изменить {conflictingKeys}");
            }
        }

        private void ApplyChanges(Dictionary<ColumnKey, PropertyWrapper> changes)
        {
            foreach (var change in changes)
            {
                _step[change.Key] = change.Value;
                StepPropertyChanged?.Invoke(change.Key);
            }
        }

        public PropertyWrapper GetProperty(ColumnKey columnKey)
        {
            if (!_step.TryGetValue(columnKey, out var propertyWrapper))
                throw new KeyNotFoundException($"ColumnKey {columnKey} not found in step properties.");

            return propertyWrapper;
        }
    }
}