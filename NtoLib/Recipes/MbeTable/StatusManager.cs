using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{
    public static class StatusManager
    {
        public static Label DbgMsg { get; set; }

        public static void WriteStatusMessage(string message, bool isError = false)
        {
            DbgMsg.Text = message;
            DbgMsg.BackColor = isError ? Color.OrangeRed : Color.White;
        }
    }
}