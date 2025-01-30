namespace NtoLib.Recipes.MbeTable.Actions
{
    internal class ActionEntry
    {
        public string Command { get; }
        public int Id { get; }
        public ActionType Type { get; }

        public ActionEntry(string command, int id, ActionType type)
        {
            Command = command;
            Id = id;
            Type = type;
        }
    }
}
