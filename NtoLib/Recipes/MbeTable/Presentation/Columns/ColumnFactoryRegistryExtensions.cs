namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// Registers all built-in column factories with their YAML <c>column_type</c> keys.
/// Call once at DI-bootstrap.
/// </summary>
internal static class ColumnFactoryRegistryExtensions
{
    public static void RegisterDefaultMappings(this IColumnFactoryRegistry registry)
    {
        registry.RegisterFactory("action_combo_box",        typeof(ActionComboBoxColumnFactory));
        registry.RegisterFactory("action_target_combo_box", typeof(ActionTargetComboBoxColumnFactory));
        registry.RegisterFactory("property_field",          typeof(PropertyColumnFactory));
        registry.RegisterFactory("step_start_time_field",   typeof(StepStartTimeColumnFactory));
        registry.RegisterFactory("text_field",              typeof(TextBoxColumnFactory));
    }
}