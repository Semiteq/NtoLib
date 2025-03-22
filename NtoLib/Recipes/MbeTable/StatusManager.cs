using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable
{

    public interface IStatusManager
    {
        void WriteStatusMessage(string message, bool isError = false);
    }
    public class StatusManager : IStatusManager
    {
        private readonly Label DbgMsg;

        public StatusManager(Label dbgMsg)
        {
            DbgMsg = dbgMsg;
        }

        public void WriteStatusMessage(string message, bool isError = false)
        {
            DbgMsg.Text = message;
            DbgMsg.BackColor = isError ? Color.OrangeRed : Color.White;
        }

        public void ClearStatusMessage()
        {
            DbgMsg.Text = string.Empty;
            DbgMsg.BackColor = Color.White;
        }

        public void EnvalidateStatusMessage()
        {
            DbgMsg.Invalidate();
        }
    }
}