using System.Drawing;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.StatusManager
{
    public class StatusManager : IStatusManager
    {
        private readonly Label _dbgMsg;

        public StatusManager(Label dbgMsg)
        {
            _dbgMsg = dbgMsg;
        }

        public void WriteStatusMessage(string message, StatusMessage statusMessage)
        {
            _dbgMsg.Text = message;
            _dbgMsg.BackColor = statusMessage == StatusMessage.Error ? Color.OrangeRed : Color.White;
        }

        public void ClearStatusMessage()
        {
            _dbgMsg.Text = string.Empty;
            _dbgMsg.BackColor = Color.White;
        }

        public void EnvalidateStatusMessage()
        {
            _dbgMsg.Invalidate();
        }
    }
}