namespace Org.SwerveRobotics.Tools.BotBug.Service
    {
    partial class BotBugService
        {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="fromUserCode">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool fromUserCode)
            {
            if (fromUserCode)
                {
                // Called from user's code. Can / should cleanup managed objects
                if (components != null)
                    {
                    components.Dispose();
                    }
                }

            // Called from finalizers (and user code). Avoid referencing other objects
            this.OleUninitialize();

            base.Dispose(fromUserCode);
            }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
            {
            // 
            // BotBugService
            // 
            this.ServiceName = "BotBug";
            }

        #endregion
        }
    }
