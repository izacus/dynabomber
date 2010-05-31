using System;
using System.Windows.Media;
using DynaBomberClient.MainMenu;

namespace DynaBomberClient
{
    public partial class Page : System.Windows.Controls.Page
    {
        private IGameState _currentState;

        public Page()
        {
            InitializeComponent();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            
            // Create the menu at the beginning
            _currentState = new MainMenuState(this);
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            _currentState.EnterFrame(0);
        }

        public IGameState ActiveState
        {
            get { return _currentState; }
            set { _currentState.Deactivate(); _currentState = value; }
        }
    }
}
